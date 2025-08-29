# Guia Windows - Microservices Project

## 🎯 Guia Específico para Windows

Este documento contém instruções específicas para usuários Windows que desejam trabalhar com o projeto de microsserviços.

## 📋 Pré-requisitos para Windows

### 1. Docker Desktop
- **Download**: https://www.docker.com/products/docker-desktop
- **Versão**: 4.0 ou superior
- **Requisitos**: Windows 10/11 Pro, Enterprise ou Education
- **WSL2**: Deve estar habilitado (recomendado)

### 2. Verificar Instalação
Abra o **Command Prompt** ou **PowerShell** e execute:

```cmd
# Verificar Docker
docker --version

# Verificar Docker Compose
docker compose version

# Verificar WSL (se estiver usando)
wsl --version
```

### 3. Configurar Docker Desktop
1. Abra o Docker Desktop
2. Vá em **Settings** > **Resources** > **WSL Integration**
3. Habilite a integração com sua distribuição WSL
4. Aplique as configurações

## � Troubleshooting

Se você encontrar problemas, consulte nosso guia completo de troubleshooting:

📖 **[TROUBLESHOOTING_Windows.md](TROUBLESHOOTING_Windows.md)** - Soluções para problemas comuns no Windows

### Problemas Mais Comuns:
- ❌ **"docker não é reconhecido"** → Instale Docker Desktop
- ❌ **"WSL 2 installation is incomplete"** → Execute `wsl --install`
- ❌ **"ExecutionPolicy"** → Execute `Set-ExecutionPolicy RemoteSigned`
- ❌ **"Port already in use"** → Feche programas usando portas 5000-5003

## �🚀 Como Usar no Windows

### Método 1: Script Batch (Recomendado)

1. **Abrir Command Prompt como Administrador**
   - Pressione `Win + X`
   - Selecione "Terminal (Admin)" ou "Command Prompt (Admin)"

2. **Navegar até a pasta do projeto**
   ```cmd
   cd C:\caminho\para\seu\projeto\micro
   ```

3. **Executar comandos**
   ```cmd
   # Para começar do zero (recomendado)
   deploy.bat fresh-start

   # Para desenvolvimento
   deploy.bat dev

   # Ver status
   deploy.bat status

   # Ver ajuda
   deploy.bat help
   ```

### Método 2: PowerShell

Se preferir usar PowerShell:

```powershell
# Navegar para o projeto
cd C:\caminho\para\seu\projeto\micro

# Usar Docker Compose diretamente
docker compose -f infra/docker-compose.yml up -d
```

## 📋 Comandos Batch Disponíveis

| Comando | Descrição |
|---------|-----------|
| `deploy.bat fresh-start` | **Recomendado** - Limpeza + reconstrução completa |
| `deploy.bat dev` | Ambiente de desenvolvimento |
| `deploy.bat status` | Verificar status dos serviços |
| `deploy.bat logs` | Ver logs de todos os serviços |
| `deploy.bat clean` | Limpeza completa (remove dados) |
| `deploy.bat help` | Lista todos os comandos |
| `deploy.bat info` | Informações dos endpoints |

## 🔧 Solução de Problemas no Windows

### 1. Erro: "docker command not found"
```cmd
# Verificar se Docker está instalado
docker --version

# Se não estiver, instalar Docker Desktop
# Link: https://www.docker.com/products/docker-desktop
```

### 2. Erro: "docker compose command not found"
```cmd
# Docker Compose V2 vem com Docker Desktop
# Verificar versão
docker compose version

# Se não funcionar, tentar o comando antigo
docker-compose --version
```

### 3. Erro de Permissões
- Execute o Command Prompt como **Administrador**
- Ou configure Docker Desktop para não requerer privilégios elevados

### 4. Portas Já Em Uso
```cmd
# Verificar portas em uso
netstat -ano | findstr :5000
netstat -ano | findstr :5432

# Liberar portas ou alterar no docker-compose.yml
```

### 5. Problemas com WSL2
```cmd
# Verificar status do WSL
wsl --list --verbose

# Reiniciar WSL
wsl --shutdown
wsl --start
```

### 6. Containers Não Sobem
```cmd
# Limpeza completa
deploy.bat clean
deploy.bat fresh-start
```

## 🌐 Endpoints no Windows

Após subir os serviços, acesse:

- **API Gateway**: http://localhost:5000
- **Auth Service**: http://localhost:5001
- **Sales Service**: http://localhost:5002
- **Stock Service**: http://localhost:5003
- **RabbitMQ**: http://localhost:15672 (guest/guest)

## 🗄️ Bancos de Dados no Windows

- **Auth DB**: localhost:5432
- **Sales DB**: localhost:5434
- **Stock DB**: localhost:5433
- **Usuário**: postgres
- **Senha**: password123

## 📁 Estrutura de Arquivos

```
micro/
├── deploy.bat          # Script principal para Windows
├── infra/
│   └── docker-compose.yml
├── api-gateway/
├── auth-service/
├── sales-service/
└── stock-service/
```

## 💡 Dicas para Windows

### 1. **Executar como Administrador**
- Sempre execute o Command Prompt como Administrador
- Ou configure Docker Desktop adequadamente

### 2. **Caminhos**
- Use barras invertidas `\` no Windows
- Ou barras normais `/` (funciona em comandos modernos)

### 3. **PowerShell vs CMD**
- Ambos funcionam
- PowerShell tem melhor autocomplete
- CMD é mais rápido para scripts simples

### 4. **Docker Desktop**
- Mantenha sempre rodando em background
- Verifique se o ícone está verde na barra de tarefas

### 5. **WSL2 Performance**
- Use WSL2 para melhor performance
- Evite volumes muito grandes
- Monitore uso de disco

## 🚨 Comandos de Emergência

### Parar Tudo Forçadamente
```cmd
# Parar containers
docker compose -f infra/docker-compose.yml down

# Remover containers forçadamente
docker rm -f $(docker ps -aq)

# Limpeza completa
docker system prune -a --volumes
```

### Resetar Docker Desktop
1. Clique direito no ícone do Docker Desktop
2. Selecione "Restart"
3. Ou "Quit Docker Desktop" e inicie novamente

## 📞 Suporte

### Verificar Logs no Windows
```cmd
# Logs específicos
docker compose -f infra/docker-compose.yml logs auth-service
docker compose -f infra/docker-compose.yml logs sales-service
docker compose -f infra/docker-compose.yml logs stock-service
docker compose -f infra/docker-compose.yml logs api-gateway
```

### Ferramentas Úteis
- **Docker Desktop Dashboard**: Interface gráfica para gerenciar containers
- **Windows Terminal**: Terminal moderno com abas
- **Windows Subsystem for Linux**: Para comandos Linux nativos

## ✅ Checklist para Windows

- [ ] Docker Desktop instalado e rodando
- [ ] WSL2 habilitado (opcional mas recomendado)
- [ ] Command Prompt/PowerShell como Administrador
- [ ] Navegou para a pasta do projeto
- [ ] Executou `deploy.bat fresh-start`
- [ ] Verificou status com `deploy.bat status`
- [ ] Testou endpoints no navegador
- [ ] RabbitMQ acessível

---

## 🔄 Alternativas ao Script Batch

Se preferir usar comandos diretos:

```cmd
REM Construir imagens
docker compose -f infra/docker-compose.yml build

REM Subir containers
docker compose -f infra/docker-compose.yml up -d

REM Ver status
docker compose -f infra/docker-compose.yml ps

REM Ver logs
docker compose -f infra/docker-compose.yml logs

REM Parar tudo
docker compose -f infra/docker-compose.yml down

REM Limpeza completa
docker compose -f infra/docker-compose.yml down --remove-orphans -v --rmi all
```

---

**🎉 Sucesso!** Agora você pode trabalhar com o projeto no Windows usando comandos simples!
