using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class SaleService : ISaleService
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IMapper _mapper;

    public SaleService(IUnitOfWorkFactory uowFactory, IMapper mapper)
    {
        _uowFactory = uowFactory;
        _mapper = mapper;
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, int userId)
    {
        using var uow = _uowFactory.Create();

        var saleNumber = await uow.Sales.GenerateSaleNumberAsync();

        var sale = new Sale
        {
            SaleNumber = saleNumber,
            CustomerId = dto.CustomerId,
            UserId = userId,
            Notes = dto.Notes,
            DiscountAmount = dto.DiscountAmount,
            Status = SaleStatus.Finalizada,
            SaleDate = DateTime.Now
        };

        // Autoridade de preço/desconto: operador usa o preço do cadastro; só
        // Gerente+ pode sobrescrever preço unitário ou aplicar descontos. Regra
        // verificada no servidor (não confia apenas na UI).
        var user = await uow.Users.GetByIdAsync(userId);
        bool canOverridePricing = user != null && user.Role <= UserRole.Gerente;

        if (dto.DiscountAmount < 0)
            throw new InvalidOperationException("Desconto não pode ser negativo.");
        if (dto.DiscountAmount > 0 && !canOverridePricing)
            throw new InvalidOperationException("Aplicar desconto na venda exige permissão de gerente.");

        // Carrega todos os produtos de uma vez — evita N+1 queries
        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var productMap = (await uow.Products.FindAsync(p => productIds.Contains(p.Id)))
            .ToDictionary(p => p.Id);

        decimal subtotal = 0;
        foreach (var itemDto in dto.Items)
        {
            if (!productMap.TryGetValue(itemDto.ProductId, out var product))
                throw new InvalidOperationException($"Produto {itemDto.ProductId} não encontrado.");

            if (itemDto.Quantity <= 0)
                throw new InvalidOperationException($"Quantidade inválida para o produto {product.Name}.");
            if (itemDto.UnitPrice < 0 || itemDto.DiscountAmount < 0)
                throw new InvalidOperationException($"Valores negativos não são permitidos ({product.Name}).");

            // Sobrescrita = preço diferente do cadastro OU desconto no item.
            bool overridesPricing = itemDto.UnitPrice != product.SalePrice || itemDto.DiscountAmount > 0;
            if (overridesPricing && !canOverridePricing)
                throw new InvalidOperationException(
                    $"Alterar preço/desconto do produto {product.Name} exige permissão de gerente.");

            var itemTotal = (itemDto.UnitPrice * itemDto.Quantity) - itemDto.DiscountAmount;
            subtotal += itemTotal;

            sale.Items.Add(new SaleItem
            {
                ProductId      = itemDto.ProductId,
                Quantity       = itemDto.Quantity,
                UnitPrice      = itemDto.UnitPrice,
                DiscountAmount = itemDto.DiscountAmount,
                TotalPrice     = itemTotal
            });

            var prevStock = product.StockQuantity;
            product.StockQuantity -= itemDto.Quantity;
            await uow.Products.UpdateAsync(product);

            await uow.StockMovements.AddAsync(new StockMovement
            {
                ProductId        = product.Id,
                Type             = StockMovementType.Saida,
                Quantity         = itemDto.Quantity,
                PreviousQuantity = prevStock,
                NewQuantity      = product.StockQuantity,
                Reason           = $"Venda {saleNumber}",
                UserId           = userId
            });
        }

        // TotalAmount = subtotal após descontos por item
        // FinalAmount = TotalAmount menos o desconto global da venda
        sale.TotalAmount = subtotal;
        sale.FinalAmount = subtotal - dto.DiscountAmount;

        foreach (var p in dto.Payments)
            sale.Payments.Add(new Payment { Method = p.Method, Amount = p.Amount, Reference = p.Reference });

        await uow.Sales.AddAsync(sale);
        await uow.SaveChangesAsync();

        var created = await uow.Sales.GetWithItemsAsync(sale.Id);
        return _mapper.Map<SaleDto>(created!);
    }

    public async Task<SaleDto?> GetByIdAsync(int id)
    {
        using var uow = _uowFactory.Create();
        var sale = await uow.Sales.GetWithItemsAsync(id);
        return sale == null ? null : _mapper.Map<SaleDto>(sale);
    }

    public async Task<IEnumerable<SaleDto>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        using var uow = _uowFactory.Create();
        var sales = await uow.Sales.GetByDateRangeAsync(start, end);
        return _mapper.Map<IEnumerable<SaleDto>>(sales);
    }

    public async Task<bool> CancelSaleAsync(int id, int userId)
    {
        using var uow = _uowFactory.Create();

        var sale = await uow.Sales.GetWithItemsAsync(id);
        if (sale == null || sale.Status == SaleStatus.Cancelada) return false;

        sale.Status = SaleStatus.Cancelada;

        foreach (var item in sale.Items)
        {
            var product = await uow.Products.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                var prev = product.StockQuantity;
                product.StockQuantity += item.Quantity;
                await uow.Products.UpdateAsync(product);
                await uow.StockMovements.AddAsync(new StockMovement
                {
                    ProductId = product.Id,
                    Type = StockMovementType.Devolucao,
                    Quantity = item.Quantity,
                    PreviousQuantity = prev,
                    NewQuantity = product.StockQuantity,
                    Reason = $"Cancelamento venda {sale.SaleNumber}",
                    UserId = userId
                });
            }
        }

        await uow.Sales.UpdateAsync(sale);
        await uow.SaveChangesAsync();
        return true;
    }
}
