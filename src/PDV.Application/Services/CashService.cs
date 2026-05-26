using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class CashService : ICashService
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IMapper _mapper;

    public CashService(IUnitOfWorkFactory uowFactory, IMapper mapper)
    {
        _uowFactory = uowFactory;
        _mapper = mapper;
    }

    public async Task<CashSessionDto?> GetOpenSessionAsync()
    {
        using var uow = _uowFactory.Create();
        var session = await uow.CashSessions.GetOpenSessionAsync();
        return session == null ? null : _mapper.Map<CashSessionDto>(session);
    }

    public async Task<CashSessionDto> OpenSessionAsync(OpenCashSessionDto dto, int userId)
    {
        using var uow = _uowFactory.Create();

        var existing = await uow.CashSessions.GetOpenSessionAsync();
        if (existing != null) throw new InvalidOperationException("Já existe um caixa aberto.");

        var session = new CashSession
        {
            OpeningBalance = dto.OpeningBalance,
            UserId = userId
        };
        session.Movements.Add(new CashMovement
        {
            Type = CashMovementType.Abertura,
            Amount = dto.OpeningBalance,
            Description = "Abertura de caixa"
        });
        await uow.CashSessions.AddAsync(session);
        await uow.SaveChangesAsync();
        return _mapper.Map<CashSessionDto>(session);
    }

    public async Task<CashSessionDto> CloseSessionAsync(CloseCashSessionDto dto, int userId)
    {
        using var uow = _uowFactory.Create();

        var session = await uow.CashSessions.GetOpenSessionAsync()
            ?? throw new InvalidOperationException("Nenhum caixa aberto.");

        var salesTotal = await uow.Sales.GetTotalByDateAsync(session.OpenedAt.Date);
        session.ExpectedBalance = session.OpeningBalance + salesTotal;
        session.ClosingBalance = dto.ClosingBalance;
        session.Difference = dto.ClosingBalance - session.ExpectedBalance;
        session.ClosedAt = DateTime.Now;
        session.Notes = dto.Notes;

        session.Movements.Add(new CashMovement
        {
            Type = CashMovementType.Fechamento,
            Amount = dto.ClosingBalance,
            Description = "Fechamento de caixa"
        });

        await uow.CashSessions.UpdateAsync(session);
        await uow.SaveChangesAsync();
        return _mapper.Map<CashSessionDto>(session);
    }

    public async Task AddMovementAsync(CashSupplyDto dto, int sessionId)
    {
        using var uow = _uowFactory.Create();

        var session = await uow.CashSessions.GetWithMovementsAsync(sessionId)
            ?? throw new InvalidOperationException("Sessão não encontrada.");

        session.Movements.Add(new CashMovement
        {
            CashSessionId = sessionId,
            Type = dto.Type,
            Amount = dto.Amount,
            Description = dto.Description
        });
        await uow.CashSessions.UpdateAsync(session);
        await uow.SaveChangesAsync();
    }
}
