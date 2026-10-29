namespace TaskManagement.DTOs
{
    public class UpdateTaskRequest : TaskFieldsBase
    {
        public Guid TaskReference { get; set; }

        /// <summary>Base64 row version read with the task; used for optimistic concurrency.</summary>
        public string? RowVersion { get; set; }
    }
}
