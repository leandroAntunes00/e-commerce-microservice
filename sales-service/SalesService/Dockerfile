# Multi-stage build para SalesService
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de projeto primeiro (para cache de layers)
COPY ["sales-service/SalesService/SalesService.csproj", "sales-service/SalesService/"]
COPY ["sales-service/SalesService/appsettings*.json", "sales-service/SalesService/"]

# Se houver projetos compartilhados, copiá-los também
COPY ["shared/", "shared/"]

# Restaurar dependências (cache layer)
RUN dotnet restore "sales-service/SalesService/SalesService.csproj"

# Copiar código fonte
COPY ["sales-service/SalesService/", "sales-service/SalesService/"]

# Publicar aplicação
RUN dotnet publish "sales-service/SalesService/SalesService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar aplicação publicada
COPY --from=build /app/publish .

# Configurar variáveis de ambiente
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ConnectionStrings__SalesDb="Host=postgres-sales;Port=5432;Database=salesdb;Username=postgres;Password=password123"
ENV RabbitMQ__HostName=rabbitmq
ENV RabbitMQ__Port=5672

EXPOSE 80

# Health check (rota do controller)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/api/sales/health || exit 1

ENTRYPOINT ["dotnet", "SalesService.dll"]
