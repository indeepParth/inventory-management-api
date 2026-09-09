namespace InventoryManagement.Domain.Entities
{
    public class CompanyInvitation
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string InvitedByUserId { get; set; } = string.Empty;
        public string? AcceptedByUserId { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? AcceptedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }

        public Company Company { get; set; } = null!;
    }
}
