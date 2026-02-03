using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
namespace Gestaurante.Controllers
{

        [ApiController]
        [Route("[controller]")]
        public class PlatoController : ControllerBase
        {
        [HttpGet("getall")]
        public IActionResult GetAllPlatos()
        {
            var platos = new List<Plato>
                {
                    new Plato {  Nombre = "Spaghetti Carbonara", Descripcion = "Pasta con salsa de huevo, queso y panceta.", Precio = 12.99M },
                    new Plato {  Nombre = "Margherita Pizza", Descripcion = "Pizza clásica con tomate, mozzarella y albahaca.", Precio = 10.99M },
                    new Plato {  Nombre = "Caesar Salad", Descripcion = "Ensalada con lechuga romana, crutones y aderezo César.", Precio = 8.99M }
                };
                return ResponseHelper.SendResponse(platos);
        
        }
        //private readonly Plato _platoService;
        //public PlatoController(Plato platoService)
        //{
        //    _platoService = platoService;
        //}
        //[HttpGet("getall")]
        //public IActionResult GetAllPlatos()
        //{
        //    try
        //    {
        //        var platos = _platoService.GetAllPlatos();
        //        return ResponseHelper.SendResponse(platos);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ResponseHelper.SendError(new
        //        {
        //            message = ex.Message,
        //            detail = ex.InnerException?.Message
        //        }, 500);
        //    }
        //}
    }
    
}
