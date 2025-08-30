#!/bin/bash
# verify-setup.sh - Verifica se o ambiente está pronto para execução

echo "🔍 VERIFICANDO CONFIGURAÇÃO DO AMBIENTE"
echo "========================================"

# Verificar Docker
echo "🐳 Verificando Docker..."
if ! command -v docker &> /dev/null; then
    echo "❌ Docker não está instalado"
    exit 1
fi

if ! docker info &> /dev/null; then
    echo "❌ Docker não está rodando"
    exit 1
fi
echo "✅ Docker OK"

# Verificar Docker Compose
echo "📦 Verificando Docker Compose..."
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo "❌ Docker Compose não está instalado"
    exit 1
fi
echo "✅ Docker Compose OK"

# Verificar arquivos necessários
echo "📁 Verificando arquivos..."
files=(
    "infra/docker-compose.yml"
    "api-gateway/ApiGateway/Dockerfile"
    "auth-service/AuthService/Dockerfile"
    "stock-service/StockService/Dockerfile"
    "sales-service/SalesService/Dockerfile"
    ".dockerignore"
)

for file in "${files[@]}"; do
    if [ ! -f "$file" ]; then
        echo "❌ Arquivo não encontrado: $file"
        exit 1
    fi
done
echo "✅ Todos os arquivos necessários existem"

# Verificar portas disponíveis
echo "🔌 Verificando portas..."
ports=(5000 5001 5002 5003 5432 5433 5434 5672 15672)

for port in "${ports[@]}"; do
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null; then
        echo "⚠️  Porta $port já está em uso"
    fi
done
echo "✅ Verificação de portas concluída"

echo ""
echo "🎉 CONFIGURAÇÃO VERIFICADA COM SUCESSO!"
echo ""
echo "💡 PRÓXIMOS PASSOS:"
echo "   1. make build-up     # Construir e subir tudo"
echo "   2. make status       # Verificar status"
echo "   3. make logs         # Ver logs se houver problemas"
echo ""
echo "📊 SERVIÇOS DISPONÍVEIS APÓS INICIALIZAÇÃO:"
echo "   • API Gateway:    http://localhost:5000"
echo "   • Auth Service:   http://localhost:5001"
echo "   • Sales Service:  http://localhost:5002"
echo "   • Stock Service:  http://localhost:5003"
echo "   • RabbitMQ UI:    http://localhost:15672 (guest/guest)"
