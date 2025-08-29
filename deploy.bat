@echo off
REM Microservices Project - Windows Batch Script
REM Facilita a gestão dos containers Docker para usuários Windows

REM Configurações
set COMPOSE_FILE=infra\docker-compose.yml
set PROJECT_NAME=microservices

REM Cores para output (ANSI escape codes)
set "GREEN=[92m"
set "BLUE=[94m"
set "YELLOW=[93m"
set "RED=[91m"
set "NC=[0m"
set "BOLD=[1m"

REM Função para imprimir colorido
:print_color
echo.| set /p ="[%~1%~2[0m"
goto :eof

REM Verificar se Docker está instalado
:check_docker
docker --version >nul 2>&1
if errorlevel 1 (
    call :print_color "%RED%" "ERRO: Docker não está instalado ou não está no PATH"
    echo.
    echo Instale o Docker Desktop: https://www.docker.com/products/docker-desktop
    pause
    exit /b 1
)

docker compose version >nul 2>&1
if errorlevel 1 (
    call :print_color "%RED%" "ERRO: Docker Compose não está disponível"
    echo.
    echo Certifique-se de que o Docker Compose está instalado
    pause
    exit /b 1
)
goto :eof

REM Mostrar ajuda
:help
cls
call :print_color "%BLUE%" "=== Gerenciamento de Containers Microservices (Windows) ==="
echo.
echo.
call :print_color "%YELLOW%" "Comandos disponíveis:"
echo.
echo   build          - Construir todas as imagens Docker
echo   up             - Subir todos os containers em background
echo   down           - Parar todos os containers
echo   clean          - Limpar containers, imagens e volumes (ATENÇÃO!)
echo   restart        - Reiniciar todos os serviços
echo   logs           - Ver logs de todos os serviços
echo   status         - Ver status atual dos containers
echo   rebuild        - Reconstruir imagens e subir containers
echo   fresh-start    - Limpeza completa + reconstrução
echo   dev            - Ambiente de desenvolvimento
echo   info           - Mostrar informações sobre portas e endpoints
echo   help           - Mostra esta ajuda
echo.
call :print_color "%YELLOW%" "Exemplos de uso:"
echo.
echo   deploy.bat build          # Construir imagens
echo   deploy.bat up             # Subir containers
echo   deploy.bat rebuild        # Reconstruir e subir
echo   deploy.bat down           # Parar containers
echo   deploy.bat clean          # Limpar tudo
echo   deploy.bat fresh-start    # Limpeza completa + reconstrução
echo.
call :print_color "%YELLOW%" "Pré-requisitos:"
echo.
echo   - Docker Desktop instalado e rodando
echo   - Docker Compose V2
echo   - Este script deve ser executado na raiz do projeto
echo.
pause
goto :eof

REM Construir imagens
:build
call :check_docker
call :print_color "%BLUE%" "🔨 Construindo imagens Docker..."
echo.
docker compose -f %COMPOSE_FILE% build --pull
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro ao construir imagens"
    pause
    exit /b 1
)
call :print_color "%GREEN%" "✅ Imagens construídas com sucesso!"
echo.
goto :eof

REM Subir containers
:up
call :check_docker
call :print_color "%BLUE%" "🚀 Subindo containers..."
echo.
docker compose -f %COMPOSE_FILE% up -d
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro ao subir containers"
    pause
    exit /b 1
)
call :print_color "%GREEN%" "✅ Containers iniciados!"
echo.
call :print_color "%YELLOW%" "📊 Status dos serviços:"
echo.
docker compose -f %COMPOSE_FILE% ps
goto :eof

REM Parar containers
:down
call :check_docker
call :print_color "%BLUE%" "🛑 Parando containers..."
echo.
docker compose -f %COMPOSE_FILE% down
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro ao parar containers"
    pause
    exit /b 1
)
call :print_color "%GREEN%" "✅ Containers parados!"
echo.
goto :eof

REM Limpar tudo
:clean
call :check_docker
call :print_color "%RED%" "⚠️  ATENÇÃO: Isso irá remover containers, imagens e volumes (dados serão perdidos)!"
echo.
call :print_color "%YELLOW%" "Pressione qualquer tecla para continuar ou Ctrl+C para cancelar..."
pause >nul

call :print_color "%BLUE%" "🧹 Limpando tudo..."
echo.
docker compose -f %COMPOSE_FILE% down --remove-orphans -v --rmi all
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro durante limpeza"
    pause
    exit /b 1
)
call :print_color "%GREEN%" "✅ Limpeza completa realizada!"
echo.
goto :eof

REM Reiniciar serviços
:restart
call :check_docker
call :print_color "%BLUE%" "🔄 Reiniciando serviços..."
echo.
docker compose -f %COMPOSE_FILE% restart
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro ao reiniciar serviços"
    pause
    exit /b 1
)
call :print_color "%GREEN%" "✅ Serviços reiniciados!"
echo.
goto :eof

REM Ver logs
:logs
call :check_docker
call :print_color "%BLUE%" "📋 Logs dos serviços (pressione Ctrl+C para sair):"
echo.
docker compose -f %COMPOSE_FILE% logs -f
goto :eof

REM Ver status
:status
call :check_docker
call :print_color "%BLUE%" "📊 Status dos containers:"
echo.
docker compose -f %COMPOSE_FILE% ps
echo.
call :print_color "%YELLOW%" "💡 Dica: Use 'deploy.bat logs' para ver logs detalhados"
echo.
goto :eof

REM Reconstruir
:rebuild
call :check_docker
call :print_color "%BLUE%" "🔄 Reconstruindo e subindo containers..."
echo.
docker compose -f %COMPOSE_FILE% down
docker compose -f %COMPOSE_FILE% build --pull --no-cache
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro na reconstrução"
    pause
    exit /b 1
)
docker compose -f %COMPOSE_FILE% up -d
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro ao subir containers"
    pause
    exit /b 1
)
call :print_color "%GREEN%" "✅ Reconstrução completa!"
echo.
call :print_color "%YELLOW%" "📊 Status dos serviços:"
echo.
docker compose -f %COMPOSE_FILE% ps
goto :eof

REM Fresh start
:fresh_start
call :check_docker
call :print_color "%RED%" "⚠️  ATENÇÃO: Isso fará uma limpeza completa e reconstrução do zero!"
echo.
call :print_color "%RED%" "⚠️  Todos os dados serão perdidos!"
echo.
call :print_color "%YELLOW%" "Pressione qualquer tecla para continuar ou Ctrl+C para cancelar..."
pause >nul

call :print_color "%BLUE%" "🧹 Fazendo limpeza completa..."
echo.
docker compose -f %COMPOSE_FILE% down --remove-orphans -v --rmi all
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro durante limpeza"
    pause
    exit /b 1
)

call :print_color "%BLUE%" "🔨 Construindo imagens..."
echo.
docker compose -f %COMPOSE_FILE% build --pull --no-cache
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro na construção"
    pause
    exit /b 1
)

call :print_color "%BLUE%" "🚀 Subindo containers..."
echo.
docker compose -f %COMPOSE_FILE% up -d
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro ao subir containers"
    pause
    exit /b 1
)

call :print_color "%GREEN%" "✅ Ambiente completamente reconstruído!"
echo.
call :print_color "%YELLOW%" "📊 Status dos serviços:"
echo.
docker compose -f %COMPOSE_FILE% ps
goto :eof

REM Ambiente de desenvolvimento
:dev
call :check_docker
call :print_color "%BLUE%" "🛠️  Modo desenvolvimento - Construindo..."
echo.
docker compose -f %COMPOSE_FILE% build
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro na construção"
    pause
    exit /b 1
)
docker compose -f %COMPOSE_FILE% up -d
if errorlevel 1 (
    call :print_color "%RED%" "❌ Erro ao subir containers"
    pause
    exit /b 1
)
call :print_color "%GREEN%" "✅ Ambiente de desenvolvimento pronto!"
echo.
goto :eof

REM Mostrar informações
:info
call :print_color "%BLUE%" "=== Informações dos Serviços ==="
echo.
echo.
call :print_color "%YELLOW%" "🌐 Endpoints principais:"
echo.
echo   API Gateway:    http://localhost:5000
echo   Auth Service:   http://localhost:5001
echo   Sales Service:  http://localhost:5002
echo   Stock Service:  http://localhost:5003
echo.
call :print_color "%YELLOW%" "🐰 RabbitMQ Management:"
echo.
echo   URL: http://localhost:15672
echo   User: guest
echo   Pass: guest
echo.
call :print_color "%YELLOW%" "🗄️  Bancos de dados:"
echo.
echo   Auth DB:  localhost:5432 (authdb)
echo   Sales DB: localhost:5434 (salesdb)
echo   Stock DB: localhost:5433 (stockdb)
echo.
call :print_color "%YELLOW%" "💡 Comandos úteis:"
echo.
echo   deploy.bat status     # Ver status
echo   deploy.bat logs       # Ver todos os logs
echo   deploy.bat restart    # Reiniciar serviços
echo   deploy.bat down       # Parar tudo
echo.
pause
goto :eof

REM Main script logic
if "%1"=="" goto help
if "%1"=="help" goto help
if "%1"=="build" goto build
if "%1"=="up" goto up
if "%1"=="down" goto down
if "%1"=="clean" goto clean
if "%1"=="restart" goto restart
if "%1"=="logs" goto logs
if "%1"=="status" goto status
if "%1"=="rebuild" goto rebuild
if "%1"=="fresh-start" goto fresh_start
if "%1"=="dev" goto dev
if "%1"=="info" goto info

REM Comando não reconhecido
call :print_color "%RED%" "❌ Comando não reconhecido: %1"
echo.
call :print_color "%YELLOW%" "Use 'deploy.bat help' para ver comandos disponíveis"
echo.
pause
goto :eof
