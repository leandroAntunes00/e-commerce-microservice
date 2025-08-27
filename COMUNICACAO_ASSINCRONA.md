# Comunicação Assíncrona em Microsserviços

## 📋 Visão Geral

Esta documentação explica como implementamos a **comunicação assíncrona** entre microsserviços usando **RabbitMQ** como message broker. A arquitetura permite que os serviços se comuniquem de forma **desacoplada**, **escalável** e **resiliente**.

## 🏗️ Arquitetura Implementada

```
┌─────────────────┐    OrderCreatedEvent     ┌─────────────────┐
│   SalesService  │ ──────────────────────► │  RabbitMQ       │
│                 │                          │  Exchange       │
│ • Cria pedidos  │                          │                 │
│ • Publica       │                          │ • order_created │
│   eventos       │                          │ • order_cancelled│
└─────────────────┘                          └─────────┬───────┘
                                                      │
                                                      ▼
┌─────────────────┐    Consome Eventos       ┌─────────────────┐
│ StockService    │ ◄─────────────────────── │  RabbitMQ       │
│                 │                          │  Queues         │
│ • Reserva       │                          │                 │
│   estoque       │                          │ • stock_service_│
│ • Processa      │                          │   order_created │
│   automaticamente│                         │ • stock_service_│
└─────────────────┘                          │   order_cancelled│
                                             └─────────────────┘
```

## 🔄 Como Funciona a Comunicação Assíncrona

### **1. Fluxo Básico de Comunicação**

```
1. Usuário cria pedido no SalesService
2. SalesService salva pedido no banco
3. SalesService publica OrderCreatedEvent no RabbitMQ
4. StockService consome o evento automaticamente
5. StockService reserva estoque e atualiza banco
6. StockService publica StockUpdatedEvent
```

### **2. Componentes Principais**

#### **📨 Publisher (Publicador)**
- **Responsabilidade**: Envia mensagens para o RabbitMQ
- **Implementação**: `RabbitMqPublisher` na biblioteca Messaging
- **Localização**: SalesService e StockService

#### **📥 Consumer (Consumidor)**
- **Responsabilidade**: Recebe e processa mensagens do RabbitMQ
- **Implementação**: `RabbitMqConsumer` + `OrderEventConsumerService`
- **Localização**: StockService (consome eventos de pedido)

#### **🗂️ Exchange**
- **Tipo**: Direct Exchange
- **Nome**: `microservices_exchange`
- **Função**: Roteia mensagens para as filas corretas

#### **📋 Queues**
- **Padrão de Nome**: `{service_prefix}_{event_type}`
- **Exemplos**:
  - `stock_service_order_created`
  - `stock_service_order_cancelled`
  - `sales_service_stock_updated`

## 📝 Eventos Implementados

### **OrderCreatedEvent**
```csharp
public class OrderCreatedEvent : BaseEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public List<OrderItemEvent> Items { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### **OrderCancelledEvent**
```csharp
public class OrderCancelledEvent : BaseEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public List<OrderItemEvent> Items { get; set; }
    public DateTime CancelledAt { get; set; }
}
```

### **StockUpdatedEvent**
```csharp
public class StockUpdatedEvent : BaseEvent
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public string Operation { get; set; } // "Reserved", "Released", "Updated"
    public DateTime UpdatedAt { get; set; }
}
```

## 🔧 Configuração Técnica

### **1. RabbitMQ Settings**
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "microservices_exchange",
    "QueuePrefix": "stock_service_"
  }
}
```

### **2. Injeção de Dependência**
```csharp
// Program.cs
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<IMessageConsumer, RabbitMqConsumer>();
builder.Services.AddHostedService<OrderEventConsumerService>();
```

## 🚀 Exemplo Prático

### **Cenário: Usuário cria um pedido**

#### **Passo 1: SalesService recebe o pedido**
```csharp
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
{
    // 1. Salva pedido no banco
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();

    // 2. Publica evento
    var orderCreatedEvent = new OrderCreatedEvent
    {
        OrderId = order.Id,
        UserId = userId,
        Items = order.Items.Select(i => new OrderItemEvent { ... }).ToList(),
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt
    };

    await _messagePublisher.PublishAsync(orderCreatedEvent);

    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, result);
}
```

#### **Passo 2: StockService processa automaticamente**
```csharp
public async Task ProcessOrderCreatedEvent(OrderCreatedEvent orderEvent)
{
    foreach (var item in orderEvent.Items)
    {
        var product = await _context.Products.FindAsync(item.ProductId);
        if (product.StockQuantity >= item.Quantity)
        {
            // Reserva estoque
            product.StockQuantity -= item.Quantity;
            await _context.SaveChangesAsync();

            // Publica evento de atualização
            var stockEvent = new StockUpdatedEvent { ... };
            await _messagePublisher.PublishAsync(stockEvent);
        }
    }
}
```

## 🛡️ Recursos Avançados

### **1. Dead Letter Queue (DLQ)**
- **Função**: Armazena mensagens que falharam no processamento
- **Nome**: `{queue_name}.dlq`
- **Headers**: Contém informações de erro e timestamp

### **2. Logging Estruturado**
```csharp
_logger.LogInformation("Evento publicado com sucesso. Tipo: {EventType}, Tamanho: {Size} bytes",
    message.EventType, body.Length);

_logger.LogError(ex, "Mensagem movida para DLQ. DeliveryTag: {DeliveryTag}", deliveryTag);
```

### **3. Confirmação de Mensagens**
- **ACK**: Confirma processamento bem-sucedido
- **NACK**: Rejeita mensagem com erro
- **QoS**: Processa uma mensagem por vez

## 📊 Monitoramento

### **Logs de Exemplo**
```
[INFO] RabbitMqPublisher: Evento publicado com sucesso. Tipo: OrderCreated, Tamanho: 256 bytes
[INFO] OrderEventConsumerService: Processando pedido criado: OrderId=123, UserId=456
[INFO] OrderEventConsumerService: Estoque reservado para produto Laptop: 50 -> 45
[INFO] RabbitMqPublisher: Evento publicado com sucesso. Tipo: StockUpdated, Tamanho: 128 bytes
```

### **RabbitMQ Management**
- **URL**: http://localhost:15672
- **Credenciais**: guest/guest
- **Monitoramento**: Exchanges, Queues, Messages

## 🎯 Benefícios da Comunicação Assíncrona

### **1. Desacoplamento**
- Serviços não dependem diretamente uns dos outros
- Mudanças em um serviço não afetam os outros
- Facilita desenvolvimento independente

### **2. Escalabilidade**
- Serviços podem ser escalados independentemente
- Load balancing automático via RabbitMQ
- Processamento assíncrono não bloqueia recursos

### **3. Resiliência**
- Sistema continua funcionando mesmo com falhas
- Mensagens são persistidas no RabbitMQ
- Retry automático e DLQ para tratamento de erros

### **4. Flexibilidade**
- Novos consumidores podem ser adicionados facilmente
- Roteamento flexível de mensagens
- Suporte a múltiplos tipos de evento

## 🔄 Padrões Implementados

### **1. Event-Driven Architecture**
- Comunicação baseada em eventos
- Publishers não conhecem os consumers
- Acoplamento loose entre serviços

### **2. Message Broker Pattern**
- RabbitMQ como intermediário
- Roteamento inteligente de mensagens
- Garantia de entrega

### **3. Background Processing**
- Consumers rodam como serviços em background
- Processamento não bloqueante
- Lifecycle management automático

## 🚀 Próximos Passos

### **Expansão Sugerida**
1. **NotificationService**: Consumir eventos para enviar emails/SMS
2. **AuditService**: Registrar todos os eventos para auditoria
3. **AnalyticsService**: Processar eventos para métricas de negócio
4. **InventoryService**: Gerenciar reabastecimento automático

### **Melhorias Técnicas**
1. **Retry Policy**: Implementar retry com backoff exponencial
2. **Circuit Breaker**: Proteger contra falhas em cascata
3. **Message Encryption**: Criptografar mensagens sensíveis
4. **Monitoring**: Métricas e alertas com Prometheus/Grafana

## 📚 Referências

- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Event-Driven Architecture](https://microservices.io/patterns/data/event-driven-architecture.html)
- [Message Broker Pattern](https://microservices.io/patterns/communication-style/messaging.html)

## 🧪 Como Testar a Implementação

### **Pré-requisitos**
```bash
# 1. Instalar Docker
# 2. Instalar .NET 8.0 SDK
# 3. Clonar o projeto
```

### **Passo 1: Iniciar RabbitMQ**
```bash
# Executar RabbitMQ via Docker
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:management

# Verificar se está rodando
docker ps | grep rabbitmq
```

### **Passo 2: Configurar os Serviços**
```bash
# 1. SalesService - Porta 5125
cd /home/leandro/Imagens/micro/sales-service/SalesService
dotnet run

# 2. StockService - Porta 5126 (em outro terminal)
cd /home/leandro/Imagens/micro/stock-service/StockService
dotnet run
```

### **Passo 3: Testar Comunicação Assíncrona**

#### **Cenário 1: Criar Pedido**
```bash
# Criar um pedido via API
curl -X POST http://localhost:5125/api/sales/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "items": [
      {
        "productId": 1,
        "productName": "Produto Teste",
        "unitPrice": 10.00,
        "quantity": 2
      }
    ],
    "notes": "Pedido de teste"
  }'
```

#### **Cenário 2: Verificar Logs**
```bash
# Logs do SalesService (Terminal 1)
[INFO] RabbitMqPublisher: Evento publicado com sucesso. Tipo: OrderCreated, Tamanho: 256 bytes

# Logs do StockService (Terminal 2)
[INFO] OrderEventConsumerService: Processando pedido criado: OrderId=123, UserId=456
[INFO] OrderEventConsumerService: Estoque reservado para produto Produto Teste: 100 -> 98
[INFO] RabbitMqPublisher: Evento publicado com sucesso. Tipo: StockUpdated, Tamanho: 128 bytes
```

#### **Cenário 3: Verificar RabbitMQ**
```bash
# Acessar interface web do RabbitMQ
open http://localhost:15672

# Credenciais: guest/guest

# Verificar:
# - Exchanges: microservices_exchange
# - Queues: stock_service_order_created, stock_service_order_cancelled
# - Messages: Histórico de mensagens processadas
```

### **Passo 4: Testar Tratamento de Erros**
```bash
# 1. Parar o StockService
# 2. Criar um pedido no SalesService
# 3. Verificar que a mensagem fica na fila
# 4. Reiniciar o StockService
# 5. Verificar que a mensagem é processada
```

## 🔍 Debugging e Troubleshooting

### **Ferramentas Úteis**
```bash
# Ver logs detalhados
dotnet run --verbosity detailed

# Verificar conexões RabbitMQ
docker exec rabbitmq rabbitmqctl list_connections

# Verificar filas
docker exec rabbitmq rabbitmqctl list_queues

# Ver mensagens não consumidas
docker exec rabbitmq rabbitmqctl list_queue_bindings
```

### **Problemas Comuns**

#### **1. Mensagem não é consumida**
```bash
# Verificar se a fila existe
docker exec rabbitmq rabbitmqctl list_queues

# Verificar bindings
docker exec rabbitmq rabbitmqctl list_bindings
```

#### **2. Erro de conexão RabbitMQ**
```bash
# Verificar se RabbitMQ está rodando
docker ps | grep rabbitmq

# Verificar logs do RabbitMQ
docker logs rabbitmq
```

#### **3. Mensagens na DLQ**
```bash
# Verificar mensagens na Dead Letter Queue
docker exec rabbitmq rabbitmqctl list_queues | grep dlq

# Inspecionar mensagem com erro
# Acesse: http://localhost:15672 > Queues > {queue_name}.dlq
```

## 📈 Métricas e Monitoramento

### **RabbitMQ Management**
- **URL**: http://localhost:15672
- **Métricas Disponíveis**:
  - Taxa de mensagens publicadas/consumidas
  - Número de conexões ativas
  - Status das filas
  - Taxa de erro

### **Logs Estruturados**
```json
{
  "Timestamp": "2025-01-26T10:30:00Z",
  "Level": "Information",
  "Message": "Evento publicado com sucesso",
  "Properties": {
    "EventType": "OrderCreated",
    "Size": 256,
    "DeliveryTag": 12345
  }
}
```

---

## 🚀 Expansão e Melhorias Futuras

### **1. Implementar Saga Pattern**
```csharp
// Exemplo de implementação futura
public class OrderSagaOrchestrator
{
    public async Task ProcessOrderSagaAsync(OrderCreatedEvent orderEvent)
    {
        // 1. Reservar estoque
        await _stockService.ReserveStockAsync(orderEvent);

        // 2. Processar pagamento
        await _paymentService.ProcessPaymentAsync(orderEvent);

        // 3. Confirmar pedido
        await _orderService.ConfirmOrderAsync(orderEvent.OrderId);
    }
}
```

### **2. Adicionar Circuit Breaker**
```csharp
// Usando Polly para Circuit Breaker
public class ResilientRabbitMqPublisher
{
    private readonly CircuitBreakerPolicy _circuitBreaker;

    public ResilientRabbitMqPublisher()
    {
        _circuitBreaker = Policy
            .Handle<RabbitMQ.Client.Exceptions.BrokerUnreachableException>()
            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));
    }
}
```

### **3. Implementar Message Deduplication**
```csharp
public class DeduplicationService
{
    private readonly IDistributedCache _cache;

    public async Task<bool> IsDuplicateMessageAsync(string messageId)
    {
        var key = $"processed:{messageId}";
        return await _cache.GetStringAsync(key) != null;
    }
}
```

### **4. Adicionar Métricas com Prometheus**
```csharp
// Exemplo de métricas
public class MessagingMetrics
{
    private readonly Counter _messagesPublished;
    private readonly Counter _messagesConsumed;
    private readonly Histogram _messageProcessingDuration;

    public MessagingMetrics()
    {
        _messagesPublished = Metrics.CreateCounter("messages_published_total", "Total messages published");
        _messagesConsumed = Metrics.CreateCounter("messages_consumed_total", "Total messages consumed");
        _messageProcessingDuration = Metrics.CreateHistogram("message_processing_duration", "Message processing duration");
    }
}
```

### **5. Implementar Versionamento de Eventos**
```csharp
[EventVersion("1.0")]
public class OrderCreatedEventV1
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public List<OrderItem> Items { get; set; }
}

[EventVersion("2.0")]
public class OrderCreatedEventV2 : OrderCreatedEventV1
{
    public string CustomerEmail { get; set; }
    public Address ShippingAddress { get; set; }
}
```

### **6. Adicionar Compressão de Mensagens**
```csharp
public class CompressedRabbitMqPublisher : RabbitMqPublisher
{
    public override async Task PublishAsync<T>(T eventData, string routingKey)
    {
        var json = JsonSerializer.Serialize(eventData);
        var compressed = Compress(json);

        var properties = _channel.CreateBasicProperties();
        properties.Headers = new Dictionary<string, object>
        {
            ["compressed"] = true
        };

        await base.PublishAsync(compressed, routingKey, properties);
    }

    private byte[] Compress(string data)
    {
        using var output = new MemoryStream();
        using var gzip = new GZipStream(output, CompressionMode.Compress);
        using var writer = new StreamWriter(gzip);
        writer.Write(data);
        return output.ToArray();
    }
}
```

### **7. Implementar Message Encryption**
```csharp
public class EncryptedRabbitMqPublisher : RabbitMqPublisher
{
    private readonly IEncryptionService _encryptionService;

    public override async Task PublishAsync<T>(T eventData, string routingKey)
    {
        var json = JsonSerializer.Serialize(eventData);
        var encrypted = await _encryptionService.EncryptAsync(json);

        await base.PublishAsync(encrypted, routingKey);
    }
}
```

### **8. Adicionar Health Checks**
```csharp
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConnection _connection;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection.IsOpen)
            {
                return HealthCheckResult.Healthy("RabbitMQ is healthy");
            }
            return HealthCheckResult.Unhealthy("RabbitMQ connection is closed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ health check failed", ex);
        }
    }
}
```

### **9. Implementar Message Prioritization**
```csharp
public class PriorityRabbitMqPublisher : RabbitMqPublisher
{
    public async Task PublishHighPriorityAsync<T>(T eventData, string routingKey)
    {
        var properties = _channel.CreateBasicProperties();
        properties.Priority = 9; // Alta prioridade (0-9)

        await base.PublishAsync(eventData, routingKey, properties);
    }
}
```

### **10. Adicionar Distributed Tracing**
```csharp
// Usando OpenTelemetry
public class TracedRabbitMqPublisher : RabbitMqPublisher
{
    private readonly Tracer _tracer;

    public override async Task PublishAsync<T>(T eventData, string routingKey)
    {
        using var span = _tracer.StartActiveSpan("publish_message");
        span.SetAttribute("messaging.system", "rabbitmq");
        span.SetAttribute("messaging.operation", "publish");
        span.SetAttribute("messaging.destination", routingKey);

        try
        {
            await base.PublishAsync(eventData, routingKey);
            span.SetStatus(Status.Ok);
        }
        catch (Exception ex)
        {
            span.SetStatus(Status.Error, ex.Message);
            span.RecordException(ex);
            throw;
        }
    }
}
```

---

## 📚 Referências e Recursos Adicionais

### **Documentação Oficial**
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [.NET 8.0 Release Notes](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)

### **Livros Recomendados**
- "Microservices Patterns" - Chris Richardson
- "Building Microservices" - Sam Newman
- "Enterprise Integration Patterns" - Gregor Hohpe e Bobby Woolf

### **Cursos Online**
- [Microservices Architecture](https://www.udemy.com/course/microservices-architecture/)
- [RabbitMQ for Developers](https://www.udemy.com/course/rabbitmq-for-developers/)
- [ASP.NET Core Microservices](https://www.pluralsight.com/courses/aspdotnet-core-microservices)

### **Comunidades**
- [RabbitMQ Community](https://www.rabbitmq.com/community.html)
- [.NET Community](https://dotnet.microsoft.com/platform/community)
- [Microservices Community](https://microservices.io/community.html)

---

**🎉 Conclusão**: Esta implementação fornece uma base sólida para comunicação assíncrona entre microsserviços, permitindo que o sistema seja escalável, resiliente e fácil de manter. A arquitetura está preparada para futuras expansões e melhorias conforme as necessidades do negócio evoluem.</content>
<parameter name="filePath">/home/leandro/Imagens/micro/COMUNICACAO_ASSINCRONA.md
