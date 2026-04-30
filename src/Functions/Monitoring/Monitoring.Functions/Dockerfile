FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Shared.Contracts/Shared.Contracts.csproj Shared/Shared.Contracts/
COPY Functions/Monitoring/Monitoring.Functions/Monitoring.Functions.csproj Functions/Monitoring/Monitoring.Functions/
RUN dotnet restore Functions/Monitoring/Monitoring.Functions/Monitoring.Functions.csproj

COPY Shared/Shared.Contracts/ Shared/Shared.Contracts/
COPY Functions/Monitoring/Monitoring.Functions/ Functions/Monitoring/Monitoring.Functions/
RUN dotnet publish Functions/Monitoring/Monitoring.Functions/Monitoring.Functions.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
COPY --from=build /app/publish /home/site/wwwroot
