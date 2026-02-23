using System.Text.RegularExpressions;
using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Receipt;

public class LookupNfceQueryHandler : IQueryHandler<LookupNfceQuery, NfceLookupResponse>
{
    private static readonly Regex AccessKeyRegex = new(@"(?<!\d)(\d{44})(?!\d)", RegexOptions.Compiled);

    private readonly ISefazNfceService _sefazNfceService;
    private readonly IEstablishmentRepository _establishmentRepository;
    private readonly IValidator<LookupNfceQuery> _validator;
    private readonly ILogger<LookupNfceQueryHandler> _logger;

    public LookupNfceQueryHandler(
        ISefazNfceService sefazNfceService,
        IEstablishmentRepository establishmentRepository,
        IValidator<LookupNfceQuery> validator,
        ILogger<LookupNfceQueryHandler> logger)
    {
        _sefazNfceService = sefazNfceService;
        _establishmentRepository = establishmentRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<NfceLookupResponse> HandleAsync(LookupNfceQuery query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var accessKey = ExtractAccessKey(query.Input);
        _logger.LogInformation("Looking up NFC-e data for access key {AccessKey}", accessKey);

        var nfceData = await _sefazNfceService.LookupAsync(accessKey, cancellationToken);
        var alreadyImported = await _establishmentRepository.ExistsByAccessKeyAsync(accessKey, cancellationToken);

        var response = nfceData.Adapt<NfceLookupResponse>();
        return response with { AlreadyImported = alreadyImported };
    }

    private static string ExtractAccessKey(string input)
    {
        var trimmedInput = input.Trim();

        if (trimmedInput.Length == 44 && trimmedInput.All(char.IsDigit))
        {
            return trimmedInput;
        }

        var match = AccessKeyRegex.Match(trimmedInput);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        throw new InvalidAccessKeyException(trimmedInput);
    }
}
