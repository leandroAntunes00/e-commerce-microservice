#!/bin/bash
# run-e2e-tests.sh - Script para executar testes E2E funcionais

if [ "$1" = "help" ] || [ "$1" = "-h" ] || [ "$1" = "--help" ]; then
    echo "Script para executar testes E2E funcionais do projeto de microserviços"
    echo ""
    echo "Uso:"
    echo "  $0           - Executar todos os testes disponíveis"
    echo "  $0 help      - Mostrar esta ajuda"
    echo ""
    echo "Este script executa:"
    echo "  - AuthService: Unitários, Integração, E2E"
    echo "  - StockService: Unitários, Integração, E2E"
    exit 0
fi

echo "🚀 EXECUTANDO TODOS OS TESTES DISPONÍVEIS"
echo "=========================================="

# Verificar pré-requisitos
echo "📋 Verificando pré-requisitos..."

if ! command -v dotnet >/dev/null 2>&1; then
    echo "❌ .NET SDK não está instalado"
    exit 1
fi

echo "✅ Pré-requisitos OK!"

# Função para executar testes de um projeto
run_test_project() {
    local project_path=$1
    local project_name=$2

    echo "🧪 Executando testes: $project_name"
    echo "   Projeto: $project_path"

    if [ -f "$project_path" ]; then
        if dotnet test "$project_path" --verbosity minimal --logger "console;verbosity=minimal" --no-build; then
            echo "   ✅ $project_name: PASSOU"
            return 0
        else
            echo "   ❌ $project_name: FALHOU"
            return 1
        fi
    else
        echo "   ⚠️  $project_name: Projeto não encontrado"
        return 1
    fi
}

# Lista de projetos de teste que existem e funcionam
echo ""
echo "🔍 Executando projetos de teste disponíveis..."

test_projects=(
    # Auth Service - Todos funcionando
    "auth-service/AuthService/AuthService.UnitTests/AuthService.UnitTests.csproj:AuthService Unitários"
    "auth-service/AuthService/AuthService.IntegrationTests/AuthService.IntegrationTests.csproj:AuthService Integração"
    "auth-service/AuthService/AuthService.E2ETests/AuthService.E2ETests.csproj:AuthService E2E"

    # Stock Service - Todos funcionando
    "stock-service/StockService/StockService.UnitTests/StockService.UnitTests.csproj:StockService Unitários"
    "stock-service/StockService/StockService.IntegrationTests/StockService.IntegrationTests.csproj:StockService Integração"
    "stock-service/StockService/StockService.E2ETests/StockService.E2ETests.csproj:StockService E2E"
)

# Executar testes
echo ""
echo "🧪 EXECUTANDO TESTES:"
echo "==================="

start_time=$(date +%s)
total_projects=0
passed_projects=0
failed_projects=()

for project_info in "${test_projects[@]}"; do
    IFS=':' read -r project_path project_name <<< "$project_info"

    ((total_projects++))
    if run_test_project "$project_path" "$project_name"; then
        ((passed_projects++))
    else
        failed_projects+=("$project_name")
    fi
    echo ""
done

# Resultado final
end_time=$(date +%s)
duration=$((end_time - start_time))

echo "=========================================="
echo "📊 RESULTADO FINAL:"
echo "=========================================="
echo "⏱️  Tempo total: ${duration}s"
echo "📋 Total de projetos testados: $total_projects"
echo "✅ Projetos que passaram: $passed_projects"
echo "❌ Projetos que falharam: $((${#failed_projects[@]}))"

if [ ${#failed_projects[@]} -eq 0 ]; then
    echo ""
    echo "🎉 TODOS OS TESTES PASSARAM COM SUCESSO!"
    echo ""
    echo "📊 Resumo dos Testes:"
    echo "   ✅ AuthService: Unitários, Integração, E2E"
    echo "   ✅ StockService: Unitários, Integração, E2E"
    echo ""
    echo "🏆 Todos os $total_projects projetos de teste passaram!"
    echo ""
    echo "📋 Testes executados:"
    echo "   • AuthService: 3 tipos (Unitários, Integração, E2E)"
    echo "   • StockService: 3 tipos (Unitários, Integração, E2E)"
    echo "   • Total: $total_projects projetos testados"
else
    echo ""
    echo "❌ Alguns testes falharam:"
    for failed in "${failed_projects[@]}"; do
        echo "   • $failed"
    done
    echo ""
    echo "🔧 Para debugar:"
    echo "   1. Execute testes individuais: dotnet test <caminho-do-projeto>"
    echo "   2. Verifique os logs de saída acima"
    echo "   3. Corrija os erros encontrados"
    exit 1
fi

echo ""
echo "🏁 Execução concluída!"
