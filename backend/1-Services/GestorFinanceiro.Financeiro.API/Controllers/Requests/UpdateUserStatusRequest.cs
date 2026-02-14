using System.ComponentModel.DataAnnotations;

namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public class UpdateUserStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
