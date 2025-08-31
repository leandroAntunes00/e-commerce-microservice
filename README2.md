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

O projeto inclui testes unitários, de integração e end-to-end:

```bash
# Testes unitários detalhados
make test-verbose

# Todos os testes
make test

# Testes CI (com relatórios)
make test-ci
```

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

**Feito com ❤️ para demonstrar microserviços modernos!**

Para dúvidas ou contribuições, abra uma issue no repositório.
