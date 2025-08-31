# Auth Service - Microserviços E-commerce

Este é o serviço de autenticação (Auth Service) do projeto de microserviços para gerenciamento de estoque e vendas em uma plataforma de e-commerce.

## Passo a Passo da Criação

### 1. Criação da Estrutura de Pastas
No diretório raiz do projeto (`/home/leandro/Imagens/micro`), execute:
```bash
mkdir -p auth-service stock-service sales-service api-gateway infra
```

### 2. Criação do Projeto WebAPI
Navegue para a pasta `auth-service` e crie o projeto:
```bash
cd auth-service
dotnet new webapi -n AuthService
```

Isso cria uma pasta `AuthService` dentro de `auth-service` com o projeto .NET 8.0.

### 3. Adição de Pacotes Necessários
Navegue para a pasta do projeto e adicione os pacotes:
```bash
cd AuthService
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.8
dotnet add package Microsoft.EntityFrameworkCore.Design
```

**Nota:** A versão do `Microsoft.AspNetCore.Authentication.JwtBearer` foi especificada como 8.0.8 para compatibilidade com .NET 8.0. A versão mais recente (9.0.x) requer .NET 9.0.

### 4. Verificação dos Pacotes Instalados
Para verificar os pacotes instalados:
```bash
dotnet list package
```

Pacotes atuais:
- Microsoft.AspNetCore.Authentication.JwtBearer: 8.0.8
- Microsoft.EntityFrameworkCore: 9.0.8
- Npgsql.EntityFrameworkCore.PostgreSQL: 9.0.4
- Microsoft.EntityFrameworkCore.Design: (incluído automaticamente)
- Outros pacotes padrão do template WebAPI

## Próximos Passos
- Configurar `appsettings.json` com strings de conexão para PostgreSQL e configurações de JWT.
- Criar modelos de usuário e contexto do Entity Framework.
- Implementar endpoints para registro e login.
- Configurar autenticação JWT no `Program.cs`.

## Tecnologias Utilizadas
- .NET 8.0
- ASP.NET Core WebAPI
- Entity Framework Core
- PostgreSQL
- JWT para autenticação

## Estrutura do Projeto
```
auth-service/
├── AuthService/
│   ├── AuthService.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── AuthService.http
│   ├── Properties/
│   │   └── launchSettings.json
│   └── obj/
│       └── ...
└── README.md (este arquivo)
```</content>
<parameter name="filePath">/home/leandro/Imagens/micro/auth-service/README.md
