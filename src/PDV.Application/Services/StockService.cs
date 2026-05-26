using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class StockService : IStockService
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IMapper _mapper;

    public StockService(IUnitOfWorkFactory uowFactory, IMapper mapper)
    {
        _uowFactory = uowFactory;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsAsync(int? productId = null)
    {
        using var uow = _uowFactory.Create();
        var movements = productId.HasValue
            ? await uow.StockMovements.FindAsync(m => m.ProductId == productId.Value)
            : await uow.StockMovements.GetAllAsync();
        return _mapper.Map<IEnumerable<StockMovementDto>>(movements);
    }

    public async Task AdjustStockAsync(AdjustStockDto dto, int userId)
    {
        using var uow = _uowFactory.Create();

        var product = await uow.Products.GetByIdAsync(dto.ProductId)
            ?? throw new InvalidOperationException("Produto não encontrado.");

        var previous = product.StockQuantity;
        if (dto.Type == StockMovementType.Entrada || dto.Type == StockMovementType.Devolucao)
            product.StockQuantity += dto.Quantity;
        else if (dto.Type == StockMovementType.Saida)
            product.StockQuantity -= dto.Quantity;
        else
            product.StockQuantity = dto.Quantity;

        await uow.Products.UpdateAsync(product);

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
        await uow.StockMovements.AddAsync(movement);
        await uow.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
    {
        using var uow = _uowFactory.Create();
        var products = await uow.Products.GetLowStockAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}
