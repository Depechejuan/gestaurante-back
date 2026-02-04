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
    public class IngredienteController : ControllerBase
    {
        private readonly IngredienteService _service;
        public IngredienteController(IngredienteService servicio)
        {
            _service = servicio;
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var ingredientes = await Task.Run(() => _service.GetAll());
                List<IngredienteDTO> resultado = new List<IngredienteDTO>();
                for (int i = 0; i < ingredientes.Count; i++)
                {
                    resultado.Add(new IngredienteDTO(ingredientes[i].IdIngrediente, ingredientes[i].Nombre, ingredientes[i].Alergenico, ingredientes[i].Disponible, ingredientes[i].Imagen));
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

        //[Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateIngrediente([FromBody, Required] IngredienteDTO dto)
        {
            try
            {
                var ingrediente = await Task.Run(() => _service.CreateIngrediente(dto));
                return ResponseHelper.SendResponse(ingrediente, 201);
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
        //[Authorize]
        [HttpPost("createArray")]
        public async Task<IActionResult> CreateIngrediente([FromBody, Required] IngredienteDTO[] dto)
        {
            dto = dto.ToArray();
            try
            {
                for (int i = 0; i < dto.Length; i++)
                {
                var ingrediente = await Task.Run(() => _service.CreateIngrediente(dto[i]));
                }
                return ResponseHelper.SendResponse("Ingredientes creados correctamente", 201);
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

        //[Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIngrediente([FromRoute, Required] Guid id)
        {
            try
            {
                await Task.Run (()=>_service.DeleteIngrediente(id));
                return ResponseHelper.SendResponse(new { message = "Ingrediente eliminado correctamente." }, 200);
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

        //[Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> ModifyIngrediente([FromRoute, Required] Guid id, [FromBody, Required] IngredienteDTO nuevoIngrediente)
        {
            try
            {
                await Task.Run (() => _service.UpdateIngrediente(nuevoIngrediente.IdIngrediente, nuevoIngrediente));
                return ResponseHelper.SendResponse(new { message = "Ingrediente actualizado correctamente." }, 200);
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
