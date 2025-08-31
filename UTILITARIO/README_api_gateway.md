# API Gateway - Microserviços E-commerce

Este é o serviço de API Gateway do projeto de microserviços para gerenciamento de estoque e vendas em uma plataforma de e-commerce. Ele atua como ponto de entrada único para os clientes, roteando requisições para os microserviços adequados e validando autenticação JWT.

## Passo a Passo da Criação

### 1. Criação da Estrutura de Pastas
A estrutura de pastas foi criada anteriormente no diretório raiz (`/home/leandro/Imagens/micro`):
```bash
mkdir -p auth-service stock-service sales-service api-gateway infra
```

### 2. Criação do Projeto WebAPI
Navegue para a pasta `api-gateway` e crie o projeto:
```bash
cd api-gateway
dotnet new webapi -n ApiGateway
```

Isso cria uma pasta `ApiGateway` dentro de `api-gateway` com o projeto .NET 8.0.

### 3. Adição de Pacotes Necessários
Navegue para a pasta do projeto e adicione os pacotes:
```bash
cd ApiGateway
dotnet add package YARP.ReverseProxy
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.8
```

**Nota:** A versão do `Microsoft.AspNetCore.Authentication.JwtBearer` foi especificada como 8.0.8 para compatibilidade com .NET 8.0. O `YARP.ReverseProxy` é usado para roteamento de requisições para os microserviços.

### 4. Verificação dos Pacotes Instalados
Para verificar os pacotes instalados:
```bash
dotnet list package
```

Pacotes atuais:
- YARP.ReverseProxy: 2.3.0
- Microsoft.AspNetCore.Authentication.JwtBearer: 8.0.8
- Outros pacotes padrão do template WebAPI

## Próximos Passos
- Configurar `appsettings.json` com as rotas para os microserviços (auth-service, stock-service, sales-service).
- Configurar YARP no `Program.cs` para roteamento reverso.
- Configurar autenticação JWT para validar tokens antes de rotear requisições.
- Configurar CORS se necessário para permitir requisições do frontend.
- Testar o roteamento para cada serviço.

## Tecnologias Utilizadas
- .NET 8.0
- ASP.NET Core WebAPI
- YARP (Yet Another Reverse Proxy) para roteamento
- JWT para autenticação

## Funcionalidades Planejadas
- Roteamento de requisições para os microserviços adequados
- Validação de autenticação JWT
- Agregação de respostas se necessário
- Rate limiting e segurança

## Configuração de Rotas (Exemplo)
No `appsettings.json`, configurar as rotas para cada serviço:
```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": {
        "ClusterId": "auth-cluster",
        "Match": {
          "Path": "/auth/{**catch-all}"
        }
      },
      "stock-route": {
        "ClusterId": "stock-cluster",
        "Match": {
          "Path": "/stock/{**catch-all}"
        }
      },
      "sales-route": {
        "ClusterId": "sales-cluster",
        "Match": {
          "Path": "/sales/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "auth-cluster": {
        "Destinations": {
          "auth-service": {
            "Address": "http://localhost:5001"
          }
        }
      },
      "stock-cluster": {
        "Destinations": {
          "stock-service": {
            "Address": "http://localhost:5002"
          }
        }
      },
      "sales-cluster": {
        "Destinations": {
          "sales-service": {
            "Address": "http://localhost:5003"
          }
        }
      }
    }
  }
}
```

## Estrutura do Projeto
```
api-gateway/
├── ApiGateway/
│   ├── ApiGateway.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── ApiGateway.http
│   ├── Properties/
│   │   └── launchSettings.json
│   └── obj/
│       └── ...
└── README.md (este arquivo)
```</content>
<parameter name="filePath">/home/leandro/Imagens/micro/api-gateway/README.md
