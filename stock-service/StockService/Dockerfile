# Multi-stage build para StockService - VERSÃO OTIMIZADA
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar apenas arquivos essenciais primeiro
# Copiar todo o diretório stock-service para preservar caminhos relativos entre projetos
COPY ["stock-service/", "./stock-service/"]

# Copiar projects compartilhados
COPY ["shared/", "./shared/"]

# Restaurar dependências usando o caminho original do projeto dentro de /src
RUN dotnet restore "./stock-service/StockService/StockService.csproj"

# Copiar código fonte já está incluído ao copiar o diretório inteiro

# Publicar aplicação
RUN dotnet publish "./stock-service/StockService/StockService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar aplicação publicada
COPY --from=build /app/publish .

# Configurar variáveis de ambiente
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ConnectionStrings__DefaultConnection="Host=postgres-stock;Port=5432;Database=stockdb;Username=postgres;Password=password123"
ENV RabbitMQ__HostName=rabbitmq
ENV RabbitMQ__Port=5672

EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "StockService.dll"]
