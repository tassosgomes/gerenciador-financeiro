using GestorFinanceiro.Financeiro.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class CreateInstallmentRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public TransactionType? Type { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime CompetenceDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Range(1, 120)]
    public int NumberOfInstallments { get; set; }

    [MaxLength(100)]
    public string? OperationId { get; set; }
}
