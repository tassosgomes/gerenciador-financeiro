using GestorFinanceiro.Financeiro.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class CreateRecurrenceRequest
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
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Range(1, 31)]
    public int? DayOfMonth { get; set; }

    [MaxLength(100)]
    public string? OperationId { get; set; }

    public TransactionStatus? DefaultStatus { get; set; }
}
