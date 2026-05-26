# PDV - Sistema de Ponto de Venda (Material de Construção)

Aplicação desktop **WPF / .NET 8** com Clean Architecture, MVVM e EF Core (SQLite).

## Stack
- .NET 8 (`net8.0-windows`), WPF, `win-x86`, publicação single-file self-contained
- MVVM com **CommunityToolkit.Mvvm**
- DI/Host: `Microsoft.Extensions.DependencyInjection` + `Microsoft.Extensions.Hosting`
- UI: **MaterialDesignThemes / MaterialDesignColors**
- Dados: **EF Core 8 + SQLite** (migrations em `PDV.Infrastructure/Migrations`)
- PDF: **QuestPDF**

## Arquitetura (camadas em `src/`)
- **PDV.Domain** — entidades, enums e interfaces. Sem dependências externas.
- **PDV.Application** — DTOs, Mappings e Services (regras de aplicação).
- **PDV.Infrastructure** — `AppDbContext`, Repositories, Migrations, Services de infra.
- **PDV.WPF** — Views, ViewModels, Converters, Helpers, Services de UI (camada de apresentação / entry point).

Regra de dependência: `WPF → Infrastructure → Application → Domain`. Domain não referencia ninguém.

## Comandos
```powershell
# Restaurar e compilar a solução inteira
dotnet build PDVSystem.sln

# Rodar o app
dotnet run --project src/PDV.WPF/PDV.WPF.csproj

# Migrations EF Core (DbContext está em PDV.Infrastructure)
dotnet ef migrations add <Nome> --project src/PDV.Infrastructure --startup-project src/PDV.WPF
dotnet ef database update --project src/PDV.Infrastructure --startup-project src/PDV.WPF

# Publicar release (single-file)
./scripts/build-release.ps1
```

## Convenções
- ViewModels herdam de `ObservableObject` e usam `[ObservableProperty]` / `[RelayCommand]` (CommunityToolkit), não `INotifyPropertyChanged` manual.
- Acesso a dados sempre via Repositories/Services — Views e ViewModels não tocam `AppDbContext` diretamente.
- Novas entidades exigem migration; nunca editar migrations já aplicadas.
- Nullable e ImplicitUsings habilitados em todos os projetos.
