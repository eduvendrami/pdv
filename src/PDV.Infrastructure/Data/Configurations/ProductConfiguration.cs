using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Domain.Entities;

namespace PDV.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Barcode).HasMaxLength(50);
        builder.Property(p => p.InternalCode).HasMaxLength(50);
        builder.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.SalePrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.StockQuantity).HasColumnType("decimal(18,3)");
        builder.Property(p => p.MinStockQuantity).HasColumnType("decimal(18,3)");
        builder.Property(p => p.UnitOfMeasure).HasConversion<string>();
        builder.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Supplier).WithMany(s => s.Products).HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(p => p.Barcode);

        // Concorrência otimista: o SQLite não tem rowversion nativo, então o token
        // é regenerado manualmente no AppDbContext.SaveChanges e comparado no UPDATE.
        builder.Property(p => p.RowVersion).IsConcurrencyToken();
    }
}
