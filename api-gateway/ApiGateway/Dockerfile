FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything and restore/publish
COPY . ./
# restore/publish only the ApiGateway project to avoid restoring the entire solution
RUN dotnet restore api-gateway/ApiGateway/ApiGateway.csproj
RUN dotnet publish api-gateway/ApiGateway/ApiGateway.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
