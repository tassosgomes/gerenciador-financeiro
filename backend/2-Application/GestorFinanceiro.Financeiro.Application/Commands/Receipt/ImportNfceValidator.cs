using FluentValidation;

namespace GestorFinanceiro.Financeiro.Application.Commands.Receipt;

public class ImportNfceValidator : AbstractValidator<ImportNfceCommand>
{
    public ImportNfceValidator()
    {
        RuleFor(command => command.AccessKey)
            .NotEmpty().WithMessage("AccessKey is required")
            .Length(44).WithMessage("AccessKey must contain exactly 44 digits")
            .Must(accessKey => accessKey.All(char.IsDigit)).WithMessage("AccessKey must contain only numeric digits");

        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("AccountId is required");

        RuleFor(command => command.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required");

        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(command => command.CompetenceDate)
            .NotEmpty().WithMessage("CompetenceDate is required")
            .LessThanOrEqualTo(_ => DateTime.UtcNow).WithMessage("CompetenceDate cannot be in the future");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}
