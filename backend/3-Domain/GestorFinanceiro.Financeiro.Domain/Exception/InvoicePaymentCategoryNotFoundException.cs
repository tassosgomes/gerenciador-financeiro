namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InvoicePaymentCategoryNotFoundException : DomainException
{
    public InvoicePaymentCategoryNotFoundException()
        : base("Invoice payment system category not found. Please ensure the seed task has been executed.")
    {
    }
}
