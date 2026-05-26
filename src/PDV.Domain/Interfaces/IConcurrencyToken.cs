namespace PDV.Domain.Interfaces;

/// <summary>
/// Marca entidades com controle de concorrência otimista. O token é regenerado
/// a cada gravação; se outro processo alterou a linha no intervalo, o EF Core
/// lança <c>DbUpdateConcurrencyException</c> em vez de sobrescrever silenciosamente
/// (proteção contra perda de atualização de estoque em cenário multi-terminal).
/// </summary>
public interface IConcurrencyToken
{
    byte[] RowVersion { get; set; }
}
