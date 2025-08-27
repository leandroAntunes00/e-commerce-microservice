# Testes - Auth Service

Este documento descreve a estrutura completa de testes criada para o Auth Service, incluindo testes unitários, de integração e E2E (end-to-end).

## Estrutura de Testes

### 1. Testes Unitários (`AuthService.UnitTests`)
**Localização**: `auth-service/AuthService/AuthService.UnitTests/`

**Propósito**: Testar unidades individuais de código de forma isolada, usando mocks para dependências externas.

**Tecnologias**:
- xUnit: Framework de testes
- Moq: Para criação de mocks
- FluentAssertions: Para asserções mais legíveis

**Exemplo de teste**:
```csharp
[Fact]
public void HealthCheck_ShouldReturnOk()
{
    // Arrange
    var controller = new TestController();

    // Act
    var result = controller.HealthCheck();

    // Assert
    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
    okResult.Value.Should().Be("Auth Service is running!");
}
```

### 2. Testes de Integração (`AuthService.IntegrationTests`)
**Localização**: `auth-service/AuthService/AuthService.IntegrationTests/`

**Propósito**: Testar a integração entre componentes, usando o banco de dados real e serviços externos.

**Tecnologias**:
- xUnit: Framework de testes
- Microsoft.AspNetCore.Mvc.Testing: Para testar controllers
- Testcontainers.PostgreSql: Para criar containers PostgreSQL temporários

**Exemplo de teste**:
```csharp
[Fact]
public async Task HealthCheck_ShouldReturnOk()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/health");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### 3. Testes E2E (`AuthService.E2ETests`)
**Localização**: `auth-service/AuthService/AuthService.E2ETests/`

**Propósito**: Testar o sistema completo de ponta a ponta, simulando o uso real da aplicação.

**Tecnologias**:
- xUnit: Framework de testes
- Flurl.Http: Para fazer requisições HTTP
- Flurl.Http.Testing: Para testar requisições HTTP

**Exemplo de teste**:
```csharp
[Fact]
public async Task HealthCheck_ShouldReturnSuccess_WhenServiceIsRunning()
{
    // Arrange
    var baseUrl = "http://localhost:5051";

    // Act
    var response = await baseUrl
        .AppendPathSegment("health")
        .GetAsync();

    // Assert
    response.StatusCode.Should().Be((int)HttpStatusCode.OK);
}
```

## Como Executar os Testes

### Executar Todos os Testes
```bash
cd auth-service/AuthService
dotnet test
```

### Executar Apenas Testes Unitários
```bash
cd auth-service/AuthService/AuthService.UnitTests
dotnet test
```

### Executar Apenas Testes de Integração
```bash
cd auth-service/AuthService/AuthService.IntegrationTests
dotnet test
```

### Executar Apenas Testes E2E
```bash
cd auth-service/AuthService/AuthService.E2ETests
dotnet test
```

## Pré-requisitos para Testes

### Testes Unitários
- .NET 8.0 SDK
- Pacotes NuGet já instalados automaticamente

### Testes de Integração
- .NET 8.0 SDK
- Docker (para Testcontainers)
- Pacotes NuGet já instalados automaticamente

### Testes E2E
- .NET 8.0 SDK
- Auth Service rodando na porta 5051
- Pacotes NuGet já instalados automaticamente

## Configuração dos Testes

### Testes Unitários
Não requerem configuração especial. Usam mocks para todas as dependências.

### Testes de Integração
Usam Testcontainers para criar bancos PostgreSQL temporários. A configuração é feita automaticamente no código de teste.

### Testes E2E
Requerem que o Auth Service esteja rodando. Para executar:
```bash
cd auth-service/AuthService
dotnet run
```
Em outro terminal:
```bash
cd auth-service/AuthService/AuthService.E2ETests
dotnet test
```

## Cobertura de Testes

### Funcionalidades Testadas
- ✅ Health Check endpoint
- ✅ Swagger UI accessibility
- ✅ WeatherForecast endpoint (exemplo padrão)
- 🔄 Autenticação (a implementar)
- 🔄 Autorização (a implementar)
- 🔄 Registro de usuários (a implementar)
- 🔄 Login (a implementar)

### Tipos de Cenários
- ✅ Cenários de sucesso
- ✅ Cenários de erro
- 🔄 Cenários de edge case
- 🔄 Cenários de segurança

## Próximos Passos

1. **Implementar funcionalidades reais** no Auth Service
2. **Criar testes** para autenticação e autorização
3. **Adicionar testes** para cenários de erro e edge cases
4. **Configurar CI/CD** para execução automática dos testes
5. **Replicar estrutura** para os outros serviços (Stock, Sales, API Gateway)

## Boas Práticas Implementadas

- ✅ Separação clara entre tipos de teste
- ✅ Uso de mocks para isolamento em testes unitários
- ✅ Testes de integração com containers temporários
- ✅ Testes E2E simulando uso real
- ✅ Convenções de nomenclatura claras
- ✅ Asserções legíveis com FluentAssertions
- ✅ Configuração automática de dependências

## Estrutura de Diretórios

```
auth-service/
├── AuthService/
│   ├── AuthService.UnitTests/
│   │   ├── AuthService.UnitTests.csproj
│   │   └── UnitTest1.cs
│   ├── AuthService.IntegrationTests/
│   │   ├── AuthService.IntegrationTests.csproj
│   │   └── IntegrationTest1.cs
│   └── AuthService.E2ETests/
│       ├── AuthService.E2ETests.csproj
│       └── E2ETest1.cs
└── README.md
```</content>
<parameter name="filePath">/home/leandro/Imagens/micro/auth-service/README_TESTS.md
