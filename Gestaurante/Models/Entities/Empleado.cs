using Gestaurante.Models.DTO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestaurante.Models.Entities
{
    public abstract class Empleado
    {
        [Required]
        public Guid Id { get; protected set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; protected set; } = string.Empty;
        [Required]
        [MinLength(8)]
        [MaxLength(255)]
        public string Password { get; protected set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string FirstName { get; protected set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstLastName { get; protected set; } = string.Empty;

        [Required]
        [MaxLength(100)]    
        public string SecondLastName { get; protected set; } = string.Empty;

        [Required]
        [MinLength(9)]
        public string DNI { get; protected set; } = string.Empty;
        [MinLength(13)]
        public string NUSS { get; protected set; } = string.Empty;

        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; protected set; } = DateTime.UtcNow;

        protected Empleado() { }
    }
}
