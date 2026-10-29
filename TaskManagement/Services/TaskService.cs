using TaskManagement.DTOs;
using TaskManagement.Models;
using TaskManagement.Repositories;

namespace TaskManagement.Services
{
    public sealed class TaskService : ITaskService
    {
        private readonly ITaskRepository _repository;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ITaskRepository repository, ILogger<TaskService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ServiceResult<Guid>> CreateAsync(CreateTaskRequest? request, CancellationToken ct = default)
        {
            if (request is null)
            {
                return ServiceResult<Guid>.Failure(ServiceStatus.ValidationFailed, "Request", "Request is required");
            }

            var errors = new Dictionary<string, string>();
            var title = ValidateTitle(request.Title, errors);
            var description = ValidateDescription(request.Description, errors);
            var dueDate = ValidateDueDate(request.DueDate, errors);
            var priority = NormalizePriority(request.Priority, errors);
            ValidateUserId(request.AssignedUserId, errors);

            if (errors.Count > 0)
            {
                return ServiceResult<Guid>.Failure(ServiceStatus.ValidationFailed, errors);
            }

            var result = await _repository.CreateAsync(title!, description, dueDate!.Value, priority!, request.AssignedUserId, ct);

            if (result.ResultCode == ResultCodes.Success && result.TaskReference is { } reference)
            {
                _logger.LogInformation("Task created. Operation {Operation} TaskReference {TaskReference}", "CreateTask", reference);
                return ServiceResult<Guid>.Success(reference);
            }

            var failure = MapFailure(result.ResultCode);
            _logger.LogWarning("Task operation failed. Operation {Operation} Outcome {Outcome}", "CreateTask", failure.Status);
            return ServiceResult<Guid>.Failure(failure.Status, failure.Errors);
        }

        public async Task<TaskItem?> GetByReferenceAsync(Guid taskReference, CancellationToken ct = default)
        {
            if (taskReference == Guid.Empty)
            {
                return null;
            }

            return await _repository.GetByReferenceAsync(taskReference, ct);
        }

        public async Task<ServiceResult> UpdateAsync(UpdateTaskRequest? request, CancellationToken ct = default)
        {
            if (request is null)
            {
                return ServiceResult.Failure(ServiceStatus.ValidationFailed, "Request", "Request is required");
            }

            if (request.TaskReference == Guid.Empty)
            {
                return ServiceResult.Failure(ServiceStatus.NotFound);
            }

            var errors = new Dictionary<string, string>();
            var title = ValidateTitle(request.Title, errors);
            var description = ValidateDescription(request.Description, errors);
            var dueDate = ValidateDueDate(request.DueDate, errors);
            var priority = NormalizePriority(request.Priority, errors);
            ValidateUserId(request.AssignedUserId, errors);
            var rowVersion = ParseRowVersion(request.RowVersion, errors);

            if (errors.Count > 0)
            {
                return ServiceResult.Failure(ServiceStatus.ValidationFailed, errors);
            }

            var code = await _repository.UpdateAsync(request.TaskReference, title!, description, dueDate!.Value, priority!, request.AssignedUserId, rowVersion, ct);
            return Complete(code, "UpdateTask", request.TaskReference);
        }

        public async Task<ServiceResult> UpdateStatusAsync(UpdateTaskStatusRequest? request, CancellationToken ct = default)
        {
            if (request is null)
            {
                return ServiceResult.Failure(ServiceStatus.ValidationFailed, "Request", "Request is required");
            }

            if (request.TaskReference == Guid.Empty)
            {
                return ServiceResult.Failure(ServiceStatus.NotFound);
            }

            var errors = new Dictionary<string, string>();
            var status = NormalizeStatus(request.Status, errors);
            var rowVersion = ParseRowVersion(request.RowVersion, errors);

            if (errors.Count > 0)
            {
                return ServiceResult.Failure(ServiceStatus.ValidationFailed, errors);
            }

            var code = await _repository.UpdateStatusAsync(request.TaskReference, status!, rowVersion, ct);
            return Complete(code, "UpdateTaskStatus", request.TaskReference);
        }

        public async Task<ServiceResult> AssignAsync(AssignTaskRequest? request, CancellationToken ct = default)
        {
            if (request is null)
            {
                return ServiceResult.Failure(ServiceStatus.ValidationFailed, "Request", "Request is required");
            }

            if (request.TaskReference == Guid.Empty)
            {
                return ServiceResult.Failure(ServiceStatus.NotFound);
            }

            var errors = new Dictionary<string, string>();
            ValidateUserId(request.AssignedUserId, errors);
            var rowVersion = ParseRowVersion(request.RowVersion, errors);

            if (errors.Count > 0)
            {
                return ServiceResult.Failure(ServiceStatus.ValidationFailed, errors);
            }

            var code = await _repository.AssignUserAsync(request.TaskReference, request.AssignedUserId, rowVersion, ct);
            return Complete(code, "ReassignTask", request.TaskReference);
        }

        public async Task<ServiceResult<PagedResult<TaskListItemDto>>> SearchAsync(TaskSearchCriteria? criteria, CancellationToken ct = default)
        {
            criteria ??= new TaskSearchCriteria();
            var errors = new Dictionary<string, string>();

            var term = string.IsNullOrWhiteSpace(criteria.SearchTerm) ? null : criteria.SearchTerm.Trim();
            if (term is { Length: > TaskConstants.SearchTermMaxLength })
            {
                errors["SearchTerm"] = "Search term cannot exceed 100 characters";
            }

            if (criteria.AssignedUserId is <= 0)
            {
                errors["AssignedUserId"] = "Assigned user filter is invalid";
            }

            string? status = null;
            if (!string.IsNullOrWhiteSpace(criteria.Status))
            {
                status = NormalizeStatus(criteria.Status, errors);
            }

            string? priority = null;
            if (!string.IsNullOrWhiteSpace(criteria.Priority))
            {
                priority = NormalizePriority(criteria.Priority, errors);
            }

            if (criteria.Page < 1)
            {
                errors["Page"] = "Page is invalid";
            }

            if (criteria.PageSize < 1 || criteria.PageSize > TaskConstants.MaxPageSize)
            {
                errors["PageSize"] = "Page size is invalid";
            }

            if (errors.Count > 0)
            {
                return ServiceResult<PagedResult<TaskListItemDto>>.Failure(ServiceStatus.ValidationFailed, errors);
            }

            var page = await _repository.SearchAsync(term, criteria.AssignedUserId, status, priority, criteria.Page, criteria.PageSize, ct);
            return ServiceResult<PagedResult<TaskListItemDto>>.Success(page);
        }

        public Task<IReadOnlyList<UserDto>> GetActiveUsersAsync(CancellationToken ct = default) =>
            _repository.GetActiveUsersAsync(ct);

        private ServiceResult Complete(int code, string operation, Guid taskReference)
        {
            if (code == ResultCodes.Success)
            {
                _logger.LogInformation("Task operation succeeded. Operation {Operation} TaskReference {TaskReference}", operation, taskReference);
                return ServiceResult.Success();
            }

            var failure = MapFailure(code);
            _logger.LogWarning("Task operation failed. Operation {Operation} TaskReference {TaskReference} Outcome {Outcome}", operation, taskReference, failure.Status);
            return ServiceResult.Failure(failure.Status, failure.Errors);
        }

        private static ServiceResult MapFailure(int code) => code switch
        {
            ResultCodes.NotFound => ServiceResult.Failure(ServiceStatus.NotFound),
            ResultCodes.Conflict => ServiceResult.Failure(ServiceStatus.Conflict, "RowVersion", "The task was changed by someone else. Reload and try again."),
            ResultCodes.InvalidUser => ServiceResult.Failure(ServiceStatus.ValidationFailed, "AssignedUserId", "Assigned user does not exist or is inactive"),
            ResultCodes.InvalidInput => ServiceResult.Failure(ServiceStatus.ValidationFailed, "Request", "The request contains invalid values"),
            _ => throw new InvalidOperationException($"Unexpected stored procedure result code {code}.")
        };

        private static string? ValidateTitle(string? title, Dictionary<string, string> errors)
        {
            var trimmed = title?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                errors["Title"] = "Title is required";
                return null;
            }

            if (trimmed.Length > TaskConstants.TitleMaxLength)
            {
                errors["Title"] = "Title cannot exceed 200 characters";
                return null;
            }

            return trimmed;
        }

        private static string? ValidateDescription(string? description, Dictionary<string, string> errors)
        {
            if (description is { Length: > TaskConstants.DescriptionMaxLength })
            {
                errors["Description"] = "Description cannot exceed 2000 characters";
                return null;
            }

            return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        }

        private static DateTime? ValidateDueDate(DateTime? dueDate, Dictionary<string, string> errors)
        {
            if (dueDate is null || dueDate.Value.Year < 1753)
            {
                errors["DueDate"] = "Due date is required";
                return null;
            }

            return dueDate.Value.Date;
        }

        private static string? NormalizePriority(string? priority, Dictionary<string, string> errors) =>
            Normalize(priority, TaskConstants.Priorities, "Priority", errors);

        private static string? NormalizeStatus(string? status, Dictionary<string, string> errors) =>
            Normalize(status, TaskConstants.Statuses, "Status", errors);

        private static string? Normalize(string? value, IReadOnlyList<string> allowed, string field, Dictionary<string, string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors[field] = $"{field} is required";
                return null;
            }

            var match = allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                errors[field] = $"{field} is invalid";
            }

            return match;
        }

        private static void ValidateUserId(int userId, Dictionary<string, string> errors)
        {
            if (userId <= 0)
            {
                errors["AssignedUserId"] = "Assigned user is invalid";
            }
        }

        private static byte[]? ParseRowVersion(string? rowVersion, Dictionary<string, string> errors)
        {
            if (string.IsNullOrWhiteSpace(rowVersion))
            {
                return null;
            }

            Span<byte> buffer = stackalloc byte[8];
            if (Convert.TryFromBase64String(rowVersion, buffer, out var written) && written == 8)
            {
                return buffer[..written].ToArray();
            }

            errors["RowVersion"] = "Row version is invalid";
            return null;
        }
    }
}
