using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class ImportNfceRequest
{
    [Required]
    [StringLength(44, MinimumLength = 44)]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime CompetenceDate { get; set; }

    [MaxLength(100)]
    public string? OperationId { get; set; }
}
