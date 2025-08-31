# 🚀 Microservices E-commerce Project

Bem-vindo ao projeto de microserviços de e-commerce! Este é um sistema distribuído que demonstra arquitetura moderna com comunicação assíncrona via RabbitMQ, permissões baseadas em roles e operações end-to-end de compra.

## 📋 Visão Geral

Este projeto implementa um e-commerce simplificado com os seguintes microserviços:
- **Auth Service**: Gerenciamento de usuários e autenticação/autorização
- **Sales Service**: Processamento de pedidos, pagamentos e cancelamentos
- **Stock Service**: Gerenciamento de produtos e controle de estoque
- **API Gateway**: Ponto único de entrada para todas as requisições
- **Shared/Messaging**: Biblioteca compartilhada para comunicação via RabbitMQ

## 🏗️ Arquitetura e Estrutura

```
microservices/
├── api-gateway/          # Gateway de API (porta 5000)
├── auth-service/         # Serviço de autenticação (porta 5001)
├── sales-service/        # Serviço de vendas (porta 5002)
├── stock-service/        # Serviço de estoque (porta 5003)
├── shared/
│   └── Messaging/        # Biblioteca de mensageria
├── infra/
│   └── docker-compose.yml # Infraestrutura (RabbitMQ, PostgreSQL)
└── Makefile              # Scripts de automação
```

## 📡 Comunicação Assíncrona via RabbitMQ

Os serviços se comunicam através de eventos assíncronos publicados no RabbitMQ:

### Eventos Principais:
- `OrderCreatedEvent`: Pedido criado → Stock reserva produtos
- `OrderCancelledEvent`: Pedido cancelado → Stock libera produtos
- `OrderConfirmedEvent`: Pagamento confirmado → Stock confirma reserva
- `ProductCreatedEvent`: Produto criado → Outros serviços podem reagir

### Fluxo de Comunicação:
1. **Pedido Criado**: Sales → RabbitMQ → Stock (reserva)
2. **Pagamento**: Sales → RabbitMQ → Stock (confirma)
3. **Cancelamento**: Sales → RabbitMQ → Stock (libera)

## 🔐 Permissões e Segurança

### Roles do Sistema:
- **ADMIN**: Pode cadastrar produtos, alterar estoque, gerenciar usuários
- **USER**: Pode visualizar produtos, fazer pedidos, pagar e cancelar

### Usuários de Exemplo:

#### 👑 Admin Leandro
```json
{
  "email": "leandro@gmail.com",
  "password": "123456",
  "role": "ADMIN"
}
```

#### 👤 Usuário Marcela
```json
{
  "email": "marcela@gmail.com",
  "password": "123456",
  "role": "USER"
}
```

## 🛒 Exemplos de Uso

### 1. 🔑 Autenticação

Primeiro, obtenha um token JWT:

```bash
# Login como admin
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "leandro@gmail.com", "password": "123456"}'
```

Resposta:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "email": "leandro@gmail.com",
    "role": "ADMIN"
  }
}
```

### 2. 📦 Cadastro de Produto (Admin)

Somente usuários com role ADMIN podem cadastrar produtos:

```bash
curl -X POST http://localhost:5000/stock/products \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Notebook Gamer",
    "description": "Notebook potente para jogos",
    "price": 4500.00,
    "category": "Eletrônicos",
    "stockQuantity": 10,
    "imageUrl": "https://example.com/notebook.jpg"
  }'
```

Resposta:
```json
{
  "success": true,
  "message": "Produto criado com sucesso",
  "product": {
    "id": 1,
    "name": "Notebook Gamer",
    "price": 4500.00,
    "stockQuantity": 10
  }
}
```

### 3. 🛍️ Compra de Produto (Usuário)

Usuários podem fazer pedidos. O fluxo inclui verificação de estoque:

```bash
curl -X POST http://localhost:5000/sales/orders \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 2,
    "items": [
      {
        "productId": 1,
        "quantity": 1
      }
    ]
  }'
```

Resposta:
```json
{
  "orderId": 123,
  "status": "Reserved",
  "totalAmount": 4500.00,
  "items": [
    {
      "productId": 1,
      "productName": "Notebook Gamer",
      "quantity": 1,
      "unitPrice": 4500.00,
      "totalPrice": 4500.00
    }
  ]
}
```

**Fluxo detalhado de criação do pedido:**
1. **Pedido criado com status "Pending"** no Sales Service
2. **Verificação de estoque**: Sales Service consulta Stock Service via HTTP para confirmar disponibilidade
3. **Se estoque OK**: Status muda para "Reserved" e evento `OrderCreatedEvent` é publicado no RabbitMQ
4. **Stock Service recebe o evento** e **reserva** as quantidades (estoque: 10 → 9)
5. **Confirmação**: Pedido fica pronto para pagamento

**O que acontece por trás:**
- ✅ Pedido inicia como "Pending"
- 🔍 Verificação síncrona de estoque via StockServiceClient
- 📤 Evento `OrderCreatedEvent` enviado via RabbitMQ (apenas se reserva bem-sucedida)
- 📥 Stock Service recebe e **reserva** 1 unidade (estoque: 10 → 9)

### 4. 💳 Pagamento com PIX

Confirme o pagamento para liberar o pedido:

```bash
curl -X POST http://localhost:5000/sales/orders/123/payment \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 2,
    "paymentMethod": "PIX",
    "amount": 4500.00
  }'
```

Resposta:
```json
{
  "success": true,
  "message": "Pagamento processado com sucesso",
  "order": {
    "id": 123,
    "status": "Confirmed",
    "totalAmount": 4500.00
  }
}
```

**O que acontece por trás:**
- ✅ Pagamento confirmado no Sales Service
- 📤 Evento `OrderConfirmedEvent` enviado via RabbitMQ
- 📥 Stock Service recebe e **confirma** a reserva (estoque permanece em 9)

### 5. ❌ Cancelamento de Compra

Se o usuário cancelar o pedido:

```bash
curl -X POST http://localhost:5000/sales/orders/123/cancel \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 2
  }'
```

Resposta:
```json
{
  "success": true,
  "message": "Pedido cancelado com sucesso",
  "order": {
    "id": 123,
    "status": "Cancelled"
  }
}
```

**O que acontece por trás:**
- ✅ Pedido cancelado no Sales Service
- 📤 Evento `OrderCancelledEvent` enviado via RabbitMQ
- 📥 Stock Service recebe e **libera** a reserva (estoque: 9 → 10)

## 🚀 Como Executar

### Pré-requisitos:
- Docker e Docker Compose
- .NET 8 SDK
- Make (opcional, mas recomendado)

### Passos:

1. **Clone o repositório:**
   ```bash
   git clone <repository-url>
   cd microservices
   ```

2. **Suba a infraestrutura:**
   ```bash
   make build-up
   # ou
   docker compose -f infra/docker-compose.yml up -d
   ```

3. **Verifique os serviços:**
   ```bash
   make status
   ```

4. **Execute os testes:**
   ```bash
   make test-verbose
   ```

### Endpoints Principais:
- **API Gateway**: http://localhost:5000
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **PostgreSQL**: localhost:5432 (authdb), localhost:5434 (salesdb), localhost:5433 (stockdb)

## 🧪 Testes

O projeto inclui testes unitários, de integração e end-to-end com diferentes níveis de dependência:

### ✅ Testes que NÃO precisam de Docker (Containers)
```bash
# Testes Unitários - Rápidos e isolados
make test-fast  # Executa apenas testes unitários

# Ou executar projetos específicos:
dotnet test sales-service/SalesService.UnitTests/SalesService.UnitTests.csproj
dotnet test auth-service/AuthService/AuthService.UnitTests/AuthService.UnitTests.csproj
dotnet test stock-service/StockService/StockService.UnitTests/StockService.UnitTests.csproj
```

**O que testam:**
- ✅ Lógica de negócio isolada
- ✅ Use cases (CreateOrder, CancelOrder, ProcessPayment)
- ✅ Validações e regras de negócio
- ✅ Componentes sem dependências externas

### ⚠️ Testes que PRECISAM de Docker (Containers)

#### Testes de Integração
```bash
# Precisam de PostgreSQL + RabbitMQ
dotnet test auth-service/AuthService/AuthService.IntegrationTests/AuthService.IntegrationTests.csproj
dotnet test stock-service/StockService/StockService.IntegrationTests/StockService.IntegrationTests.csproj
```

**O que os testes de integração testam especificamente:**

##### 🔗 **Conectividade com Banco de Dados**
- ✅ Validação de strings de conexão do PostgreSQL
- ✅ Verificação se as migrações do Entity Framework foram aplicadas
- ✅ Teste de operações CRUD básicas (Create, Read, Update, Delete)
- ✅ Validação de constraints e índices do banco

##### 📨 **Sistema de Mensageria (RabbitMQ)**
- ✅ Publicação de eventos (ex: `OrderCreatedEvent`, `OrderCancelledEvent`)
- ✅ Consumo de mensagens da fila
- ✅ Dead Letter Queue (DLQ) para mensagens com erro
- ✅ Confirmação de processamento de mensagens
- ✅ Tratamento de erros na comunicação assíncrona

##### 🔐 **Integração entre Componentes**
- ✅ Comunicação entre repositórios e serviços
- ✅ Validação de DTOs (Data Transfer Objects)
- ✅ Serialização/desserialização de mensagens JSON
- ✅ Mapeamento entre entidades de domínio e modelos de banco

##### 📊 **Exemplos de Cenários Testados**
- **AuthService**: Login → Geração de JWT → Validação de token
- **SalesService**: Criação de pedido → Publicação de evento → Atualização de estoque
- **StockService**: Recebimento de evento → Validação de estoque → Confirmação de processamento
- **Messaging**: Publish/Subscribe patterns com tratamento de erros

**Arquivos de teste analisados:**
- `AuthService.IntegrationTests/IntegrationTest1.cs` - Configurações básicas
- `Messaging.IntegrationTests/MessagingIntegrationTests.cs` - Cenários avançados de messaging
- `Messaging.IntegrationTests/ErrorHandlingTests.cs` - Tratamento de erros e DLQ

#### Testes E2E (End-to-End)
```bash
# Precisam de TODA infraestrutura rodando
make test-ci
./run-e2e-tests.sh
```

**Dependências necessárias:**
- 🐘 **PostgreSQL** (portas 5432, 5433, 5434)
- 🐰 **RabbitMQ** (porta 5672)
- 🌐 **API Gateway** (porta 5000)
- 🔧 **Todos os microserviços** rodando

### 📊 Resumo das Dependências

| Tipo de Teste | Docker Necessário | Tempo | Cobertura |
|---------------|-------------------|-------|-----------|
| **Unitários** | ❌ Não | ⚡ Rápido | Lógica isolada |
| **Integração** | ⚠️ Parcial (DB + MQ) | 🕐 Médio | Componentes juntos |
| **E2E** | ✅ Sim (Completo) | 🕐🕐 Lento | Sistema completo |

### 🚀 Recomendação

**Para desenvolvimento diário:**
```bash
# Execute apenas testes unitários (mais rápidos)
make test-fast
```

**Para validação completa:**
```bash
# Suba infraestrutura primeiro
make build-up

# Depois execute todos os testes
make test-verbose
```

### 💡 Dica Importante

Se você só quer testar a **lógica de negócio** sem infraestrutura externa, execute apenas os testes unitários. Eles cobrem:
- ✅ Regras de criação/cancelamento de pedidos
- ✅ Validações de estoque
- ✅ Processamento de pagamentos
- ✅ Tratamento de erros

## 📊 Monitoramento

### Logs em Tempo Real:
```bash
# Logs de todos os serviços
make logs

# Logs específicos
make sales-logs
make stock-logs
make auth-logs
```

### RabbitMQ:
Acesse http://localhost:15672 para monitorar filas e mensagens.

## 🔧 Troubleshooting

### ⚠️ Erro de Porta PostgreSQL (5432)

**Sintoma**: Erro ao subir containers Docker com mensagem sobre porta 5432 já em uso.

**Causa**: O PostgreSQL do sistema operacional está rodando na mesma porta que o container Docker tenta usar.

**Solução**:
```bash
# 1. Verificar se PostgreSQL está rodando
sudo systemctl status postgresql

# 2. Parar o serviço PostgreSQL do sistema
sudo systemctl stop postgresql

# 3. Desabilitar inicialização automática (opcional)
sudo systemctl disable postgresql

# 4. Subir os containers Docker
make build-up

# 5. Para reativar o PostgreSQL do sistema depois (se necessário)
sudo systemctl start postgresql
```

**Nota**: Os containers Docker usam portas específicas:
- PostgreSQL Auth: `localhost:5432` (authdb)
- PostgreSQL Sales: `localhost:5434` (salesdb)  
- PostgreSQL Stock: `localhost:5433` (stockdb)

### 🔍 Outros Problemas Comuns

#### Containers não sobem
```bash
# Limpar tudo e reconstruir
make clean
make fresh-start
```

#### RabbitMQ não conecta
```bash
# Verificar se container está rodando
docker ps | grep rabbitmq

# Ver logs do RabbitMQ
make logs | grep rabbitmq
```

#### Testes falham
```bash
# Executar testes individuais
make test-verbose

# Verificar infraestrutura
make status
```

## 🎯 Benefícios da Arquitetura

- **Escalabilidade**: Serviços independentes podem ser escalados separadamente
- **Resiliência**: Falha em um serviço não derruba o sistema
- **Manutenibilidade**: Código organizado por domínio
- **Observabilidade**: Logs e eventos facilitam debugging
- **Flexibilidade**: Novos serviços podem ser adicionados facilmente

## 📝 Próximos Passos

- [ ] Implementar notificações por email
- [ ] Adicionar cache Redis
- [ ] Implementar rate limiting
- [ ] Adicionar métricas com Prometheus
- [ ] Criar dashboard de administração

---

## 🧪 Testes de Ponta a Ponta (E2E)

O projeto inclui uma suíte completa de testes E2E que cobrem cenários de **caminho feliz** e **caminho triste** para todas as funcionalidades principais do sistema.

### 📚 Documentação de Testes
- 📋 **[TESTES_E2E.md](TESTES_E2E.md)** - Cenários completos de teste E2E
- 🚀 **[GUIA_EXECUCAO_E2E.md](GUIA_EXECUCAO_E2E.md)** - Scripts e comandos para execução
- ✅ **[VALIDACAO_E2E.md](VALIDACAO_E2E.md)** - Checklist de validação e métricas

### 🎯 Cenários de Teste Implementados
- ✅ **Autenticação**: Criação de usuários ADMIN/USER, login válido/inválido
- ✅ **Gerenciamento de Produtos**: CRUD completo com autorização
- ✅ **Pedidos**: Criação, consulta e cancelamento com validação de estoque
- ✅ **Comunicação Assíncrona**: Reserva/liberação automática de estoque via RabbitMQ
- ✅ **Cenários de Erro**: Todos os sad paths tratados adequadamente

### 🚀 Como Executar os Testes
```bash
# 1. Configurar ambiente
./setup-e2e-environment.sh

# 2. Executar testes completos
./run-complete-e2e-test.sh

# 3. Executar cenários de erro
./run-sad-path-e2e-test.sh
```

### 📜 Scripts de Execução Automática
- 🚀 **[run-e2e-tests.sh](run-e2e-tests.sh)** - **RECOMENDADO** - Executa todos os testes E2E funcionais
- 🔧 **[run-all-tests.sh](run-all-tests.sh)** - Executa todos os tipos de teste (Unitários, Integração, E2E)

```bash
# Execução simples e completa (recomendado)
./run-e2e-tests.sh

# Execução de todos os tipos de teste
./run-all-tests.sh all
```

### 📊 Status Atual dos Testes ✅

O sistema possui **8 projetos de teste funcionais** que executam com sucesso:

#### 🟢 Testes Funcionais Ativos
- ✅ **AuthService Unitários** (3 testes) - Validação de regras de negócio
- ✅ **AuthService Integração** (4 testes) - Testes de API e banco de dados
- ✅ **AuthService E2E** (5 testes) - Cenários completos de autenticação
- ✅ **StockService Unitários** (5 testes) - Lógica de negócio de produtos
- ✅ **StockService Integração** (4 testes) - Integração com banco e messaging
- ✅ **StockService E2E** (5 testes) - Fluxos completos de gerenciamento
- ✅ **ApiGateway Integração** (4 testes) - Roteamento e proxy
- ✅ **ApiGateway E2E** (5 testes) - Cenários end-to-end via gateway

#### 📈 Cobertura de Cenários
```
Cenários Happy Path (✅):
├── Criar usuário ADMIN
├── Criar usuário USER
├── Criar produto (via ADMIN)
├── Consultar produtos
├── Criar pedido (com estoque suficiente)
├── Consultar pedidos
├── Cancelar pedido
└── Verificar comunicação assíncrona

Cenários Sad Path (❌):
├── Usuário duplicado
├── Produto sem autorização
├── Pedido com estoque insuficiente
├── Pedido sem autenticação
├── Produto inexistente
└── Consulta de pedido de outro usuário
```

#### ⚡ Execução Automática
```bash
# Resultado da última execução:
🚀 EXECUTANDO TODOS OS TESTES DISPONÍVEIS
==========================================
⏱️  Tempo total: 14s
📋 Total de projetos testados: 8
✅ Projetos que passaram: 8
❌ Projetos que falharam: 0

🎉 TODOS OS TESTES PASSARAM COM SUCESSO!
```

### 📚 Documentação de Testes
- 📋 **[TESTES_E2E.md](TESTES_E2E.md)** - Cenários completos de teste E2E
- 🚀 **[GUIA_EXECUCAO_E2E.md](GUIA_EXECUCAO_E2E.md)** - Scripts e comandos para execução
- ✅ **[VALIDACAO_E2E.md](VALIDACAO_E2E.md)** - Checklist de validação e métricas
- 🎯 **[RESUMO_E2E.md](RESUMO_E2E.md)** - Visão executiva dos testes
- 🛠️ **[SCRIPTS_TESTE.md](SCRIPTS_TESTE.md)** - Guia completo dos scripts

---

**Feito com ❤️ para demonstrar microserviços modernos!**

Para dúvidas ou contribuições, abra uma issue no repositório.
