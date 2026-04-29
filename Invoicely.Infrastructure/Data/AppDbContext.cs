using Invoicely.Core.Entities;
using Invoicely.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Invoicely.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InvoiceComment> InvoiceComments => Set<InvoiceComment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Vendor>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.Property(i => i.Amount).HasPrecision(18, 2);
            e.Property(i => i.Status).HasConversion<string>();
            e.Property(i => i.PaymentStatus).HasConversion<string>();
            e.HasOne(i => i.Vendor).WithMany(v => v.Invoices).HasForeignKey(i => i.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.CreatedBy).WithMany(u => u.CreatedInvoices).HasForeignKey(i => i.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.ReviewedBy).WithMany(u => u.ReviewedInvoices).HasForeignKey(i => i.ReviewedByUserId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Quantity).HasPrecision(18, 4);
            e.Property(i => i.UnitPrice).HasPrecision(18, 2);
            e.Property(i => i.Total).HasPrecision(18, 2);
            e.HasOne(i => i.Invoice).WithMany(inv => inv.Items).HasForeignKey(i => i.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.PaymentMethod).HasConversion<string>();
            e.HasOne(p => p.Invoice).WithMany(i => i.Payments).HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.CreatedBy).WithMany(u => u.Payments).HasForeignKey(p => p.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceComment>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.Invoice).WithMany(i => i.Comments).HasForeignKey(c => c.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.User).WithMany(u => u.Comments).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).HasConversion<string>();
            e.HasOne(a => a.User).WithMany(u => u.AuditLogs).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Invoice).WithMany(i => i.AuditLogs).HasForeignKey(a => a.EntityId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is User u) { u.CreatedAt = now; u.UpdatedAt = now; u.Id = u.Id == Guid.Empty ? Guid.NewGuid() : u.Id; }
                else if (entry.Entity is Vendor v) { v.CreatedAt = now; v.UpdatedAt = now; v.Id = v.Id == Guid.Empty ? Guid.NewGuid() : v.Id; }
                else if (entry.Entity is Invoice i) { i.CreatedAt = now; i.UpdatedAt = now; i.Id = i.Id == Guid.Empty ? Guid.NewGuid() : i.Id; }
                else if (entry.Entity is InvoiceItem ii) { ii.CreatedAt = now; ii.Id = ii.Id == Guid.Empty ? Guid.NewGuid() : ii.Id; }
                else if (entry.Entity is Payment p) { p.CreatedAt = now; p.Id = p.Id == Guid.Empty ? Guid.NewGuid() : p.Id; }
                else if (entry.Entity is InvoiceComment c) { c.CreatedAt = now; c.Id = c.Id == Guid.Empty ? Guid.NewGuid() : c.Id; }
                else if (entry.Entity is AuditLog al) { al.CreatedAt = now; al.Id = al.Id == Guid.Empty ? Guid.NewGuid() : al.Id; }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is User u) u.UpdatedAt = now;
                else if (entry.Entity is Vendor v) v.UpdatedAt = now;
                else if (entry.Entity is Invoice i) i.UpdatedAt = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
