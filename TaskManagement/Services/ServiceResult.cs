namespace TaskManagement.Services
{
    public enum ServiceStatus
    {
        Success,
        ValidationFailed,
        NotFound,
        Conflict
    }

    public class ServiceResult
    {
        private static readonly IReadOnlyDictionary<string, string> NoErrors = new Dictionary<string, string>();

        public ServiceStatus Status { get; init; }
        public IReadOnlyDictionary<string, string> Errors { get; init; } = NoErrors;
        public bool IsSuccess => Status == ServiceStatus.Success;

        public static ServiceResult Success() => new() { Status = ServiceStatus.Success };

        public static ServiceResult Failure(ServiceStatus status, IReadOnlyDictionary<string, string>? errors = null) =>
            new() { Status = status, Errors = errors ?? NoErrors };

        public static ServiceResult Failure(ServiceStatus status, string field, string message) =>
            Failure(status, new Dictionary<string, string> { [field] = message });
    }

    public sealed class ServiceResult<T> : ServiceResult
    {
        public T? Value { get; init; }

        public static ServiceResult<T> Success(T value) => new() { Status = ServiceStatus.Success, Value = value };

        public static new ServiceResult<T> Failure(ServiceStatus status, IReadOnlyDictionary<string, string>? errors = null) =>
            new() { Status = status, Errors = errors ?? new Dictionary<string, string>() };

        public static new ServiceResult<T> Failure(ServiceStatus status, string field, string message) =>
            Failure(status, new Dictionary<string, string> { [field] = message });
    }
}
