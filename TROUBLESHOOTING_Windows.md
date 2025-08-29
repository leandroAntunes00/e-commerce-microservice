# Troubleshooting Windows - Microservices Project

## 🚨 Problemas Comuns no Windows

Este guia aborda os problemas mais comuns que usuários Windows podem enfrentar ao trabalhar com containers Docker e scripts de deployment.

## 🐳 Problemas com Docker

### **Erro: "docker não é reconhecido como comando"**

**Sintomas:**
```
'docker' não é reconhecido como um comando interno
ou externo, um programa ou arquivo em lotes operável.
```

**Soluções:**

1. **Verificar instalação:**
   ```cmd
   REM Abrir PowerShell como Administrador
   docker --version
   ```

2. **Se não estiver instalado:**
   - Baixe e instale [Docker Desktop](https://www.docker.com/products/docker-desktop)
   - Reinicie o computador
   - Certifique-se que Docker Desktop está rodando

3. **Adicionar ao PATH (se necessário):**
   ```cmd
   REM Verificar onde Docker está instalado
   where docker

   REM Se não encontrar, reinstalar Docker Desktop
   ```

### **Erro: "Docker Desktop is not running"**

**Sintomas:**
```
error during connect: This error may indicate that the docker daemon is not running
```

**Soluções:**

1. **Iniciar Docker Desktop:**
   - Abra Docker Desktop
   - Aguarde até que apareça "Docker Desktop is running"
   - Tente novamente o comando

2. **Reiniciar serviço:**
   ```cmd
   REM Abrir PowerShell como Administrador
   Restart-Service docker
   ```

3. **Verificar status:**
   ```cmd
   docker info
   ```

### **Erro: "WSL 2 installation is incomplete"**

**Sintomas:**
```
The command 'docker' could not be found in this WSL 2 distro
```

**Soluções:**

1. **Instalar WSL2:**
   ```cmd
   REM Abrir PowerShell como Administrador
   wsl --install
   wsl --set-default-version 2
   ```

2. **Verificar versão WSL:**
   ```cmd
   wsl --version
   ```

3. **Reiniciar Docker Desktop**

## 📁 Problemas com Paths e Arquivos

### **Erro: "O sistema não pode encontrar o arquivo especificado"**

**Sintomas:**
```
deploy.bat
O sistema não pode encontrar o arquivo especificado.
```

**Soluções:**

1. **Verificar localização:**
   ```cmd
   REM Listar arquivos na pasta
   dir

   REM Verificar se deploy.bat existe
   if exist deploy.bat echo Arquivo encontrado
   ```

2. **Executar com caminho completo:**
   ```cmd
   REM Usar caminho absoluto
   C:\path\to\your\project\deploy.bat fresh-start
   ```

3. **Verificar permissões:**
   - Clique direito no arquivo `deploy.bat`
   - Propriedades → Segurança → Verificar permissões

### **Erro: "Access is denied"**

**Sintomas:**
```
Access is denied.
```

**Soluções:**

1. **Executar como Administrador:**
   - Clique direito no Command Prompt
   - "Executar como administrador"

2. **Verificar permissões de arquivo:**
   ```cmd
   REM Verificar permissões
   icacls deploy.bat

   REM Conceder permissões
   icacls deploy.bat /grant Users:F
   ```

## 🔧 Problemas com Scripts PowerShell

### **Erro: "ExecutionPolicy"**

**Sintomas:**
```
File cannot be loaded because running scripts is disabled on this system
```

**Soluções:**

1. **Alterar Execution Policy:**
   ```powershell
   # Abrir PowerShell como Administrador
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

   # Verificar
   Get-ExecutionPolicy
   ```

2. **Executar sem política:**
   ```powershell
   # Usar bypass (temporário)
   PowerShell -ExecutionPolicy Bypass -File .\deploy.ps1 -Command help
   ```

### **Erro: "Cannot find path" no PowerShell**

**Sintomas:**
```
Cannot find path 'C:\path\to\file' because it does not exist
```

**Soluções:**

1. **Verificar caminho:**
   ```powershell
   # Verificar localização atual
   Get-Location

   # Listar arquivos
   Get-ChildItem

   # Verificar se arquivo existe
   Test-Path .\deploy.ps1
   ```

2. **Usar caminhos absolutos:**
   ```powershell
   # Usar caminho completo
   & "C:\Users\YourName\Documents\micro\deploy.ps1" -Command help
   ```

## 🌐 Problemas de Rede e Portas

### **Erro: "Port already in use"**

**Sintomas:**
```
Port already in use: bind: address already in use
```

**Soluções:**

1. **Verificar processos usando portas:**
   ```cmd
   REM Verificar porta 5000
   netstat -ano | findstr :5000

   REM Verificar porta 5432
   netstat -ano | findstr :5432
   ```

2. **Matar processo:**
   ```cmd
   REM Matar processo (substitua PID)
   taskkill /PID 1234 /F
   ```

3. **Usar portas alternativas:**
   - Edite `infra/docker-compose.yml`
   - Mude as portas dos serviços

### **Erro: "Connection refused"**

**Sintomas:**
```
connection refused
```

**Soluções:**

1. **Verificar se containers estão rodando:**
   ```cmd
   deploy.bat status
   ```

2. **Verificar logs:**
   ```cmd
   deploy.bat logs
   ```

3. **Testar conectividade:**
   ```cmd
   REM Testar API Gateway
   curl http://localhost:5000/health

   REM Testar RabbitMQ
   curl http://localhost:15672
   ```

## 💾 Problemas com Volumes e Dados

### **Erro: "Volume is in use"**

**Sintomas:**
```
volume is in use
```

**Soluções:**

1. **Parar containers usando o volume:**
   ```cmd
   deploy.bat down
   ```

2. **Remover volumes:**
   ```cmd
   docker volume prune -f
   ```

3. **Limpeza completa:**
   ```cmd
   deploy.bat clean
   ```

### **Dados não persistem**

**Sintomas:**
- Dados desaparecem após `deploy.bat down`

**Soluções:**

1. **Verificar volumes:**
   ```cmd
   docker volume ls
   ```

2. **Backup de dados:**
   ```cmd
   REM Fazer backup antes de clean
   docker exec postgres_auth pg_dump -U postgres authdb > backup_auth.sql
   ```

## 🔄 Problemas de Build e Reconstrução

### **Erro: "Build failed"**

**Sintomas:**
```
ERROR: Service 'auth-service' failed to build
```

**Soluções:**

1. **Limpar cache:**
   ```cmd
   deploy.bat clean
   deploy.bat build
   ```

2. **Build sem cache:**
   ```cmd
   docker compose build --no-cache
   ```

3. **Verificar logs detalhados:**
   ```cmd
   docker compose build --progress=plain
   ```

### **Erro: "No space left on device"**

**Sintomas:**
```
no space left on device
```

**Soluções:**

1. **Limpar Docker:**
   ```cmd
   docker system prune -a -f
   docker volume prune -f
   ```

2. **Verificar espaço em disco:**
   ```cmd
   REM Verificar espaço
   wmic logicaldisk get size,freespace,caption
   ```

3. **Limpar Docker Desktop:**
   - Docker Desktop → Settings → Docker Engine
   - Aumentar espaço alocado

## 🛠️ Ferramentas de Diagnóstico

### **Script de Diagnóstico**

```cmd
REM Criar arquivo diagnose.bat
echo === DIAGNOSTICO WINDOWS ===
echo.

echo 1. Verificando Docker...
docker --version
if %errorlevel% neq 0 echo ❌ Docker não instalado

echo.
echo 2. Verificando Docker Compose...
docker compose version
if %errorlevel% neq 0 echo ❌ Docker Compose não disponível

echo.
echo 3. Verificando WSL...
wsl --version
if %errorlevel% neq 0 echo ❌ WSL não instalado

echo.
echo 4. Verificando arquivos do projeto...
if exist deploy.bat echo ✅ deploy.bat encontrado
if exist deploy.ps1 echo ✅ deploy.ps1 encontrado
if exist infra\docker-compose.yml echo ✅ docker-compose.yml encontrado

echo.
echo 5. Verificando portas...
netstat -ano | findstr :5000 >nul
if %errorlevel% equ 0 echo ⚠️ Porta 5000 em uso

echo.
echo === FIM DIAGNOSTICO ===
pause
```

### **Comandos Úteis para Debug**

```cmd
REM Ver todos os containers
docker ps -a

REM Ver logs de um container específico
docker logs container_name

REM Ver uso de recursos
docker stats

REM Ver redes
docker network ls

REM Ver imagens
docker images

REM Ver volumes
docker volume ls
```

## 📞 Quando Pedir Ajuda

Se nenhum dos passos acima resolver seu problema:

1. **Colete informações:**
   ```cmd
   REM Execute o diagnóstico
   diagnose.bat

   REM Colete logs
   deploy.bat logs > logs.txt
   ```

2. **Documente o problema:**
   - Qual comando executou
   - Qual erro recebeu
   - Sistema operacional e versão
   - Versão do Docker Desktop

3. **Verifique documentação:**
   - [README_Windows.md](README_Windows.md)
   - [README_Windows_Scripts.md](README_Windows_Scripts.md)

## 🎯 Prevenção de Problemas

### **Boas Práticas:**

1. **Sempre execute como administrador** para comandos Docker
2. **Mantenha Docker Desktop atualizado**
3. **Feche outros programas** que usam portas 5000-5003, 5432-5434
4. **Não modifique arquivos** enquanto containers estão rodando
5. **Faça backup** antes de executar `clean`

### **Monitoramento Contínuo:**

```cmd
REM Verificar status periodicamente
deploy.bat status

REM Monitorar logs
deploy.bat logs

REM Verificar recursos
docker stats
```

---

**💡 Lembre-se**: A maioria dos problemas no Windows são relacionados a permissões, caminhos ou Docker não estar rodando. Verifique sempre estes pontos primeiro!
