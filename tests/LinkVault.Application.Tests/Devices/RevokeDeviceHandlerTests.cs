using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Devices.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Devices;

public class RevokeDeviceHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RevokeDeviceHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _deviceId = Guid.NewGuid();

    public RevokeDeviceHandlerTests()
    {
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _currentUser = Substitute.For<ICurrentUser>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new RevokeDeviceHandler(
            _refreshTokenRepository,
            _currentUser,
            _unitOfWork);

        _currentUser.UserId.Returns(_userId);
        _refreshTokenRepository
            .FindByIdAsync(_deviceId, Arg.Any<CancellationToken>())
            .Returns(CreateRefreshToken(_userId));
    }

    private RevokeDeviceCommand ValidCommand() => new(_deviceId);

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnUnit()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().Contain("revoked");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldRevokeToken()
    {
        RefreshTokenEntity? capturedToken = null;
        _refreshTokenRepository
            .FindByIdAsync(_deviceId, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedToken = CreateRefreshToken(_userId);
                return capturedToken;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDeviceNotFound_ShouldThrowResourceNotFoundException()
    {
        _refreshTokenRepository
            .FindByIdAsync(_deviceId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenDeviceBelongsToAnotherUser_ShouldThrowResourceForbiddenException()
    {
        _refreshTokenRepository
            .FindByIdAsync(_deviceId, Arg.Any<CancellationToken>())
            .Returns(CreateRefreshToken(Guid.NewGuid()));

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenDeviceBelongsToAnotherUser_ShouldNotSaveChanges()
    {
        _refreshTokenRepository
            .FindByIdAsync(_deviceId, Arg.Any<CancellationToken>())
            .Returns(CreateRefreshToken(Guid.NewGuid()));

        await Assert.ThrowsAsync<ResourceForbiddenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static RefreshTokenEntity CreateRefreshToken(Guid userId)
        => RefreshTokenEntity.Create(
            userId,
            "hashed-token",
            "Chrome / Windows",
            "127.0.0.1");
}