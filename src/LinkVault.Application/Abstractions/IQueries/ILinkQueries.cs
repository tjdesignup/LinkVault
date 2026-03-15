using LinkVault.Application.DTOs;

namespace LinkVault.Application.Abstractions.IQueries;

public interface ILinkQueries
{
    Task<PagedResultDto<LinkSummaryDto>> GetPagedAsync(
        Guid userId,
        List<string>? tags,
        string? searchTerm,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LinkDto?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}