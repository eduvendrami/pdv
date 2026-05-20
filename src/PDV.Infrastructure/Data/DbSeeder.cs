using Microsoft.EntityFrameworkCore;
using PDV.Domain.Entities;

namespace PDV.Infrastructure.Data;

public static class DbSeeder
{
    /// <summary>
    /// Aplica as migrations pendentes e popula dados de referência (categorias padrão).
    /// Não cria usuários — o setup de primeira instalação cuida disso.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        // Categorias padrão para material de construção (só na primeira vez)
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "Cimento e Argamassa" },
                new Category { Name = "Tintas e Vernizes" },
                new Category { Name = "Ferramentas" },
                new Category { Name = "Elétrico" },
                new Category { Name = "Hidráulico" },
                new Category { Name = "Madeiras" },
                new Category { Name = "Pisos e Revestimentos" },
                new Category { Name = "Telhas e Coberturas" },
                new Category { Name = "Perfis e Chapas" },
                new Category { Name = "Impermeabilizantes" }
            );
            await context.SaveChangesAsync();
        }
    }
}
