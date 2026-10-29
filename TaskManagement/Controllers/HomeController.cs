using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TaskManagement.Extensions;
using TaskManagement.Models;

namespace TaskManagement.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [IgnoreAntiforgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier;

            if (Request.IsAjaxRequest())
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "An unexpected error occurred.",
                    correlationId = requestId
                });
            }

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return View(new ErrorViewModel { RequestId = requestId });
        }

        /// <summary>Friendly pages for empty-body status codes such as 404 (UseStatusCodePagesWithReExecute).</summary>
        [IgnoreAntiforgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusCodePage(int code)
        {
            var requestId = Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier;
            Response.StatusCode = code;

            if (Request.IsAjaxRequest())
            {
                return Json(new
                {
                    error = code == StatusCodes.Status404NotFound ? "The requested item was not found." : "The request could not be completed.",
                    correlationId = requestId
                });
            }

            var model = new ErrorViewModel { RequestId = requestId };
            return code == StatusCodes.Status404NotFound ? View("NotFound", model) : View("Error", model);
        }
    }
}
