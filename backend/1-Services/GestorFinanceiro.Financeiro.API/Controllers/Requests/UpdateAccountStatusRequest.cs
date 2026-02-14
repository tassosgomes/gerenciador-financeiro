using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class UpdateAccountStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
