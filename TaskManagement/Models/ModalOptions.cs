namespace TaskManagement.Models
{
    /// <summary>Settings for the reusable modal dialog partial (Views/Shared/_ModalDialog.cshtml).</summary>
    public class ModalOptions
    {
        public string Id { get; set; } = "appModal";
        public string Title { get; set; } = string.Empty;
        public string SaveText { get; set; } = "Save";
        public string CancelText { get; set; } = "Cancel";
        /// <summary>Hide the Save button for read-only dialogs.</summary>
        public bool ShowSave { get; set; } = true;
        /// <summary>Bootstrap size class: "", "modal-sm", "modal-lg", "modal-xl".</summary>
        public string Size { get; set; } = "modal-lg";
    }
}
