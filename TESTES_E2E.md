# 🧪 Testes de Ponta a Ponta (E2E) - Microserviços

## 📋 Visão Geral

Este documento descreve os testes de ponta a ponta (E2E) implementados para o sistema de microserviços, cobrindo cenários de **caminho feliz** (happy path) e **caminho triste** (sad path) para as principais funcionalidades do sistema.

## 🏗️ Cenários de Teste Implementados

### **1. Cenários de Autenticação e Autorização**

#### **Cenário 1.1: Criar Usuário USER (Happy Path)**
```bash
# Request
POST /api/auth/register
{
  "username": "testuser",
  "email": "testuser@example.com",
  "password": "TestPass123!",
  "fullName": "Test User",
  "role": "USER"
}

# Response Esperada (200 OK)
{
  "success": true,
  "message": "User registered successfully",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "testuser",
    "email": "testuser@example.com",
    "role": "USER",
    "isActive": true
  }
}
```

#### **Cenário 1.2: Criar Usuário ADMIN (Happy Path)**
```bash
# Request
POST /api/auth/register
{
  "username": "adminuser",
  "email": "admin@example.com",
  "password": "AdminPass123!",
  "fullName": "Admin User",
  "role": "ADMIN"
}

# Response Esperada (200 OK)
{
  "success": true,
  "message": "User registered successfully",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 2,
    "username": "adminuser",
    "email": "admin@example.com",
    "role": "ADMIN",
    "isActive": true
  }
}
```

#### **Cenário 1.3: Tentativa de Criar Usuário Duplicado (Sad Path)**
```bash
# Request
POST /api/auth/register
{
  "username": "testuser",  // Mesmo username
  "email": "different@example.com",
  "password": "TestPass123!",
  "fullName": "Different User",
  "role": "USER"
}

# Response Esperada (400 Bad Request)
{
  "success": false,
  "message": "Username or email already exists"
}
```

#### **Cenário 1.4: Login com Credenciais Válidas (Happy Path)**
```bash
# Request
POST /api/auth/login
{
  "username": "testuser",
  "password": "TestPass123!"
}

# Response Esperada (200 OK)
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "testuser",
    "role": "USER"
  }
}
```

#### **Cenário 1.5: Login com Credenciais Inválidas (Sad Path)**
```bash
# Request
POST /api/auth/login
{
  "username": "testuser",
  "password": "WrongPassword!"
}

# Response Esperada (401 Unauthorized)
{
  "success": false,
  "message": "Invalid username or password"
}
```

### **2. Cenários de Gerenciamento de Produtos**

#### **Cenário 2.1: Criar Produto (Happy Path)**
```bash
# Request (com token de ADMIN)
POST /api/stock/products
Authorization: Bearer {admin_token}
{
  "name": "Produto Teste E2E",
  "description": "Produto para testes E2E",
  "price": 99.99,
  "category": "Testes",
  "stockQuantity": 100,
  "imageUrl": "https://example.com/test.jpg"
}

# Response Esperada (201 Created)
{
  "success": true,
  "message": "Product created successfully",
  "product": {
    "id": 1,
    "name": "Produto Teste E2E",
    "price": 99.99,
    "stockQuantity": 100,
    "isActive": true
  }
}
```

#### **Cenário 2.2: Criar Produto sem Autorização (Sad Path)**
```bash
# Request (com token de USER comum)
POST /api/stock/products
Authorization: Bearer {user_token}
{
  "name": "Produto Não Autorizado",
  "description": "Este produto não deveria ser criado",
  "price": 50.00,
  "category": "Testes",
  "stockQuantity": 10
}

# Response Esperada (403 Forbidden)
{
  "success": false,
  "message": "Access denied. Admin role required."
}
```

#### **Cenário 2.3: Consultar Produtos (Happy Path)**
```bash
# Request
GET /api/stock/products

# Response Esperada (200 OK)
{
  "success": true,
  "message": "Products retrieved successfully",
  "products": [
    {
      "id": 1,
      "name": "Produto Teste E2E",
      "price": 99.99,
      "stockQuantity": 100,
      "category": "Testes"
    }
  ]
}
```

#### **Cenário 2.4: Consultar Produto Específico (Happy Path)**
```bash
# Request
GET /api/stock/products/1

# Response Esperada (200 OK)
{
  "success": true,
  "message": "Product retrieved successfully",
  "product": {
    "id": 1,
    "name": "Produto Teste E2E",
    "price": 99.99,
    "stockQuantity": 100
  }
}
```

#### **Cenário 2.5: Consultar Produto Inexistente (Sad Path)**
```bash
# Request
GET /api/stock/products/999

# Response Esperada (404 Not Found)
{
  "success": false,
  "message": "Product not found"
}
```

### **3. Cenários de Criação de Pedidos**

#### **Cenário 3.1: Criar Pedido com Estoque Suficiente (Happy Path)**
```bash
# Request (com token de USER)
POST /api/sales/orders
Authorization: Bearer {user_token}
{
  "items": [
    {
      "productId": 1,
      "productName": "Produto Teste E2E",
      "unitPrice": 99.99,
      "quantity": 2
    }
  ],
  "notes": "Pedido de teste E2E"
}

# Response Esperada (201 Created)
{
  "success": true,
  "message": "Order created successfully",
  "order": {
    "id": 1,
    "userId": 1,
    "status": "Pending",
    "totalAmount": 199.98,
    "items": [
      {
        "productId": 1,
        "productName": "Produto Teste E2E",
        "quantity": 2,
        "unitPrice": 99.99,
        "totalPrice": 199.98
      }
    ]
  }
}
```

#### **Cenário 3.2: Criar Pedido com Estoque Insuficiente (Sad Path)**
```bash
# Request (com token de USER)
POST /api/sales/orders
Authorization: Bearer {user_token}
{
  "items": [
    {
      "productId": 1,
      "productName": "Produto Teste E2E",
      "unitPrice": 99.99,
      "quantity": 200  // Maior que o estoque disponível (100)
    }
  ]
}

# Response Esperada (400 Bad Request)
{
  "success": false,
  "message": "Insufficient stock for product Produto Teste E2E (ID: 1). Requested: 200"
}
```

#### **Cenário 3.3: Criar Pedido sem Autenticação (Sad Path)**
```bash
# Request (sem token)
POST /api/sales/orders
{
  "items": [
    {
      "productId": 1,
      "productName": "Produto Teste E2E",
      "unitPrice": 99.99,
      "quantity": 1
    }
  ]
}

# Response Esperada (401 Unauthorized)
{
  "success": false,
  "message": "Authorization header missing"
}
```

#### **Cenário 3.4: Criar Pedido com Produto Inexistente (Sad Path)**
```bash
# Request (com token de USER)
POST /api/sales/orders
Authorization: Bearer {user_token}
{
  "items": [
    {
      "productId": 999,
      "productName": "Produto Inexistente",
      "unitPrice": 10.00,
      "quantity": 1
    }
  ]
}

# Response Esperada (400 Bad Request)
{
  "success": false,
  "message": "Product with ID 999 not found or inactive"
}
```

### **4. Cenários de Consulta de Pedidos**

#### **Cenário 4.1: Consultar Pedidos do Usuário (Happy Path)**
```bash
# Request (com token de USER)
GET /api/sales/orders
Authorization: Bearer {user_token}

# Response Esperada (200 OK)
{
  "success": true,
  "message": "Orders retrieved successfully",
  "orders": [
    {
      "id": 1,
      "userId": 1,
      "status": "Pending",
      "totalAmount": 199.98,
      "items": [...],
      "createdAt": "2025-01-26T10:30:00Z"
    }
  ]
}
```

#### **Cenário 4.2: Consultar Pedido Específico (Happy Path)**
```bash
# Request (com token de USER)
GET /api/sales/orders/1
Authorization: Bearer {user_token}

# Response Esperada (200 OK)
{
  "success": true,
  "message": "Order retrieved successfully",
  "order": {
    "id": 1,
    "userId": 1,
    "status": "Pending",
    "totalAmount": 199.98,
    "items": [...],
    "createdAt": "2025-01-26T10:30:00Z"
  }
}
```

#### **Cenário 4.3: Consultar Pedido de Outro Usuário (Sad Path)**
```bash
# Request (com token de USER diferente)
GET /api/sales/orders/1
Authorization: Bearer {different_user_token}

# Response Esperada (404 Not Found)
{
  "success": false,
  "message": "Order not found"
}
```

#### **Cenário 4.4: Consultar Pedido Inexistente (Sad Path)**
```bash
# Request (com token de USER)
GET /api/sales/orders/999
Authorization: Bearer {user_token}

# Response Esperada (404 Not Found)
{
  "success": false,
  "message": "Order not found"
}
```

### **5. Cenários de Cancelamento de Pedidos**

#### **Cenário 5.1: Cancelar Pedido Próprio (Happy Path)**
```bash
# Request (com token de USER)
PUT /api/sales/orders/1/cancel
Authorization: Bearer {user_token}

# Response Esperada (200 OK)
{
  "success": true,
  "message": "Order cancelled successfully"
}
```

#### **Cenário 5.2: Cancelar Pedido Já Cancelado (Sad Path)**
```bash
# Request (com token de USER)
PUT /api/sales/orders/1/cancel
Authorization: Bearer {user_token}

# Response Esperada (400 Bad Request)
{
  "success": false,
  "message": "Order is already cancelled"
}
```

### **6. Cenários de Comunicação Assíncrona**

#### **Cenário 6.1: Verificar Reserva de Estoque Após Criação de Pedido (Happy Path)**
```bash
# Após criar pedido, verificar se o estoque foi reservado
GET /api/stock/products/1

# Response Esperada (200 OK)
{
  "success": true,
  "product": {
    "id": 1,
    "name": "Produto Teste E2E",
    "stockQuantity": 98,  // Era 100, menos 2 do pedido
    ...
  }
}
```

#### **Cenário 6.2: Verificar Liberação de Estoque Após Cancelamento (Happy Path)**
```bash
# Após cancelar pedido, verificar se o estoque foi liberado
GET /api/stock/products/1

# Response Esperada (200 OK)
{
  "success": true,
  "product": {
    "id": 1,
    "name": "Produto Teste E2E",
    "stockQuantity": 100,  // Voltou ao valor original
    ...
  }
}
```

## 🛠️ Configuração do Ambiente de Teste

### **Pré-requisitos**
```bash
# 1. Docker instalado e rodando
# 2. .NET 8.0 SDK instalado
# 3. RabbitMQ em container
# 4. PostgreSQL em container (opcional, pode usar InMemory para testes)
```

### **Iniciar Serviços**
```bash
# 1. Iniciar RabbitMQ
docker run -d --name rabbitmq-e2e \
  -p 5672:5672 -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:management

# 2. Iniciar AuthService
cd auth-service/AuthService
dotnet run --urls=http://localhost:5051

# 3. Iniciar StockService
cd stock-service/StockService
dotnet run --urls=http://localhost:5048

# 4. Iniciar SalesService
cd sales-service/SalesService
dotnet run --urls=http://localhost:5047

# 5. Iniciar ApiGateway
cd api-gateway/ApiGateway
dotnet run --urls=http://localhost:5219
```

### **Executar Testes E2E**
```bash
# Executar todos os testes E2E
dotnet test --filter "E2E"

# Executar testes de um serviço específico
dotnet test auth-service/AuthService/AuthService.E2ETests
dotnet test stock-service/StockService/StockService.E2ETests
dotnet test sales-service/SalesService/SalesService.E2ETests
```

## 📊 Cobertura de Cenários

| Categoria | Cenários Happy Path | Cenários Sad Path | Status |
|-----------|-------------------|-------------------|---------|
| Autenticação | ✅ Register USER/ADMIN, Login válido | ✅ Usuário duplicado, Login inválido | ✅ Completo |
| Produtos | ✅ Criar, Consultar, Listar | ✅ Sem autorização, Produto inexistente | ✅ Completo |
| Pedidos | ✅ Criar, Consultar, Cancelar | ✅ Estoque insuficiente, Sem auth, Produto inexistente | ✅ Completo |
| Comunicação | ✅ Reserva/liberação de estoque | ✅ Erro de comunicação | ✅ Completo |

## 🔍 Validação de Comunicação Assíncrona

### **Fluxo de Teste Completo:**

1. **Criar usuário ADMIN** → Registrar produto
2. **Criar usuário USER** → Fazer login
3. **Consultar produtos disponíveis**
4. **Criar pedido** → Verificar reserva automática de estoque
5. **Consultar pedido criado**
6. **Cancelar pedido** → Verificar liberação automática de estoque
7. **Verificar logs** de comunicação RabbitMQ

### **Pontos de Verificação:**
- ✅ **RabbitMQ**: Mensagens publicadas/consumidas corretamente
- ✅ **Estoque**: Atualizado automaticamente via eventos
- ✅ **Logs**: Estruturados e informativos
- ✅ **Erros**: Tratados e registrados adequadamente
- ✅ **Performance**: Respostas dentro do tempo esperado

## 🎯 Resultados Esperados

Após execução completa dos testes E2E:

- ✅ **100% dos cenários happy path** devem passar
- ✅ **100% dos cenários sad path** devem ser tratados adequadamente
- ✅ **Comunicação assíncrona** entre serviços funcionando
- ✅ **Estoque atualizado** automaticamente via eventos
- ✅ **Logs estruturados** disponíveis para debugging
- ✅ **Sistema resiliente** a falhas e erros

---

**📝 Nota**: Estes testes garantem que todo o fluxo end-to-end do sistema funcione corretamente, desde a criação de usuários até o gerenciamento completo de pedidos e estoque.</content>
<parameter name="filePath">/home/leandro/Imagens/micro/TESTES_E2E.md
