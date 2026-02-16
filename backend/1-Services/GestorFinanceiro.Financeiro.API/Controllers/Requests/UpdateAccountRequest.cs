using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class UpdateAccountRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public bool? AllowNegativeBalance { get; set; }

    // Campos de cartão de crédito (opcionais, aplicáveis apenas a contas tipo Cartao)
    [Range(0.01, double.MaxValue)]
    public decimal? CreditLimit { get; set; }

    [Range(1, 28)]
    public int? ClosingDay { get; set; }

    [Range(1, 28)]
    public int? DueDay { get; set; }

    public Guid? DebitAccountId { get; set; }

    public bool? EnforceCreditLimit { get; set; }
}
