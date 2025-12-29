using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class RegistroDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; private set; } = string.Empty;
        [Required]
        [MinLength(8)]
        public string Password { get; private set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string FirstName { get; private set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string FirstLastName { get; private set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string SecondLastName { get; private set; } = string.Empty;

        [Required]
        public string DNI { get; private set; } = string.Empty;
        [Required]
        [MaxLength(13)]
        [MinLength(13)]
        public string NUSS { get; private set; } = string.Empty;


        public RegistroDTO(string email, string password, string firstname, string firstlastname, string secondlastname, string dni, string nuss)
        {
            this.Email = email;
            this.Password = password;
            this.FirstName = firstname;
            this.FirstLastName = firstlastname;
            this.SecondLastName = secondlastname;
            this.DNI = dni;
            this.NUSS = nuss;
        }
    }
}
