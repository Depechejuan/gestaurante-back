using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class MesaPublicSession
    {
        [Key]
        [Required]
        public Guid IdMesaPublicSession { get; private set; }

        [Required]
        public Guid IdMesa { get; set; }

        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(4);

        public DateTime? LastSeenAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public MesaPublicSession() { }

        public MesaPublicSession(Guid idMesaPublicSession, Guid idMesa, string tokenHash, DateTime expiresAt)
        {
            IdMesaPublicSession = idMesaPublicSession;
            IdMesa = idMesa;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
        }
    }
}
