using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Application.Subscriptions.Queries;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Subscriptions;

public class GetSubscriptionHandlerTests
{
    private readonly ISubscriptionQueries _subscriptionQueries;
    private readonly ICurrentUser _currentUser;
    private readonly GetSubscriptionHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetSubscriptionHandlerTests()
    {
        _subscriptionQueries = Substitute.For<ISubscriptionQueries>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new GetSubscriptionHandler(_subscriptionQueries, _currentUser);

        _currentUser.UserId.Returns(_userId);
        _subscriptionQueries
            .GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(CreateSubscriptionDto());
    }

    [Fact]
    public async Task Handle_ShouldReturnSubscriptionDto()
    {
        var result = await _handler.Handle(
            new GetSubscriptionQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<SubscriptionDto>();
    }

    [Fact]
    public async Task Handle_ShouldCallQueriesWithCorrectUserId()
    {
        await _handler.Handle(new GetSubscriptionQuery(), CancellationToken.None);

        await _subscriptionQueries.Received(1).GetByUserIdAsync(
            Arg.Is<Guid>(id => id == _userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldReturnNull()
    {
        _subscriptionQueries
            .GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await _handler.Handle(
            new GetSubscriptionQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    private static SubscriptionDto CreateSubscriptionDto() => new(
        Tier: "Free",
        Status: "Active",
        CurrentPeriodEnd: null,
        IsProActive: false);
}