# Multi-stage build para ApiGateway
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar apenas arquivos necessários
COPY ["api-gateway/ApiGateway/ApiGateway.csproj", "api-gateway/ApiGateway/"]
COPY ["api-gateway/ApiGateway/Program.cs", "api-gateway/ApiGateway/"]
COPY ["api-gateway/ApiGateway/ProxyRoutesDocumentFilter.cs", "api-gateway/ApiGateway/"]
COPY ["api-gateway/ApiGateway/appsettings*.json", "api-gateway/ApiGateway/"]

# Restaurar dependências
RUN dotnet restore "api-gateway/ApiGateway/ApiGateway.csproj"

# Copiar código fonte e publicar
COPY ["api-gateway/ApiGateway/", "api-gateway/ApiGateway/"]
RUN dotnet publish "api-gateway/ApiGateway/ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar aplicação publicada
COPY --from=build /app/publish .

# Configurar ambiente
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
