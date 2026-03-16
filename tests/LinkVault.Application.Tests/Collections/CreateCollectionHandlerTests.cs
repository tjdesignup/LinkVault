using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.Collections.Commands;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Collections;

public class CreateCollectionHandlerTests
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICollectionQueries _collectionQueries;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateCollectionHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public CreateCollectionHandlerTests()
    {
        _collectionRepository = Substitute.For<ICollectionRepository>();
        _collectionQueries = Substitute.For<ICollectionQueries>();
        _currentUser = Substitute.For<ICurrentUser>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new CreateCollectionHandler(
            _collectionRepository,
            _collectionQueries,
            _currentUser,
            _unitOfWork);

        _currentUser.UserId.Returns(_userId);
        _collectionRepository
            .SlugExistsForUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _collectionQueries
            .GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([CreateCollectionDto()]);
    }

    private static CreateCollectionCommand ValidCommand() => new(
        Name: "Backend dev",
        FilterTags: ["dotnet", "csharp"],
        IsPublic: false);

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnCollectionDto()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<CollectionDto>();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldAddCollectionToRepository()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _collectionRepository.Received(1).AddAsync(
            Arg.Is<CollectionEntity>(c =>
                c.UserId == _userId &&
                c.Name == "Backend dev" &&
                c.IsPublic == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldGenerateSlugFromName()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _collectionRepository.Received(1).AddAsync(
            Arg.Is<CollectionEntity>(c =>
                c.Slug.Value == "backend-dev"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldGenerateUniqueSlug()
    {
        _collectionRepository
            .SlugExistsForUserAsync(
                Arg.Any<Guid>(),
                Arg.Is<string>(s => s == "backend-dev"),
                Arg.Any<CancellationToken>())
            .Returns(true);

        _collectionRepository
            .SlugExistsForUserAsync(
                Arg.Any<Guid>(),
                Arg.Is<string>(s => s != "backend-dev-1"),
                Arg.Any<CancellationToken>())
            .Returns(false);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _collectionRepository.Received(1).AddAsync(
            Arg.Is<CollectionEntity>(c => c.Slug.Value != "backend-dev-1"),
            Arg.Any<CancellationToken>());
    }

    private static CollectionDto CreateCollectionDto() => new(
        Id: Guid.NewGuid(),
        Name: "Backend dev",
        Slug: "backend-dev",
        FilterTags: ["dotnet"],
        IsPublic: false,
        CreatedAt: DateTime.UtcNow);
}