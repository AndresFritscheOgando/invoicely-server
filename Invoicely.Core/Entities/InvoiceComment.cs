namespace Invoicely.Core.Entities;

public class InvoiceComment
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public User User { get; set; } = null!;
}
