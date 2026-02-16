using GestorFinanceiro.Financeiro.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class CreateAccountRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AccountType? Type { get; set; }

    [Range(0, double.MaxValue)]
    public decimal InitialBalance { get; set; }

    public bool AllowNegativeBalance { get; set; }

    // Campos de cartão de crédito (opcionais, obrigatórios apenas quando Type == Cartao)
    [Range(0.01, double.MaxValue)]
    public decimal? CreditLimit { get; set; }

    [Range(1, 28)]
    public int? ClosingDay { get; set; }

    [Range(1, 28)]
    public int? DueDay { get; set; }

    public Guid? DebitAccountId { get; set; }

    public bool? EnforceCreditLimit { get; set; }
}
