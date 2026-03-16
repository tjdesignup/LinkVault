using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Application.Links.Queries;
using NSubstitute;

namespace LinkVault.Application.Tests.Links;

public class GetLinksHandlerTests
{
    private readonly ILinkQueries _linkQueries;
    private readonly ICurrentUser _currentUser;
    private readonly GetLinksHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetLinksHandlerTests()
    {
        _linkQueries = Substitute.For<ILinkQueries>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new GetLinksHandler(_linkQueries, _currentUser);

        _currentUser.UserId.Returns(_userId);
        _linkQueries
            .GetPagedAsync(
                Arg.Any<Guid>(),
                Arg.Any<List<string>?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(CreatePagedResult());
    }

    private static GetLinksQuery ValidQuery() => new(
        Tags: null,
        SearchTerm: null,
        Cursor: null,
        PageSize: 20);

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldReturnPagedResult()
    {
        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<PagedResultDto<LinkSummaryDto>>();
    }

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldCallQueriesWithCorrectUserId()
    {
        await _handler.Handle(ValidQuery(), CancellationToken.None);

        await _linkQueries.Received(1).GetPagedAsync(
            Arg.Is<Guid>(id => id == _userId),
            Arg.Any<List<string>?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldPassTagsAndSearchTerm()
    {
        var query = new GetLinksQuery(
            Tags: ["dotnet", "csharp"],
            SearchTerm: "github",
            Cursor: null,
            PageSize: 10);

        await _handler.Handle(query, CancellationToken.None);

        await _linkQueries.Received(1).GetPagedAsync(
            Arg.Any<Guid>(),
            Arg.Is<List<string>?>(tags => tags != null && tags.Contains("dotnet")),
            Arg.Is<string?>(s => s == "github"),
            Arg.Any<string?>(),
            Arg.Is<int>(p => p == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoLinks_ShouldReturnEmptyPagedResult()
    {
        _linkQueries
            .GetPagedAsync(
                Arg.Any<Guid>(),
                Arg.Any<List<string>?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PagedResultDto<LinkSummaryDto>([], null, 0));

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.NextCursor.Should().BeNull();
        result.TotalCount.Should().Be(0);
    }

    private static PagedResultDto<LinkSummaryDto> CreatePagedResult() => new(
        Items:
        [
            new LinkSummaryDto(
                Id: Guid.NewGuid(),
                Url: "https://github.com",
                Title: "GitHub",
                Tags: ["dotnet"],
                OgImageUrl: null,
                MetadataStatus: "Pending",
                CreatedAt: DateTime.UtcNow)
        ],
        NextCursor: null,
        TotalCount: 1);
}