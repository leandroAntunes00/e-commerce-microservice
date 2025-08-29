FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV ConnectionStrings__AuthDb="Host=postgres-auth;Port=5432;Database=authdb;Username=postgres;Password=password123"
ENV RabbitMQ__HostName=rabbitmq
ENV RabbitMQ__Port=5672
ENV Jwt__Key="your-super-secret-jwt-key-here-make-it-long-and-secure"
ENV Jwt__Issuer="AuthService"
ENV Jwt__Audience="Microservices"
EXPOSE 80
ENTRYPOINT ["dotnet", "AuthService.dll"]
