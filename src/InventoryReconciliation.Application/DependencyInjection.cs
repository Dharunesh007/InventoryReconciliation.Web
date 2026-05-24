using InventoryReconciliation.Application.Reconciliation;
using InventoryReconciliation.Application.Dashboard;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryReconciliation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IReconciliationEngine, ReconciliationEngine>();
        services.AddScoped<IExecutiveDashboardService, ExecutiveDashboardService>();
        services.AddSingleton<Imports.InventoryImportValidator>();
        services.AddSingleton<Imports.SmartDuplicateDetector>();
        return services;
    }
}
