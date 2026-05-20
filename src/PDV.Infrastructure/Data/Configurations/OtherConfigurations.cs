using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Domain.Entities;

namespace PDV.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Cnpj).HasMaxLength(18);
        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.Email).HasMaxLength(100);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.CpfCnpj).HasMaxLength(18);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.CreditLimit).HasColumnType("decimal(18,2)");
        builder.Property(c => c.CurrentDebt).HasColumnType("decimal(18,2)");
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(100);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Role).HasConversion<string>();
        builder.HasIndex(u => u.Username).IsUnique();
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Quantity).HasColumnType("decimal(18,3)");
        builder.Property(m => m.PreviousQuantity).HasColumnType("decimal(18,3)");
        builder.Property(m => m.NewQuantity).HasColumnType("decimal(18,3)");
        builder.Property(m => m.Type).HasConversion<string>();
        builder.HasOne(m => m.Product).WithMany(p => p.StockMovements).HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ClosingBalance).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ExpectedBalance).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Difference).HasColumnType("decimal(18,2)");
        builder.HasMany(s => s.Movements).WithOne(m => m.CashSession).HasForeignKey(m => m.CashSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Amount).HasColumnType("decimal(18,2)");
        builder.Property(m => m.Type).HasConversion<string>();
    }
}
