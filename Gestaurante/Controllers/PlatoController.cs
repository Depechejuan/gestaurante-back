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
        private readonly PlatoService _service;
        public PlatoController(PlatoService servicio)
        {
            _service = servicio;
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var platos = await Task.Run(() => _service.GetAll());
                List<PlatoDTO> resultado = new List<PlatoDTO>();
                for (int i = 0; i < platos.Count; i++)
                {
                    resultado.Add(new PlatoDTO(platos[i].Nombre, platos[i].Descripcion, platos[i].Imagen, platos[i].Disponible, platos[i].Precio, platos[i].Categoria, platos[i].PlatoIngredientes));
                }
                return ResponseHelper.SendResponse(resultado, 200);
            }
            catch (Exception ex)
            {
                return ResponseHelper.SendError(new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                }, 500);
            }
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreatePlato([FromBody, Required] PlatoDTO dto)
        {
            try
            {
                await Task.Run(() => _service.CreatePlato(dto));
                return ResponseHelper.SendResponse(dto, 201);
            }
            catch (Exception ex)
            {
                return ResponseHelper.SendError(new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                }, 500);
            }
        }
    }
}


