using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using Xunit;

namespace PDV.Tests;

public class SaleServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();

    private async Task<(int userId, int productId)> SeedUserAndProductAsync(
        decimal stock = 100, decimal salePrice = 10, UserRole role = UserRole.Operador)
    {
        await using var ctx = _db.NewContext();
        var user = new User { Username = "op", FullName = "Operador", PasswordHash = "x", Role = role };
        var product = new Product { Name = "Cimento CP-II", SalePrice = salePrice, StockQuantity = stock, UnitOfMeasure = UnitOfMeasure.Saco };
        ctx.Users.Add(user);
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        return (user.Id, product.Id);
    }

    [Fact]
    public async Task CreateSale_persiste_venda_e_baixa_estoque()
    {
        var (userId, productId) = await SeedUserAndProductAsync(stock: 100, salePrice: 10);
        var sut = new SaleService(_db.UowFactory, _db.Mapper);

        var dto = new CreateSaleDto
        {
            Items = { new CreateSaleItemDto { ProductId = productId, Quantity = 3, UnitPrice = 10 } },
            Payments = { new PaymentDto { Method = PaymentMethod.Dinheiro, Amount = 30 } }
        };

        var result = await sut.CreateSaleAsync(dto, userId);

        result.SaleNumber.Should().NotBeNullOrWhiteSpace();
        result.Status.Should().Be(SaleStatus.Finalizada);
        result.TotalAmount.Should().Be(30m);
        result.FinalAmount.Should().Be(30m);
        result.Items.Should().ContainSingle();

        await using var ctx = _db.NewContext();
        (await ctx.Products.FindAsync(productId))!.StockQuantity.Should().Be(97m);
        var movements = await ctx.StockMovements.Where(m => m.ProductId == productId).ToListAsync();
        movements.Should().ContainSingle()
            .Which.Type.Should().Be(StockMovementType.Saida);
    }

    [Fact]
    public async Task CreateSale_gerente_aplica_descontos_por_item_e_global()
    {
        var (userId, productId) = await SeedUserAndProductAsync(salePrice: 10, role: UserRole.Gerente);
        var sut = new SaleService(_db.UowFactory, _db.Mapper);

        var dto = new CreateSaleDto
        {
            DiscountAmount = 2, // desconto global
            Items = { new CreateSaleItemDto { ProductId = productId, Quantity = 3, UnitPrice = 10, DiscountAmount = 5 } },
            Payments = { new PaymentDto { Method = PaymentMethod.Pix, Amount = 23 } }
        };

        var result = await sut.CreateSaleAsync(dto, userId);

        result.TotalAmount.Should().Be(25m);  // 3*10 - 5
        result.FinalAmount.Should().Be(23m);  // 25 - 2
    }

    [Fact]
    public async Task CreateSale_operador_alterando_preco_lanca()
    {
        var (userId, productId) = await SeedUserAndProductAsync(salePrice: 10, role: UserRole.Operador);
        var sut = new SaleService(_db.UowFactory, _db.Mapper);

        var dto = new CreateSaleDto
        {
            // preço 8 != cadastro 10 → sobrescrita sem permissão
            Items = { new CreateSaleItemDto { ProductId = productId, Quantity = 1, UnitPrice = 8 } }
        };

        await sut.Invoking(s => s.CreateSaleAsync(dto, userId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateSale_gerente_pode_alterar_preco()
    {
        var (userId, productId) = await SeedUserAndProductAsync(salePrice: 10, role: UserRole.Gerente);
        var sut = new SaleService(_db.UowFactory, _db.Mapper);

        var dto = new CreateSaleDto
        {
            Items = { new CreateSaleItemDto { ProductId = productId, Quantity = 2, UnitPrice = 8 } },
            Payments = { new PaymentDto { Method = PaymentMethod.Dinheiro, Amount = 16 } }
        };

        var result = await sut.CreateSaleAsync(dto, userId);
        result.FinalAmount.Should().Be(16m);
    }

    [Fact]
    public async Task CreateSale_operador_com_desconto_global_lanca()
    {
        var (userId, productId) = await SeedUserAndProductAsync(salePrice: 10, role: UserRole.Operador);
        var sut = new SaleService(_db.UowFactory, _db.Mapper);

        var dto = new CreateSaleDto
        {
            DiscountAmount = 1,
            Items = { new CreateSaleItemDto { ProductId = productId, Quantity = 1, UnitPrice = 10 } }
        };

        await sut.Invoking(s => s.CreateSaleAsync(dto, userId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateSale_produto_inexistente_lanca()
    {
        var (userId, _) = await SeedUserAndProductAsync();
        var sut = new SaleService(_db.UowFactory, _db.Mapper);

        var dto = new CreateSaleDto
        {
            Items = { new CreateSaleItemDto { ProductId = 9999, Quantity = 1, UnitPrice = 5 } }
        };

        await sut.Invoking(s => s.CreateSaleAsync(dto, userId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CancelSale_estorna_estoque_e_marca_cancelada()
    {
        var (userId, productId) = await SeedUserAndProductAsync(stock: 100, salePrice: 10);
        var sut = new SaleService(_db.UowFactory, _db.Mapper);

        var sale = await sut.CreateSaleAsync(new CreateSaleDto
        {
            Items = { new CreateSaleItemDto { ProductId = productId, Quantity = 4, UnitPrice = 10 } }
        }, userId);

        var ok = await sut.CancelSaleAsync(sale.Id, userId);

        ok.Should().BeTrue();
        await using var ctx = _db.NewContext();
        (await ctx.Products.FindAsync(productId))!.StockQuantity.Should().Be(100m); // 100 -4 +4
        (await ctx.Sales.FindAsync(sale.Id))!.Status.Should().Be(SaleStatus.Cancelada);
        (await ctx.StockMovements.CountAsync(m => m.Type == StockMovementType.Devolucao)).Should().Be(1);
    }

    [Fact]
    public async Task CancelSale_ja_cancelada_retorna_false()
    {
        var (userId, productId) = await SeedUserAndProductAsync();
        var sut = new SaleService(_db.UowFactory, _db.Mapper);

        var sale = await sut.CreateSaleAsync(new CreateSaleDto
        {
            Items = { new CreateSaleItemDto { ProductId = productId, Quantity = 1, UnitPrice = 10 } }
        }, userId);

        (await sut.CancelSaleAsync(sale.Id, userId)).Should().BeTrue();
        (await sut.CancelSaleAsync(sale.Id, userId)).Should().BeFalse();
    }

    public void Dispose() => _db.Dispose();
}
