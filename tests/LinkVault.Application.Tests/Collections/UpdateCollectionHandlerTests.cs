using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.Collections.Commands;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.ValueObjects;
using LinkVault.Domain.Exceptions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Collections;

public class UpdateCollectionHandlerTests
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICollectionQueries _collectionQueries;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateCollectionHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _collectionId = Guid.NewGuid();

    public UpdateCollectionHandlerTests()
    {
        _collectionRepository = Substitute.For<ICollectionRepository>();
        _collectionQueries = Substitute.For<ICollectionQueries>();
        _currentUser = Substitute.For<ICurrentUser>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new UpdateCollectionHandler(
            _collectionRepository,
            _collectionQueries,
            _currentUser,
            _unitOfWork);

        _currentUser.UserId.Returns(_userId);

        var collection = CreateCollection(_userId);

        _collectionRepository
            .FindByIdAsync(_collectionId, Arg.Any<CancellationToken>())
            .Returns(collection);

        _collectionRepository
            .SlugExistsForUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _collectionQueries
            .GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([CreateCollectionDto(collection.Id)]);
    }

    private UpdateCollectionCommand ValidCommand() => new(
        CollectionId: _collectionId,
        Name: "Updated Collection",
        FilterTags: ["csharp"],
        IsPublic: true);

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnCollectionDto()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<CollectionDto>();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldUpdateCollectionDetails()
    {
        CollectionEntity? capturedCollection = null;

        _collectionRepository
            .FindByIdAsync(_collectionId, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedCollection = CreateCollection(_userId);
                return capturedCollection;
            });

        _collectionQueries
        .GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
        .Returns(x => capturedCollection is null
            ? []
            : [CreateCollectionDto(capturedCollection.Id)]);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedCollection!.Name.Should().Be("Updated Collection");
        capturedCollection.IsPublic.Should().BeTrue();
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

    private static CollectionEntity CreateCollection(Guid userId)
        => CollectionEntity.Create(
            userId,
            "Original Collection",
            new Slug("original-collection"),
            [],
            false);

    private static CollectionDto CreateCollectionDto(Guid collectionId) => new(
        Id: collectionId,
        Name: "Updated Collection",
        Slug: "updated-collection",
        FilterTags: ["csharp"],
        IsPublic: true,
        CreatedAt: DateTime.UtcNow);
}