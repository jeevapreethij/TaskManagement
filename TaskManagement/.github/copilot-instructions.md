Task Management System — Complete Development Specification
1. Purpose and Scope
Build a Task Management System using the technology stack specified by the assignment. Prioritize correct behaviour, clear architecture, validation, safe error handling, and demonstrable tests over unnecessary complexity.
1.1 Technology stack
Project type: ASP.NET Core MVC
Framework: .NET 10
Language: C#
Database: SQL Server
Data access: Dapper
Database access rule: Stored procedures only. No Entity Framework and no inline SQL in C# repository code.
UI: Razor Views, HTML, CSS, Bootstrap 5
Client-side interactions: jQuery and AJAX where useful
Logging: Serilog, rolling file logs, and database logs
Testing: xUnit unit tests; YAML test data for data-driven test cases. Use integration tests for database and HTTP pipeline behaviour where unit tests alone are insufficient.
1.2 Functional scope
Users: The source assignment says to create user tables and seed entries, with no code development, although it also lists user CRUD/search features. For this plan, user management remains database setup only unless the evaluator confirms that user-management screens are expected. The application reads active users for task assignment.
Tasks: Implement create, view, edit, search, filter, assign/reassign, and status update. The source requirement does not clearly require task deletion; do not implement deletion unless clarified.
Task fields:
Task ID: database-generated integer identifier
Task reference: unique GUID used in the details URL
Title
Description (optional)
Due date
Priority: `Low`, `Medium`, `High`
Assigned user
Status: `Open`, `In Progress`, `Hold`, `Closed`
Created and updated timestamps
1.3 Explicit scope decisions
No Angular or separate Web API project is needed for this assignment.
No authentication or role-based access system is added unless requested. Anti-forgery protection is still required for state-changing MVC forms.
Do not add task deletion until the evaluator confirms it is in scope.
Use a unique GUID reference in task URLs; do not rely on a predictable integer ID as the only public reference.
Date-only due dates are interpreted as calendar dates. Use consistent date handling and avoid timezone conversion that changes the selected date.
---
2. Architecture
Use one MVC project with separation of concerns.
```text
TaskManagement/
├── Controllers/
│   └── TasksController.cs
├── Models/
│   └── TaskItem.cs
├── DTOs/
│   ├── CreateTaskRequest.cs
│   ├── UpdateTaskRequest.cs
│   ├── UpdateTaskStatusRequest.cs
│   └── TaskListItemDto.cs
├── Services/
│   ├── ITaskService.cs
│   └── TaskService.cs
├── Repositories/
│   ├── ITaskRepository.cs
│   └── TaskRepository.cs
├── Data/
│   └── IDbConnectionFactory.cs
├── Middleware/
│   └── CorrelationIdMiddleware.cs (only if not using built-in tracing)
├── Views/
│   ├── Shared/
│   │   └── Error.cshtml
│   └── Tasks/
│       ├── Index.cshtml
│       ├── Create.cshtml
│       ├── Edit.cshtml
│       └── Details.cshtml
├── wwwroot/
│   ├── css/
│   └── js/
├── SQL/
│   ├── 01\_CreateTables.sql
│   ├── 02\_SeedUsers.sql
│   └── 03\_StoredProcedures.sql
├── Tests/
│   ├── TaskManagement.UnitTests/
│   └── TaskManagement.IntegrationTests/ (if time permits)
├── appsettings.json
├── Program.cs
└── README.md
```
Layer responsibilities
Controller: HTTP binding, model-state checks, anti-forgery on state-changing form endpoints, and view/HTTP response selection.
Service: Business rules, field and status validation, active-user assignment rules, and operation coordination.
Repository: Dapper calls to stored procedures only. No inline SQL in C#.
DTOs/view models: Input/output contracts. Do not bind unrestricted database entities directly from requests.
Database: Constraints, referential integrity, stored procedures, indexes, and transactions when multiple changes must succeed atomically.
Error handler: Converts unexpected exceptions into a safe HTML error page or generic API/AJAX error response.
Logging: Serilog with structured properties and a correlation/trace identifier.
---
3. Database Design
3.1 Users table
Suggested schema:
`UserId INT IDENTITY(1,1) PRIMARY KEY`
`FullName NVARCHAR(150) NOT NULL`
`Email NVARCHAR(256) NOT NULL UNIQUE`
`DateOfJoining DATE NOT NULL`
`IsActive BIT NOT NULL DEFAULT 1`
`CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()`
`UpdatedAt DATETIME2 NULL`
Seed several representative users, including active users. If needed for testing, include an inactive user. Seed scripts should be repeatable or clearly documented as one-time scripts; avoid failing on every rerun because the same sample emails already exist.
3.2 Tasks table
Suggested schema:
`TaskId INT IDENTITY(1,1) PRIMARY KEY`
`TaskReference UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()` with a unique constraint
`Title NVARCHAR(200) NOT NULL`
`Description NVARCHAR(MAX) NULL`
`DueDate DATE NOT NULL`
`Priority NVARCHAR(10) NOT NULL`
`AssignedUserId INT NOT NULL` referencing `Users(UserId)`
`Status NVARCHAR(20) NOT NULL DEFAULT 'Open'`
`CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()`
`UpdatedAt DATETIME2 NULL`
Optional for concurrency: `RowVersion ROWVERSION`
Database constraints:
Priority restricted to `Low`, `Medium`, `High`.
Status restricted to `Open`, `In Progress`, `Hold`, `Closed`.
Task title cannot be null or empty/whitespace. Use application validation and a database check constraint where appropriate.
Assigned user must exist through a foreign key.
Task reference must be unique.
Email must be unique.
Do not hard-delete users who are referenced by tasks. Since user management is seed-only in this scope, no user deletion UI is needed.
Decide whether inactive users may remain assigned to existing tasks. Recommended: allow existing assignments to remain but block new assignments/reassignments to inactive users.
Recommended indexes:
Unique index on `TaskReference`
Index on `AssignedUserId`
Index on `Status`
Consider an index on `DueDate`
Evaluate title search needs before selecting a regular or full-text index.
Avoid adding redundant indexes without considering query patterns and write overhead.
3.3 Date handling
Treat `DueDate` as a calendar date, not a timestamp.
Validate that a due date is present and valid.
Do not prohibit past due dates unless the evaluator/business rule requires it; a task may legitimately be overdue.
Use UTC timestamps for audit columns (`CreatedAt`, `UpdatedAt`).
Set `UpdatedAt` when an update succeeds.
---
4. Validation and Null-Handling Specification
Validate input at the boundary and enforce business rules in the service and database. Client-side validation improves usability but never replaces server-side validation.
4.1 Required and optional fields
Field	Rule	Null/invalid handling
Task title	Required; trim whitespace; 1–200 characters	Reject null, empty, whitespace-only, or too long
Description	Optional; define a reasonable maximum length for the UI/request	Accept null; do not convert null into a runtime failure
Due date	Required valid date	Reject missing or invalid date
Priority	Required; exactly Low/Medium/High (case handling defined consistently)	Reject missing or unsupported value
Assigned user ID	Required positive ID	Reject missing, zero, negative, nonexistent, or inactive for new assignment
Status	Required/known value; defaults to Open only on creation if explicitly designed that way	Reject unsupported values; never silently accept arbitrary strings
Task reference	Valid GUID for details/edit/status endpoints	Reject malformed GUID; return 404 for a valid GUID with no matching task
Search term	Optional, trimmed, length-limited	Null/blank means no text filter; no matches means empty list
User filter	Optional positive ID	Missing means no user filter; invalid value is validation error or a documented safe response
Repository result for one task	May be absent	Return a nullable result/explicit not-found result; never dereference null
Repository list result	Should be a collection	Return an empty collection when no rows match; do not return null for a list
Optional DB description	Nullable	Map to `string?`
Configuration value	Required where app cannot run without it	Fail fast at startup with a clear diagnostic that does not reveal secrets
4.2 Null checks by layer
Controller
Check `ModelState.IsValid` before invoking service methods.
Do not assume bound models or service results are non-null.
Reject malformed references and invalid query parameters safely.
Return a not-found page/HTTP 404 when a task does not exist.
Return field-level validation messages for normal MVC forms.
For AJAX endpoints, return a documented JSON error shape and appropriate status code.
Service
Reject null request objects if service methods can be called outside MVC binding.
Trim and validate title.
Enforce allowed priority/status values.
Check user existence and active status before a new assignment or reassignment.
Distinguish missing task from database failure.
Treat optional description as valid when null.
Do not rely only on client-side validation.
Repository/Dapper
A single-row lookup may return no row: represent this as `null` or an explicit result.
List queries return an empty collection when no rows match.
Allow nullable database columns to map to nullable C# properties.
Check affected-row/result contracts for update operations.
Do not convert database exceptions into empty lists or fabricated success responses.
Use `CommandType.StoredProcedure` and parameters for all data operations.
Use reasonable command timeouts and cancellation tokens where supported.
Dispose connections/commands correctly; prefer `using`/`await using` and a connection factory.
4.3 Validation response contract
For regular MVC form submissions:
Invalid input: redisplay the form with field validation messages.
Valid input: perform the operation and use Post/Redirect/Get where appropriate.
Missing task: render a not-found page or return 404.
For AJAX endpoints:
`400 Bad Request`: malformed request or validation failure, with safe structured validation details.
`404 Not Found`: task/reference does not exist.
`409 Conflict`: concurrency conflict or a defined business conflict.
`500 Internal Server Error`: unexpected failure, with generic user-facing text and a correlation ID.
`503 Service Unavailable`: use only when the application can reliably identify temporary unavailability.
Do not expose exception messages, stack traces, SQL details, or connection strings.
---
5. Stored Procedures and Data Access Contract
All database access must use stored procedures. No inline SQL in C# repository methods.
Suggested procedures:
`usp\_Task\_Create`
`usp\_Task\_GetByReference`
`usp\_Task\_GetById` (only if needed internally)
`usp\_Task\_Update`
`usp\_Task\_UpdateStatus`
`usp\_Task\_AssignUser`
`usp\_Task\_Search`
`usp\_Task\_GetAll`
`usp\_User\_GetActiveForAssignment`
For every procedure, document:
Parameter names, SQL types, sizes, and required/optional status.
Validation and allowed values.
Return/result shape.
How “not found” is represented.
How callers determine whether an update affected a row.
Whether `UpdatedAt` is updated.
Transaction behaviour, if multiple changes are involved.
Expected constraint failures and how the service translates them.
Use parameters for every value. Do not concatenate user-provided search strings into dynamic SQL. If dynamic SQL is unavoidable inside a stored procedure, parameterize it safely and review allowed sort/filter identifiers against an allowlist.
For writes, avoid relying on a returned identity alone as proof of complete success. Define the stored procedure output/result contract explicitly. Consider `SET NOCOUNT ON` in procedures and return a clear result set/output parameter where required.
Concurrency
For this assignment, optimistic concurrency is recommended only if time permits. If using `RowVersion`, include it in the read DTO and require it on update. When the stored procedure detects that the row changed since it was read, return a conflict result; the UI should ask the user to reload rather than silently overwriting another user's changes.
Transactions
Use a transaction where a single business operation performs multiple database changes that must succeed or fail together. Do not add unnecessary transactions around every single stored-procedure call.
---
6. UI and AJAX Requirements
6.1 Task list
Search by numeric Task ID, task reference, or title, according to a documented search contract.
Filter by assigned user.
Optionally filter by status and priority if useful.
Display status and priority badges.
Highlight overdue tasks.
Show a clear empty-state message.
Add pagination if the dataset could grow; do not load unbounded data in a production-sized list.
6.2 Create/Edit forms
Use Bootstrap 5.
Use client-side validation for usability and server-side validation for enforcement.
Populate the assignment dropdown from active users.
Preserve valid user input when validation fails.
Use anti-forgery tokens for state-changing form requests.
Use POST for create/update/status changes; do not mutate data through GET.
Avoid overposting by binding to dedicated request DTOs/view models.
6.3 AJAX behaviour
Handle success, validation failure, not-found, server error, service unavailable, timeout, and network failure.
Display safe messages; never show raw server exception text.
Prevent duplicate form submission while a request is in progress where practical.
Do not automatically retry a write request unless idempotency/duplicate protection has been designed.
On success, update the relevant UI or reload the affected task list.
Keep a normal MVC form flow where practical as a fallback.
6.4 Output safety
Rely on Razor's default HTML encoding.
Do not render task titles/descriptions as raw HTML.
If any raw HTML rendering is ever introduced, sanitize it with a well-reviewed sanitizer.
---
7. Exception Handling
7.1 Centralized handling
Configure centralized exception handling in `Program.cs`, with:
A user-friendly HTML error page for normal MVC requests.
A generic JSON error response for AJAX endpoints where appropriate.
A correlation/trace ID included in the response and logs.
Detailed exception information available only in server-side logs.
Development-only diagnostics enabled only in Development, never exposed in production.
7.2 Expected conditions versus unexpected exceptions
Validation failures: return validation errors; do not throw exceptions for ordinary invalid user input.
Task not found: return a not-found result/404; do not treat normal absence as a server failure.
Invalid/inactive assignment: return a business validation result or conflict where appropriate.
Database constraint errors: translate known cases into safe business messages where useful.
SQL connection errors/timeouts: log at the appropriate boundary and return a safe error response.
Unexpected exceptions: allow centralized handling to log and return a generic response.
7.3 Exception handling rules
Never use an empty `catch` block.
Never catch an exception and return an empty task list as if the query succeeded.
Do not log and rethrow at every layer. Log once at the appropriate boundary with operation context.
Preserve the original exception when rethrowing (`throw;`, not `throw ex;`).
Do not expose exception messages, stack traces, SQL text, or secrets to users.
Ensure error handling itself does not create recursive logging failures.
---
8. Logging with Serilog
Configure Serilog with structured logging and:
Rolling file logs with a documented path, retention policy, and file-size strategy if needed.
Database logging through a suitable sink or controlled implementation.
Appropriate minimum levels by environment.
Request/trace/correlation ID.
Structured properties such as operation name and task reference.
Exception details for unexpected failures.
Startup and shutdown events.
8.1 Suggested log levels
Debug: Detailed diagnostics useful during development; avoid noisy production logging.
Information: Application lifecycle and important successful business operations.
Warning: Unexpected but recoverable conditions or suspicious repeated invalid requests, where useful.
Error: Failed operations and exceptions requiring investigation.
Fatal: Startup or other failures that prevent the application from continuing.
Avoid logging every successful read at Information level by default. Avoid duplicate exception logs at each layer.
8.2 What to log
Task create/update/status/assignment operation outcomes.
Failed database operations and timeouts.
Unexpected exceptions.
Correlation/trace ID, operation name, task reference, and elapsed time where useful.
Configuration failures without exposing secret values.
8.3 What not to log
Passwords, access tokens, connection strings with credentials, or secret values.
Entire request bodies by default.
Unnecessary personal data.
Raw SQL parameter values where they may contain sensitive information.
8.4 Logging sink failures
Configure and test file and database sinks independently.
Avoid recursive logging if a sink fails.
Decide and document whether database logging is best-effort or operationally critical. For this assignment, business functionality should not be silently reported as failed only because a secondary logging sink is unavailable, unless the application's explicit compliance requirement says otherwise.
Ensure file retention prevents unbounded log growth.
Document log locations and basic troubleshooting steps in README.
---
9. Security and Reliability
Use anti-forgery tokens for state-changing MVC form requests.
Restrict HTTP methods; GET requests must not change state.
Bind only expected fields through request DTOs/view models to prevent overposting.
Validate all user input on the server.
Use parameterized stored procedure calls.
Review any dynamic SQL inside stored procedures.
Use Razor output encoding.
Keep production secrets out of source control.
Use a generic production error page and safe AJAX error messages.
Set reasonable request/field length limits.
Use cancellation tokens and command timeouts where practical.
Avoid duplicate writes from repeated clicks; disable submit while pending and use stronger idempotency protection if the workflow requires it.
Authentication/authorization are outside scope unless the evaluator requires them; document this limitation rather than implying the application is production-secure without them.
---
10. Testing Strategy
Use xUnit for unit tests. Use YAML files as data-driven test-case definitions, parsed by a YAML library such as YamlDotNet in the test project. YAML is a test data format, not a unit-testing framework. Use integration tests for SQL Server stored procedures, database constraints, MVC filters, anti-forgery, middleware, and real HTTP response behaviour.
Recommended test layers:
Service unit tests: mock `ITaskRepository`; verify validation and business rules.
Controller tests: verify model-state behaviour, response selection, and view/redirect results.
Repository/database integration tests: execute stored procedures against a dedicated test database; do not use a production database.
HTTP pipeline tests: verify centralized errors, status codes, anti-forgery, and AJAX response format where practical.
Manual UI checks: verify client-side validation, empty states, AJAX errors, and accessibility/usability.
Do not mock Dapper itself as a substitute for testing SQL stored procedures. Use a dedicated SQL Server test database for integration tests. Keep unit tests fast and independent from SQL Server.
10.1 Example YAML test-case file
Save as `Tests/TaskManagement.UnitTests/TestCases/task-validation-cases.yml`.
```yaml
# Data-driven validation cases for the TaskService.
# The test runner should deserialize these cases and pass them to xUnit \[Theory] tests.
# YAML is test data; xUnit executes the tests.

- id: VAL001
  scenario: "Null request"
  title: null
  dueDate: "2026-11-01"
  priority: "Medium"
  assignedUserId: 1
  expectedValid: false
  expectedError: "Request is required"

- id: VAL002
  scenario: "Missing title"
  title: null
  dueDate: "2026-11-01"
  priority: "Medium"
  assignedUserId: 1
  expectedValid: false
  expectedError: "Title is required"

- id: VAL003
  scenario: "Whitespace-only title"
  title: "   "
  dueDate: "2026-11-01"
  priority: "Medium"
  assignedUserId: 1
  expectedValid: false
  expectedError: "Title is required"

- id: VAL004
  scenario: "Title exceeds maximum length"
  title: "GENERATE\_201\_CHARACTERS"
  dueDate: "2026-11-01"
  priority: "Medium"
  assignedUserId: 1
  expectedValid: false
  expectedError: "Title cannot exceed 200 characters"

- id: VAL005
  scenario: "Optional description is null"
  title: "Prepare fund report"
  description: null
  dueDate: "2026-11-01"
  priority: "High"
  assignedUserId: 1
  expectedValid: true
  expectedError: null

- id: VAL006
  scenario: "Invalid priority"
  title: "Prepare fund report"
  dueDate: "2026-11-01"
  priority: "Urgent"
  assignedUserId: 1
  expectedValid: false
  expectedError: "Priority is invalid"

- id: VAL007
  scenario: "Missing priority"
  title: "Prepare fund report"
  dueDate: "2026-11-01"
  priority: null
  assignedUserId: 1
  expectedValid: false
  expectedError: "Priority is required"

- id: VAL008
  scenario: "Assigned user ID is zero"
  title: "Prepare fund report"
  dueDate: "2026-11-01"
  priority: "Low"
  assignedUserId: 0
  expectedValid: false
  expectedError: "Assigned user is invalid"

- id: VAL009
  scenario: "Valid task"
  title: "Prepare fund report"
  description: "Prepare the monthly report"
  dueDate: "2026-11-01"
  priority: "Medium"
  assignedUserId: 1
  expectedValid: true
  expectedError: null
```
Implementation note: The YAML is a test-case specification. `GENERATE\_201\_CHARACTERS` is a placeholder and must be expanded by the test loader into a 201-character title before execution. The date is sample data; tests should avoid depending on the actual current date unless specifically testing date rules.
10.2 Suggested unit test cases
Test ID	Component	Scenario	Expected result
UT001	Service	Null create request	Validation failure; repository not called
UT002	Service	Null/empty/whitespace title	Validation failure
UT003	Service	Title exceeds 200 characters	Validation failure
UT004	Service	Null description	Accepted
UT005	Service	Invalid priority	Validation failure
UT006	Service	Invalid status	Validation failure
UT007	Service	Assigned user does not exist	Assignment rejected
UT008	Service	Assigned user inactive for new assignment	Assignment rejected
UT009	Service	Valid create request	Repository create called with validated values
UT010	Service	Task lookup returns null	Not-found result
UT011	Service	Search returns empty list	Empty collection
UT012	Service	Invalid task reference format	Rejected before repository lookup
UT013	Service	Update task that does not exist	Not-found result
UT014	Service	Unsupported status transition, if transitions are restricted	Rejected
UT015	Controller	Invalid model state	Form returned; service not called
UT016	Controller	Valid create request	Service invoked and redirect/result returned
UT017	Controller	Task not found	Not-found result
UT018	Error handler	Unexpected exception	Generic response; exception logged once
UT019	Logging	Exception log	Correlation ID and operation context present
UT020	Security	Missing anti-forgery token on protected form post	Request rejected by integration test
10.3 Integration and database test cases
Test ID	Scenario	Expected result
IT001	Run all schema scripts on a clean test database	Tables, constraints, and procedures created
IT002	Run seed script twice	Behaviour is documented and repeatable, or second run safely avoids duplicates
IT003	Create task with valid active user	Row created; task reference returned
IT004	Create task with nonexistent user	Foreign key/business validation prevents invalid assignment
IT005	Insert invalid priority directly through test procedure	Database constraint or procedure rejects it
IT006	Insert invalid status directly through test procedure	Database constraint or procedure rejects it
IT007	Retrieve unknown GUID	No row; application maps to not found
IT008	Search with no matches	Empty result set and empty collection
IT009	Update unknown task	Clear no-row/not-found result
IT010	Update valid task	Fields and `UpdatedAt` updated
IT011	Assign inactive user to a new task	Procedure/service rejects it according to the defined rule
IT012	SQL connection unavailable	Safe error handling and diagnostic log
IT013	Stored procedure times out	Timeout handled without fabricated success
IT014	Database logging sink unavailable	Application follows documented sink-failure policy
IT015	File logging path unavailable	Failure is handled without recursive logging
IT016	Two concurrent updates, if rowversion is implemented	One update conflicts rather than silently overwriting
IT017	Search text includes SQL metacharacters	Treated as input; no SQL injection
IT018	Task title/description contains HTML markup	Output safely encoded
10.4 YAML-driven test execution requirements
Add the YAML test file to the test project and ensure it is copied to the test output directory.
Use YamlDotNet (or another maintained YAML parser) to deserialize the cases.
Use xUnit `\[Theory]` and `\[MemberData]` or a custom test-case loader to execute each case.
Keep test data separate from assertions and test logic.
Include the YAML case ID in assertion messages so failures are traceable.
Validate the YAML structure and fail clearly if a required field is missing or has the wrong type.
Do not use YAML as a replacement for integration tests or actual assertions.
Avoid tests that depend on a real production database or current time without controlling those dependencies.
---
11. Test Matrix: All Major Edge Cases
Before considering the project complete, verify the following.
Input and null handling
[ ] Null request
[ ] Missing/empty/whitespace title
[ ] Title at minimum and maximum allowed length
[ ] Title beyond maximum length
[ ] Null optional description
[ ] Description at and beyond the defined length limit
[ ] Missing/invalid due date
[ ] Past due date behaves according to the documented rule
[ ] Null/invalid priority
[ ] Null/invalid status
[ ] Missing, zero, negative, or nonexistent assigned user ID
[ ] Inactive-user assignment rejected for new assignment
[ ] Malformed and unknown task references
[ ] Null search term, whitespace search term, long search term
[ ] Invalid filter values
CRUD and business behaviour
[ ] Create, read, update, search, filter, assign, reassign, and status update
[ ] Unknown task handled as not found
[ ] Empty list handled without exceptions
[ ] Invalid status transition handled if transitions are restricted
[ ] Duplicate submission handled where required
[ ] Concurrent update behaviour defined and tested if implemented
Database
[ ] Stored procedure parameters match C# types and names
[ ] Constraints reject invalid priority/status and broken foreign keys
[ ] Unique task reference enforced
[ ] Unique email enforced
[ ] No inline SQL in C# repositories
[ ] No SQL concatenation from user input
[ ] Database outage and timeout handled
[ ] Update with zero affected rows handled
[ ] Transactions used only when needed
[ ] Test database isolated from production
Exception handling and logging
[ ] Unexpected exception is centrally handled
[ ] No stack trace or SQL details shown to user
[ ] Correlation/trace ID included in logs and error response
[ ] Exceptions are not swallowed
[ ] Exceptions are not redundantly logged at every layer
[ ] File sink configured with rolling and retention
[ ] Database sink failure policy documented and tested
[ ] Secrets and unnecessary personal data are not logged
UI, AJAX, and security
[ ] Client-side and server-side validation
[ ] AJAX validation failure displays useful messages
[ ] AJAX 404, 500, 503, timeout, and network failure handled
[ ] Duplicate form submission prevented where practical
[ ] Anti-forgery token applied to state-changing MVC form posts
[ ] GET endpoints do not mutate state
[ ] DTO/view-model binding prevents overposting
[ ] Razor safely encodes user-entered content
[ ] Empty search results have a clear message
[ ] Loading state and error state are understandable
---
12. Implementation Plan
Create an ASP.NET Core MVC project targeting .NET 8.
Add Dapper, SQL Server client, Serilog, xUnit, and test YAML parser packages as appropriate.
Create SQL schema, constraints, indexes, and repeatable sample-user seed script.
Create stored procedures and document each procedure's parameters and result contract.
Test the procedures against a dedicated development/test database.
Configure connection factory, dependency injection, Serilog, centralized exception handling, and tracing/correlation ID.
Implement DTOs and validation rules.
Implement repository methods using stored procedures only.
Implement service rules and null/missing-record behaviour.
Implement controller actions and safe response handling.
Build Razor views with Bootstrap 5.
Add jQuery/AJAX with explicit success and failure paths.
Add YAML-driven xUnit validation tests.
Add service/controller unit tests and database/HTTP integration tests where appropriate.
Verify the test matrix and document any out-of-scope items.
Write README setup, test execution, database setup, log locations, and troubleshooting instructions.
---
13. Acceptance Checklist
[ ] .NET 8 ASP.NET Core MVC application
[ ] Dapper only; no Entity Framework
[ ] All C# data access uses stored procedures
[ ] Users table and sample data created
[ ] Task create/read/update/search/filter/assign/reassign/status update works
[ ] Unique task reference used in details URL
[ ] Database constraints and indexes included where appropriate
[ ] Null and missing-record handling defined at controller, service, and repository layers
[ ] Client-side and server-side validation implemented
[ ] Centralized exception handling configured
[ ] Safe error responses and appropriate HTTP status codes
[ ] Serilog file and database logging configured
[ ] Correlation/trace ID available for diagnostics
[ ] Logging secrets and recursive logging failures avoided
[ ] AJAX success and failure paths implemented
[ ] Anti-forgery protection applied to state-changing form posts
[ ] DTOs/view models prevent overposting
[ ] YAML-driven xUnit test cases are loaded and executed
[ ] Unit and integration tests cover the key edge cases
[ ] README includes database setup, application setup, test execution, and logging guidance
14. Setup and Test Execution
Install the .NET 8 SDK and SQL Server/LocalDB.
Create a development database and a separate test database.
Run SQL scripts in the documented order.
Configure the connection string through local user secrets or environment-specific configuration; do not commit real secrets.
Restore packages and run the MVC application.
Run unit tests with `dotnet test`.
Run integration tests only when the dedicated test database is configured.
Confirm that test output identifies the YAML case ID for data-driven failures.
Verify log files, retention, error responses, and correlation IDs.
Record any intentionally unimplemented items, such as authentication or task deletion, in the README.
15. Final Scope Notes
User CRUD screens are not implemented unless the evaluator clarifies that they are required; the assignment's wording is ambiguous.
Task deletion is not included until confirmed.
Authentication/authorization is not included unless required. This must not be described as fully production-secure without an appropriate access-control model.
Optimistic concurrency and pagination are recommended enhancements if time and evaluation criteria justify them.
YAML provides data-driven test cases; xUnit is the test runner and assertion framework.