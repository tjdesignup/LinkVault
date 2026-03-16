using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Links.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.ValueObjects;
using LinkVault.Domain.Exceptions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Links;

public class DeleteLinkHandlerTests
{
    private readonly ILinkRepository _linkRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteLinkHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _linkId = Guid.NewGuid();

    public DeleteLinkHandlerTests()
    {
        _linkRepository = Substitute.For<ILinkRepository>();
        _currentUser = Substitute.For<ICurrentUser>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new DeleteLinkHandler(
            _linkRepository,
            _currentUser,
            _unitOfWork);

        _currentUser.UserId.Returns(_userId);
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(CreateLink(_userId));
    }

    private DeleteLinkCommand ValidCommand() => new(_linkId);

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnSuccessMessage()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().Contain("deleted");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSoftDeleteLink()
    {
        LinkEntity? capturedLink = null;
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedLink = CreateLink(_userId);
                return capturedLink;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedLink!.IsDeleted.Should().BeTrue();
        capturedLink.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLinkNotFound_ShouldThrowResourceNotFoundException()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenLinkNotFound_ShouldNotSaveChanges()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLinkBelongsToAnotherUser_ShouldThrowResourceForbiddenException()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(CreateLink(Guid.NewGuid()));

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenLinkBelongsToAnotherUser_ShouldNotSaveChanges()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(CreateLink(Guid.NewGuid()));

        await Assert.ThrowsAsync<ResourceForbiddenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static LinkEntity CreateLink(Guid userId)
        => LinkEntity.Create(
            userId,
            new Url("https://github.com"),
            "Test Link",
            null,
            []);
}