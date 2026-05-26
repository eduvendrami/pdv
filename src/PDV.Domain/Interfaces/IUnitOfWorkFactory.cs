namespace PDV.Domain.Interfaces;

/// <summary>
/// Cria uma <see cref="IUnitOfWork"/> nova por operação.
/// <para>
/// Necessário porque o WPF resolve tudo do container raiz: um <see cref="IUnitOfWork"/>
/// (IDisposable) injetado como Transient ficaria enraizado no container e nunca seria
/// liberado, acumulando DbContexts/change-trackers durante todo o expediente.
/// Cada chamada de serviço faz <c>using var uow = _uowFactory.Create();</c>,
/// garantindo descarte determinístico do contexto ao fim da unidade de trabalho.
/// </para>
/// </summary>
public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}
