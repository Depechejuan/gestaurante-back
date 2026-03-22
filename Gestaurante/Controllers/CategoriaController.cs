using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CategoriaController : ControllerBase
    {
        private readonly CategoriaService _service;

        public CategoriaController(CategoriaService service)
        {
            _service = service;
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var categorias = await _service.GetAll();
            var result = categorias.Select(c => new CategoriaDTO(c.IdCategoria, c.Descripcion)).ToList();
            return Ok(result);
        }

        //[Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CategoriaDTO dto)
        {
            await _service.Create(dto);
            return Ok();
        }

        //[Authorize]
        [HttpPut("modify")]
        public async Task<IActionResult> Update([FromBody] CategoriaDTO dto)
        {
            var updated = await _service.Update(dto);
            if (!updated) return NotFound();
            return Ok();
        }

        //[Authorize]
        [HttpDelete("delete{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.Delete(id);
            if (!deleted) return NotFound();
            return Ok();
        }
    }
}