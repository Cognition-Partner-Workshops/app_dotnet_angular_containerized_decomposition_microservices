# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (copy only project files first for better layer caching)
COPY ["Services/Order/Order.API/Order.API.csproj", "Services/Order/Order.API/"]
COPY ["Services/Order/Order.Domain/Order.Domain.csproj", "Services/Order/Order.Domain/"]
COPY ["Services/Order/Order.Infrastructure/Order.Infrastructure.csproj", "Services/Order/Order.Infrastructure/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/Order/Order.API/Order.API.csproj"

# Copy the rest of the sources and publish
COPY . .
RUN dotnet publish "Services/Order/Order.API/Order.API.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is used by the container HEALTHCHECK below
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Listening port is configurable; nothing host/secret specific is baked in.
ENV APP_PORT=5003
ENV ASPNETCORE_URLS=http://+:${APP_PORT}
EXPOSE 5003

COPY --from=build /app/publish .

# Run as the non-root user shipped in the .NET base image.
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -fsS "http://localhost:${APP_PORT}/health" || exit 1

ENTRYPOINT ["dotnet", "Order.API.dll"]
