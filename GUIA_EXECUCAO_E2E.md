# 🚀 Guia de Execução dos Testes E2E

## 📋 Scripts de Teste Práticos

### **Script 1: Configuração do Ambiente**
```bash
#!/bin/bash
# setup-e2e-environment.sh

echo "🚀 Configurando ambiente de testes E2E..."

# 1. Iniciar RabbitMQ
echo "📦 Iniciando RabbitMQ..."
docker run -d --name rabbitmq-e2e \
  -p 5672:5672 -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:management

# 2. Aguardar RabbitMQ ficar pronto
echo "⏳ Aguardando RabbitMQ..."
sleep 30

# 3. Verificar se RabbitMQ está saudável
curl -f http://localhost:15672/api/overview || echo "RabbitMQ não está pronto"

echo "✅ Ambiente configurado com sucesso!"
```

### **Script 2: Teste Completo de Fluxo E2E**
```bash
#!/bin/bash
# run-complete-e2e-test.sh

BASE_URL="http://localhost:5219"  # API Gateway
AUTH_URL="http://localhost:5051"  # Auth Service direto

echo "🧪 Iniciando teste completo E2E..."

# ===========================================
# 1. TESTE DE AUTENTICAÇÃO
# ===========================================

echo "1️⃣  Testando criação de usuário ADMIN..."
ADMIN_REGISTER_RESPONSE=$(curl -s -X POST $AUTH_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin_e2e",
    "email": "admin.e2e@example.com",
    "password": "AdminE2E123!",
    "fullName": "Admin E2E",
    "role": "ADMIN"
  }')

echo "Resposta registro admin: $ADMIN_REGISTER_RESPONSE"

# Extrair token do admin
ADMIN_TOKEN=$(echo $ADMIN_REGISTER_RESPONSE | jq -r '.token')
echo "Token admin obtido: ${ADMIN_TOKEN:0:50}..."

echo "2️⃣  Testando criação de usuário comum..."
USER_REGISTER_RESPONSE=$(curl -s -X POST $AUTH_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "user_e2e",
    "email": "user.e2e@example.com",
    "password": "UserE2E123!",
    "fullName": "User E2E",
    "role": "USER"
  }')

echo "Resposta registro user: $USER_REGISTER_RESPONSE"

# Extrair token do user
USER_TOKEN=$(echo $USER_REGISTER_RESPONSE | jq -r '.token')
echo "Token user obtido: ${USER_TOKEN:0:50}..."

# ===========================================
# 2. TESTE DE PRODUTOS
# ===========================================

echo "3️⃣  Testando criação de produto..."
PRODUCT_CREATE_RESPONSE=$(curl -s -X POST $BASE_URL/api/stock/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "name": "Produto E2E Teste",
    "description": "Produto criado para testes E2E",
    "price": 99.99,
    "category": "Testes E2E",
    "stockQuantity": 50,
    "imageUrl": "https://example.com/e2e-test.jpg"
  }')

echo "Resposta criação produto: $PRODUCT_CREATE_RESPONSE"

# Extrair ID do produto
PRODUCT_ID=$(echo $PRODUCT_CREATE_RESPONSE | jq -r '.product.id')
echo "Produto criado com ID: $PRODUCT_ID"

echo "4️⃣  Testando consulta de produtos..."
PRODUCTS_LIST_RESPONSE=$(curl -s -X GET $BASE_URL/api/stock/products)
echo "Lista de produtos: $PRODUCTS_LIST_RESPONSE"

# ===========================================
# 3. TESTE DE PEDIDOS
# ===========================================

echo "5️⃣  Testando criação de pedido..."
ORDER_CREATE_RESPONSE=$(curl -s -X POST $BASE_URL/api/sales/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -d "{
    \"items\": [
      {
        \"productId\": $PRODUCT_ID,
        \"productName\": \"Produto E2E Teste\",
        \"unitPrice\": 99.99,
        \"quantity\": 5
      }
    ],
    \"notes\": \"Pedido criado via teste E2E\"
  }")

echo "Resposta criação pedido: $ORDER_CREATE_RESPONSE"

# Extrair ID do pedido
ORDER_ID=$(echo $ORDER_CREATE_RESPONSE | jq -r '.order.id')
echo "Pedido criado com ID: $ORDER_ID"

echo "6️⃣  Testando consulta de pedidos do usuário..."
USER_ORDERS_RESPONSE=$(curl -s -X GET $BASE_URL/api/sales/orders \
  -H "Authorization: Bearer $USER_TOKEN")
echo "Pedidos do usuário: $USER_ORDERS_RESPONSE"

echo "7️⃣  Testando consulta de pedido específico..."
SPECIFIC_ORDER_RESPONSE=$(curl -s -X GET $BASE_URL/api/sales/orders/$ORDER_ID \
  -H "Authorization: Bearer $USER_TOKEN")
echo "Pedido específico: $SPECIFIC_ORDER_RESPONSE"

# ===========================================
# 4. TESTE DE COMUNICAÇÃO ASSÍNCRONA
# ===========================================

echo "8️⃣  Verificando reserva de estoque após criação do pedido..."
sleep 2  # Aguardar processamento assíncrono

PRODUCT_AFTER_ORDER_RESPONSE=$(curl -s -X GET $BASE_URL/api/stock/products/$PRODUCT_ID)
STOCK_AFTER_ORDER=$(echo $PRODUCT_AFTER_ORDER_RESPONSE | jq -r '.product.stockQuantity')
echo "Estoque após pedido: $STOCK_AFTER_ORDER (deve ser 45)"

# ===========================================
# 5. TESTE DE CANCELAMENTO
# ===========================================

echo "9️⃣  Testando cancelamento de pedido..."
ORDER_CANCEL_RESPONSE=$(curl -s -X PUT $BASE_URL/api/sales/orders/$ORDER_ID/cancel \
  -H "Authorization: Bearer $USER_TOKEN")
echo "Resposta cancelamento: $ORDER_CANCEL_RESPONSE"

# ===========================================
# 6. VERIFICAÇÃO FINAL
# ===========================================

echo "🔟 Verificando liberação de estoque após cancelamento..."
sleep 2  # Aguardar processamento assíncrono

PRODUCT_AFTER_CANCEL_RESPONSE=$(curl -s -X GET $BASE_URL/api/stock/products/$PRODUCT_ID)
STOCK_AFTER_CANCEL=$(echo $PRODUCT_AFTER_CANCEL_RESPONSE | jq -r '.product.stockQuantity')
echo "Estoque após cancelamento: $STOCK_AFTER_CANCEL (deve ser 50)"

# ===========================================
# 7. VALIDAÇÃO DOS RESULTADOS
# ===========================================

echo ""
echo "📊 VALIDAÇÃO DOS RESULTADOS:"
echo "================================"

# Verificar se estoque foi reservado corretamente
if [ "$STOCK_AFTER_ORDER" = "45" ]; then
    echo "✅ Estoque reservado corretamente (50 -> 45)"
else
    echo "❌ Erro: Estoque não foi reservado corretamente"
fi

# Verificar se estoque foi liberado após cancelamento
if [ "$STOCK_AFTER_CANCEL" = "50" ]; then
    echo "✅ Estoque liberado corretamente após cancelamento (45 -> 50)"
else
    echo "❌ Erro: Estoque não foi liberado corretamente"
fi

# Verificar se pedido foi criado
if [ "$ORDER_ID" != "null" ] && [ "$ORDER_ID" != "" ]; then
    echo "✅ Pedido criado com sucesso (ID: $ORDER_ID)"
else
    echo "❌ Erro: Pedido não foi criado"
fi

echo ""
echo "🎉 Teste E2E concluído!"
```

### **Script 3: Teste de Cenários de Erro (Sad Path)**
```bash
#!/bin/bash
# run-sad-path-e2e-test.sh

BASE_URL="http://localhost:5219"
AUTH_URL="http://localhost:5051"

echo "😞 Executando testes de caminho triste (Sad Path)..."

# ===========================================
# 1. TENTATIVA DE CRIAR USUÁRIO DUPLICADO
# ===========================================

echo "1️⃣  Testando criação de usuário duplicado..."
DUPLICATE_USER_RESPONSE=$(curl -s -X POST $AUTH_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin_e2e",
    "email": "different@example.com",
    "password": "DifferentPass123!",
    "fullName": "Different Admin",
    "role": "ADMIN"
  }')

echo "Resposta usuário duplicado: $DUPLICATE_USER_RESPONSE"

# ===========================================
# 2. TENTATIVA DE CRIAR PRODUTO SEM AUTORIZAÇÃO
# ===========================================

echo "2️⃣  Testando criação de produto sem autorização..."
# Primeiro obter token de user comum
USER_TOKEN_RESPONSE=$(curl -s -X POST $AUTH_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "user_e2e",
    "email": "user.e2e@example.com",
    "password": "UserE2E123!"
  }')

USER_TOKEN=$(echo $USER_TOKEN_RESPONSE | jq -r '.token')

UNAUTHORIZED_PRODUCT_RESPONSE=$(curl -s -X POST $BASE_URL/api/stock/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -d '{
    "name": "Produto Não Autorizado",
    "description": "Este produto não deveria ser criado",
    "price": 50.00,
    "category": "Testes",
    "stockQuantity": 10
  }')

echo "Resposta produto não autorizado: $UNAUTHORIZED_PRODUCT_RESPONSE"

# ===========================================
# 3. TENTATIVA DE CRIAR PEDIDO COM ESTOQUE INSUFICIENTE
# ===========================================

echo "3️⃣  Testando criação de pedido com estoque insuficiente..."
INSUFFICIENT_STOCK_RESPONSE=$(curl -s -X POST $BASE_URL/api/sales/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -d '{
    "items": [
      {
        "productId": 1,
        "productName": "Produto E2E Teste",
        "unitPrice": 99.99,
        "quantity": 100
      }
    ],
    "notes": "Pedido com quantidade excessiva"
  }')

echo "Resposta estoque insuficiente: $INSUFFICIENT_STOCK_RESPONSE"

# ===========================================
# 4. TENTATIVA DE CRIAR PEDIDO SEM AUTENTICAÇÃO
# ===========================================

echo "4️⃣  Testando criação de pedido sem autenticação..."
NO_AUTH_RESPONSE=$(curl -s -X POST $BASE_URL/api/sales/orders \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {
        "productId": 1,
        "productName": "Produto E2E Teste",
        "unitPrice": 99.99,
        "quantity": 1
      }
    ]
  }')

echo "Resposta sem autenticação: $NO_AUTH_RESPONSE"

# ===========================================
# 5. TENTATIVA DE CONSULTAR PEDIDO DE OUTRO USUÁRIO
# ===========================================

echo "5️⃣  Testando consulta de pedido de outro usuário..."
# Criar outro usuário
OTHER_USER_RESPONSE=$(curl -s -X POST $AUTH_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "other_user",
    "email": "other@example.com",
    "password": "OtherPass123!",
    "fullName": "Other User",
    "role": "USER"
  }')

OTHER_USER_TOKEN=$(echo $OTHER_USER_RESPONSE | jq -r '.token')

OTHER_USER_ORDER_RESPONSE=$(curl -s -X GET $BASE_URL/api/sales/orders/1 \
  -H "Authorization: Bearer $OTHER_USER_TOKEN")

echo "Resposta pedido de outro usuário: $OTHER_USER_ORDER_RESPONSE"

# ===========================================
# 6. VALIDAÇÃO DOS CENÁRIOS DE ERRO
# ===========================================

echo ""
echo "📊 VALIDAÇÃO DOS CENÁRIOS DE ERRO:"
echo "==================================="

# Verificar usuário duplicado
if [[ $DUPLICATE_USER_RESPONSE == *"already exists"* ]]; then
    echo "✅ Usuário duplicado rejeitado corretamente"
else
    echo "❌ Erro: Usuário duplicado não foi rejeitado"
fi

# Verificar produto não autorizado
if [[ $UNAUTHORIZED_PRODUCT_RESPONSE == *"Access denied"* ]]; then
    echo "✅ Produto não autorizado rejeitado corretamente"
else
    echo "❌ Erro: Produto não autorizado não foi rejeitado"
fi

# Verificar estoque insuficiente
if [[ $INSUFFICIENT_STOCK_RESPONSE == *"Insufficient stock"* ]]; then
    echo "✅ Pedido com estoque insuficiente rejeitado corretamente"
else
    echo "❌ Erro: Pedido com estoque insuficiente não foi rejeitado"
fi

# Verificar sem autenticação
if [[ $NO_AUTH_RESPONSE == *"401"* ]] || [[ $NO_AUTH_RESPONSE == *"Unauthorized"* ]]; then
    echo "✅ Pedido sem autenticação rejeitado corretamente"
else
    echo "❌ Erro: Pedido sem autenticação não foi rejeitado"
fi

# Verificar pedido de outro usuário
if [[ $OTHER_USER_ORDER_RESPONSE == *"not found"* ]]; then
    echo "✅ Consulta de pedido de outro usuário rejeitada corretamente"
else
    echo "❌ Erro: Consulta de pedido de outro usuário não foi rejeitada"
fi

echo ""
echo "🎯 Testes de Sad Path concluídos!"
```

### **Script 4: Limpeza do Ambiente**
```bash
#!/bin/bash
# cleanup-e2e-environment.sh

echo "🧹 Limpando ambiente de testes E2E..."

# 1. Parar e remover container RabbitMQ
echo "Parando RabbitMQ..."
docker stop rabbitmq-e2e
docker rm rabbitmq-e2e

# 2. Limpar dados de teste (se houver banco de dados)
echo "Limpando dados de teste..."
# Adicionar comandos para limpar banco se necessário

echo "✅ Ambiente limpo com sucesso!"
```

## 🛠️ Como Usar os Scripts

### **Execução Completa dos Testes**
```bash
# 1. Tornar scripts executáveis
chmod +x *.sh

# 2. Executar configuração
./setup-e2e-environment.sh

# 3. Aguardar serviços ficarem prontos
# (Iniciar AuthService, StockService, SalesService, ApiGateway manualmente)

# 4. Executar teste completo
./run-complete-e2e-test.sh

# 5. Executar testes de erro
./run-sad-path-e2e-test.sh

# 6. Limpar ambiente
./cleanup-e2e-environment.sh
```

### **Execução Individual de Cenários**
```bash
# Testar apenas autenticação
curl -X POST http://localhost:5051/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@example.com","password":"Test123!","fullName":"Test User","role":"USER"}'

# Testar apenas produtos
curl -X GET http://localhost:5219/api/stock/products

# Testar apenas pedidos
curl -X POST http://localhost:5219/api/sales/orders \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"items":[{"productId":1,"productName":"Test","unitPrice":10.00,"quantity":1}]}'
```

## 📊 Monitoramento Durante os Testes

### **Monitor RabbitMQ**
```bash
# Acessar interface web
open http://localhost:15672

# Verificar filas via API
curl -u guest:guest http://localhost:15672/api/queues

# Verificar conexões
curl -u guest:guest http://localhost:15672/api/connections
```

### **Logs dos Serviços**
```bash
# AuthService logs
tail -f /path/to/auth-service/logs

# StockService logs
tail -f /path/to/stock-service/logs

# SalesService logs
tail -f /path/to/sales-service/logs
```

### **Monitoramento de Banco de Dados**
```bash
# Se usando PostgreSQL
docker exec -it postgres-container psql -U postgres -d microservices_db

# Verificar tabelas
\dt
SELECT * FROM products;
SELECT * FROM orders;
SELECT * FROM users;
```

## 🎯 Cenários de Teste Automatizados

### **Estrutura de Testes com xUnit**
```csharp
// Exemplo de teste E2E automatizado
[Fact]
public async Task CompleteOrderFlow_ShouldWorkCorrectly()
{
    // Arrange
    var client = _factory.CreateClient();
    var adminToken = await RegisterAndLoginAdminAsync(client);
    var userToken = await RegisterAndLoginUserAsync(client);

    // Act
    var productId = await CreateProductAsync(client, adminToken);
    var orderId = await CreateOrderAsync(client, userToken, productId);

    // Assert
    var order = await GetOrderAsync(client, userToken, orderId);
    var product = await GetProductAsync(client, productId);

    Assert.NotNull(order);
    Assert.Equal(45, product.StockQuantity); // 50 - 5
}
```

---

**💡 Dica**: Execute os testes em ordem sequencial para garantir que os dados de teste estejam disponíveis para os próximos cenários.</content>
<parameter name="filePath">/home/leandro/Imagens/micro/GUIA_EXECUCAO_E2E.md
