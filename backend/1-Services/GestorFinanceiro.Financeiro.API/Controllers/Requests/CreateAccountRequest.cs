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
}
