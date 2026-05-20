using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class CashService : ICashService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CashService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<CashSessionDto?> GetOpenSessionAsync()
    {
        var session = await _uow.CashSessions.GetOpenSessionAsync();
        return session == null ? null : _mapper.Map<CashSessionDto>(session);
    }

    public async Task<CashSessionDto> OpenSessionAsync(OpenCashSessionDto dto, int userId)
    {
        var existing = await _uow.CashSessions.GetOpenSessionAsync();
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
        await _uow.CashSessions.AddAsync(session);
        await _uow.SaveChangesAsync();
        return _mapper.Map<CashSessionDto>(session);
    }

    public async Task<CashSessionDto> CloseSessionAsync(CloseCashSessionDto dto, int userId)
    {
        var session = await _uow.CashSessions.GetOpenSessionAsync()
            ?? throw new InvalidOperationException("Nenhum caixa aberto.");

        var salesTotal = await _uow.Sales.GetTotalByDateAsync(session.OpenedAt.Date);
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

        await _uow.CashSessions.UpdateAsync(session);
        await _uow.SaveChangesAsync();
        return _mapper.Map<CashSessionDto>(session);
    }

    public async Task AddMovementAsync(CashSupplyDto dto, int sessionId)
    {
        var session = await _uow.CashSessions.GetWithMovementsAsync(sessionId)
            ?? throw new InvalidOperationException("Sessão não encontrada.");

        session.Movements.Add(new CashMovement
        {
            CashSessionId = sessionId,
            Type = dto.Type,
            Amount = dto.Amount,
            Description = dto.Description
        });
        await _uow.CashSessions.UpdateAsync(session);
        await _uow.SaveChangesAsync();
    }
}
