using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Utils
{
    public static class ResponseHelper
    {
        public static IActionResult SendResponse(object? data, int status = 200)
        {
            var response = new
            {
                status,
                data
            };

            return new ObjectResult(response) { StatusCode = status };
        }

        /// <summary>
        /// Recibe 2 parámetros: error y status code.
        /// </summary>
        /// <param name="error">Excepción</param>
        /// <param name="status">el código de excepción (o nada)</param>
        /// <returns></returns>
        public static IActionResult SendError(object? error, int status = 400)
        {
            var response = new
            {
                status,
                error
            };

            return new ObjectResult(response) { StatusCode = status };
        }

        /// <summary>
        /// Recibe uno o ningún parámetro de error.
        /// </summary>
        /// <param name="error">Null or Exception</param>
        /// <returns>ObjectResult</returns>
        public static IActionResult GenericError(object? error)
        {
            int status = 400;
            if (error == null)
                error = "Ha ocurrido un error. Contacta con el administrador para más información";
            var response = new
            {
                status,
                error
            };
            return new ObjectResult(response) { StatusCode = status };
        }

        public static IActionResult NotAuthorized(object? error)
        {
            int status = 401;
            if (error == null)
                error = "Not Authorized.";

            var response = new
            {
                status,
                error
            };
            return new ObjectResult(response) { StatusCode = status };
        }

        public static IActionResult TimeOut()
        {
            int status = 408;
            string error = "Time Out.";
            var response = new
            {
                status,
                error
            };
            return new ObjectResult(response) { StatusCode = status };
        }

        public static IActionResult Forbidden(object? error = null)
        {
            int status = 403;
            error ??= "Access denied.";

            return new ObjectResult(new { status, error }) { StatusCode = status };
        }

        public static IActionResult NotFound(object? error = null)
        {
            int status = 404;
            error ??= "Resource not found.";

            return new ObjectResult(new { status, error }) { StatusCode = status };
        }

        public static IActionResult ValidationError(object? error)
        {
            int status = 422;
            error ??= "Validation error.";

            return new ObjectResult(new { status, error }) { StatusCode = status };
        }
        public static IActionResult Conflict(object? error = null)
        {
            int status = 409;
            error ??= "Conflict with current resource state.";

            return new ObjectResult(new { status, error }) { StatusCode = status };
        }

        public static IActionResult ServerError(object? error = null)
        {
            int status = 500;
            error ??= "Internal server error.";

            return new ObjectResult(new { status, error }) { StatusCode = status };
        }

    }
}
