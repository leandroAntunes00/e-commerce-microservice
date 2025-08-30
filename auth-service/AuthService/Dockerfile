# Multi-stage build para AuthService
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de projeto primeiro (para cache de layers)
COPY ["auth-service/AuthService/AuthService.csproj", "auth-service/AuthService/"]
COPY ["auth-service/AuthService/appsettings*.json", "auth-service/AuthService/"]

# Restaurar dependências (cache layer)
RUN dotnet restore "auth-service/AuthService/AuthService.csproj"

# Copiar código fonte
COPY ["auth-service/AuthService/", "auth-service/AuthService/"]

# Publicar aplicação
RUN dotnet publish "auth-service/AuthService/AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar aplicação publicada
COPY --from=build /app/publish .

# Configurar variáveis de ambiente
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ConnectionStrings__AuthDb="Host=postgres-auth;Port=5432;Database=authdb;Username=postgres;Password=password123"
ENV RabbitMQ__HostName=rabbitmq
ENV RabbitMQ__Port=5672

EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "AuthService.dll"]
