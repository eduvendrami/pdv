using FluentAssertions;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using Xunit;

namespace PDV.Tests;

public class CashServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();

    private async Task<int> SeedUserAsync()
    {
        await using var ctx = _db.NewContext();
        var user = new User { Username = "ger", FullName = "Gerente", PasswordHash = "x", Role = UserRole.Gerente };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task OpenSession_cria_caixa_aberto()
    {
        var userId = await SeedUserAsync();
        var sut = new CashService(_db.UowFactory, _db.Mapper);

        await sut.OpenSessionAsync(new OpenCashSessionDto { OpeningBalance = 150 }, userId);

        var open = await sut.GetOpenSessionAsync();
        open.Should().NotBeNull();
        open!.OpeningBalance.Should().Be(150m);
        open.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task OpenSession_com_caixa_ja_aberto_lanca()
    {
        var userId = await SeedUserAsync();
        var sut = new CashService(_db.UowFactory, _db.Mapper);
        await sut.OpenSessionAsync(new OpenCashSessionDto { OpeningBalance = 100 }, userId);

        await sut.Invoking(s => s.OpenSessionAsync(new OpenCashSessionDto { OpeningBalance = 50 }, userId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CloseSession_calcula_diferenca_sem_vendas()
    {
        var userId = await SeedUserAsync();
        var sut = new CashService(_db.UowFactory, _db.Mapper);
        await sut.OpenSessionAsync(new OpenCashSessionDto { OpeningBalance = 100 }, userId);

        var closed = await sut.CloseSessionAsync(new CloseCashSessionDto { ClosingBalance = 90 }, userId);

        closed.ExpectedBalance.Should().Be(100m); // abertura + 0 vendas
        closed.Difference.Should().Be(-10m);       // 90 contado - 100 esperado
        closed.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CloseSession_sem_caixa_aberto_lanca()
    {
        var userId = await SeedUserAsync();
        var sut = new CashService(_db.UowFactory, _db.Mapper);

        await sut.Invoking(s => s.CloseSessionAsync(new CloseCashSessionDto { ClosingBalance = 0 }, userId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose() => _db.Dispose();
}
