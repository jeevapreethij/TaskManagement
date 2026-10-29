# TaskManagement

ASP.NET Core MVC task tracker backed by SQL Server stored procedures (Dapper-style repository, Serilog logging).

## Setup

1. Create a database (for example `TaskMgnt`) and run the scripts in `SQL/` **in order**:
   1. `01_CreateTables.sql`
   2. `02_SeedUsers.sql`
   3. `03_StoredProcedures.sql`
2. Point `ConnectionStrings:DefaultConnection` (`appsettings.json`, user secrets, or the
   `ConnectionStrings__DefaultConnection` environment variable) at that database:
   `Server=localhost;Database=TaskMgnt;Integrated Security=True;TrustServerCertificate=True;`
3. Run `dotnet run` (or F5 in Visual Studio) and open the HTTPS URL shown.

> "Could not find stored procedure" means the app is connected to a database where `03_StoredProcedures.sql` has not been run.

## Features

| Action | How it works |
|---|---|
| List / search | `Tasks` page with filters (text, user, status, priority) and paging |
| New task | Opens in a modal pop-up |
| Details | Opens in a modal pop-up (includes status update) |
| Edit | Opens in a modal pop-up |
| Reassign | Opens in a modal pop-up; changes the assigned user (stored procedure `usp_Task_AssignUser`) |

Every pop-up has a header close (X) button, plus Cancel/Save (or Close for read-only Details).
Clicking outside a pop-up does not dismiss it. Pop-ups cannot be minimized (not supported by browser modals).
The full pages (`/Tasks/Create`, `/Tasks/Edit/{id}`, `/Tasks/Details/{id}`) still work as a fallback.

## Reusable modal component

Built to be reused by any module.

| Part | File |
|---|---|
| Dialog shell | `Views/Shared/_ModalDialog.cshtml`, options in `Models/ModalOptions.cs` |
| Behavior | `wwwroot/js/modal-form.js` (loaded in `_Layout.cshtml`) |
| AJAX detection | `Extensions/HttpRequestExtensions.cs` (`Request.IsAjaxRequest()`) |

### Using it for a new module

1. Add a dialog and a trigger to the page:
   ```razor
   <a asp-controller="Users" asp-action="Create" data-modal-target="#userModal">New user</a>
   <partial name="_ModalDialog" model="@(new ModalOptions { Id = "userModal", Title = "New user" })" />
   ```
   `ModalOptions`: `Id`, `Title`, `SaveText`, `CancelText`, `Size` (`modal-sm|md|lg|xl`), `ShowSave`.
2. GET action: return `PartialView(...)` for AJAX requests (`Request.IsAjaxRequest()`), otherwise the full view.
   The partial must contain a `<form data-modal-form>`; Save submits that form.
3. POST action: on success return `Json(new { success = true })` (optional `redirectUrl`);
   on validation failure return the same partial (errors are re-rendered in the dialog).
   JSON errors (`{ error, errors }`) are shown as an alert inside the dialog.
4. Optional: listen for `modalform:success` on the dialog element; call `preventDefault()` to skip the default page reload/redirect.

Forms inside a pop-up that are not marked `data-modal-form` (for example the status form, marked `data-ajax-form`) are handled by `site.js` instead.

## Reusable components inventory

| Component | Location | Reused by |
|---|---|---|
| Modal dialog shell (`ModalOptions`) | `Views/Shared/_ModalDialog.cshtml`, `Models/ModalOptions.cs` | New task, Details, Edit, Reassign (any future module) |
| Modal behaviour (load, validate, submit, errors) | `wwwroot/js/modal-form.js` | Every `data-modal-target` trigger |
| Shared AJAX error messages | `wwwroot/js/ajax-messages.js` (`AppAjax`) | `modal-form.js`, `site.js` |
| AJAX detection | `Extensions/HttpRequestExtensions.cs` | `TasksController`, `HomeController` |
| Task field validation rules | `DTOs/TaskFieldsBase.cs` | `CreateTaskRequest`, `UpdateTaskRequest` |
| Task form fields | `Views/Tasks/_TaskFields.cshtml` | `_CreateForm`, `_EditForm` |
| Priority/status badge styles | `Models/BadgeStyles.cs` | Task list, Details |
| Correlation ID + logging context | `Middleware/CorrelationIdMiddleware.cs` | All requests, logs, error responses |
| DB connection factory, stored-procedure command helper | `Data/IDbConnectionFactory.cs`, `TaskRepository.Command(...)` | All repository methods |
| Result pattern | `Services/ServiceResult.cs` | All service operations |
| Error/404 pages and JSON error shape | `HomeController.Error/StatusCodePage`, `Views/Shared/Error.cshtml`, `NotFound.cshtml` | Whole app (HTML and AJAX) |

## Project layout

- `Controllers/` MVC controllers
- `Services/`, `Repositories/` business logic and stored-procedure data access
- `DTOs/`, `Models/` request/view models
- `Views/Tasks/` `_CreateForm`, `_EditForm`, `_Details`, `_ReassignForm` partials used by the pop-ups
- `SQL/` database scripts
