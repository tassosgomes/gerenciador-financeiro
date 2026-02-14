using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Category;

namespace GestorFinanceiro.Financeiro.Application.Commands.Category;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid category type");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}