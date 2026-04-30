FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5006

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["Services/Monitoring/AIMonitoringAgent/AIMonitoringAgent.csproj", "Services/Monitoring/AIMonitoringAgent/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/Monitoring/AIMonitoringAgent/AIMonitoringAgent.csproj"
COPY . .
WORKDIR "/src/Services/Monitoring/AIMonitoringAgent"
RUN dotnet build "AIMonitoringAgent.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AIMonitoringAgent.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5006
ENTRYPOINT ["dotnet", "AIMonitoringAgent.dll"]
