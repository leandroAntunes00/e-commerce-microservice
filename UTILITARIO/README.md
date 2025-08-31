# Sales Service - Microserviços E-commerce

Este é o serviço de gestão de vendas (Sales Service) do projeto de microserviços para gerenciamento de estoque e vendas em uma plataforma de e-commerce.

## Passo a Passo da Criação

### 1. Criação da Estrutura de Pastas
A estrutura de pastas foi criada anteriormente no diretório raiz (`/home/leandro/Imagens/micro`):
```bash
mkdir -p auth-service stock-service sales-service api-gateway infra
```

### 2. Criação do Projeto WebAPI
Navegue para a pasta `sales-service` e crie o projeto:
```bash
cd sales-service
dotnet new webapi -n SalesService
```

Isso cria uma pasta `SalesService` dentro de `sales-service` com o projeto .NET 8.0.

### 3. Adição de Pacotes Necessários
Navegue para a pasta do projeto e adicione os pacotes:
```bash
cd SalesService
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package RabbitMQ.Client
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.8
dotnet add package Microsoft.EntityFrameworkCore.Design
```

**Nota:** A versão do `Microsoft.AspNetCore.Authentication.JwtBearer` foi especificada como 8.0.8 para compatibilidade com .NET 8.0. O `RabbitMQ.Client` foi adicionado para comunicação assíncrona com o Stock Service.

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
- Criar modelos de pedido (Order) e contexto do Entity Framework.
- Implementar endpoints para criação de pedidos e consulta de pedidos.
- Configurar comunicação via RabbitMQ para enviar eventos de venda ao Stock Service.
- Configurar autenticação JWT no `Program.cs`.
- Integrar com o Stock Service para validar disponibilidade de estoque antes de confirmar vendas.

## Tecnologias Utilizadas
- .NET 8.0
- ASP.NET Core WebAPI
- Entity Framework Core
- PostgreSQL
- RabbitMQ (para comunicação assíncrona)
- JWT para autenticação

## Funcionalidades Planejadas
- Criação de pedidos de venda com validação de estoque
- Consulta de pedidos realizados
- Envio de evento via RabbitMQ para atualização de estoque no Stock Service
- Integração com Auth Service para autenticação de usuários

## Estrutura do Projeto
```
sales-service/
├── SalesService/
│   ├── SalesService.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── SalesService.http
│   ├── Properties/
│   │   └── launchSettings.json
│   └── obj/
│       └── ...
└── README.md (este arquivo)
```</content>
<parameter name="filePath">/home/leandro/Imagens/micro/sales-service/README.md
