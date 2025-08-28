# Biblioteca de Mensageria (Messaging)

Esta biblioteca fornece uma abstração robusta e fácil de usar para comunicação assíncrona entre microsserviços utilizando RabbitMQ. Ela foi desenhada para simplificar a publicação e o consumo de eventos com boas práticas incorporadas.

## Funcionalidades

- **Gerenciamento de Conexão Centralizado**: Utiliza uma conexão singleton para toda a aplicação, evitando a sobrecarga de múltiplas conexões.
- **Publicação de Eventos Simplificada**: Interface `IMessagePublisher` para publicar eventos de forma fácil e consistente.
- **Consumidores como Serviços de Background**: Classe base `QueueConsumerBackgroundService` para criar consumidores de fila como serviços hospedados (`IHostedService`), gerenciados pelo host da aplicação.
- **Configuração via Injeção de Dependência**: Registro simplificado dos serviços da biblioteca no contêiner de DI com um único método de extensão.
- **Tratamento de Erros (DLQ)**: Suporte automático a Dead Letter Queue (DLQ) para mensagens que falham no processamento.
- **Eventos de Domínio Pré-definidos**: Um conjunto de eventos comuns para cenários de microsserviços.

## Configuração

### 1. Adicionar Referência ao Projeto

Adicione a referência ao projeto `Messaging` no seu arquivo `.csproj`:

```xml
<ProjectReference Include="../../shared/Messaging/Messaging/Messaging.csproj" />
```

### 2. Configurar o RabbitMQ no `appsettings.json`

Adicione a seção de configuração do RabbitMQ. O `QueuePrefix` é usado para nomear as filas de forma padronizada (e.g., `sales.order_created`).

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "microservices_exchange",
    "QueuePrefix": "servicename."
  }
}
```

### 3. Registrar Serviços no `Program.cs`

Use o método de extensão `AddRabbitMqMessaging` para registrar todos os componentes necessários da biblioteca.

```csharp
using Messaging;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços de mensageria
builder.Services.AddRabbitMqMessaging(builder.Configuration);

// ... outros serviços

var app = builder.Build();
```

## Uso

### Publicação de Eventos

Injete `IMessagePublisher` em seus controllers ou serviços para publicar eventos.

```csharp
public class MyController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;

    public MyController(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order newOrder)
    {
        // ... lógica para salvar o pedido ...

        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = newOrder.Id,
            // ... outros dados
        };

        // Publica o evento. A routing key será 'ordercreated'.
        await _messagePublisher.PublishAsync(orderCreatedEvent);

        return Ok();
    }
}
```

### Consumo de Eventos

Para consumir eventos, crie uma classe que herde de `QueueConsumerBackgroundService` e a registre como um serviço hospedado.

**1. Crie a classe do consumidor:**

```csharp
using Messaging;
using System.Text.Json;

// Exemplo de um consumidor no StockService
public class OrderCreatedConsumer : QueueConsumerBackgroundService
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    // O nome da fila a ser consumida.
    protected override string QueueName => "stock.order_created";

    public OrderCreatedConsumer(
        ILogger<OrderCreatedConsumer> logger,
        IRabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqSettings> settings) 
        : base(logger, connectionManager, settings)
    {
        _logger = logger;
    }

    // Lógica para processar a mensagem.
    protected override async Task<bool> HandleMessageAsync(string message, IBasicProperties properties)
    {
        _logger.LogInformation("Processando evento de criação de pedido.");
        var eventData = JsonSerializer.Deserialize<OrderCreatedEvent>(message);

        // ... lógica para reservar o estoque ...

        // Retorne 'true' se a mensagem foi processada com sucesso.
        // Retorne 'false' para enviá-la para a Dead Letter Queue (DLQ).
        return true;
    }
}
```

**2. Registre o consumidor no `Program.cs`:**

```csharp
// Adiciona o consumidor como um serviço de background
builder.Services.AddHostedService<OrderCreatedConsumer>();
```

## Implementação Técnica

- **`RabbitMqConnectionManager`**: Um serviço singleton que gerencia uma única conexão persistente e resiliente com o RabbitMQ.
- **`RabbitMqPublisher`**: Implementa `IMessagePublisher` e utiliza a conexão gerenciada para criar um canal e publicar mensagens. É registrado como singleton.
- **`QueueConsumerBackgroundService`**: Uma classe base abstrata que herda de `BackgroundService`. Ela lida com toda a complexidade de criar um consumidor (canal, declaração de fila, DLQ, loop de consumo, acks/nacks).
- **`ServiceCollectionExtensions`**: Fornece o método `AddRabbitMqMessaging` para uma configuração de DI limpa e centralizada.

## Próximos Passos

1. **Implementar Consumidores**: Criar as classes de consumidores nos microsserviços apropriados.
2. **Monitoramento**: Adicionar logging e métricas detalhadas para observar a saúde do sistema de mensageria.
3. **Testes de Integração**: Criar testes que validem a comunicação de ponta a ponta entre os serviços através do RabbitMQ.
