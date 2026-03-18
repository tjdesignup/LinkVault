using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.Devices.Queries;
using LinkVault.Application.DTOs;
using NSubstitute;

namespace LinkVault.Application.Tests.Devices;

public class GetDevicesHandlerTests
{
    private readonly IDeviceQueries _deviceQueries;
    private readonly ICurrentUser _currentUser;
    private readonly GetDevicesHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetDevicesHandlerTests()
    {
        _deviceQueries = Substitute.For<IDeviceQueries>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new GetDevicesHandler(_deviceQueries, _currentUser);

        _currentUser.UserId.Returns(_userId);
        _deviceQueries
            .GetActiveByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([CreateDeviceDto()]);
    }

    [Fact]
    public async Task Handle_ShouldReturnDevices()
    {
        var result = await _handler.Handle(new GetDevicesQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldCallQueriesWithCorrectUserId()
    {
        await _handler.Handle(new GetDevicesQuery(), CancellationToken.None);

        await _deviceQueries.Received(1).GetActiveByUserIdAsync(
            Arg.Is<Guid>(id => id == _userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoDevices_ShouldReturnEmptyList()
    {
        _deviceQueries
            .GetActiveByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new GetDevicesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static DeviceDto CreateDeviceDto() => new(
        Id: Guid.NewGuid(),
        DeviceName: "Chrome / Windows",
        IpAddress: "127.0.0.1",
        CreatedAt: DateTime.UtcNow,
        ExpiresAt: DateTime.UtcNow.AddDays(7));
}