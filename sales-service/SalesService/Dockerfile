FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy entire repo so referenced projects (shared/Messaging) are available
COPY . ./

# publish only the SalesService project to avoid building tests/solution
RUN dotnet restore ./sales-service/SalesService/SalesService.csproj
RUN dotnet publish ./sales-service/SalesService/SalesService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV ConnectionStrings__SalesDb="Host=postgres-sales;Port=5432;Database=salesdb;Username=postgres;Password=password123"
ENV RabbitMQ__HostName=rabbitmq
ENV RabbitMQ__Port=5672
ENV Jwt__Key="your-super-secret-jwt-key-for-sales-service-2024"
ENV Jwt__Issuer="SalesService"
ENV Jwt__Audience="SalesServiceUsers"
EXPOSE 80
ENTRYPOINT ["dotnet", "SalesService.dll"]
