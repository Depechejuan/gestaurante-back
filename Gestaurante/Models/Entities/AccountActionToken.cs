using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class AccountActionToken
    {
        [Key]
        [Required]
        public Guid IdAccountActionToken { get; set; }

        [Required]
        public AccountActionTokenUserType UserType { get; set; }

        [Required]
        public AccountActionTokenPurpose Purpose { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? ConsumedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
