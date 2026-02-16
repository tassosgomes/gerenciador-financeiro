using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class ApplicationServiceExtensionsTests
{
    [Fact]
    public void AddApplicationServices_ShouldRegisterAllCommandAndQueryHandlers()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        var handlerContracts = typeof(IDispatcher).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .SelectMany(type => type.GetInterfaces())
            .Where(i => i.IsGenericType)
            .Where(i =>
                i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
            .Distinct()
            .ToList();

        var missingRegistrations = handlerContracts
            .Where(contract => services.All(descriptor => descriptor.ServiceType != contract))
            .Select(contract => contract.FullName)
            .OrderBy(name => name)
            .ToList();

        missingRegistrations.Should().BeEmpty();
    }
}
