using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Receipt;

public record LookupNfceQuery(string Input) : IQuery<NfceLookupResponse>;
