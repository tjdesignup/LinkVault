namespace LinkVault.Application.DTOs;

public record PagedResultDto<T>(
    List<T> Items,
    string? NextCursor,
    int TotalCount
);
