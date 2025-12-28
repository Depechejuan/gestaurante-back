using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestaurante.Models.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Auto-increment
        public int Id { get; }

        [Required]
        [EmailAddress]  // Validación de email
        [MaxLength(100)]  // VARCHAR(100)
        [Column(TypeName = "nvarchar(100)")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(200)]
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
        [MaxLength(9)]
        public string DNI { get; set; } = string.Empty;




        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Constructor opcional
        public User()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
}
