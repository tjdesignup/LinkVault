using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Application.Links.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Links;

public class UpdateLinkHandlerTests
{
    private readonly ILinkRepository _linkRepository;
    private readonly ILinkQueries _linkQueries;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateLinkHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _linkId = Guid.NewGuid();

    public UpdateLinkHandlerTests()
    {
        _linkRepository = Substitute.For<ILinkRepository>();
        _linkQueries = Substitute.For<ILinkQueries>();
        _currentUser = Substitute.For<ICurrentUser>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new UpdateLinkHandler(
            _linkRepository,
            _linkQueries,
            _currentUser,
            _unitOfWork);

        _currentUser.UserId.Returns(_userId);
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(CreateLink(_userId));
        _linkQueries
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateLinkDto());
    }

    private UpdateLinkCommand ValidCommand() => new(
        LinkId: _linkId,
        Url: "https://updated.com",
        Title: "Updated Title",
        Note: "Updated Note",
        Tags: ["csharp"]);

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnLinkDto()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<LinkDto>();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldUpdateLinkDetails()
    {
        LinkEntity? capturedLink = null;
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedLink = CreateLink(_userId);
                return capturedLink;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedLink!.Url.Value.Should().Be("https://updated.com");
        capturedLink.Title.Should().Be("Updated Title");
        capturedLink.Note.Should().Be("Updated Note");
        capturedLink.Tags.Should().ContainSingle(t => t.Value == "csharp");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLinkNotFound_ShouldThrowResourceNotFoundException()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenLinkNotFound_ShouldNotSaveChanges()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLinkBelongsToAnotherUser_ShouldThrowResourceForbiddenException()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(CreateLink(Guid.NewGuid()));

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenLinkBelongsToAnotherUser_ShouldNotSaveChanges()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(CreateLink(Guid.NewGuid()));

        await Assert.ThrowsAsync<ResourceForbiddenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static LinkEntity CreateLink(Guid userId)
        => LinkEntity.Create(
            userId,
            new Url("https://github.com"),
            "Original Title",
            null,
            []);

    private static LinkDto CreateLinkDto() => new(
        Id: Guid.NewGuid(),
        Url: "https://updated.com",
        Title: "Updated Title",
        Note: "Updated Note",
        Tags: ["csharp"],
        OgTitle: null,
        OgDescription: null,
        OgImageUrl: null,
        MetadataStatus: "Pending",
        Attachments: [],
        CreatedAt: DateTime.UtcNow);
}