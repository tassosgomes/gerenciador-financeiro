using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class CreateTransferRequest
{
    [Required]
    public Guid SourceAccountId { get; set; }

    [Required]
    public Guid DestinationAccountId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime CompetenceDate { get; set; }

    public DateTime? DueDate { get; set; }

    [MaxLength(100)]
    public string? OperationId { get; set; }
}
