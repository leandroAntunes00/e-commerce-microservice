# PowerShell Scripts para Windows - Microservices Project

## 📋 Scripts PowerShell

Para usuários Windows que preferem PowerShell, criamos scripts mais avançados com melhor tratamento de erros e funcionalidades extras.

## 🚀 Como Usar

### 1. Executar Scripts
```powershell
# Permitir execução de scripts (primeira vez)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Executar script
.\deploy.ps1 -Command fresh-start
```

### 2. Ou usar diretamente
```powershell
# Importar funções
. .\deploy.ps1

# Usar comandos
Start-FreshDeployment
Get-ServiceStatus
```

## 📄 deploy.ps1

```powershell
<#
.SYNOPSIS
    Scripts PowerShell para gerenciamento do projeto Microservices

.DESCRIPTION
    Conjunto de funções PowerShell para facilitar o gerenciamento dos containers Docker
    do projeto de microsserviços no Windows.

.EXAMPLE
    .\deploy.ps1 -Command fresh-start
    .\deploy.ps1 -Command status
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("build", "up", "down", "clean", "restart", "logs", "status", "rebuild", "fresh-start", "dev", "info", "help")]
    [string]$Command
)

# Configurações
$ComposeFile = "infra/docker-compose.yml"
$ProjectName = "microservices"

# Cores para output
$Colors = @{
    Green = "Green"
    Blue = "Cyan"
    Yellow = "Yellow"
    Red = "Red"
    White = "White"
}

# Função para imprimir colorido
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Verificar pré-requisitos
function Test-Prerequisites {
    try {
        $dockerVersion = docker --version 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "❌ ERRO: Docker não está instalado ou não está no PATH" $Colors.Red
            Write-ColorOutput "Instale o Docker Desktop: https://www.docker.com/products/docker-desktop" $Colors.Yellow
            exit 1
        }

        $composeVersion = docker compose version 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "❌ ERRO: Docker Compose não está disponível" $Colors.Red
            Write-ColorOutput "Certifique-se de que o Docker Compose V2 está instalado" $Colors.Yellow
            exit 1
        }

        Write-ColorOutput "✅ Pré-requisitos verificados com sucesso" $Colors.Green
    }
    catch {
        Write-ColorOutput "❌ Erro ao verificar pré-requisitos: $($_.Exception.Message)" $Colors.Red
        exit 1
    }
}

# Construir imagens
function Start-Build {
    Write-ColorOutput "🔨 Construindo imagens Docker..." $Colors.Blue
    try {
        & docker compose -f $ComposeFile build --pull
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Imagens construídas com sucesso!" $Colors.Green
        } else {
            Write-ColorOutput "❌ Erro ao construir imagens" $Colors.Red
            exit 1
        }
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
        exit 1
    }
}

# Subir containers
function Start-Containers {
    Write-ColorOutput "🚀 Subindo containers..." $Colors.Blue
    try {
        & docker compose -f $ComposeFile up -d
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Containers iniciados!" $Colors.Green
            Get-ServiceStatus
        } else {
            Write-ColorOutput "❌ Erro ao subir containers" $Colors.Red
            exit 1
        }
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
        exit 1
    }
}

# Parar containers
function Stop-Containers {
    Write-ColorOutput "🛑 Parando containers..." $Colors.Blue
    try {
        & docker compose -f $ComposeFile down
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Containers parados!" $Colors.Green
        } else {
            Write-ColorOutput "❌ Erro ao parar containers" $Colors.Red
        }
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
    }
}

# Limpar tudo
function Clear-All {
    Write-ColorOutput "⚠️  ATENÇÃO: Isso irá remover containers, imagens e volumes (dados serão perdidos)!" $Colors.Red
    $confirmation = Read-Host "Digite 'SIM' para confirmar"
    if ($confirmation -ne "SIM") {
        Write-ColorOutput "Operação cancelada" $Colors.Yellow
        return
    }

    Write-ColorOutput "🧹 Limpando tudo..." $Colors.Blue
    try {
        & docker compose -f $ComposeFile down --remove-orphans -v --rmi all
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Limpeza completa realizada!" $Colors.Green
        } else {
            Write-ColorOutput "❌ Erro durante limpeza" $Colors.Red
        }
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
    }
}

# Reiniciar serviços
function Restart-Services {
    Write-ColorOutput "🔄 Reiniciando serviços..." $Colors.Blue
    try {
        & docker compose -f $ComposeFile restart
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Serviços reiniciados!" $Colors.Green
        } else {
            Write-ColorOutput "❌ Erro ao reiniciar serviços" $Colors.Red
        }
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
    }
}

# Ver logs
function Get-Logs {
    Write-ColorOutput "📋 Logs dos serviços (pressione Ctrl+C para sair):" $Colors.Blue
    try {
        & docker compose -f $ComposeFile logs -f
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
    }
}

# Ver status
function Get-ServiceStatus {
    Write-ColorOutput "📊 Status dos containers:" $Colors.Blue
    try {
        & docker compose -f $ComposeFile ps
        Write-ColorOutput "`n💡 Dica: Use '.\deploy.ps1 -Command logs' para ver logs detalhados" $Colors.Yellow
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
    }
}

# Reconstruir
function Start-Rebuild {
    Write-ColorOutput "🔄 Reconstruindo e subindo containers..." $Colors.Blue
    try {
        Stop-Containers
        & docker compose -f $ComposeFile build --pull --no-cache
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "❌ Erro na reconstrução" $Colors.Red
            exit 1
        }
        Start-Containers
        Write-ColorOutput "✅ Reconstrução completa!" $Colors.Green
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
        exit 1
    }
}

# Fresh start
function Start-FreshDeployment {
    Write-ColorOutput "⚠️  ATENÇÃO: Isso fará uma limpeza completa e reconstrução do zero!" $Colors.Red
    Write-ColorOutput "⚠️  Todos os dados serão perdidos!" $Colors.Red
    $confirmation = Read-Host "Digite 'SIM' para confirmar"
    if ($confirmation -ne "SIM") {
        Write-ColorOutput "Operação cancelada" $Colors.Yellow
        return
    }

    Write-ColorOutput "🧹 Fazendo limpeza completa..." $Colors.Blue
    try {
        & docker compose -f $ComposeFile down --remove-orphans -v --rmi all
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "❌ Erro durante limpeza" $Colors.Red
            exit 1
        }

        Write-ColorOutput "🔨 Construindo imagens..." $Colors.Blue
        & docker compose -f $ComposeFile build --pull --no-cache
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "❌ Erro na construção" $Colors.Red
            exit 1
        }

        Write-ColorOutput "🚀 Subindo containers..." $Colors.Blue
        & docker compose -f $ComposeFile up -d
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "❌ Erro ao subir containers" $Colors.Red
            exit 1
        }

        Write-ColorOutput "✅ Ambiente completamente reconstruído!" $Colors.Green
        Get-ServiceStatus
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
        exit 1
    }
}

# Ambiente de desenvolvimento
function Start-DevEnvironment {
    Write-ColorOutput "🛠️  Modo desenvolvimento - Construindo..." $Colors.Blue
    try {
        & docker compose -f $ComposeFile build
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "❌ Erro na construção" $Colors.Red
            exit 1
        }
        & docker compose -f $ComposeFile up -d
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "❌ Erro ao subir containers" $Colors.Red
            exit 1
        }
        Write-ColorOutput "✅ Ambiente de desenvolvimento pronto!" $Colors.Green
    }
    catch {
        Write-ColorOutput "❌ Erro: $($_.Exception.Message)" $Colors.Red
        exit 1
    }
}

# Mostrar informações
function Show-Info {
    Write-ColorOutput "=== Informações dos Serviços ===" $Colors.Blue
    Write-Host ""
    Write-ColorOutput "🌐 Endpoints principais:" $Colors.Yellow
    Write-Host "  API Gateway:    http://localhost:5000"
    Write-Host "  Auth Service:   http://localhost:5001"
    Write-Host "  Sales Service:  http://localhost:5002"
    Write-Host "  Stock Service:  http://localhost:5003"
    Write-Host ""
    Write-ColorOutput "🐰 RabbitMQ Management:" $Colors.Yellow
    Write-Host "  URL: http://localhost:15672"
    Write-Host "  User: guest"
    Write-Host "  Pass: guest"
    Write-Host ""
    Write-ColorOutput "🗄️  Bancos de dados:" $Colors.Yellow
    Write-Host "  Auth DB:  localhost:5432 (authdb)"
    Write-Host "  Sales DB: localhost:5434 (salesdb)"
    Write-Host "  Stock DB: localhost:5433 (stockdb)"
    Write-Host ""
    Write-ColorOutput "💡 Comandos úteis:" $Colors.Yellow
    Write-Host "  .\deploy.ps1 -Command status     # Ver status"
    Write-Host "  .\deploy.ps1 -Command logs       # Ver todos os logs"
    Write-Host "  .\deploy.ps1 -Command restart    # Reiniciar serviços"
    Write-Host "  .\deploy.ps1 -Command down       # Parar tudo"
}

# Mostrar ajuda
function Show-Help {
    Clear-Host
    Write-ColorOutput "=== Gerenciamento de Containers Microservices (PowerShell) ===" $Colors.Blue
    Write-Host ""
    Write-ColorOutput "Comandos disponíveis:" $Colors.Yellow
    Write-Host "  build          - Construir todas as imagens Docker"
    Write-Host "  up             - Subir todos os containers em background"
    Write-Host "  down           - Parar todos os containers"
    Write-Host "  clean          - Limpar containers, imagens e volumes"
    Write-Host "  restart        - Reiniciar todos os serviços"
    Write-Host "  logs           - Ver logs de todos os serviços"
    Write-Host "  status         - Ver status atual dos containers"
    Write-Host "  rebuild        - Reconstruir imagens e subir containers"
    Write-Host "  fresh-start    - Limpeza completa + reconstrução"
    Write-Host "  dev            - Ambiente de desenvolvimento"
    Write-Host "  info           - Mostrar informações sobre portas e endpoints"
    Write-Host "  help           - Mostra esta ajuda"
    Write-Host ""
    Write-ColorOutput "Exemplos de uso:" $Colors.Yellow
    Write-Host "  .\deploy.ps1 -Command build          # Construir imagens"
    Write-Host "  .\deploy.ps1 -Command up             # Subir containers"
    Write-Host "  .\deploy.ps1 -Command rebuild        # Reconstruir e subir"
    Write-Host "  .\deploy.ps1 -Command down           # Parar containers"
    Write-Host "  .\deploy.ps1 -Command clean          # Limpar tudo"
    Write-Host "  .\deploy.ps1 -Command fresh-start    # Limpeza completa"
    Write-Host ""
    Write-ColorOutput "Pré-requisitos:" $Colors.Yellow
    Write-Host "  - Docker Desktop instalado e rodando"
    Write-Host "  - Docker Compose V2"
    Write-Host "  - PowerShell com ExecutionPolicy RemoteSigned"
    Write-Host ""
    Read-Host "Pressione Enter para continuar"
}

# Main script logic
if ($Command) {
    Test-Prerequisites

    switch ($Command) {
        "help" { Show-Help }
        "build" { Start-Build }
        "up" { Start-Containers }
        "down" { Stop-Containers }
        "clean" { Clear-All }
        "restart" { Restart-Services }
        "logs" { Get-Logs }
        "status" { Get-ServiceStatus }
        "rebuild" { Start-Rebuild }
        "fresh-start" { Start-FreshDeployment }
        "dev" { Start-DevEnvironment }
        "info" { Show-Info }
        default {
            Write-ColorOutput "❌ Comando não reconhecido: $Command" $Colors.Red
            Write-ColorOutput "Use '.\deploy.ps1 -Command help' para ver comandos disponíveis" $Colors.Yellow
        }
    }
} else {
    Show-Help
}

# Exportar funções para uso interativo
Export-ModuleMember -Function * -Alias *
```

## 🚀 Como Usar o Script PowerShell

### 1. **Executar via Parâmetro**
```powershell
# Permitir execução de scripts (primeira vez)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Executar comandos
.\deploy.ps1 -Command fresh-start
.\deploy.ps1 -Command status
.\deploy.ps1 -Command logs
```

### 2. **Usar Interativamente**
```powershell
# Importar funções
. .\deploy.ps1

# Agora você pode usar as funções diretamente
Start-FreshDeployment
Get-ServiceStatus
Start-Build
Start-Containers
```

### 3. **Exemplos Práticos**
```powershell
# Deploy completo
.\deploy.ps1 -Command fresh-start

# Desenvolvimento
.\deploy.ps1 -Command dev

# Verificar status
.\deploy.ps1 -Command status

# Limpeza de emergência
.\deploy.ps1 -Command clean
```

## 🔧 Vantagens do PowerShell

- **Melhor tratamento de erros**
- **Validação de parâmetros**
- **Funções reutilizáveis**
- **Autocomplete inteligente**
- **Integração com Windows**
- **Objetos estruturados**

## 📋 Comparação: Batch vs PowerShell

| Aspecto | Batch (.bat) | PowerShell (.ps1) |
|---------|--------------|-------------------|
| Simplicidade | ✅ Muito simples | ⚠️ Mais complexo |
| Tratamento de erros | ❌ Básico | ✅ Avançado |
| Validação | ❌ Limitada | ✅ Robusta |
| Interatividade | ❌ Não | ✅ Sim |
| Portabilidade | ✅ Universal | ⚠️ Windows only |
| Performance | ✅ Rápido | ⚠️ Mais lento |

## 💡 Recomendações

- **Para iniciantes**: Use o script `.bat`
- **Para usuários avançados**: Use o script `.ps1`
- **Para automação**: Prefira PowerShell
- **Para simplicidade**: Use Batch

---

**🎉 Agora você tem opções para Windows também!**
