using TaskManagement.DTOs;

namespace TaskManagement.Models
{
    public class TaskListViewModel
    {
        public TaskSearchCriteria Criteria { get; set; } = new();
        public PagedResult<TaskListItemDto> Result { get; set; } = new();
        public IReadOnlyList<UserDto> Users { get; set; } = Array.Empty<UserDto>();
    }
}
