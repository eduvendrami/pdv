using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using Xunit;

namespace PDV.Tests;

public class ConcurrencyTests : IDisposable
{
    private readonly TestDatabase _db = new();

    [Fact]
    public async Task Atualizacao_concorrente_de_estoque_lanca_DbUpdateConcurrencyException()
    {
        int productId;
        await using (var seed = _db.NewContext())
        {
            var p = new Product { Name = "Areia", StockQuantity = 10, UnitOfMeasure = UnitOfMeasure.MetroCubico };
            seed.Products.Add(p);
            await seed.SaveChangesAsync();
            productId = p.Id;
        }

        // Dois contextos carregam a mesma linha (mesmo token original)
        await using var ctxA = _db.NewContext();
        await using var ctxB = _db.NewContext();
        var a = await ctxA.Products.FindAsync(productId);
        var b = await ctxB.Products.FindAsync(productId);

        // A grava primeiro: regenera o token
        a!.StockQuantity -= 1;
        await ctxA.SaveChangesAsync();

        // B grava com token desatualizado → conflito detectado
        b!.StockQuantity -= 2;
        await ctxB.Invoking(c => c.SaveChangesAsync())
            .Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    public void Dispose() => _db.Dispose();
}
