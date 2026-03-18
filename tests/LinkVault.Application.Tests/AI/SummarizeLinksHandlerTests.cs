using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.AI.Queries;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Exceptions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.AI;

public class SummarizeLinksHandlerTests
{
    private readonly ILinkQueries _linkQueries;
    private readonly IAiSummaryService _aiSummaryService;
    private readonly ICurrentUser _currentUser;
    private readonly SummarizeLinksHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public SummarizeLinksHandlerTests()
    {
        _linkQueries = Substitute.For<ILinkQueries>();
        _aiSummaryService = Substitute.For<IAiSummaryService>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new SummarizeLinksHandler(
            _linkQueries,
            _aiSummaryService,
            _currentUser);

        _currentUser.UserId.Returns(_userId);
        _currentUser.IsProTier.Returns(true);

        _linkQueries
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateLinkDto());

        _aiSummaryService
            .SummarizeAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable(["AI ", "summary ", "result"]));
    }

    private SummarizeLinksQuery ValidQuery() => new(
        LinkIds: [Guid.NewGuid(), Guid.NewGuid()]);

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldReturnAsyncEnumerable()
    {
        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldStreamTokens()
    {
        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        var tokens = new List<string>();
        await foreach (var token in result)
            tokens.Add(token);

        tokens.Should().HaveCount(3);
        string.Concat(tokens).Should().Be("AI summary result");
    }

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldCallAiWithLinkDescriptions()
    {
        await _handler.Handle(ValidQuery(), CancellationToken.None);

        _aiSummaryService.Received(1).SummarizeAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFreeTier_ShouldThrowProTierRequiredException()
    {
        _currentUser.IsProTier.Returns(false);

        var act = async () => await _handler.Handle(ValidQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ProTierRequiredException>();
    }

    [Fact]
    public async Task Handle_WhenNoLinkIds_ShouldThrowResourceNotFoundException()
    {
        var act = async () => await _handler.Handle(
            new SummarizeLinksQuery([]), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
    private static LinkDto CreateLinkDto() => new(
        Id: Guid.NewGuid(),
        Url: "https://github.com",
        Title: "GitHub",
        Note: "Useful resource",
        Tags: ["dotnet"],
        OgTitle: "GitHub",
        OgDescription: "Where the world builds software",
        OgImageUrl: null,
        MetadataStatus: "Fetched",
        Attachments: [],
        CreatedAt: DateTime.UtcNow);

    private static async IAsyncEnumerable<string> CreateAsyncEnumerable(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}