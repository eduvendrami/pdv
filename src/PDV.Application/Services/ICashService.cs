using PDV.Application.DTOs;

namespace PDV.Application.Services;

public interface ICashService
{
    Task<CashSessionDto?> GetOpenSessionAsync();
    Task<CashSessionDto> OpenSessionAsync(OpenCashSessionDto dto, int userId);
    Task<CashSessionDto> CloseSessionAsync(CloseCashSessionDto dto, int userId);
    Task AddMovementAsync(CashSupplyDto dto, int sessionId);
}
