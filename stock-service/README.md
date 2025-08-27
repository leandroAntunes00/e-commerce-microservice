# Stock Service - Microserviços E-commerce

Este é o serviço de gestão de estoque (Stock Service) do projeto de microserviços para gerenciamento de estoque e vendas em uma plataforma de e-commerce.

## Passo a Passo da Criação

### 1. Criação da Estrutura de Pastas
A estrutura de pastas foi criada anteriormente no diretório raiz (`/home/leandro/Imagens/micro`):
```bash
mkdir -p auth-service stock-service sales-service api-gateway infra
```

### 2. Criação do Projeto WebAPI
Navegue para a pasta `stock-service` e crie o projeto:
```bash
cd stock-service
dotnet new webapi -n StockService
```

Isso cria uma pasta `StockService` dentro de `stock-service` com o projeto .NET 8.0.

### 3. Adição de Pacotes Necessários
Navegue para a pasta do projeto e adicione os pacotes:
```bash
cd StockService
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package RabbitMQ.Client
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.8
dotnet add package Microsoft.EntityFrameworkCore.Design
```

**Nota:** A versão do `Microsoft.AspNetCore.Authentication.JwtBearer` foi especificada como 8.0.8 para compatibilidade com .NET 8.0. O `RabbitMQ.Client` foi adicionado para comunicação assíncrona com outros serviços.

### 4. Verificação dos Pacotes Instalados
Para verificar os pacotes instalados:
```bash
dotnet list package
```

Pacotes atuais:
- Microsoft.AspNetCore.Authentication.JwtBearer: 8.0.8
- Microsoft.EntityFrameworkCore: 9.0.8
- Npgsql.EntityFrameworkCore.PostgreSQL: 9.0.4
- RabbitMQ.Client: 7.1.2
- Microsoft.EntityFrameworkCore.Design: 9.0.8
- Outros pacotes padrão do template WebAPI

## Próximos Passos
- Configurar `appsettings.json` com strings de conexão para PostgreSQL, configurações de RabbitMQ e JWT.
- Criar modelos de produto e contexto do Entity Framework.
- Implementar endpoints para cadastro, consulta e atualização de produtos/estoque.
- Configurar comunicação via RabbitMQ para receber eventos de vendas.
- Configurar autenticação JWT no `Program.cs`.

## Tecnologias Utilizadas
- .NET 8.0
- ASP.NET Core WebAPI
- Entity Framework Core
- PostgreSQL
- RabbitMQ (para comunicação assíncrona)
- JWT para autenticação

## Funcionalidades Planejadas
- Cadastro de produtos (nome, descrição, preço, quantidade em estoque)
- Consulta de produtos e estoque disponível
- Atualização de estoque ao receber evento de venda via RabbitMQ
- Validação de disponibilidade de estoque para vendas

## Estrutura do Projeto
```
stock-service/
├── StockService/
│   ├── StockService.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── StockService.http
│   ├── Properties/
│   │   └── launchSettings.json
│   └── obj/
│       └── ...
└── README.md (este arquivo)
```</content>
<parameter name="filePath">/home/leandro/Imagens/micro/stock-service/README.md
