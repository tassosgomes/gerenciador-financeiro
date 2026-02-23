namespace GestorFinanceiro.Financeiro.Infra.Services;

public sealed class SefazSettings
{
    public string BaseUrl { get; set; } = "https://www4.sefaz.pb.gov.br/atf/";
    public int TimeoutSeconds { get; set; } = 15;
    public string UserAgent { get; set; } = "GestorFinanceiro/1.0 (+https://github.com/tassosgomes/gerenciador-financeiro)";
}
