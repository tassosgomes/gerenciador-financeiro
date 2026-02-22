namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class BudgetPercentageExceededException : DomainException
{
    public BudgetPercentageExceededException(decimal percentage, decimal available, int month, int year)
        : base($"Percentual de orçamento inválido para {month:D2}/{year}. Solicitado: {percentage:N2}%. Disponível: {available:N2}%.")
    {
    }
}
