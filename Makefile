# Microservices Project Makefile
# Facilita a gestão dos containers Docker para usuários sem conhecimento avançado

.PHONY: help build up down clean restart logs status rebuild fresh-start

# Variáveis
COMPOSE_FILE := infra/docker-compose.yml
PROJECT_NAME := microservices

# Cores para output
GREEN := \033[0;32m
BLUE := \033[0;34m
YELLOW := \033[1;33m
RED := \033[0;31m
NC := \033[0m # No Color

# Default target
help: ## Mostra esta ajuda
	@echo "$(BLUE)=== Gerenciamento de Containers Microservices ===$(NC)"
	@echo ""
	@echo "$(YELLOW)Comandos disponíveis:$(NC)"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(GREEN)%-15s$(NC) %s\n", $$1, $$2}'
	@echo ""
	@echo "$(YELLOW)Exemplos de uso:$(NC)"
	@echo "  make build        # Construir imagens"
	@echo "  make up           # Subir containers"
	@echo "  make rebuild      # Reconstruir e subir"
	@echo "  make down         # Parar containers"
	@echo "  make clean        # Limpar tudo"
	@echo "  make fresh-start  # Limpeza completa + reconstrução"

build: ## Construir todas as imagens Docker
	@echo "$(BLUE)🔨 Construindo imagens Docker...$(NC)"
	docker compose -f $(COMPOSE_FILE) build --pull
	@echo "$(GREEN)✅ Imagens construídas com sucesso!$(NC)"

up: ## Subir todos os containers em background
	@echo "$(BLUE)🚀 Subindo containers...$(NC)"
	docker compose -f $(COMPOSE_FILE) up -d
	@echo "$(GREEN)✅ Containers iniciados!$(NC)"
	@echo "$(YELLOW)📊 Status dos serviços:$(NC)"
	@docker compose -f $(COMPOSE_FILE) ps

down: ## Parar todos os containers
	@echo "$(BLUE)🛑 Parando containers...$(NC)"
	docker compose -f $(COMPOSE_FILE) down
	@echo "$(GREEN)✅ Containers parados!$(NC)"

clean: ## Limpar containers, imagens e volumes (ATENÇÃO: remove dados!)
	@echo "$(RED)⚠️  ATENÇÃO: Isso irá remover containers, imagens e volumes (dados serão perdidos)!$(NC)"
	@echo "$(YELLOW)Pressione Ctrl+C para cancelar ou aguarde 5 segundos...$(NC)"
	@sleep 5
	@echo "$(BLUE)🧹 Limpando tudo...$(NC)"
	docker compose -f $(COMPOSE_FILE) down --remove-orphans -v --rmi all
	@echo "$(GREEN)✅ Limpeza completa realizada!$(NC)"

restart: ## Reiniciar todos os serviços
	@echo "$(BLUE)🔄 Reiniciando serviços...$(NC)"
	docker compose -f $(COMPOSE_FILE) restart
	@echo "$(GREEN)✅ Serviços reiniciados!$(NC)"

logs: ## Ver logs de todos os serviços
	@echo "$(BLUE)📋 Logs dos serviços (pressione Ctrl+C para sair):$(NC)"
	docker compose -f $(COMPOSE_FILE) logs -f

status: ## Ver status atual dos containers
	@echo "$(BLUE)📊 Status dos containers:$(NC)"
	@docker compose -f $(COMPOSE_FILE) ps
	@echo ""
	@echo "$(YELLOW)💡 Dica: Use 'make logs' para ver logs detalhados$(NC)"

rebuild: ## Reconstruir imagens e subir containers
	@echo "$(BLUE)🔄 Reconstruindo e subindo containers...$(NC)"
	docker compose -f $(COMPOSE_FILE) down
	docker compose -f $(COMPOSE_FILE) build --pull --no-cache
	docker compose -f $(COMPOSE_FILE) up -d
	@echo "$(GREEN)✅ Reconstrução completa!$(NC)"
	@echo "$(YELLOW)📊 Status dos serviços:$(NC)"
	@docker compose -f $(COMPOSE_FILE) ps

fresh-start: ## Limpeza completa + reconstrução (recomendado para começar do zero)
	@echo "$(RED)⚠️  ATENÇÃO: Isso fará uma limpeza completa e reconstrução do zero!$(NC)"
	@echo "$(RED)⚠️  Todos os dados serão perdidos!$(NC)"
	@echo "$(YELLOW)Pressione Ctrl+C para cancelar ou aguarde 5 segundos...$(NC)"
	@sleep 5
	@echo "$(BLUE)🧹 Fazendo limpeza completa...$(NC)"
	docker compose -f $(COMPOSE_FILE) down --remove-orphans -v --rmi all
	@echo "$(BLUE)🔨 Construindo imagens...$(NC)"
	docker compose -f $(COMPOSE_FILE) build --pull --no-cache
	@echo "$(BLUE)🚀 Subindo containers...$(NC)"
	docker compose -f $(COMPOSE_FILE) up -d
	@echo "$(GREEN)✅ Ambiente completamente reconstruído!$(NC)"
	@echo "$(YELLOW)📊 Status dos serviços:$(NC)"
	@docker compose -f $(COMPOSE_FILE) ps

# Targets específicos para serviços individuais
auth-logs: ## Ver logs apenas do auth-service
	@echo "$(BLUE)📋 Logs do Auth Service:$(NC)"
	docker compose -f $(COMPOSE_FILE) logs -f auth-service

sales-logs: ## Ver logs apenas do sales-service
	@echo "$(BLUE)📋 Logs do Sales Service:$(NC)"
	docker compose -f $(COMPOSE_FILE) logs -f sales-service

stock-logs: ## Ver logs apenas do stock-service
	@echo "$(BLUE)📋 Logs do Stock Service:$(NC)"
	docker compose -f $(COMPOSE_FILE) logs -f stock-service

api-logs: ## Ver logs apenas do api-gateway
	@echo "$(BLUE)📋 Logs do API Gateway:$(NC)"
	docker compose -f $(COMPOSE_FILE) logs -f api-gateway

# Target para desenvolvimento
dev: ## Ambiente de desenvolvimento (com rebuild automático)
	@echo "$(BLUE)🛠️  Modo desenvolvimento - Reconstruindo com cache...$(NC)"
	docker compose -f $(COMPOSE_FILE) build
	docker compose -f $(COMPOSE_FILE) up -d
	@echo "$(GREEN)✅ Ambiente de desenvolvimento pronto!$(NC)"

# Informações úteis
info: ## Mostrar informações sobre portas e endpoints
	@echo "$(BLUE)=== Informações dos Serviços ===$(NC)"
	@echo ""
	@echo "$(YELLOW)🌐 Endpoints principais:$(NC)"
	@echo "  API Gateway:    http://localhost:5000"
	@echo "  Auth Service:   http://localhost:5001"
	@echo "  Sales Service:  http://localhost:5002"
	@echo "  Stock Service:  http://localhost:5003"
	@echo ""
	@echo "$(YELLOW)🐰 RabbitMQ Management:$(NC)"
	@echo "  URL: http://localhost:15672"
	@echo "  User: guest"
	@echo "  Pass: guest"
	@echo ""
	@echo "$(YELLOW)🗄️  Bancos de dados:$(NC)"
	@echo "  Auth DB:  localhost:5432 (authdb)"
	@echo "  Sales DB: localhost:5434 (salesdb)"
	@echo "  Stock DB: localhost:5433 (stockdb)"
	@echo ""
	@echo "$(YELLOW)💡 Comandos úteis:$(NC)"
	@echo "  make status     # Ver status"
	@echo "  make logs       # Ver todos os logs"
	@echo "  make restart    # Reiniciar serviços"
	@echo "  make down       # Parar tudo"
