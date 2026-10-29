using TaskManagement.DTOs;
using TaskManagement.Models;

namespace TaskManagement.Repositories
{
    public sealed record CreateTaskResult(int ResultCode, int? TaskId, Guid? TaskReference);

    /// <summary>Write result codes: 0 Success, 1 NotFound, 2 Conflict, 3 InvalidUser, 4 InvalidInput.</summary>
    public static class ResultCodes
    {
        public const int Success = 0;
        public const int NotFound = 1;
        public const int Conflict = 2;
        public const int InvalidUser = 3;
        public const int InvalidInput = 4;
    }

    public interface ITaskRepository
    {
        Task<CreateTaskResult> CreateAsync(string title, string? description, DateTime dueDate, string priority, int assignedUserId, CancellationToken ct);
        Task<TaskItem?> GetByReferenceAsync(Guid taskReference, CancellationToken ct);
        Task<int> UpdateAsync(Guid taskReference, string title, string? description, DateTime dueDate, string priority, int assignedUserId, byte[]? rowVersion, CancellationToken ct);
        Task<int> UpdateStatusAsync(Guid taskReference, string status, byte[]? rowVersion, CancellationToken ct);
        Task<int> AssignUserAsync(Guid taskReference, int assignedUserId, byte[]? rowVersion, CancellationToken ct);
        Task<PagedResult<TaskListItemDto>> SearchAsync(string? searchTerm, int? assignedUserId, string? status, string? priority, int page, int pageSize, CancellationToken ct);
        Task<IReadOnlyList<UserDto>> GetActiveUsersAsync(CancellationToken ct);
    }
}
