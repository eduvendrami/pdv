using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class StockService : IStockService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public StockService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsAsync(int? productId = null)
    {
        var movements = productId.HasValue
            ? await _uow.StockMovements.FindAsync(m => m.ProductId == productId.Value)
            : await _uow.StockMovements.GetAllAsync();
        return _mapper.Map<IEnumerable<StockMovementDto>>(movements);
    }

    public async Task AdjustStockAsync(AdjustStockDto dto, int userId)
    {
        var product = await _uow.Products.GetByIdAsync(dto.ProductId)
            ?? throw new InvalidOperationException("Produto não encontrado.");

        var previous = product.StockQuantity;
        if (dto.Type == StockMovementType.Entrada || dto.Type == StockMovementType.Devolucao)
            product.StockQuantity += dto.Quantity;
        else if (dto.Type == StockMovementType.Saida)
            product.StockQuantity -= dto.Quantity;
        else
            product.StockQuantity = dto.Quantity;

        await _uow.Products.UpdateAsync(product);

        var movement = new StockMovement
        {
            ProductId = dto.ProductId,
            Type = dto.Type,
            Quantity = dto.Quantity,
            PreviousQuantity = previous,
            NewQuantity = product.StockQuantity,
            Reason = dto.Reason,
            UserId = userId
        };
        await _uow.StockMovements.AddAsync(movement);
        await _uow.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
    {
        var products = await _uow.Products.GetLowStockAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}
