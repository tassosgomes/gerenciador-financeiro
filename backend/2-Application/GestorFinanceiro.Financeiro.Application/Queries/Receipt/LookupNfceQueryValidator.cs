using System.Text.RegularExpressions;
using FluentValidation;

namespace GestorFinanceiro.Financeiro.Application.Queries.Receipt;

public class LookupNfceQueryValidator : AbstractValidator<LookupNfceQuery>
{
    private static readonly Regex AccessKeyRegex = new(@"^\d{44}$", RegexOptions.Compiled);

    public LookupNfceQueryValidator()
    {
        RuleFor(query => query.Input)
            .NotEmpty()
            .WithMessage("Input is required.");

        RuleFor(query => query.Input)
            .Must(BeValidInput)
            .WithMessage("Input must be a 44-digit access key or a valid URL.");
    }

    private static bool BeValidInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmedInput = input.Trim();
        var digitsOnly = trimmedInput.All(char.IsDigit);
        if (digitsOnly)
        {
            return AccessKeyRegex.IsMatch(trimmedInput);
        }

        return trimmedInput.Contains("http", StringComparison.OrdinalIgnoreCase);
    }
}
