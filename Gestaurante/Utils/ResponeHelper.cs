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


        public static IActionResult SendError(object? error, int status = 400)
        {
            var response = new
            {
                status,
                error
            };

            return new ObjectResult(response) { StatusCode = status };
        }
    }
}
