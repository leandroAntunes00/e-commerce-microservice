# RELATORIO_FUNCIONAL

Este documento descreve o funcionamento funcional da aplicação de microserviços, com foco na comunicação assíncrona via RabbitMQ, topologia de eventos, contratos, comportamento de produtores/consumidores, configurações relevantes e recomendações operacionais.

## Visão geral

A solução é composta por microserviços .NET (AuthService, SalesService, StockService) e um ApiGateway (YARP). Cada serviço tem seu banco (Postgres) e comunica-se de forma assíncrona usando RabbitMQ e uma biblioteca de mensageria compartilhada localizada em `shared/Messaging`.

Objetivos principais do fluxo assíncrono:
- Desacoplar processo de criação de pedido da reserva de estoque.
- Garantir disponibilidade/elasticidade independentes por serviço.
- Tornar o fluxo observável e testável via E2E.

## Componentes principais

- ApiGateway (`api-gateway/ApiGateway`): roteia requisições do cliente para serviços internos e expõe endpoints públicos.
- AuthService (`auth-service/AuthService`): gera JWTs e gerencia autenticação/usuários.
- SalesService (`sales-service/SalesService`): orquestra pedidos, publica eventos de pedido criado e consome resultados de reserva.
- StockService (`stock-service/StockService`): escuta eventos de pedido criado, tenta reservar inventário e responde com o resultado.
- Shared Messaging (`shared/Messaging`): abstrações para publisher/consumer, eventos base, configuração de exchange e prefixos de fila.
- RabbitMQ (container gerenciado via `infra/docker-compose.yml`): broker central.

## Topologia RabbitMQ

- Exchange: `microservices_exchange` (tipo: direct).
- Queue prefix configurável via `RabbitMq:QueuePrefix` (valor adotado: `microservices_`).
- Convenção de nome de fila: `${QueuePrefix}{service}_{event}`. Ex.: `stock_service_order_created`.
- Derivação de routing key:
  - Publisher transforma o `EventType` (PascalCase) em snake_case (ex.: `OrderReservationCompleted` -> `order_reservation_completed`).
  - Consumer declara fila com prefixo e faz bind usando `queueName.Replace(QueuePrefix, "")` como routingKey. Portanto publisher e consumer convergem para a mesma chave.

Queues e bindings observados no ambiente local (exemplos):
- `stock_service_order_created` bound to routing key `order_created` on `microservices_exchange`.
- `sales_service_order_reservation_completed` bound to routing key `order_reservation_completed` on `microservices_exchange`.

## Eventos principais e contratos (exemplos JSON)

1) OrderCreatedEvent
{
  "EventType": "OrderCreated",
  "OrderId": "guid",
  "UserId": "guid",
  "Items": [ { "ProductId": "guid", "Quantity": 2 } ],
  "Total": 123.45,
  "OccurredAt": "2025-08-29T...Z"
}

2) OrderReservationCompletedEvent
{
  "EventType": "OrderReservationCompleted",
  "OrderId": "guid",
  "Success": true,
  "Reason": null,
  "OccurredAt": "2025-08-29T...Z"
}

3) StockUpdatedEvent
{
  "EventType": "StockUpdated",
  "ProductId": "guid",
  "Delta": -2,
  "CurrentStock": 10,
  "OccurredAt": "2025-08-29T...Z"
}

Observação: o projeto já contém classes de evento em `shared/Messaging/Events.cs` — siga esses contratos ou estenda com `CorrelationId`/`IdempotencyKey` se necessário.

## Fluxo funcional (sequência)

1. Cliente (via ApiGateway) cria usuário / faz login no `AuthService` e obtém JWT.
2. Cliente chama endpoint de criação de pedido no `ApiGateway` (roteado para `SalesService`), enviando `productId` e `quantity`.
3. `SalesService`:
   - Persiste o pedido localmente com status `Pending`.
   - Publica `OrderCreatedEvent` para `microservices_exchange` (routing key: `order_created`).
4. `StockService` (consumer do `order_created`):
   - Recebe `OrderCreatedEvent` da fila `stock_service_order_created`.
   - Tenta reservar itens: decrementa estoque temporariamente / marca reserva na base.
   - Publica um `StockUpdatedEvent` para cada produto e, ao final, publica `OrderReservationCompletedEvent` (routing key: `order_reservation_completed`).
5. `SalesService` (consumer do `order_reservation_completed`):
   - Recebe o evento e atualiza o pedido para `Reserved` se `Success==true` ou `Cancelled` caso contrário.
   - Em caso de falha, pode publicar `OrderCancelledEvent` (e acionar compensações se houver).
6. Pagamentos: `SalesService` expõe endpoints de pagamento (`/pay/card, /pay/pix, /pay/boleto`) que marcam pedido como `Confirmed` e publicam `OrderConfirmedEvent`.

Observação: a atualização definitiva do estoque (persistência) ocorre no `StockService` quando a reserva é aplicada. A convenção atual publica `StockUpdatedEvent` para propagar mudanças.

## Implementação técnica (onde olhar no código)

- Publisher: `shared/Messaging/RabbitMqPublisher.cs` — implementa PublishAsync; converte EventType → snake_case.
- Consumer: `shared/Messaging/RabbitMqConsumer.cs` — declara exchange, declara fila `${QueuePrefix}{...}` e faz bind usando `queueName.Replace(QueuePrefix, "")`.
- Stock consumer service: `stock-service/StockService/Services/OrderEventConsumerService.cs` (BackgroundService que processa `OrderCreatedEvent`).
- Sales reservation consumer: `sales-service/SalesService/Services/ReservationResultConsumerService.cs` (BackgroundService que processa `OrderReservationCompletedEvent`).
- Program/Startup: ver `sales-service/SalesService/Program.cs` — registra `IMessageConsumer` e hosts.
- Docker Compose: `infra/docker-compose.yml` — define containers, JWT env via `x-jwt`, RabbitMQ e Postgres.

## Configurações importantes

- RabbitMQ settings (config key examples):
  - `RabbitMq:Host` (host/port)
  - `RabbitMq:ExchangeName` (ex.: `microservices_exchange`)
  - `RabbitMq:QueuePrefix` (ex.: `microservices_`)
- JWT: unificado via anchor `x-jwt` no `docker-compose.yml` para `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`.
- ASPNETCORE_ENVIRONMENT=Development setado em compose para expor Swagger durante dev.

## Observabilidade e debugging

- Logs: cada serviço loga startup do consumer e mensagens de processamento. Procure por linhas indicando `RabbitMqConsumer started for queue '...'`.
- RabbitMQ introspecção: dentro do container rabbitmq usar `rabbitmqctl list_queues` e `rabbitmqctl list_bindings` para validar filas e routing keys.
- Testes E2E: `sales-service/SalesService.E2ETests` realiza fluxo completo e valida transição de status do pedido; útil como canário.

## Problemas comuns e soluções

- Mensagens não entregues → verificar coincidência de routing key (publisher usa snake_case; consumer deriva a partir do nome da fila). Se houver mismatch, alinhar `ToRoutingKey` ou `QueuePrefix`.
- Consumer não iniciado → registrar `IMessageConsumer` no DI (ex.: `builder.Services.AddTransient<IMessageConsumer, RabbitMqConsumer>()`).
- Testes E2E intermitentes → aguardar readiness do consumer antes de criar pedidos ou expor um endpoint/flag de readiness para testes.
- Mensagens duplicadas → implementar idempotência no processamento (idempotency keys) e usar transações/locks no DB.

## Requisitos operacionais e recomendações

1. Correlation & Tracing
   - Adicionar `CorrelationId` e `CausationId` em `BaseEvent`.
   - Integrar OpenTelemetry: traces e métricas para spans de publicação/consumo.

2. Idempotência e consistência
   - Incluir `IdempotencyKey` nos eventos e checar antes de aplicar alterações no DB.
   - Garantir operações atômicas (transaction + outbox pattern se necessário) para publicar eventos com consistência.

3. DLQ, retries e políticas
   - Configurar filas de retry e DLQ para mensagens rejeitadas.
   - Expor métricas de mensagens mortas e taxa de retries.

4. Saúde/readiness
   - Adicionar health checks para RabbitMQ connectivity e consumidores.
   - Testes E2E: esperar health/readiness antes de iniciar cenários.

5. Contratos e testes
   - Adotar testes de contrato (consumer-driven contracts) para garantir compatibilidade de eventos.

## Rotina de operação / passos de verificação

- Subir ambiente: `docker compose -f infra/docker-compose.yml up -d`.
- Verificar serviços: acessar `http://localhost:5000` (ApiGateway) e `http://localhost:5001/swagger/index.html` (AuthService) — Swagger exposto em Development.
- Validar filas: `docker exec -it rabbitmq rabbitmqctl list_queues`.
- Consultar logs: `docker compose logs --tail 200 sales-service`.

## Checklist de melhorias (curto prazo)
- [ ] Propagar CorrelationId em todos os eventos e logs.
- [ ] Implementar idempotency keys e checagem no consumo.
- [ ] Adicionar DLQ + retry policy nas filas.
- [ ] Health/readiness explícitos para consumidores para estabilizar testes E2E.
- [ ] Documentar os eventos e routing keys em `COMUNICACAO_ASSINCRONA.md` (atualizar).

## Conclusão

A arquitetura atual fornece uma base funcional de mensageria assíncrona, com convenções que permitem serviços publicarem e consumirem eventos sem acoplamento direto. Para produção, recomendo focar em observabilidade (tracing), idempotência, tratamento de erro (DLQ/retries) e testes de contrato para reduzir riscos operacionais.

---

Arquivo gerado automaticamente: `RELATORIO_FUNCIONAL.md`.
