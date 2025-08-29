# Scripts de Deploy para Windows - Guia Completo

## 📋 Visão Geral dos Scripts

Este projeto agora oferece **duas opções** de scripts para Windows:

### 1. **Script Batch (.bat)** - Simples e Universal
- ✅ Mais simples de usar
- ✅ Funciona em qualquer versão do Windows
- ✅ Não requer configuração adicional
- ✅ Ideal para iniciantes

### 2. **Script PowerShell (.ps1)** - Avançado e Robusto
- ✅ Melhor tratamento de erros
- ✅ Validação de parâmetros
- ✅ Funções reutilizáveis
- ✅ Ideal para usuários avançados

## 🚀 Guia Rápido de Uso

### **Opção 1: Script Batch (Recomendado para Iniciantes)**

```cmd
# Deploy completo
deploy.bat fresh-start

# Ver status
deploy.bat status

# Parar tudo
deploy.bat down

# Limpeza completa
deploy.bat clean
```

### **Opção 2: Script PowerShell (Para Usuários Avançados)**

```powershell
# Permitir execução (primeira vez)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Deploy completo
.\deploy.ps1 -Command fresh-start

# Ver status
.\deploy.ps1 -Command status

# Usar interativamente
. .\deploy.ps1
Start-FreshDeployment
Get-ServiceStatus
```

## 📊 Comparação Detalhada

| Característica | Batch (.bat) | PowerShell (.ps1) |
|---------------|--------------|-------------------|
| **Facilidade de uso** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Tratamento de erros** | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Validação de entrada** | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Interatividade** | ❌ | ✅ |
| **Performance** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Configuração inicial** | ✅ Nenhuma | ⚠️ ExecutionPolicy |
| **Compatibilidade** | ✅ Universal | ✅ Windows moderno |
| **Debugging** | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Autocompletar** | ❌ | ✅ |
| **Objetos estruturados** | ❌ | ✅ |

## 🎯 Quando Usar Cada Um

### **Use Batch (.bat) se você:**
- É novo no Windows scripting
- Quer algo simples e direto
- Não quer configurar permissões
- Prefere comandos curtos
- Trabalha em equipe com diferentes níveis

### **Use PowerShell (.ps1) se você:**
- Já conhece PowerShell
- Quer melhor controle de erros
- Precisa de validação robusta
- Quer usar interativamente
- Desenvolve scripts profissionalmente

## 📋 Comandos Disponíveis

### **Comandos Comuns a Ambos**

| Comando | Descrição | Quando usar |
|---------|-----------|-------------|
| `build` | Construir imagens Docker | Após mudanças no código |
| `up` | Subir containers | Iniciar desenvolvimento |
| `down` | Parar containers | Finalizar trabalho |
| `status` | Ver status atual | Verificar se tudo está rodando |
| `logs` | Ver logs | Debug de problemas |
| `restart` | Reiniciar serviços | Após config changes |
| `rebuild` | Reconstruir tudo | Cache issues |
| `fresh-start` | Limpeza + reconstrução | Resolver problemas graves |
| `clean` | Limpar tudo | Reset completo |
| `dev` | Ambiente dev | Desenvolvimento diário |
| `info` | Mostrar informações | Ver portas/endpoints |

### **Sintaxe dos Comandos**

```cmd
REM Batch
deploy.bat <comando>

REM PowerShell
.\deploy.ps1 -Command <comando>
```

## 🔧 Configuração Inicial

### **Para Batch (.bat)**
```cmd
REM Nenhuma configuração necessária!
REM Basta executar: deploy.bat <comando>
```

### **Para PowerShell (.ps1)**
```powershell
# Uma vez por máquina/usuário
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Verificar se funcionou
Get-ExecutionPolicy
```

## 🚨 Solução de Problemas

### **Erro: "ExecutionPolicy" no PowerShell**
```powershell
# Solução
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### **Erro: "docker não encontrado"**
```cmd
REM Verificar se Docker está instalado e no PATH
docker --version

REM Se não estiver, instalar Docker Desktop
REM https://www.docker.com/products/docker-desktop
```

### **Containers não sobem**
```cmd
REM Verificar status do Docker
deploy.bat status

REM Ver logs para detalhes
deploy.bat logs

REM Tentar reconstruir
deploy.bat rebuild
```

### **Portas ocupadas**
```cmd
REM Verificar processos usando portas
netstat -ano | findstr :5000

REM Matar processo (se necessário)
taskkill /PID <PID> /F
```

## 📈 Workflows Recomendados

### **Workflow Diário (Desenvolvimento)**
```cmd
REM Batch
deploy.bat dev
deploy.bat status

REM PowerShell
.\deploy.ps1 -Command dev
.\deploy.ps1 -Command status
```

### **Workflow de Deploy**
```cmd
REM Batch
deploy.bat fresh-start
deploy.bat status

REM PowerShell
.\deploy.ps1 -Command fresh-start
.\deploy.ps1 -Command status
```

### **Workflow de Debug**
```cmd
REM Batch
deploy.bat logs

REM PowerShell
.\deploy.ps1 -Command logs
```

### **Workflow de Reset**
```cmd
REM Batch
deploy.bat clean
deploy.bat fresh-start

REM PowerShell
.\deploy.ps1 -Command clean
.\deploy.ps1 -Command fresh-start
```

## 🎯 Dicas Profissionais

### **Para Times de Desenvolvimento**
1. **Padronizem um script**: Escolham Batch ou PowerShell para toda a equipe
2. **Documentem procedures**: Criem guias internos baseados nestes scripts
3. **Usem aliases**: Criem aliases no PowerShell para comandos frequentes
4. **Monitorem recursos**: Usem `docker stats` para monitorar uso de recursos

### **Para Desenvolvimento Solo**
1. **Crie aliases**: No PowerShell, crie aliases para comandos usados frequentemente
2. **Automatizem rotinas**: Combine comandos em scripts personalizados
3. **Monitorem logs**: Configure notificações para erros críticos
4. **Façam backups**: Antes de `clean`, considerem backup de dados importantes

## 🔗 Integração com Outras Ferramentas

### **VS Code Tasks**
```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "Docker: Fresh Start",
            "type": "shell",
            "command": "./deploy.bat",
            "args": ["fresh-start"],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": false,
                "panel": "shared"
            }
        }
    ]
}
```

### **Git Bash (Windows)**
```bash
# Usar scripts Batch do Git Bash
./deploy.bat fresh-start
./deploy.bat status
```

### **Windows Terminal**
```json
{
    "profiles": {
        "list": [
            {
                "name": "Microservices Dev",
                "commandline": "cmd.exe /k cd /d %USERPROFILE%\\Documents\\micro",
                "startingDirectory": "%USERPROFILE%\\Documents\\micro"
            }
        ]
    }
}
```

## 📚 Recursos Adicionais

- [Docker Desktop para Windows](https://www.docker.com/products/docker-desktop)
- [PowerShell Documentation](https://docs.microsoft.com/pt-br/powershell/)
- [Docker Compose](https://docs.docker.com/compose/)
- [WSL2 para Windows](https://docs.microsoft.com/pt-br/windows/wsl/)

## 🎉 Conclusão

Agora você tem **duas opções robustas** para gerenciar seus containers no Windows:

- **Batch**: Simples, universal, ideal para começar
- **PowerShell**: Avançado, robusto, ideal para profissionais

Escolha o que melhor se adapta ao seu estilo de trabalho e nível de experiência!

---

**🚀 Happy coding no Windows!**
