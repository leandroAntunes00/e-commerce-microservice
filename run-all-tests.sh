#!/bin/bash
# run-all-tests.sh - Script para executar todos os testes do projeto

set -e  # Parar execução em caso de erro

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Função para log
log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

success() {
    echo -e "${GREEN}✅ $1${NC}"
}

error() {
    echo -e "${RED}❌ $1${NC}"
}

warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

# Função para executar testes de um projeto
run_test_project() {
    local project_path=$1
    local project_name=$2
    local test_type=$3

    log "Executando testes $test_type: $project_name"

    if [ -d "$project_path" ]; then
        cd "$project_path"

        # Verificar se é um projeto de teste
        if [ -f "*.csproj" ] || find . -name "*.csproj" -type f | grep -q .; then
            if dotnet test --verbosity minimal --logger "console;verbosity=minimal"; then
                success "Testes $test_type de $project_name passaram!"
                return 0
            else
                error "Testes $test_type de $project_name falharam!"
                return 1
            fi
        else
            warning "Projeto $project_name não encontrado ou não é um projeto .NET válido"
            return 1
        fi
    else
        warning "Diretório $project_path não encontrado"
        return 1
    fi
}

# Função para executar todos os testes unitários
run_unit_tests() {
    log "=== EXECUTANDO TESTES UNITÁRIOS ==="

    local unit_test_projects=(
        "/home/leandro/Documentos/micro/auth-service/AuthService/AuthService.UnitTests:AuthService"
        "/home/leandro/Documentos/micro/stock-service/StockService/StockService.UnitTests:StockService"
        "/home/leandro/Documentos/micro/sales-service/SalesService.UnitTests:SalesService"
        "/home/leandro/Documentos/micro/api-gateway/ApiGateway.UnitTests:ApiGateway"
    )

    local failed_tests=()

    for project_info in "${unit_test_projects[@]}"; do
        IFS=':' read -r project_path project_name <<< "$project_info"

        if ! run_test_project "$project_path" "$project_name" "Unitários"; then
            failed_tests+=("$project_name Unitários")
        fi
    done

    if [ ${#failed_tests[@]} -eq 0 ]; then
        success "Todos os testes unitários passaram!"
    else
        error "Testes unitários que falharam: ${failed_tests[*]}"
        return 1
    fi
}

# Função para executar todos os testes de integração
run_integration_tests() {
    log "=== EXECUTANDO TESTES DE INTEGRAÇÃO ==="

    local integration_test_projects=(
        "/home/leandro/Documentos/micro/auth-service/AuthService/AuthService.IntegrationTests:AuthService"
        "/home/leandro/Documentos/micro/stock-service/StockService/StockService.IntegrationTests:StockService"
        "/home/leandro/Documentos/micro/sales-service/SalesService.IntegrationTests:SalesService"
        "/home/leandro/Documentos/micro/api-gateway/ApiGateway.IntegrationTests:ApiGateway"
    )

    local failed_tests=()

    for project_info in "${integration_test_projects[@]}"; do
        IFS=':' read -r project_path project_name <<< "$project_info"

        if ! run_test_project "$project_path" "$project_name" "de Integração"; then
            failed_tests+=("$project_name Integração")
        fi
    done

    if [ ${#failed_tests[@]} -eq 0 ]; then
        success "Todos os testes de integração passaram!"
    else
        error "Testes de integração que falharam: ${failed_tests[*]}"
        return 1
    fi
}

# Função para executar todos os testes E2E
run_e2e_tests() {
    log "=== EXECUTANDO TESTES E2E ==="

    local e2e_test_projects=(
        "/home/leandro/Documentos/micro/auth-service/AuthService/AuthService.E2ETests:AuthService"
        "/home/leandro/Documentos/micro/stock-service/StockService/StockService.E2ETests:StockService"
        "/home/leandro/Documentos/micro/sales-service/SalesService.E2ETests:SalesService"
        "/home/leandro/Documentos/micro/api-gateway/ApiGateway.E2ETests:ApiGateway"
    )

    local failed_tests=()

    for project_info in "${e2e_test_projects[@]}"; do
        IFS=':' read -r project_path project_name <<< "$project_info"

        if ! run_test_project "$project_path" "$project_name" "E2E"; then
            failed_tests+=("$project_name E2E")
        fi
    done

    if [ ${#failed_tests[@]} -eq 0 ]; then
        success "Todos os testes E2E passaram!"
    else
        error "Testes E2E que falharam: ${failed_tests[*]}"
        return 1
    fi
}

# Função para executar apenas testes E2E funcionais (cenários reais)
run_functional_e2e_tests() {
    log "=== EXECUTANDO TESTES E2E FUNCIONAIS (Cenários Reais) ==="

    # Verificar se os scripts E2E existem
    if [ ! -f "/home/leandro/Documentos/micro/setup-e2e-environment.sh" ]; then
        error "Scripts E2E não encontrados. Execute primeiro a configuração."
        return 1
    fi

    # Executar testes funcionais completos
    log "Configurando ambiente E2E..."
    if ! ./setup-e2e-environment.sh; then
        error "Falha ao configurar ambiente E2E"
        return 1
    fi

    log "Executando cenários Happy Path..."
    if ! ./run-complete-e2e-test.sh; then
        error "Falha nos cenários Happy Path"
        return 1
    fi

    log "Executando cenários Sad Path..."
    if ! ./run-sad-path-e2e-test.sh; then
        error "Falha nos cenários Sad Path"
        return 1
    fi

    log "Gerando relatório..."
    if ./generate-e2e-report.sh; then
        success "Relatório E2E gerado com sucesso!"
    else
        warning "Falha ao gerar relatório, mas testes foram executados"
    fi

    success "Todos os testes E2E funcionais passaram!"
}

# Função para executar todos os testes
run_all_tests() {
    log "🚀 INICIANDO EXECUÇÃO COMPLETA DE TODOS OS TESTES"
    echo "======================================================="

    local start_time=$(date +%s)
    local failed_categories=()

    # 1. Executar testes unitários
    if ! run_unit_tests; then
        failed_categories+=("Unitários")
    fi

    # 2. Executar testes de integração
    if ! run_integration_tests; then
        failed_categories+=("Integração")
    fi

    # 3. Executar testes E2E
    if ! run_e2e_tests; then
        failed_categories+=("E2E")
    fi

    # 4. Executar testes E2E funcionais (cenários reais)
    if ! run_functional_e2e_tests; then
        failed_categories+=("E2E Funcionais")
    fi

    # Calcular tempo total
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))

    echo ""
    echo "======================================================="
    log "⏱️  TEMPO TOTAL DE EXECUÇÃO: ${duration}s"

    if [ ${#failed_categories[@]} -eq 0 ]; then
        success "🎉 TODOS OS TESTES PASSARAM COM SUCESSO!"
        echo ""
        echo "📊 Resumo:"
        echo "   ✅ Testes Unitários: OK"
        echo "   ✅ Testes de Integração: OK"
        echo "   ✅ Testes E2E: OK"
        echo "   ✅ Testes E2E Funcionais: OK"
        echo ""
        echo "📋 Relatórios gerados:"
        echo "   - Relatório E2E: e2e-report.md"
        echo "   - Logs detalhados nos diretórios dos projetos"
    else
        error "❌ Alguns testes falharam: ${failed_categories[*]}"
        echo ""
        echo "🔧 Para debugar:"
        echo "   1. Verifique os logs nos diretórios dos projetos"
        echo "   2. Execute testes individuais: dotnet test <project>"
        echo "   3. Verifique conectividade com RabbitMQ"
        return 1
    fi
}

# Função para mostrar ajuda
show_help() {
    echo "Script para executar todos os testes do projeto de microserviços"
    echo ""
    echo "Uso:"
    echo "  $0 [opção]"
    echo ""
    echo "Opções:"
    echo "  all          - Executar TODOS os testes (Unitários + Integração + E2E + E2E Funcionais)"
    echo "  unit         - Executar apenas testes unitários"
    echo "  integration  - Executar apenas testes de integração"
    echo "  e2e          - Executar apenas testes E2E automatizados"
    echo "  functional   - Executar apenas testes E2E funcionais (cenários reais)"
    echo "  help         - Mostrar esta ajuda"
    echo ""
    echo "Exemplos:"
    echo "  $0 all          # Executar tudo"
    echo "  $0 unit         # Apenas unitários"
    echo "  $0 functional   # Cenários E2E reais"
    echo ""
    echo "Pré-requisitos:"
    echo "  - .NET 8.0 SDK instalado"
    echo "  - Docker instalado (para testes E2E)"
    echo "  - RabbitMQ via Docker (para testes funcionais)"
}

# Função principal
main() {
    local option=${1:-"all"}

    case $option in
        "all")
            run_all_tests
            ;;
        "unit")
            run_unit_tests
            ;;
        "integration")
            run_integration_tests
            ;;
        "e2e")
            run_e2e_tests
            ;;
        "functional")
            run_functional_e2e_tests
            ;;
        "help"|"-h"|"--help")
            show_help
            ;;
        *)
            error "Opção inválida: $option"
            echo ""
            show_help
            exit 1
            ;;
    esac
}

# Executar função principal com os argumentos passados

# Executar função principal com os argumentos passados
main "$@"
