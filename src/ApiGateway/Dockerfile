# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy ONLY the .csproj first for layer-cached restore (ApiGateway has no project references).
COPY ["ApiGateway/ApiGateway.csproj", "ApiGateway/"]
RUN dotnet restore "ApiGateway/ApiGateway.csproj"
COPY . .
RUN dotnet publish "ApiGateway/ApiGateway.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# curl is required by the HEALTHCHECK below and is NOT in the aspnet image.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
# Fixed container-internal port; nothing host/secret specific is baked in.
# Remap externally with `docker run -p <host>:5000`.
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
COPY --from=build /app/publish .
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -fsS "http://localhost:5000/health" || exit 1
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
