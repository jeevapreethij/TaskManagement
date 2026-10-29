using TaskManagement.DTOs;
using TaskManagement.Models;

namespace TaskManagement.Services
{
    public interface ITaskService
    {
        Task<ServiceResult<Guid>> CreateAsync(CreateTaskRequest? request, CancellationToken ct = default);
        Task<TaskItem?> GetByReferenceAsync(Guid taskReference, CancellationToken ct = default);
        Task<ServiceResult> UpdateAsync(UpdateTaskRequest? request, CancellationToken ct = default);
        Task<ServiceResult> UpdateStatusAsync(UpdateTaskStatusRequest? request, CancellationToken ct = default);
        Task<ServiceResult> AssignAsync(AssignTaskRequest? request, CancellationToken ct = default);
        Task<ServiceResult<PagedResult<TaskListItemDto>>> SearchAsync(TaskSearchCriteria? criteria, CancellationToken ct = default);
        Task<IReadOnlyList<UserDto>> GetActiveUsersAsync(CancellationToken ct = default);
    }
}
