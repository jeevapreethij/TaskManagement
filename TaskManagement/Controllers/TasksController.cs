using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManagement.DTOs;
using TaskManagement.Extensions;
using TaskManagement.Models;
using TaskManagement.Services;

namespace TaskManagement.Controllers
{
    public class TasksController : Controller
    {
        private readonly ITaskService _service;

        public TasksController(ITaskService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] TaskSearchCriteria criteria, CancellationToken ct)
        {
            var model = new TaskListViewModel
            {
                Criteria = criteria,
                Users = await _service.GetActiveUsersAsync(ct)
            };

            if (ModelState.IsValid)
            {
                var result = await _service.SearchAsync(criteria, ct);
                if (result.IsSuccess && result.Value is not null)
                {
                    model.Result = result.Value;
                }
                else
                {
                    AddErrors(result.Errors);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var task = await _service.GetByReferenceAsync(id, ct);
            if (task is null)
            {
                return NotFound();
            }

            ViewBag.Users = await GetUserListAsync(ct, task.AssignedUserId);
            if (Request.IsAjaxRequest())
            {
                return PartialView("_Details", task);
            }
            return View(task);
        }

        [HttpGet]
        public async Task<IActionResult> Reassign(Guid id, CancellationToken ct)
        {
            var task = await _service.GetByReferenceAsync(id, ct);
            if (task is null)
            {
                return NotFound();
            }

            ViewBag.Users = await GetUserListAsync(ct, task.AssignedUserId);
            ViewBag.RowVersion = Convert.ToBase64String(task.RowVersion);
            return PartialView("_ReassignForm", task);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadUsersAsync(ct);
            var model = new CreateTaskRequest { DueDate = DateTime.Today };
            if (Request.IsAjaxRequest())
            {
                return PartialView("_CreateForm", model);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken ct)
        {
            if (ModelState.IsValid)
            {
                var result = await _service.CreateAsync(request, ct);
                if (result.IsSuccess)
                {
                    TempData["Success"] = "Task created.";
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }
                    return RedirectToAction(nameof(Details), new { id = result.Value });
                }

                AddErrors(result.Errors);
            }

            await LoadUsersAsync(ct, request?.AssignedUserId);
            if (Request.IsAjaxRequest())
            {
                return PartialView("_CreateForm", request);
            }
            return View(request);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var task = await _service.GetByReferenceAsync(id, ct);
            if (task is null)
            {
                return NotFound();
            }

            await LoadUsersAsync(ct, task.AssignedUserId);
            var model = new UpdateTaskRequest
            {
                TaskReference = task.TaskReference,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate.Date,
                Priority = task.Priority,
                AssignedUserId = task.AssignedUserId,
                RowVersion = Convert.ToBase64String(task.RowVersion)
            };
            if (Request.IsAjaxRequest())
            {
                return PartialView("_EditForm", model);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UpdateTaskRequest request, CancellationToken ct)
        {
            if (ModelState.IsValid)
            {
                var result = await _service.UpdateAsync(request, ct);
                if (result.IsSuccess)
                {
                    TempData["Success"] = "Task updated.";
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }
                    return RedirectToAction(nameof(Details), new { id = request.TaskReference });
                }

                if (result.Status == ServiceStatus.NotFound)
                {
                    return NotFound();
                }

                AddErrors(result.Errors);
            }

            await LoadUsersAsync(ct, request?.AssignedUserId);
            if (Request.IsAjaxRequest())
            {
                return PartialView("_EditForm", request);
            }
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(UpdateTaskStatusRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return Fail(request?.TaskReference ?? Guid.Empty, ServiceResult.Failure(ServiceStatus.ValidationFailed, ModelErrors()));
            }

            var result = await _service.UpdateStatusAsync(request, ct);
            return Respond(request.TaskReference, result, "Status updated.");
        }

        [HttpPost]
        public async Task<IActionResult> Reassign(AssignTaskRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return Fail(request?.TaskReference ?? Guid.Empty, ServiceResult.Failure(ServiceStatus.ValidationFailed, ModelErrors()));
            }

            var result = await _service.AssignAsync(request, ct);
            return Respond(request.TaskReference, result, "Task reassigned.");
        }

        private IActionResult Respond(Guid reference, ServiceResult result, string successMessage)
        {
            if (!result.IsSuccess)
            {
                return Fail(reference, result);
            }

            if (IsAjax)
            {
                return Json(new { success = true, message = successMessage });
            }

            TempData["Success"] = successMessage;
            return RedirectToAction(nameof(Details), new { id = reference });
        }

        private IActionResult Fail(Guid reference, ServiceResult result)
        {
            var (statusCode, message) = result.Status switch
            {
                ServiceStatus.NotFound => (StatusCodes.Status404NotFound, "Task not found."),
                ServiceStatus.Conflict => (StatusCodes.Status409Conflict, "The task was changed by someone else. Please reload and try again."),
                _ => (StatusCodes.Status400BadRequest, "The request is invalid.")
            };

            if (IsAjax)
            {
                return StatusCode(statusCode, new
                {
                    error = message,
                    errors = result.Errors,
                    correlationId = Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier
                });
            }

            if (result.Status == ServiceStatus.NotFound)
            {
                return NotFound();
            }

            var detail = result.Errors.Count > 0 ? string.Join(" ", result.Errors.Values) : message;
            TempData["Error"] = detail;
            return RedirectToAction(nameof(Details), new { id = reference });
        }

        private bool IsAjax =>
            string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        private Dictionary<string, string> ModelErrors() =>
            ModelState.Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(e => e.Key, e => e.Value!.Errors[0].ErrorMessage);

        private void AddErrors(IReadOnlyDictionary<string, string> errors)
        {
            foreach (var (field, message) in errors)
            {
                ModelState.AddModelError(field == "Request" ? string.Empty : field, message);
            }
        }

        private async Task LoadUsersAsync(CancellationToken ct, int? selectedUserId = null)
        {
            ViewBag.Users = await GetUserListAsync(ct, selectedUserId);
        }

        private async Task<SelectList> GetUserListAsync(CancellationToken ct, int? selectedUserId)
        {
            var users = await _service.GetActiveUsersAsync(ct);
            return new SelectList(users, nameof(UserDto.UserId), nameof(UserDto.FullName), selectedUserId);
        }
    }
}
