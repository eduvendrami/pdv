using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Enums;
using PDV.WPF.Services;

namespace PDV.WPF.ViewModels;

// ── Linha da grade de produtos ────────────────────────────────────────────────
public partial class NFeItemVm : ObservableObject
{
    [ObservableProperty] private bool    _import    = true;
    [ObservableProperty] private string  _name         = string.Empty;
    [ObservableProperty] private string  _internalCode = string.Empty;
    [ObservableProperty] private string? _barcode;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _costPrice;
    [ObservableProperty] private decimal _salePrice;

    public string        Status       { get; init; } = "Novo";
    public bool          IsExisting   { get; init; }
    public int?          ExistingId   { get; init; }
    public string        UnitLabel    { get; init; } = string.Empty;
    public UnitOfMeasure Unit         { get; init; }
}

// ── ViewModel principal ───────────────────────────────────────────────────────
public partial class NFeImportViewModel : BaseViewModel
{
    private readonly IProductService  _productService;
    private readonly ISupplierService _supplierService;

    // ── Arquivo ───────────────────────────────────────────────────────────────
    [ObservableProperty] private string?    _filePath;
    [ObservableProperty] private bool       _fileLoaded;
    [ObservableProperty] private NFeInfoDto? _nfeInfo;

    // ── Fornecedor ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _supplierStatus  = string.Empty;
    [ObservableProperty] private bool   _supplierIsNew   = true;
    [ObservableProperty] private int?   _existingSupplierId;

    // ── Opções ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool    _addToStock    = true;

    // ── Itens / resultado ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<NFeItemVm> _items = new();
    [ObservableProperty] private string _resultMessage = string.Empty;
    [ObservableProperty] private string _errorMessage  = string.Empty;
    [ObservableProperty] private bool   _importDone;

    public int TotalNew      => Items.Count(i => !i.IsExisting);
    public int TotalExisting => Items.Count(i =>  i.IsExisting);
    public int TotalSelected => Items.Count(i =>  i.Import);

    /// <summary>Disparado quando a lista de itens termina de carregar (para a View focar o 1º item).</summary>
    public event Action? ItemsLoaded;

    public NFeImportViewModel(IProductService productService, ISupplierService supplierService)
    {
        _productService  = productService;
        _supplierService = supplierService;
    }

    // ── Selecionar arquivo ────────────────────────────────────────────────────
    [RelayCommand]
    public async Task SelectFileAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Selecionar NF-e",
            Filter = "XML NF-e (*.xml)|*.xml|Todos os arquivos (*.*)|*.*",
        };
        if (dlg.ShowDialog() != true) return;

        FilePath   = dlg.FileName;
        FileLoaded = false;
        ErrorMessage  = string.Empty;
        ResultMessage = string.Empty;
        ImportDone    = false;

        await LoadFileAsync(dlg.FileName);
    }

    private async Task LoadFileAsync(string path)
    {
        IsBusy = true;
        try
        {
            // Parse XML
            var nfe = NFeParseService.Parse(path);
            NfeInfo = nfe;

            // Verificar fornecedor
            var suppliers = (await _supplierService.GetAllAsync()).ToList();
            var cnpjClean = new string(nfe.Supplier.Cnpj.Where(char.IsDigit).ToArray());
            var existingSupplier = suppliers.FirstOrDefault(s =>
                !string.IsNullOrWhiteSpace(s.Cnpj) &&
                new string(s.Cnpj.Where(char.IsDigit).ToArray()) == cnpjClean);

            if (existingSupplier != null)
            {
                SupplierIsNew      = false;
                ExistingSupplierId = existingSupplier.Id;
                SupplierStatus     = $"Já cadastrado (será atualizado)";
            }
            else
            {
                SupplierIsNew      = true;
                ExistingSupplierId = null;
                SupplierStatus     = "Novo — será criado";
            }

            // Carregar produtos existentes para comparação
            var allProducts = (await _productService.GetAllIncludingInactiveAsync()).ToList();

            var itemVms = nfe.Items.Select(item =>
            {
                // Busca por EAN ou código interno
                var existing = allProducts.FirstOrDefault(p =>
                    (!string.IsNullOrWhiteSpace(item.Barcode)      && p.Barcode      == item.Barcode) ||
                    (!string.IsNullOrWhiteSpace(item.InternalCode) && p.InternalCode == item.InternalCode));

                // Produto já cadastrado mantém seu preço atual; novos começam zerados
                // para que o operador informe o preço de venda antes de importar.
                var salePrice = existing?.SalePrice > 0 ? existing.SalePrice : 0m;

                return new NFeItemVm
                {
                    IsExisting   = existing != null,
                    ExistingId   = existing?.Id,
                    Status       = existing != null ? "Existente" : "Novo",
                    InternalCode = item.InternalCode,
                    Barcode      = item.Barcode,
                    Name         = item.Name,
                    UnitLabel    = item.UnitLabel,
                    Unit         = NFeParseService.MapUnit(item.UnitLabel),
                    Quantity     = item.Quantity,
                    CostPrice    = item.UnitCostPrice,
                    SalePrice    = salePrice,
                };
            }).ToList();

            Items = new ObservableCollection<NFeItemVm>(itemVms);
            OnPropertyChanged(nameof(TotalNew));
            OnPropertyChanged(nameof(TotalExisting));
            OnPropertyChanged(nameof(TotalSelected));
            FileLoaded = true;
            ItemsLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao ler o arquivo: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ── Importar ──────────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task ImportAsync()
    {
        if (NfeInfo == null) return;

        ErrorMessage  = string.Empty;
        ResultMessage = string.Empty;

        // Não permite importar enquanto houver item selecionado sem preço de venda preenchido.
        var semPreco = Items.Where(i => i.Import && i.SalePrice <= 0).Select(i => i.Name).ToList();
        if (semPreco.Count > 0)
        {
            ErrorMessage = "Preencha o preço de venda antes de importar: " + string.Join(", ", semPreco) + ".";
            return;
        }

        IsBusy        = true;

        int createdProducts  = 0;
        int updatedProducts  = 0;

        try
        {
            // 1. Fornecedor
            int supplierId;
            var supplierDto = new CreateSupplierDto
            {
                Name      = NfeInfo.Supplier.Name,
                TradeName = NfeInfo.Supplier.TradeName,
                Cnpj      = NfeInfo.Supplier.Cnpj,
                Phone     = NfeInfo.Supplier.Phone,
                Address   = NfeInfo.Supplier.Address,
                City      = NfeInfo.Supplier.City,
                State     = NfeInfo.Supplier.State,
            };

            if (SupplierIsNew)
            {
                var created = await _supplierService.CreateAsync(supplierDto);
                supplierId = created.Id;
            }
            else
            {
                await _supplierService.UpdateAsync(ExistingSupplierId!.Value, supplierDto);
                supplierId = ExistingSupplierId.Value;
            }

            // 2. Produtos selecionados
            foreach (var item in Items.Where(i => i.Import))
            {
                var dto = new CreateProductDto
                {
                    Name             = item.Name,
                    InternalCode     = item.InternalCode,
                    Barcode          = item.Barcode,
                    UnitOfMeasure    = item.Unit,
                    CostPrice        = item.CostPrice,
                    SalePrice        = item.SalePrice,
                    SupplierId       = supplierId,
                    StockQuantity    = AddToStock ? item.Quantity : 0,
                    MinStockQuantity = 0,
                };

                if (item.IsExisting && item.ExistingId.HasValue)
                {
                    // Atualiza — mantém estoque atual + adiciona se marcado
                    var existing = await _productService.GetByIdAsync(item.ExistingId.Value);
                    if (existing != null)
                    {
                        dto.StockQuantity    = AddToStock
                            ? existing.StockQuantity + item.Quantity
                            : existing.StockQuantity;
                        dto.MinStockQuantity = existing.MinStockQuantity;
                    }
                    await _productService.UpdateAsync(item.ExistingId.Value, dto);
                    updatedProducts++;
                }
                else
                {
                    await _productService.CreateAsync(dto);
                    createdProducts++;
                }
            }

            // 3. Resultado
            var parts = new List<string>();
            if (createdProducts > 0)  parts.Add($"{createdProducts} produto(s) criado(s)");
            if (updatedProducts > 0)  parts.Add($"{updatedProducts} produto(s) atualizado(s)");
            parts.Add(SupplierIsNew ? "fornecedor criado" : "fornecedor atualizado");
            if (AddToStock) parts.Add("estoque atualizado");

            ResultMessage = "✅ Importação concluída: " + string.Join(", ", parts) + ".";
            ImportDone    = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro durante a importação: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ── Novo arquivo (reset) ──────────────────────────────────────────────────
    [RelayCommand]
    public void Reset()
    {
        FilePath      = null;
        FileLoaded    = false;
        NfeInfo       = null;
        Items         = new();
        ResultMessage = string.Empty;
        ErrorMessage  = string.Empty;
        ImportDone    = false;
    }
}
