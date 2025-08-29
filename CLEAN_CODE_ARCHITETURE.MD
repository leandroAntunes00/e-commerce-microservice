# CLEAN_CODE_ARCHITETURE

Documento com propostas práticas para migrar o projeto para uma arquitetura limpa, desacoplada e mais fácil de entender e manter.

Resumo rápido do objetivo
- Definir camadas claras: API (Controllers/DTOs), Application (UseCases/Services), Domain (Entities/ValueObjects/Interfaces), Infrastructure (EF Core, Repositories, Messaging).
- Padronizar DTOs, contracts de eventos e boundary interfaces (portas) para evitar vazamento de detalhes de infra nos controllers.
- Introduzir patterns: Repository, UnitOfWork (se necessário), Outbox Pattern, Idempotency, Correlation/Tracing.
- Limpeza de artefatos versionados (bin/obj/arquivos gerados) e sugestões de `.gitignore` + comandos para remover do git cache.

Checklist (itens a implementar)
- [ ] Separar pastas por camada dentro de cada microserviço: `Api/`, `Application/`, `Domain/`, `Infrastructure/`.
- [ ] Mover controllers para `Api/Controllers` e expor apenas DTOs (entrada/saída) definidos em `Api/Dto`.
- [ ] Implementar Services/UseCases em `Application/` que recebem DTOs ou IDs e coordenam repositórios e publicação de eventos.
- [ ] Definir `Domain/Entities`, `Domain/ValueObjects`, `Domain/Interfaces` (ex.: `IOrderRepository`).
- [ ] Implementar `Infrastructure/Repositories` que implementam as interfaces de domínio usando EF Core.
- [ ] Centralizar a infra de mensageria em `Infrastructure/Messaging` com contratos e uma camada adapter (IMessagePublisher / IMessageConsumer).
- [ ] Adotar Outbox Pattern (tabela `OutboxMessages`) para garantir consistência entre DB e event publishing.
- [ ] Adicionar CorrelationId/CausationId em `BaseEvent` e propagar em logs e headers HTTP.
- [ ] Criar testes unitários para Application/Domain e contratos de eventos (consumer-driven contract tests).
- [ ] Health/Readiness endpoints: incluir checagem do broker e readiness do consumer antes de aceitar tráfego nos testes E2E.

Arquitetura proposta (pastas exemplares dentro de cada microserviço)
- ServiceName/
  - Api/
    - Controllers/
    - Dto/
    - Mappers/ (AutoMapper profiles)
  - Application/
    - UseCases/
    - Services/
    - Commands/Queries (se usar CQRS)
    - Dtos/ (application-level DTOs, não HTTP)
  - Domain/
    - Entities/
    - ValueObjects/
    - Events/
    - Interfaces/ (ex.: IOrderRepository)
    - Exceptions/
  - Infrastructure/
    - Persistence/
      - Repositories/ (implementações EFCore)
      - Migrations/
      - DbContext.cs
    - Messaging/
      - RabbitMq/
        - RabbitMqPublisher.cs (adapter)
        - RabbitMqConsumer.cs (adapter)
      - Outbox/ (if used)
    - Logging/
  - Tests/
    - UnitTests/
    - IntegrationTests/
    - E2ETests/

Contrato e DTOs (exemplo)
- Api/Dto/CreateOrderRequest.cs
  - Fields: ProductId (Guid), Quantity (int)
- Api/Dto/OrderResponse.cs
  - Fields: OrderId, Status, Items[], Total

Application UseCase (exemplo)
- CreateOrderUseCase :
  - Assinatura: Execute(CreateOrderCommand cmd) -> returns OrderId
  - Responsabilidade: validar regras, persistir order (via IOrderRepository), criar evento OrderCreated (populate CorrelationId) e salvar em Outbox.

Repository interface (Domain/Interfaces/IOrderRepository.cs)
- Task<Order> GetAsync(Guid id);
- Task SaveAsync(Order order);
- Optional: BeginTransaction/Commit (ou UnitOfWork)

Outbox Pattern (por quê)
- Evita perda de evento quando commit do DB succeed mas publish falha.
- Fluxo: dentro de uma transação DB -> persistir entidade + persistir outbox message -> commit -> um worker (ou publisher) lê outbox e publica para RabbitMQ e marca como publicado.

Mensageria (boas práticas)
- Padronizar nomes de eventos e routing keys (snake_case) em uma classe central `EventName` ou `RoutingKeyHelper`.
- Não derivar routingKey dinamicamente de nomes de fila; preferir um contrato claro (ex.: `RoutingKeys.OrderCreated = "order_created"`).
- Adicionar `CorrelationId` e `IdempotencyKey` nos eventos.
- Implementar idempotency no consumidor: checar se evento já aplicado com base em `EventId` ou `IdempotencyKey`.
- DLQ + Retry: use políticas de retry expondo métricas.

Observações sobre problemas vistos no repositório atual
- Publisher/Consumer mismatch: havia conversão inconsistente de EventType -> routing key; padronizar para evitar bugs sutis.
- Falta de registro de implementação `IMessageConsumer` em alguns serviços (causou consumer não iniciar). Garantir que Composition Root registre todos os adapters de infra.
- Alguns serviços expõem lógica de persistência diretamente no controller ou fazem chamadas HTTP síncronas para outras services (melhor isolar via Application/UseCase layer).
- Código gerado/artifacts (`bin/`, `obj/`, arquivos `*.log`, `*.binlog`, `*.db`) parecem presentes; precisam ser ignorados no git.

Sugestões de refatorações concretas (prioridade)
1) Corrigir composição (Program.cs) para registrar:
   - IOrderRepository => OrderRepository
   - IMessagePublisher => RabbitMqPublisher
   - IMessageConsumer => RabbitMqConsumer
   - IUnitOfWork se existir
2) Introduzir `CreateOrderUseCase` e mover lógica do `SalesController` para lá; controller apenas mapeia DTO->Command e retorna 202/201.
3) Implementar Outbox: criar `OutboxMessage` entity, migrations, e um worker `OutboxPublisherService` para publicar.
4) Padronizar contratos de eventos (`shared/Messaging/Events.cs`) adicionando `CorrelationId`, `EventId` (guid), `OccurredAt`.
5) Adicionar idempotency store: `ProcessedEvent` table para evitar reprocessamento.
6) Criar `Messaging/Contracts` central com routing keys e nomes de fila esperados.

Limpeza de repositório (sugestões e comandos)
- Arquivos a ignorar em `.gitignore` (exemplo mínimo):
  - bin/
  - obj/
  - **/bin/
  - **/obj/
  - **/*.db
  - **/*.log
  - **/*.user
  - .vs/
  - .vscode/
  - **/*.binlog

Exemplo de `.gitignore` para o root:
```
# .gitignore (exemplo)
bin/
obj/
**/bin/
**/obj/
.vs/
.vscode/
*.user
*.suo
*.log
*.db
*.sqlite
*.binlog
docker-compose.override.yml
.env

# Rider/IDE
.idea/

# visual studio
*.user
*.suo

# publish
publish/

# node
node_modules/
```

Remover arquivos já versionados (não executar automaticamente; comandos sugeridos):
```bash
# 1) adicionar .gitignore e commitar
git add .gitignore
git commit -m "add gitignore to ignore build artifacts"

# 2) remover artefatos do index (mantém local)
git rm -r --cached **/bin **/obj || true
# Remover arquivos específicos se necessários
git rm --cached *.log || true

git commit -m "remove build artifacts from repo cache"
```

Boas práticas para PRs e revisão de código
- PRs pequenos e com único propósito (ex.: refatoração de camada Application, ou adição de Outbox).
- Checklist de PR: tests, migrations, contratos de evento, atualização de documentação (`RELATORIO_FUNCIONAL.md`).
- Documentar contratos de eventos em Markdown (ex.: `docs/events.md`) e versioná-los.

Exemplo mínimo de código (pseudocódigo) — Controller -> UseCase -> Repository
```csharp
// Api/Controllers/OrdersController.cs
[ApiController]
public class OrdersController : ControllerBase
{
  private readonly ICreateOrderUseCase _createOrder;
  public OrdersController(ICreateOrderUseCase createOrder) { _createOrder = createOrder; }

  [HttpPost]
  public async Task<IActionResult> Post(CreateOrderRequest req)
  {
    var cmd = new CreateOrderCommand(req.ProductId, req.Quantity, correlationId: Request.Headers["X-Correlation-Id"]);
    var orderId = await _createOrder.ExecuteAsync(cmd);
    return Accepted(new { orderId });
  }
}

// Application/UseCases/CreateOrderUseCase.cs
public class CreateOrderUseCase : ICreateOrderUseCase
{
  private readonly IOrderRepository _repo;
  private readonly IOutboxRepository _outbox;
  public async Task<Guid> ExecuteAsync(CreateOrderCommand cmd)
  {
    var order = Order.Create(cmd.ProductId, cmd.Quantity);
    await _repo.SaveAsync(order);
    var evt = new OrderCreatedEvent { OrderId = order.Id, CorrelationId = cmd.CorrelationId };
    await _outbox.AddAsync(evt);
    return order.Id;
  }
}
```

Métricas/monitoramento recomendados
- Counters: published_events, consumed_events, failed_events, outbox_pending
- Gauges: queue_length per queue
- Traces: span por publish/consume com CorrelationId

Checklist de entrega mínima (MVP refactor)
- [ ] Aplicar .gitignore e limpar cache do git
- [ ] Separar uma service (ex.: SalesService) nas camadas sugeridas
- [ ] Extrair lógica de controller para UseCase e criar testes unitários
- [ ] Implementar Outbox básico e worker para publicar eventos
- [ ] Adicionar CorrelationId à base de eventos

Próximos passos que posso executar para você
- Gerar PR com mudanças pequenas (ex.: mover controller logic para UseCase e adicionar interface `IOrderRepository`).
- Criar `OutboxMessage` migration e um worker que publica mensagens existentes.
- Atualizar `shared/Messaging` para incluir `CorrelationId` e `EventId` nos eventos.
- Criar `.gitignore` e aplicar os comandos para remover `bin/obj` do repositório (com sua confirmação).

Fim do relatório. Se quiser que eu aplique automaticamente as mudanças, diga quais 1–2 tarefas devo executar primeiro (ex.: criar `.gitignore` + git rm cached; ou implementar `CreateOrderUseCase` e mover lógica do controller).
