using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Collections.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.ValueObjects;
using LinkVault.Domain.Exceptions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Collections;

public class DeleteCollectionHandlerTests
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteCollectionHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _collectionId = Guid.NewGuid();

    public DeleteCollectionHandlerTests()
    {
        _collectionRepository = Substitute.For<ICollectionRepository>();
        _currentUser = Substitute.For<ICurrentUser>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new DeleteCollectionHandler(
            _collectionRepository,
            _currentUser,
            _unitOfWork);

        _currentUser.UserId.Returns(_userId);
        _collectionRepository
            .FindByIdAsync(_collectionId, Arg.Any<CancellationToken>())
            .Returns(CreateCollection(_userId));
    }

    private DeleteCollectionCommand ValidCommand() => new(_collectionId);

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnUnit()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().Contain("deleted");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSoftDeleteCollection()
    {
        CollectionEntity? capturedCollection = null;
        _collectionRepository
            .FindByIdAsync(_collectionId, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedCollection = CreateCollection(_userId);
                return capturedCollection;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedCollection!.IsDeleted.Should().BeTrue();
        capturedCollection.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCollectionNotFound_ShouldThrowResourceNotFoundException()
    {
        _collectionRepository
            .FindByIdAsync(_collectionId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCollectionBelongsToAnotherUser_ShouldThrowResourceForbiddenException()
    {
        _collectionRepository
            .FindByIdAsync(_collectionId, Arg.Any<CancellationToken>())
            .Returns(CreateCollection(Guid.NewGuid()));

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenCollectionBelongsToAnotherUser_ShouldNotSaveChanges()
    {
        _collectionRepository
            .FindByIdAsync(_collectionId, Arg.Any<CancellationToken>())
            .Returns(CreateCollection(Guid.NewGuid()));

        await Assert.ThrowsAsync<ResourceForbiddenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CollectionEntity CreateCollection(Guid userId)
        => CollectionEntity.Create(
            userId,
            "Test Collection",
            new Slug("test-collection"),
            [],
            false);
}