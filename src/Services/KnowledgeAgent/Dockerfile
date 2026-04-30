FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KnowledgeAgent.API/KnowledgeAgent.API.csproj", "KnowledgeAgent.API/"]
COPY ["KnowledgeAgent.Core/KnowledgeAgent.Core.csproj", "KnowledgeAgent.Core/"]
COPY ["KnowledgeAgent.Infrastructure/KnowledgeAgent.Infrastructure.csproj", "KnowledgeAgent.Infrastructure/"]
RUN dotnet restore "KnowledgeAgent.API/KnowledgeAgent.API.csproj"
COPY . .
WORKDIR "/src/KnowledgeAgent.API"
RUN dotnet build "KnowledgeAgent.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KnowledgeAgent.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KnowledgeAgent.API.dll"]
