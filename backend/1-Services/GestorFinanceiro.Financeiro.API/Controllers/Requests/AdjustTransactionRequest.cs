using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class AdjustTransactionRequest
{
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal NewAmount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? OperationId { get; set; }
}
