using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class IdRequestDTO
    {
        [Required]
        public Guid Id { get; set; }
    }
}
