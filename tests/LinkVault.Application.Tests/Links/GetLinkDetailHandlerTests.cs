using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Application.Links.Queries;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Links;

public class GetLinkDetailHandlerTests
{
    private readonly ILinkQueries _linkQueries;
    private readonly ICurrentUser _currentUser;
    private readonly GetLinkDetailHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _linkId = Guid.NewGuid();

    public GetLinkDetailHandlerTests()
    {
        _linkQueries = Substitute.For<ILinkQueries>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new GetLinkDetailHandler(_linkQueries, _currentUser);

        _currentUser.UserId.Returns(_userId);
        _linkQueries
            .GetByIdAsync(_linkId, _userId, Arg.Any<CancellationToken>())
            .Returns(CreateLinkDto());
    }

    private GetLinkDetailQuery ValidQuery() => new(_linkId);

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldReturnLinkDto()
    {
        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<LinkDto>();
    }

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldCallQueriesWithCorrectIds()
    {
        await _handler.Handle(ValidQuery(), CancellationToken.None);

        await _linkQueries.Received(1).GetByIdAsync(
            Arg.Is<Guid>(id => id == _linkId),
            Arg.Is<Guid>(id => id == _userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLinkNotFound_ShouldReturnNull()
    {
        _linkQueries
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    private static LinkDto CreateLinkDto() => new(
        Id: Guid.NewGuid(),
        Url: "https://github.com",
        Title: "GitHub",
        Note: null,
        Tags: ["dotnet"],
        OgTitle: null,
        OgDescription: null,
        OgImageUrl: null,
        MetadataStatus: "Pending",
        Attachments: [],
        CreatedAt: DateTime.UtcNow);
}