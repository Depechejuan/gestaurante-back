using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Gestaurante.Validation;

namespace Gestaurante.Models.DTO
{
    public enum TipoEmpleado
    {
        Administrador,
        Camarero,
        Cocinero
    }

    public class RegistroDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get;  set; } = string.Empty;
        [Required]
        [MinLength(8)]
        public string Password { get;  set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string FirstName { get;  set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string FirstLastName { get;  set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string SecondLastName { get;  set; } = string.Empty;

        [Required]
        [Dni]
        public string DNI { get;  set; } = string.Empty;
        [Required]
        [Nuss]
        public string NUSS { get;  set; } = string.Empty;
        [Required]
        public TipoEmpleado Tipo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public Guid Id { get; set; } = Guid.NewGuid();

        public RegistroDTO(string email, string password, string firstname, string firstlastname, string secondlastname, string dni, string nuss, TipoEmpleado tipo)
        {
            this.Email = email;
            this.Password = password;
            this.FirstName = firstname;
            this.FirstLastName = firstlastname;
            this.SecondLastName = secondlastname;
            this.DNI = dni;
            this.NUSS = nuss;
            this.Tipo = tipo;
        }
    }
}
