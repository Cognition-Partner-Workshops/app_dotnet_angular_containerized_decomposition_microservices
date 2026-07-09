# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy ONLY .csproj files first (one COPY per referenced project) for layer-cached restore
COPY ["Services/Identity/Identity.API/Identity.API.csproj", "Services/Identity/Identity.API/"]
COPY ["Services/Identity/Identity.Domain/Identity.Domain.csproj", "Services/Identity/Identity.Domain/"]
COPY ["Services/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "Services/Identity/Identity.Infrastructure/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/Identity/Identity.API/Identity.API.csproj"
COPY . .
RUN dotnet publish "Services/Identity/Identity.API/Identity.API.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# curl is required by the HEALTHCHECK below and is NOT in the aspnet image (Gotcha G4)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
# Fixed container-internal port; nothing host/secret specific is baked in.
# Remap externally with `docker run -p <host>:5001`.
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
COPY --from=build /app/publish .
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -fsS "http://localhost:5001/health" || exit 1
ENTRYPOINT ["dotnet", "Identity.API.dll"]
