FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json InventoryReconciliation.sln ./
COPY src/InventoryReconciliation.Domain/InventoryReconciliation.Domain.csproj src/InventoryReconciliation.Domain/
COPY src/InventoryReconciliation.Application/InventoryReconciliation.Application.csproj src/InventoryReconciliation.Application/
COPY src/InventoryReconciliation.Infrastructure/InventoryReconciliation.Infrastructure.csproj src/InventoryReconciliation.Infrastructure/
COPY src/InventoryReconciliation.Web/InventoryReconciliation.Web.csproj src/InventoryReconciliation.Web/
COPY tests/InventoryReconciliation.Tests/InventoryReconciliation.Tests.csproj tests/InventoryReconciliation.Tests/
RUN dotnet restore InventoryReconciliation.sln
COPY . .
RUN dotnet publish src/InventoryReconciliation.Web/InventoryReconciliation.Web.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data
COPY samples/ITAssetImportTemplate.xlsx /app/App_Data/ITAssetImportTemplate.xlsx
ENTRYPOINT ["dotnet", "InventoryReconciliation.Web.dll"]
