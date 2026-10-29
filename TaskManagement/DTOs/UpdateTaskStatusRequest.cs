using System.ComponentModel.DataAnnotations;

namespace TaskManagement.DTOs
{
    public class UpdateTaskStatusRequest
    {
        public Guid TaskReference { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string? Status { get; set; }

        public string? RowVersion { get; set; }
    }

    public class AssignTaskRequest
    {
        public Guid TaskReference { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Assigned user is invalid")]
        public int AssignedUserId { get; set; }

        public string? RowVersion { get; set; }
    }
}
