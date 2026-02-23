using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using GestorFinanceiro.Financeiro.Domain.Dto;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Infra.Services;

public sealed class SefazPbNfceService : ISefazNfceService
{
    private static readonly Regex AccessKeyRegex = new(@"(?<!\d)(\d{44})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex QueryPRegex = new(@"(?:^|[?&])p=([^&#]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CnpjRegex = new(@"\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}", RegexOptions.Compiled);
    private static readonly Regex DateTimeRegex = new(@"\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}(?::\d{2})?", RegexOptions.Compiled);
    private static readonly Regex NumericRegex = new(@"-?\d{1,3}(?:\.\d{3})*(?:,\d{1,4})|-?\d+(?:[\.,]\d{1,4})?", RegexOptions.Compiled);
    private static readonly string[] EstablishmentNameSelectors = ["#establishment-name", ".establishment-name", "#u20", ".txtTopo"];
    private static readonly string[] EstablishmentCnpjSelectors = ["#establishment-cnpj", ".establishment-cnpj", "#u21", ".text", ".conteudo"];
    private static readonly string[] IssuedAtSelectors = ["#issued-at", ".issued-at", "#u23", ".txtCenter"];
    private static readonly string[] TotalAmountSelectors = ["#total-amount", ".total-amount", "#valorTotal", ".totalNumb"];
    private static readonly string[] DiscountAmountSelectors = ["#discount-amount", ".discount-amount", "#valorDesconto", ".valorDesconto"];
    private static readonly string[] PaidAmountSelectors = ["#paid-amount", ".paid-amount", "#valorPago", ".valorPago"];
    private static readonly string[] ItemRowSelectors = ["#receipt-items tbody tr", "table.receipt-items tbody tr", ".receipt-items tbody tr", "#tabResult tbody tr"];
    private static readonly string[] NotFoundSelectors = ["#nota-nao-encontrada", ".nfce-not-found", ".mensagem-erro"];
    private static readonly string[] NotFoundKeywords = ["não encontrada", "nao encontrada", "não foi encontrada", "nao foi encontrada", "inexistente", "não disponível", "nao disponivel"];
    private static readonly CultureInfo PtBrCulture = new("pt-BR");
    private const int HtmlPreviewMaxLength = 1200;
    private const string EntryPath = "seg/SEGf_AcessarFuncao.jsp";
    private const string ConsultPath = "fis/FISf_ConsultarNFCE.do";
    private const string DisplayPath = "fis/FISf_ExibirNFCE.do";

    private readonly HttpClient _httpClient;
    private readonly ILogger<SefazPbNfceService> _logger;

    public SefazPbNfceService(HttpClient httpClient, ILogger<SefazPbNfceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<NfceData> LookupAsync(string accessKey, CancellationToken cancellationToken)
    {
        var (normalizedAccessKey, lookupPayload) = ExtractLookupParameters(accessKey);

        await EnsureSuccessAsync($"{EntryPath}?cdFuncao=FIS_1410&p={Uri.EscapeDataString(lookupPayload)}", normalizedAccessKey, cancellationToken);
        await EnsureSuccessAsync($"{ConsultPath}?limparSessao=true&p={Uri.EscapeDataString(lookupPayload)}", normalizedAccessKey, cancellationToken);
        var html = await ReadContentAsync(await EnsureSuccessAsync(DisplayPath, normalizedAccessKey, cancellationToken), cancellationToken);

        var htmlPreview = html.Length > HtmlPreviewMaxLength ? html[..HtmlPreviewMaxLength] : html;
        _logger.LogDebug("SEFAZ HTML preview: {HtmlPreview}", htmlPreview);

        try
        {
            return ParseNfceData(html, normalizedAccessKey);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SefazParsingException(exception);
        }
    }

    private async Task<HttpResponseMessage> EnsureSuccessAsync(string relativeUrl, string accessKey, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SefazUnavailableException(exception);
        }
        catch (HttpRequestException exception)
        {
            throw new SefazUnavailableException(exception);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NfceNotFoundException(accessKey);
        }

        if ((int)response.StatusCode >= 500)
        {
            throw new SefazUnavailableException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new SefazParsingException();
        }

        return response;
    }

    private static async Task<string> ReadContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static (string AccessKey, string LookupPayload) ExtractLookupParameters(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new InvalidAccessKeyException(input);
        }

        var trimmedInput = input.Trim();
        var accessKeyMatch = AccessKeyRegex.Match(trimmedInput);
        if (!accessKeyMatch.Success)
        {
            throw new InvalidAccessKeyException(trimmedInput);
        }

        var accessKey = accessKeyMatch.Groups[1].Value;

        var payloadFromUrl = TryExtractPayloadFromUrl(trimmedInput, accessKey);
        if (!string.IsNullOrWhiteSpace(payloadFromUrl))
        {
            return (accessKey, payloadFromUrl);
        }

        if (trimmedInput.Length == 44 && trimmedInput.All(char.IsDigit))
        {
            return (trimmedInput, $"{trimmedInput}|2|1|1");
        }

        if (trimmedInput.Contains('|') && trimmedInput.StartsWith(accessKey, StringComparison.Ordinal))
        {
            return (accessKey, trimmedInput);
        }

        return (accessKey, $"{accessKey}|2|1|1");
    }

    private static string? TryExtractPayloadFromUrl(string input, string accessKey)
    {
        if (!IsUrlInput(input))
        {
            return null;
        }

        var match = QueryPRegex.Match(input);
        if (!match.Success)
        {
            return null;
        }

        var rawPayload = Uri.UnescapeDataString(match.Groups[1].Value).Trim();
        if (!rawPayload.StartsWith(accessKey, StringComparison.Ordinal))
        {
            return null;
        }

        return rawPayload;
    }

    private static bool IsUrlInput(string input)
    {
        return input.Contains("http", StringComparison.OrdinalIgnoreCase)
            || input.Contains("sefaz", StringComparison.OrdinalIgnoreCase)
            || input.Contains('/', StringComparison.Ordinal)
            || input.Contains('?', StringComparison.Ordinal);
    }

    private NfceData ParseNfceData(string html, string accessKey)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        if (IsNfceNotFound(document))
        {
            throw new NfceNotFoundException(accessKey);
        }

        var establishmentName = ParseRequiredText(document, EstablishmentNameSelectors, "establishment name", ["edtNomeEmi"]);
        var establishmentCnpjRaw = ParseRequiredText(document, EstablishmentCnpjSelectors, "establishment CNPJ", ["edtDocumentoEmi"]);
        var establishmentCnpj = ParseCnpj(establishmentCnpjRaw);

        var issuedAtRaw = ParseRequiredText(document, IssuedAtSelectors, "issued at", ["edtDEmis"]);
        var issuedAt = ParseDateTime(issuedAtRaw);

        var items = ParseItems(document);
        if (items.Count == 0)
        {
            throw new SefazParsingException();
        }

        var totalAmount = ParseRequiredDecimal(document, TotalAmountSelectors, "total amount", ["edtVNF", "edtVTotNF"]);
        var discountAmount = ParseOptionalDecimal(document, DiscountAmountSelectors, ["edtVTotDesc"]);
        var paidAmount = ParseOptionalDecimal(document, PaidAmountSelectors, ["edtVPagamento"]);

        if (paidAmount <= 0)
        {
            paidAmount = totalAmount - discountAmount;
        }

        if (paidAmount <= 0)
        {
            throw new SefazParsingException();
        }

        if (items.Any(item => string.IsNullOrWhiteSpace(item.Description) || string.IsNullOrWhiteSpace(item.UnitOfMeasure)))
        {
            _logger.LogWarning("SEFAZ parsing returned incomplete item data for access key {AccessKey}", accessKey);
        }

        return new NfceData(
            accessKey,
            establishmentName,
            establishmentCnpj,
            issuedAt,
            totalAmount,
            discountAmount,
            paidAmount,
            items);
    }

    private static bool IsNfceNotFound(IDocument document)
    {
        foreach (var selector in NotFoundSelectors)
        {
            if (!string.IsNullOrWhiteSpace(document.QuerySelector(selector)?.TextContent))
            {
                return true;
            }
        }

        var bodyText = NormalizeWhiteSpace(document.Body?.TextContent ?? string.Empty).ToLowerInvariant();
        return NotFoundKeywords.Any(keyword => bodyText.Contains(keyword, StringComparison.Ordinal));
    }

    private static string ParseRequiredText(IDocument document, IReadOnlyList<string> selectors, string fieldName, IReadOnlyList<string>? inputNames = null)
    {
        if (inputNames is not null)
        {
            foreach (var inputName in inputNames)
            {
                var inputValue = NormalizeWhiteSpace(document.QuerySelector($"input[name='{inputName}']")?.GetAttribute("value") ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(inputValue))
                {
                    return inputValue;
                }
            }
        }

        foreach (var selector in selectors)
        {
            var value = NormalizeWhiteSpace(document.QuerySelector(selector)?.TextContent ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        if (fieldName == "establishment CNPJ")
        {
            var body = NormalizeWhiteSpace(document.Body?.TextContent ?? string.Empty);
            var cnpjMatch = CnpjRegex.Match(body);
            if (cnpjMatch.Success)
            {
                return cnpjMatch.Value;
            }
        }

        if (fieldName == "issued at")
        {
            var body = NormalizeWhiteSpace(document.Body?.TextContent ?? string.Empty);
            var dateTimeMatch = DateTimeRegex.Match(body);
            if (dateTimeMatch.Success)
            {
                return dateTimeMatch.Value;
            }
        }

        throw new SefazParsingException();
    }

    private static decimal ParseRequiredDecimal(IDocument document, IReadOnlyList<string> selectors, string fieldName, IReadOnlyList<string>? inputNames = null)
    {
        var value = ParseOptionalDecimal(document, selectors, inputNames);
        if (value <= 0)
        {
            throw new SefazParsingException();
        }

        return value;
    }

    private static decimal ParseOptionalDecimal(IDocument document, IReadOnlyList<string> selectors, IReadOnlyList<string>? inputNames = null)
    {
        if (inputNames is not null)
        {
            foreach (var inputName in inputNames)
            {
                var value = NormalizeWhiteSpace(document.QuerySelector($"input[name='{inputName}']")?.GetAttribute("value") ?? string.Empty);
                if (TryParseDecimal(value, out var parsedByInput))
                {
                    return parsedByInput;
                }
            }
        }

        foreach (var selector in selectors)
        {
            var value = NormalizeWhiteSpace(document.QuerySelector(selector)?.TextContent ?? string.Empty);
            if (TryParseDecimal(value, out var parsed))
            {
                return parsed;
            }
        }

        return 0m;
    }

    private static IReadOnlyList<NfceItemData> ParseItems(IDocument document)
    {
        foreach (var itemRowSelector in ItemRowSelectors)
        {
            var rows = document.QuerySelectorAll(itemRowSelector);
            if (rows.Length == 0)
            {
                continue;
            }

            var parsedItems = new List<NfceItemData>();
            foreach (var row in rows)
            {
                var columns = row.QuerySelectorAll("td");
                if (columns.Length < 6)
                {
                    continue;
                }

                var description = NormalizeWhiteSpace(columns[0].TextContent);
                var productCodeText = NormalizeWhiteSpace(columns[1].TextContent);
                var productCode = string.IsNullOrWhiteSpace(productCodeText) || productCodeText == "-"
                    ? null
                    : productCodeText;

                if (!TryParseDecimal(columns[2].TextContent, out var quantity)
                    || !TryParseDecimal(columns[4].TextContent, out var unitPrice)
                    || !TryParseDecimal(columns[5].TextContent, out var totalPrice)
                    || string.IsNullOrWhiteSpace(description))
                {
                    throw new SefazParsingException();
                }

                parsedItems.Add(new NfceItemData(
                    description,
                    productCode,
                    quantity,
                    NormalizeWhiteSpace(columns[3].TextContent),
                    unitPrice,
                    totalPrice));
            }

            if (parsedItems.Count > 0)
            {
                return parsedItems;
            }
        }

        return ParseItemsFromInputs(document);
    }

    private static IReadOnlyList<NfceItemData> ParseItemsFromInputs(IDocument document)
    {
        var descriptions = SelectInputValues(document, "edtDescProd");
        var quantities = SelectInputValues(document, "edtQtdProd");
        var units = SelectInputValues(document, "edtUnidTrib");
        var totals = SelectInputValues(document, "edtValorProd");
        var unitPrices = SelectInputValues(document, "edtvlUniCom");

        var itemCount = new[] { descriptions.Count, quantities.Count, units.Count, totals.Count }.Min();
        if (itemCount <= 0)
        {
            return [];
        }

        var parsedItems = new List<NfceItemData>();
        for (var index = 0; index < itemCount; index++)
        {
            if (!TryParseDecimal(quantities[index], out var quantity)
                || !TryParseDecimal(totals[index], out var totalPrice)
                || string.IsNullOrWhiteSpace(descriptions[index]))
            {
                throw new SefazParsingException();
            }

            decimal unitPrice;
            if (index < unitPrices.Count && TryParseDecimal(unitPrices[index], out var parsedUnitPrice))
            {
                unitPrice = parsedUnitPrice;
            }
            else
            {
                if (quantity <= 0)
                {
                    throw new SefazParsingException();
                }

                unitPrice = totalPrice / quantity;
            }

            parsedItems.Add(new NfceItemData(
                descriptions[index],
                null,
                quantity,
                units[index],
                unitPrice,
                totalPrice));
        }

        return parsedItems;
    }

    private static IReadOnlyList<string> SelectInputValues(IDocument document, string inputName)
    {
        return document
            .QuerySelectorAll($"input[name='{inputName}']")
            .Select(input => NormalizeWhiteSpace(input.GetAttribute("value") ?? string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static DateTime ParseDateTime(string value)
    {
        if (DateTime.TryParseExact(
            value,
            ["dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm"],
            PtBrCulture,
            DateTimeStyles.AssumeLocal,
            out var issuedAt))
        {
            return issuedAt;
        }

        throw new SefazParsingException();
    }

    private static bool TryParseDecimal(string? rawValue, out decimal parsedValue)
    {
        parsedValue = default;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var normalized = NormalizeWhiteSpace(rawValue)
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var matches = NumericRegex.Matches(normalized);
        if (matches.Count == 0)
        {
            return false;
        }

        var numericToken = matches[^1].Value;

        return decimal.TryParse(numericToken, NumberStyles.Number, PtBrCulture, out parsedValue)
            || decimal.TryParse(numericToken.Replace('.', ',').Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out parsedValue);
    }

    private static string ParseCnpj(string rawCnpj)
    {
        var digitsOnly = new string(rawCnpj.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length != 14)
        {
            throw new SefazParsingException();
        }

        return digitsOnly;
    }

    private static string NormalizeWhiteSpace(string value)
    {
        return string.Join(' ', value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
    }
}
