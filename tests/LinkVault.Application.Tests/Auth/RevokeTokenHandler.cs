using FluentAssertions;
using LinkVault.Application.Auth.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using LinkVault.Application.Abstractions;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Auth;

public class RevokeTokenHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly RevokeTokenHandler _handler;

    public RevokeTokenHandlerTests()
    {
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _tokenService = Substitute.For<ITokenService>();

        _handler = new RevokeTokenHandler(
            _refreshTokenRepository,
            _unitOfWork,
            _tokenService);

        _tokenService.HashRefreshToken(Arg.Any<string>()).Returns("hashed-token");
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateValidToken());
    }

    private static RevokeTokenCommand ValidCommand() => new("plain-refresh-token");

    private static RevokeTokenCommand RevokeValidAllCommand() => new("plain-refresh-token", true);

    [Fact]
    public async Task Handle_WhenValidToken_ShouldReturnUnit()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().Contain("successfully");
        result.Message.Should().Contain("logged out");
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldRevokeToken()
    {
        RefreshTokenEntity? capturedToken = null;
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedToken = CreateValidToken();
                return capturedToken;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldHashIncomingToken()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _tokenService.Received(1).HashRefreshToken("plain-refresh-token");
    }

    [Fact]
    public async Task Handle_WhenRevokeValidAll_ShouldReturnUnit()
    {
        var result = await _handler.Handle(RevokeValidAllCommand(), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().Contain("successfully");
        result.Message.Should().Contain("logged out");
    }

    [Fact]
    public async Task Handle_Handle_WhenRevokeValidAll_ShouldRevokeToken()
    {
        RefreshTokenEntity? capturedToken = null;
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedToken = CreateRevokedToken();
                return capturedToken;
            });

        await _handler.Handle(RevokeValidAllCommand(), CancellationToken.None);

        capturedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Handle_WhenRevokeValidAll_ShouldCallRepositoryRevokeAll()
    {
        await _handler.Handle(RevokeValidAllCommand(), CancellationToken.None);

        await _refreshTokenRepository.Received(1).RevokeAllByUserIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldThrowInvalidTokenException()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTokenException>();
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldNotSaveChanges()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await Assert.ThrowsAsync<InvalidTokenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyRevoked_ShouldStillReturnUnit()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateRevokedToken());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private static RefreshTokenEntity CreateValidToken()
        => RefreshTokenEntity.Create(
            Guid.NewGuid(),
            "hashed-token",
            "Chrome / Windows",
            "127.0.0.1");

    private static RefreshTokenEntity CreateRevokedToken()
    {
        var token = CreateValidToken();
        token.Revoke();
        return token;
    }
}