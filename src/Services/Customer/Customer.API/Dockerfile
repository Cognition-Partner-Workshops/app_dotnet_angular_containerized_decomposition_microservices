# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy ONLY .csproj files first (one COPY per referenced project) for layer-cached restore
COPY ["Services/Customer/Customer.API/Customer.API.csproj", "Services/Customer/Customer.API/"]
COPY ["Services/Customer/Customer.Domain/Customer.Domain.csproj", "Services/Customer/Customer.Domain/"]
COPY ["Services/Customer/Customer.Infrastructure/Customer.Infrastructure.csproj", "Services/Customer/Customer.Infrastructure/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/Customer/Customer.API/Customer.API.csproj"
COPY . .
RUN dotnet publish "Services/Customer/Customer.API/Customer.API.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# curl is required by the HEALTHCHECK below and is NOT in the aspnet image
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
# Fixed container-internal port; nothing host/secret specific is baked in.
# Remap externally with `docker run -p <host>:5002`.
ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002
COPY --from=build /app/publish .
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -fsS "http://localhost:5002/health" || exit 1
ENTRYPOINT ["dotnet", "Customer.API.dll"]
