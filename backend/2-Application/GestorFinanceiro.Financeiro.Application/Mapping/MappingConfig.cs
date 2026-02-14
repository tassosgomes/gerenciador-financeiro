using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using Mapster;

namespace GestorFinanceiro.Financeiro.Application.Mapping;

public static class MappingConfig
{
    public static void ConfigureMappings()
    {
        TypeAdapterConfig<Account, AccountResponse>.NewConfig();
        TypeAdapterConfig<Transaction, TransactionResponse>.NewConfig();
        TypeAdapterConfig<Category, CategoryResponse>.NewConfig();
        TypeAdapterConfig<RecurrenceTemplate, RecurrenceTemplateResponse>.NewConfig();
    }
}