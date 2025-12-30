using Gestaurante.Models.DTO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestaurante.Models.Entities
{
    public abstract class Empleado
    {
        public Guid Id { get; protected set; }

        public string Email { get; protected set; } = string.Empty;
        public string Password { get; protected set; } = string.Empty;

        public string FirstName { get; protected set; } = string.Empty;
        public string FirstLastName { get; protected set; } = string.Empty;
        public string SecondLastName { get; protected set; } = string.Empty;

        public string DNI { get; protected set; } = string.Empty;
        public string NUSS { get; protected set; } = string.Empty;

        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; protected set; }

        protected Empleado() { }
    }
}
