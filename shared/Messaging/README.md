# Biblioteca Messaging

Esta biblioteca fornece uma implementação simples e robusta para comunicação assíncrona entre microsserviços usando RabbitMQ.

## Funcionalidades

- **Publicação de Eventos**: Permite publicar eventos para um exchange RabbitMQ
- **Consumo de Mensagens**: Permite consumir mensagens de filas RabbitMQ
- **Eventos Definidos**: Conjunto de eventos de domínio pré-definidos
- **Configuração Flexível**: Configuração via appsettings.json

## Configuração

### 1. Adicionar referência ao projeto

```xml
<ProjectReference Include="../../shared/Messaging/Messaging/Messaging.csproj" />
```

### 2. Configurar RabbitMQ no appsettings.json

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "microservices_exchange",
    "QueuePrefix": "service_name_"
  }
}
```

### 3. Registrar serviços no Program.cs

```csharp
using Messaging;

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
```

## Uso

### Publicação de Eventos

```csharp
public class MyController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;

    public MyController(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // Lógica de criação do pedido...

        // Publicar evento
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            UserId = userId,
            Items = order.Items.Select(i => new OrderItemEvent
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        };

        await _messagePublisher.PublishAsync(orderCreatedEvent);

        return Ok();
    }
}
```

### Eventos Disponíveis

#### OrderCreatedEvent
Disparado quando um novo pedido é criado.
- `OrderId`: ID do pedido
- `UserId`: ID do usuário
- `Items`: Lista de itens do pedido
- `TotalAmount`: Valor total
- `CreatedAt`: Data de criação

#### OrderCancelledEvent
Disparado quando um pedido é cancelado.
- `OrderId`: ID do pedido
- `UserId`: ID do usuário
- `Items`: Lista de itens do pedido
- `CancelledAt`: Data de cancelamento

#### StockUpdatedEvent
Disparado quando o estoque de um produto é atualizado.
- `ProductId`: ID do produto
- `ProductName`: Nome do produto
- `PreviousStock`: Estoque anterior
- `NewStock`: Novo estoque
- `Operation`: Tipo de operação ("Reserved", "Released", "Updated")
- `UpdatedAt`: Data de atualização

## Implementação Técnica

A biblioteca usa reflexão para acessar os tipos do RabbitMQ.Client dinamicamente, evitando problemas de compatibilidade de versão entre diferentes versões do pacote RabbitMQ.Client.

### RabbitMqPublisher
- Implementa `IMessagePublisher`
- Publica mensagens para um exchange do tipo "direct"
- Serializa eventos usando System.Text.Json

### RabbitMqConsumer
- Implementa `IMessageConsumer`
- Consome mensagens de filas específicas
- Desserializa mensagens automaticamente

## Exemplo de Uso Completo

### SalesService
O SalesService foi configurado para publicar eventos `OrderCreatedEvent` quando um pedido é criado com sucesso.

### StockService
O StockService foi configurado para publicar eventos `StockUpdatedEvent` quando o estoque de um produto é atualizado.

## Próximos Passos

1. **Implementar Consumidores**: Criar consumidores nos serviços apropriados para processar os eventos
2. **Tratamento de Erros**: Implementar retry e dead letter queues
3. **Monitoramento**: Adicionar logging e métricas
4. **Testes**: Criar testes de integração para a comunicação entre serviços

## Dependências

- RabbitMQ.Client (>= 6.0.0)
- System.Text.Json
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection</content>
<parameter name="filePath">/home/leandro/Imagens/micro/shared/Messaging/README.md
