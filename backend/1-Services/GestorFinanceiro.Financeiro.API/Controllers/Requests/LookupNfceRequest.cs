using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class LookupNfceRequest
{
    [Required]
    [MaxLength(2048)]
    public string Input { get; set; } = string.Empty;
}
