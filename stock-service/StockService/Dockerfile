FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy full repo so shared projects are available
COPY . ./

# restore and publish only the StockService project
RUN dotnet restore ./stock-service/StockService/StockService.csproj
RUN dotnet publish ./stock-service/StockService/StockService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV ConnectionStrings__DefaultConnection="Host=postgres-stock;Port=5432;Database=stockdb;Username=postgres;Password=password123"
ENV RabbitMQ__HostName=rabbitmq
ENV RabbitMQ__Port=5672
EXPOSE 80
ENTRYPOINT ["dotnet", "StockService.dll"]
