using FluentAssertions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.Collections.Queries;
using LinkVault.Application.DTOs;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Collections;

public class GetPublicCollectionHandlerTests
{
    private readonly ICollectionQueries _collectionQueries;
    private readonly GetPublicCollectionHandler _handler;

    public GetPublicCollectionHandlerTests()
    {
        _collectionQueries = Substitute.For<ICollectionQueries>();
        _handler = new GetPublicCollectionHandler(_collectionQueries);

        _collectionQueries
            .GetPublicBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreatePublicCollectionDto());
    }

    private static GetPublicCollectionQuery ValidQuery() => new("backend-dev");

    [Fact]
    public async Task Handle_WhenValidSlug_ShouldReturnPublicCollectionDto()
    {
        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<PublicCollectionDto>();
    }

    [Fact]
    public async Task Handle_WhenValidSlug_ShouldCallQueriesWithCorrectSlug()
    {
        await _handler.Handle(ValidQuery(), CancellationToken.None);

        await _collectionQueries.Received(1).GetPublicBySlugAsync(
            Arg.Is<string>(s => s == "backend-dev"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCollectionNotFound_ShouldReturnNull()
    {
        _collectionQueries
            .GetPublicBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    private static PublicCollectionDto CreatePublicCollectionDto() => new(
        Name: "Backend dev",
        Slug: "backend-dev",
        FilterTags: ["dotnet"],
        Links:
        [
            new LinkSummaryDto(
                Id: Guid.NewGuid(),
                Url: "https://github.com",
                Title: "GitHub",
                Tags: ["dotnet"],
                OgImageUrl: null,
                MetadataStatus: "Fetched",
                CreatedAt: DateTime.UtcNow)
        ]);
}