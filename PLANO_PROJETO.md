🛒 Microserviços E-commerce (.NET + PostgreSQL + RabbitMQ + Docker)

Este projeto implementa uma arquitetura de microserviços para gerenciamento de estoque e vendas em uma plataforma de e-commerce.

A arquitetura segue princípios de Clean Code e Clean Architecture, com separação clara de responsabilidades e comunicação assíncrona via RabbitMQ.

📂 Estrutura do Projeto
/auth-service        # Autenticação e JWT
/stock-service       # Gestão de produtos e estoque
/sales-service       # Gestão de pedidos e vendas
/api-gateway         # Gateway de entrada para clientes
/infra               # Docker Compose (bancos, rabbitmq, rede)


Cada serviço tem sua própria solução .NET (.sln) e bancos separados (PostgreSQL).

🧩 Tecnologias

.NET 8 + C#

Entity Framework Core

PostgreSQL (1 banco por serviço)

RabbitMQ (mensageria)

JWT (autenticação)

Ocelot ou YARP (API Gateway)

Docker e Docker Compose

⚙️ Funcionalidades
Auth Service

Registro de usuário

Login e emissão de JWT

Stock Service

Cadastro de produtos

Consulta de produtos e estoque

Atualização de estoque ao receber evento de venda

Sales Service

Criação de pedidos

Consulta de pedidos

Envio de evento para atualização de estoque

API Gateway

Entrada única para o cliente

Roteamento para os microserviços

Validação de autenticação

📌 Endpoints
Auth Service

POST /auth/register

POST /auth/login

Stock Service

POST /products

GET /products

GET /products/{id}

PUT /products/{id}

POST /stock/validate-reserve

Sales Service

POST /orders

GET /orders

GET /orders/{id}

🐳 Configuração com Docker
1. Infraestrutura (Postgres + RabbitMQ)

Dentro da pasta /infra crie o arquivo docker-compose.yml:

version: '3.8'
services:
  postgres-auth:
    image: postgres:15
    container_name: postgres-auth
    environment:
      POSTGRES_USER: auth_user
      POSTGRES_PASSWORD: auth_pass
      POSTGRES_DB: auth_db
    ports:
      - "5433:5432"

  postgres-stock:
    image: postgres:15
    container_name: postgres-stock
    environment:
      POSTGRES_USER: stock_user
      POSTGRES_PASSWORD: stock_pass
      POSTGRES_DB: stock_db
    ports:
      - "5434:5432"

  postgres-sales:
    image: postgres:15
    container_name: postgres-sales
    environment:
      POSTGRES_USER: sales_user
      POSTGRES_PASSWORD: sales_pass
      POSTGRES_DB: sales_db
    ports:
      - "5435:5432"

  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672" # painel


Suba com:

docker-compose up -d

2. Criar cada serviço
Auth Service
dotnet new webapi -n AuthService
cd AuthService
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.EntityFrameworkCore.Design


Exemplo de appsettings.json:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=auth_db;Username=auth_user;Password=auth_pass"
  },
  "Jwt": {
    "Key": "chave-secreta-aqui",
    "Issuer": "auth-service",
    "Audience": "ecommerce"
  }
}


Criar modelo de usuário + migrations:

dotnet ef migrations add InitialCreate -o Infrastructure/Migrations
dotnet ef database update


Rodar:

dotnet run

Stock Service
dotnet new webapi -n StockService
cd StockService
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package RabbitMQ.Client
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer


Configuração appsettings.json:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5434;Database=stock_db;Username=stock_user;Password=stock_pass"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672
  }
}


Migrations:

dotnet ef migrations add InitialCreate -o Infrastructure/Migrations
dotnet ef database update


Rodar:

dotnet run

Sales Service
dotnet new webapi -n SalesService
cd SalesService
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package RabbitMQ.Client
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer


appsettings.json:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5435;Database=sales_db;Username=sales_user;Password=sales_pass"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672
  }
}


Migrations:

dotnet ef migrations add InitialCreate -o Infrastructure/Migrations
dotnet ef database update


Rodar:

dotnet run

API Gateway
dotnet new webapi -n ApiGateway
cd ApiGateway
dotnet add package Ocelot


Crie ocelot.json:

{
  "Routes": [
    {
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [ { "Host": "localhost", "Port": 5001 } ],
      "UpstreamPathTemplate": "/auth/{everything}",
      "UpstreamHttpMethod": [ "POST" ]
    },
    {
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [ { "Host": "localhost", "Port": 5002 } ],
      "UpstreamPathTemplate": "/stock/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT" ]
    },
    {
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [ { "Host": "localhost", "Port": 5003 } ],
      "UpstreamPathTemplate": "/sales/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST" ]
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000"
  }
}


Rodar:

dotnet run

🔑 Autenticação

Registrar usuário:
POST http://localhost:5000/auth/register

Fazer login:
POST http://localhost:5000/auth/login
→ Retorna JWT

Usar token nos headers:

Authorization: Bearer <seu_token>

🧪 Testes

Adicionar pacotes:

dotnet add package xunit
dotnet add package FluentAssertions


Rodar:

dotnet test

✅ Critérios de Aceitação

Cadastro de produtos no Stock Service

Criação de pedidos no Sales Service com validação de estoque

Comunicação entre serviços via RabbitMQ

API Gateway roteando chamadas

JWT protegendo endpoints

🚀 Como rodar tudo

Subir infraestrutura:

cd infra
docker-compose up -d


Rodar cada serviço:

cd auth-service && dotnet run
cd stock-service && dotnet run
cd sales-service && dotnet run
cd api-gateway && dotnet run


Testar no Postman / Insomnia:

Criar usuário → Login → Criar produto → Criar pedido

# infra 

a pasta infra/ com o docker-compose.yml que sobe Postgres (um por serviço) e o RabbitMQ.
Mas ainda não detalhei os Dockerfiles de cada microserviço e nem mostrei como integrar tudo no mesmo compose para rodar só com docker-compose up.

Se quiser, podemos deixar a pasta assim:

/auth-service
   /Dockerfile
/stock-service
   /Dockerfile
/sales-service
   /Dockerfile
/api-gateway
   /Dockerfile
/infra
   /docker-compose.yml


Aí no docker-compose.yml a gente sobe:

postgres-auth, postgres-stock, postgres-sales

rabbitmq

auth-service, stock-service, sales-service, api-gateway

👉 Assim você roda tudo de uma vez com:

docker-compose up --build


e já acessa o sistema.