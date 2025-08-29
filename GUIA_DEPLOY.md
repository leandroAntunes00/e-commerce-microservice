# Guia de Deploy - Microservices Project

## 🎯 Objetivo
Este documento serve como guia completo para o time responsável por fazer o deploy e manutenção do projeto de microsserviços.

## 📋 Pré-requisitos

### Sistema Operacional
- Linux (recomendado)
- macOS
- Windows com WSL2

### Software Necessário
- **Docker**: Versão 20.10 ou superior
- **Docker Compose**: Versão 2.0 ou superior
- **Make**: Geralmente já instalado no Linux/macOS
- **Git**: Para clonar o repositório

### Verificar Instalação
```bash
# Verificar Docker
docker --version
docker compose version

# Verificar Make
make --version

# Verificar Git
git --version
```

## 🚀 Primeiro Deploy (Ambiente Novo)

### 1. Clonar o Repositório
```bash
git clone <url-do-repositorio>
cd microservices-project
```

### 2. Deploy Completo
```bash
# Comando recomendado para primeira vez
make fresh-start
```

Este comando irá:
- ✅ Parar containers existentes
- ✅ Remover containers, imagens e volumes antigos
- ✅ Construir novas imagens Docker
- ✅ Subir todos os serviços
- ✅ Executar migrações de banco automaticamente

### 3. Verificar se Está Tudo Funcionando
```bash
# Ver status dos serviços
make status

# Ver informações dos endpoints
make info
```

## 🔄 Desenvolvimento Diário

### Iniciar Ambiente
```bash
# Para desenvolvimento (usa cache)
make dev
```

### Verificar Status
```bash
make status
```

### Ver Logs
```bash
# Todos os logs
make logs

# Logs específicos
make auth-logs   # Auth Service
make sales-logs  # Sales Service
make stock-logs  # Stock Service
make api-logs    # API Gateway
```

## 🛠️ Manutenção e Troubleshooting

### Reiniciar Serviços
```bash
make restart
```

### Reconstruir Imagens
```bash
# Reconstruir sem cache
make rebuild

# Ou apenas build
make build
```

### Limpeza de Ambiente
```bash
# Parar tudo
make down

# Limpeza completa (ATENÇÃO: remove dados!)
make clean
```

### Problemas Comuns

#### 1. Porta Já Está Em Uso
```bash
# Verificar portas em uso
sudo lsof -i :5000,5001,5002,5003,5432,5433,5434,15672

# Liberar portas ou alterar no docker-compose.yml
```

#### 2. Containers Não Sobem
```bash
# Limpeza completa
make clean
make fresh-start
```

#### 3. Erro de Banco de Dados
```bash
# Verificar logs do banco
docker compose -f infra/docker-compose.yml logs postgres-auth
docker compose -f infra/docker-compose.yml logs postgres-sales
docker compose -f infra/docker-compose.yml logs postgres-stock
```

#### 4. Erro de RabbitMQ
```bash
# Verificar logs do RabbitMQ
docker compose -f infra/docker-compose.yml logs rabbitmq
```

## 📊 Monitoramento

### Health Checks
```bash
# Verificar se serviços estão respondendo
curl http://localhost:5000/health  # API Gateway
curl http://localhost:5001/health  # Auth Service
curl http://localhost:5002/health  # Sales Service
curl http://localhost:5003/health  # Stock Service
```

### RabbitMQ Management
- URL: http://localhost:15672
- Usuário: guest
- Senha: guest

### Logs Persistentes
```bash
# Salvar logs em arquivo
docker compose -f infra/docker-compose.yml logs > logs_$(date +%Y%m%d_%H%M%S).txt
```

## 🔧 Configuração Personalizada

### Alterar Portas
Editar `infra/docker-compose.yml`:
```yaml
ports:
  - "5000:80"  # Host:Container
```

### Variáveis de Ambiente
As configurações estão em:
- `infra/docker-compose.yml` (variáveis do Docker)
- `appsettings.json` de cada serviço (configurações da aplicação)

### Banco de Dados
- **Auth DB**: localhost:5432, database: authdb
- **Sales DB**: localhost:5434, database: salesdb
- **Stock DB**: localhost:5433, database: stockdb
- **Usuário**: postgres
- **Senha**: password123

## 📝 Comandos Essenciais

| Comando | Descrição | Quando Usar |
|---------|-----------|-------------|
| `make fresh-start` | Deploy completo do zero | Primeiro deploy ou problemas graves |
| `make dev` | Ambiente de desenvolvimento | Desenvolvimento diário |
| `make status` | Verificar status dos serviços | Sempre que precisar verificar |
| `make logs` | Ver logs de todos os serviços | Investigar problemas |
| `make restart` | Reiniciar serviços | Após mudanças de configuração |
| `make rebuild` | Reconstruir imagens | Após mudanças no código |
| `make clean` | Limpeza completa | Quando precisar limpar tudo |
| `make info` | Informações dos endpoints | Para documentar ou compartilhar |

## 🚨 Procedimentos de Emergência

### Sistema Instável
```bash
# Parada de emergência
make down

# Verificar containers órfãos
docker ps -a

# Remover containers órfãos
docker rm $(docker ps -aq --filter "status=exited")
```

### Perda de Dados
```bash
# Backup dos volumes (se necessário)
docker run --rm -v infra_postgres_auth_data:/data -v $(pwd):/backup alpine tar czf /backup/auth_backup.tar.gz -C /data .

# Restauração
docker run --rm -v infra_postgres_auth_data:/data -v $(pwd):/backup alpine tar xzf /backup/auth_backup.tar.gz -C /data
```

## 📞 Suporte

Para problemas específicos:
1. Verificar logs com `make logs`
2. Usar `make status` para ver estado dos containers
3. Consultar documentação em `CLEAN_CODE_ARCHITECTURE.MD`
4. Verificar portas em uso no sistema

## ✅ Checklist de Deploy

- [ ] Repositório clonado
- [ ] Docker e Docker Compose instalados
- [ ] Make instalado
- [ ] `make fresh-start` executado
- [ ] `make status` mostra todos os serviços UP
- [ ] Endpoints respondendo (usar `make info`)
- [ ] RabbitMQ acessível
- [ ] Logs sem erros críticos

---

**💡 Dica**: Sempre use `make help` para ver todos os comandos disponíveis!
