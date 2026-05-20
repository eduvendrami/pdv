using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class SaleService : ISaleService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public SaleService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, int userId)
    {
        var saleNumber = await _uow.Sales.GenerateSaleNumberAsync();

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

        decimal total = 0;
        foreach (var itemDto in dto.Items)
        {
            var product = await _uow.Products.GetByIdAsync(itemDto.ProductId)
                ?? throw new InvalidOperationException($"Produto {itemDto.ProductId} não encontrado.");

            var itemTotal = (itemDto.UnitPrice * itemDto.Quantity) - itemDto.DiscountAmount;
            total += itemTotal;

            sale.Items.Add(new SaleItem
            {
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                DiscountAmount = itemDto.DiscountAmount,
                TotalPrice = itemTotal
            });

            var prevStock = product.StockQuantity;
            product.StockQuantity -= itemDto.Quantity;
            await _uow.Products.UpdateAsync(product);

            await _uow.StockMovements.AddAsync(new StockMovement
            {
                ProductId = product.Id,
                Type = StockMovementType.Saida,
                Quantity = itemDto.Quantity,
                PreviousQuantity = prevStock,
                NewQuantity = product.StockQuantity,
                Reason = $"Venda {saleNumber}",
                UserId = userId
            });
        }

        sale.TotalAmount = total + dto.DiscountAmount;
        sale.FinalAmount = total - dto.DiscountAmount;

        foreach (var p in dto.Payments)
            sale.Payments.Add(new Payment { Method = p.Method, Amount = p.Amount, Reference = p.Reference });

        await _uow.Sales.AddAsync(sale);
        await _uow.SaveChangesAsync();

        var created = await _uow.Sales.GetWithItemsAsync(sale.Id);
        return _mapper.Map<SaleDto>(created!);
    }

    public async Task<SaleDto?> GetByIdAsync(int id)
    {
        var sale = await _uow.Sales.GetWithItemsAsync(id);
        return sale == null ? null : _mapper.Map<SaleDto>(sale);
    }

    public async Task<IEnumerable<SaleDto>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        var sales = await _uow.Sales.GetByDateRangeAsync(start, end);
        return _mapper.Map<IEnumerable<SaleDto>>(sales);
    }

    public async Task<bool> CancelSaleAsync(int id, int userId)
    {
        var sale = await _uow.Sales.GetWithItemsAsync(id);
        if (sale == null || sale.Status == SaleStatus.Cancelada) return false;

        sale.Status = SaleStatus.Cancelada;

        foreach (var item in sale.Items)
        {
            var product = await _uow.Products.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                var prev = product.StockQuantity;
                product.StockQuantity += item.Quantity;
                await _uow.Products.UpdateAsync(product);
                await _uow.StockMovements.AddAsync(new StockMovement
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

        await _uow.Sales.UpdateAsync(sale);
        await _uow.SaveChangesAsync();
        return true;
    }
}
