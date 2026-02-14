using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestorFinanceiro.Financeiro.Infra.Context;

public class FinanceiroDbContextFactory : IDesignTimeDbContextFactory<FinanceiroDbContext>
{
    public FinanceiroDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FinanceiroDbContext>();
        var connectionString = "Host=localhost;Port=5432;Database=gestor_financeiro;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new FinanceiroDbContext(optionsBuilder.Options);
    }
}
