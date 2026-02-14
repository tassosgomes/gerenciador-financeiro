namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class DuplicateOperationException : DomainException
{
    public DuplicateOperationException(string operationId)
        : base($"Operation '{operationId}' was already processed.")
    {
    }
}
