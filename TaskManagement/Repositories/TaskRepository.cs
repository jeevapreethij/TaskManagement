using System.Data;
using Dapper;
using TaskManagement.Data;
using TaskManagement.DTOs;
using TaskManagement.Models;

namespace TaskManagement.Repositories
{
    public sealed class TaskRepository : ITaskRepository
    {
        private const int CommandTimeoutSeconds = 30;
        private readonly IDbConnectionFactory _connectionFactory;

        public TaskRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<CreateTaskResult> CreateAsync(string title, string? description, DateTime dueDate, string priority, int assignedUserId, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("Title", title, DbType.String, size: 200);
            p.Add("Description", description, DbType.String, size: -1);
            p.Add("DueDate", dueDate.Date, DbType.Date);
            p.Add("Priority", priority, DbType.String, size: 10);
            p.Add("AssignedUserId", assignedUserId, DbType.Int32);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
            var row = await connection.QuerySingleAsync<CreateRow>(Command("dbo.usp_Task_Create", p, ct));
            return new CreateTaskResult(row.ResultCode, row.TaskId, row.TaskReference);
        }

        public async Task<TaskItem?> GetByReferenceAsync(Guid taskReference, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("TaskReference", taskReference, DbType.Guid);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
            return await connection.QuerySingleOrDefaultAsync<TaskItem>(Command("dbo.usp_Task_GetByReference", p, ct));
        }

        public async Task<int> UpdateAsync(Guid taskReference, string title, string? description, DateTime dueDate, string priority, int assignedUserId, byte[]? rowVersion, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("TaskReference", taskReference, DbType.Guid);
            p.Add("Title", title, DbType.String, size: 200);
            p.Add("Description", description, DbType.String, size: -1);
            p.Add("DueDate", dueDate.Date, DbType.Date);
            p.Add("Priority", priority, DbType.String, size: 10);
            p.Add("AssignedUserId", assignedUserId, DbType.Int32);
            p.Add("RowVersion", rowVersion, DbType.Binary, size: 8);

            return await ExecuteForResultCodeAsync("dbo.usp_Task_Update", p, ct);
        }

        public async Task<int> UpdateStatusAsync(Guid taskReference, string status, byte[]? rowVersion, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("TaskReference", taskReference, DbType.Guid);
            p.Add("Status", status, DbType.String, size: 20);
            p.Add("RowVersion", rowVersion, DbType.Binary, size: 8);

            return await ExecuteForResultCodeAsync("dbo.usp_Task_UpdateStatus", p, ct);
        }

        public async Task<int> AssignUserAsync(Guid taskReference, int assignedUserId, byte[]? rowVersion, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("TaskReference", taskReference, DbType.Guid);
            p.Add("AssignedUserId", assignedUserId, DbType.Int32);
            p.Add("RowVersion", rowVersion, DbType.Binary, size: 8);

            return await ExecuteForResultCodeAsync("dbo.usp_Task_AssignUser", p, ct);
        }

        public async Task<PagedResult<TaskListItemDto>> SearchAsync(string? searchTerm, int? assignedUserId, string? status, string? priority, int page, int pageSize, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("SearchTerm", searchTerm, DbType.String, size: 100);
            p.Add("AssignedUserId", assignedUserId, DbType.Int32);
            p.Add("Status", status, DbType.String, size: 20);
            p.Add("Priority", priority, DbType.String, size: 10);
            p.Add("PageNumber", page, DbType.Int32);
            p.Add("PageSize", pageSize, DbType.Int32);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
            var rows = (await connection.QueryAsync<TaskListItemDto>(Command("dbo.usp_Task_Search", p, ct))).ToList();

            return new PagedResult<TaskListItemDto>
            {
                Items = rows,
                TotalCount = rows.Count > 0 ? rows[0].TotalCount : 0,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IReadOnlyList<UserDto>> GetActiveUsersAsync(CancellationToken ct)
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
            var users = await connection.QueryAsync<UserDto>(Command("dbo.usp_User_GetActiveForAssignment", null, ct));
            return users.ToList();
        }

        private async Task<int> ExecuteForResultCodeAsync(string procedure, DynamicParameters parameters, CancellationToken ct)
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
            return await connection.QuerySingleAsync<int>(Command(procedure, parameters, ct));
        }

        private static CommandDefinition Command(string procedure, DynamicParameters? parameters, CancellationToken ct) =>
            new(procedure, parameters, commandType: CommandType.StoredProcedure, commandTimeout: CommandTimeoutSeconds, cancellationToken: ct);

        private sealed class CreateRow
        {
            public int ResultCode { get; set; }
            public int? TaskId { get; set; }
            public Guid? TaskReference { get; set; }
        }
    }
}
