# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against project files only (better layer caching).
COPY ["Services/Product/Product.API/Product.API.csproj", "Services/Product/Product.API/"]
COPY ["Services/Product/Product.Domain/Product.Domain.csproj", "Services/Product/Product.Domain/"]
COPY ["Services/Product/Product.Infrastructure/Product.Infrastructure.csproj", "Services/Product/Product.Infrastructure/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/Product/Product.API/Product.API.csproj"

# Copy the rest of the sources and publish.
COPY . .
RUN dotnet publish "Services/Product/Product.API/Product.API.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is used by the container HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# All runtime configuration is supplied via environment variables — no
# hardcoded hosts/credentials. Provide only safe, overridable defaults here.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=5004 \
    ASPNETCORE_URLS=http://+:5004 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

EXPOSE 5004

# Run as the non-root user shipped with the .NET runtime images.
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:5004/health || exit 1

ENTRYPOINT ["dotnet", "Product.API.dll"]
