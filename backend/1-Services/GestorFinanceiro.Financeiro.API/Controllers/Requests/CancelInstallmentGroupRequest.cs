using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class CancelInstallmentGroupRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(100)]
    public string? OperationId { get; set; }
}
