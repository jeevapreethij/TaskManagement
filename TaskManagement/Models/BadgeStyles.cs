using TaskManagement.Models;

namespace TaskManagement.Models
{
    /// <summary>Bootstrap badge classes for task priority/status, shared by all views.</summary>
    public static class BadgeStyles
    {
        public static string Priority(string? priority) => priority switch
        {
            "High" => "bg-danger",
            "Medium" => "bg-warning text-dark",
            _ => "bg-secondary"
        };

        public static string Status(string? status) => status switch
        {
            "Closed" => "bg-success",
            "In Progress" => "bg-primary",
            "Hold" => "bg-warning text-dark",
            _ => "bg-info text-dark"
        };
    }
}
