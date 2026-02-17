using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class MarkTransactionAsPaidRequest
{
    [MaxLength(100)]
    public string? OperationId { get; set; }
}
