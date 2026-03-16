using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.Collections.Queries;
using LinkVault.Application.DTOs;
using NSubstitute;

namespace LinkVault.Application.Tests.Collections;

public class GetCollectionsHandlerTests
{
    private readonly ICollectionQueries _collectionQueries;
    private readonly ICurrentUser _currentUser;
    private readonly GetCollectionsHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetCollectionsHandlerTests()
    {
        _collectionQueries = Substitute.For<ICollectionQueries>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new GetCollectionsHandler(_collectionQueries, _currentUser);

        _currentUser.UserId.Returns(_userId);
        _collectionQueries
            .GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([CreateCollectionDto()]);
    }

    [Fact]
    public async Task Handle_ShouldReturnCollections()
    {
        var result = await _handler.Handle(new GetCollectionsQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldCallQueriesWithCorrectUserId()
    {
        await _handler.Handle(new GetCollectionsQuery(), CancellationToken.None);

        await _collectionQueries.Received(1).GetByUserIdAsync(
            Arg.Is<Guid>(id => id == _userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoCollections_ShouldReturnEmptyList()
    {
        _collectionQueries
            .GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new GetCollectionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static CollectionDto CreateCollectionDto() => new(
        Id: Guid.NewGuid(),
        Name: "Backend dev",
        Slug: "backend-dev",
        FilterTags: ["dotnet"],
        IsPublic: false,
        CreatedAt: DateTime.UtcNow);
}