namespace TaskManagement.Models
{
    public static class TaskConstants
    {
        public const int TitleMaxLength = 200;
        public const int DescriptionMaxLength = 2000;
        public const int SearchTermMaxLength = 100;
        public const int MaxPageSize = 100;

        public static readonly IReadOnlyList<string> Priorities = new[] { "Low", "Medium", "High" };
        public static readonly IReadOnlyList<string> Statuses = new[] { "Open", "In Progress", "Hold", "Closed" };
    }

    public class TaskItem
    {
        public int TaskId { get; set; }
        public Guid TaskReference { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public string Priority { get; set; } = string.Empty;
        public int AssignedUserId { get; set; }
        public string AssignedUserName { get; set; } = string.Empty;
        public bool AssignedUserIsActive { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? ClosedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
