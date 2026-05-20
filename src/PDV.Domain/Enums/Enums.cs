namespace PDV.Domain.Enums;

public enum UnitOfMeasure
{
    Unidade,
    Caixa,
    Pacote,
    Saco,
    Kg,
    Grama,
    Litro,
    Ml,
    Metro,
    MetroQuadrado,
    MetroCubico,
    Rolo,
    Balde,
    Tambor,
    Duzia,
    Peca,
    Conjunto
}

public enum PaymentMethod
{
    Dinheiro,
    CartaoDebito,
    CartaoCredito,
    Pix,
    Boleto,
    Cheque,
    Crediario
}

public enum SaleStatus
{
    Aberta,
    Finalizada,
    Cancelada,
    Suspensa
}

public enum StockMovementType
{
    Entrada,
    Saida,
    Ajuste,
    Devolucao
}

public enum CashMovementType
{
    Abertura,
    Suprimento,
    Sangria,
    Venda,
    Devolucao,
    Fechamento
}

public enum UserRole
{
    Administrador,
    Gerente,
    Operador
}
