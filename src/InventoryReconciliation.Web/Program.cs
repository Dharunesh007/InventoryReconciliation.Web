using InventoryReconciliation.Application;
using InventoryReconciliation.Infrastructure;
using InventoryReconciliation.Infrastructure.Data;
using InventoryReconciliation.Web.Components;
using InventoryReconciliation.Web.Endpoints;
using InventoryReconciliation.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .SetApplicationName("InventoryReconciliation.Dev")
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddFluentUIComponents();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSignalR();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var mvcBuilder = builder.Services.AddControllersWithViews();

if (builder.Configuration.GetValue<bool>("Authentication:EnableEntraId"))
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
    mvcBuilder.AddMicrosoftIdentityUI();
}
else
{
    builder.Services.AddAuthentication(DevAuthenticationDefaults.Scheme)
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationDefaults.Scheme, _ => { });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanImportInventory", policy => policy.RequireRole("SuperAdmin", "InventoryAdmin"));
    options.AddPolicy("CanVerifyAssets", policy => policy.RequireRole("SuperAdmin", "InventoryAdmin", "Auditor", "ItSupport"));
    options.AddPolicy("CanApproveReconciliation", policy => policy.RequireRole("SuperAdmin", "InventoryAdmin", "RegionalManager", "ComplianceTeam"));
    options.AddPolicy("CanViewExecutiveReports", policy => policy.RequireRole("SuperAdmin", "ReadOnlyExecutive", "ComplianceTeam", "RegionalManager"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();

    if (builder.Configuration.GetValue<bool>("Development:InitializeDatabase"))
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            if (builder.Configuration.GetValue<bool>("Development:SeedDemoData"))
            {
                await SeedData.SeedDevelopmentAsync(dbContext);
            }
        }
        catch (Exception exception)
        {
            app.Logger.LogWarning(exception, "Development database initialization was skipped. Configure ConnectionStrings:InventoryDb to enable database-backed workflows.");
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapInventoryEndpoints();
app.MapReportEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
