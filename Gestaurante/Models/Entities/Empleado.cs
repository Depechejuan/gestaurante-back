using Gestaurante.Models.DTO;
using Gestaurante.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestaurante.Models.Entities
{
    public abstract class Empleado
    {
        [Required]
        [Key]
        public Guid Id { get; protected set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; protected set; } = string.Empty;
        [Required]
        [MinLength(8)]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstLastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]    
        public string SecondLastName { get; set; } = string.Empty;

        [Required]
        [MinLength(9)]
        [Dni]
        public string DNI { get; protected set; } = string.Empty;
        [MinLength(13)]
        [Nuss]
        public string NUSS { get; protected set; } = string.Empty;
        public bool Activo { get; set; } = true;
        [MaxLength(500)]
        public string ImageURL { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Empleado() { }

        protected Empleado(Guid id, string password, string firstName, string firstLastName, string secondLastName, string dni, string nuss)
        {
            Id = id;
            Password = password;
            FirstName = firstName;
            FirstLastName = firstLastName;
            SecondLastName = secondLastName;
            DNI = dni;
            NUSS = nuss;
        }

        public void UpdateIdentity(string email, string dni, string nuss)
        {
            Email = email;
            DNI = dni;
            NUSS = nuss;
        }

        public void UpdateNames(string firstName, string firstLastName, string secondLastName)
        {
            FirstName = firstName;
            FirstLastName = firstLastName;
            SecondLastName = secondLastName;
        }
    }
}
