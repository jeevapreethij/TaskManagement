using System.ComponentModel.DataAnnotations;
using TaskManagement.Models;

namespace TaskManagement.DTOs
{
    public class TaskListItemDto
    {
        public int TaskId { get; set; }
        public Guid TaskReference { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int AssignedUserId { get; set; }
        public string AssignedUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TotalCount { get; set; }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int TotalCount { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>Search contract: blank term = no text filter; numeric = Task ID; GUID = reference; otherwise title contains.</summary>
    public class TaskSearchCriteria
    {
        [StringLength(TaskConstants.SearchTermMaxLength, ErrorMessage = "Search term cannot exceed 100 characters")]
        public string? SearchTerm { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Assigned user filter is invalid")]
        public int? AssignedUserId { get; set; }

        public string? Status { get; set; }
        public string? Priority { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page is invalid")]
        public int Page { get; set; } = 1;

        [Range(1, TaskConstants.MaxPageSize, ErrorMessage = "Page size is invalid")]
        public int PageSize { get; set; } = 10;
    }
}
