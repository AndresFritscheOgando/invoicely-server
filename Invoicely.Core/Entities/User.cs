using Invoicely.Core.Enums;

namespace Invoicely.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Invoice> CreatedInvoices { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<InvoiceComment> Comments { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
