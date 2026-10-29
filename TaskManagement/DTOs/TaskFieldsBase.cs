using System.ComponentModel.DataAnnotations;
using TaskManagement.Models;

namespace TaskManagement.DTOs
{
    /// <summary>Editable task fields shared by create and update requests (single source of validation rules).</summary>
    public abstract class TaskFieldsBase
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(TaskConstants.TitleMaxLength, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [StringLength(TaskConstants.DescriptionMaxLength, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DueDate { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public string? Priority { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Assigned user is invalid")]
        public int AssignedUserId { get; set; }
    }
}
