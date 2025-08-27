# 🧪 Scripts de Teste Automatizados

Este diretório contém scripts para executar todos os testes do projeto de microserviços de forma automatizada.

## 📋 Scripts Disponíveis

### **1. run-e2e-tests.sh** (Recomendado)
Script simplificado para executar apenas os testes E2E funcionais completos.

```bash
# Executar todos os testes E2E funcionais
./run-e2e-tests.sh
```

**O que este script faz:**
- ✅ Verifica pré-requisitos (Docker, .NET SDK)
- ✅ Configura ambiente automaticamente (RabbitMQ)
- ✅ Executa cenários Happy Path
- ✅ Executa cenários Sad Path
- ✅ Gera relatório detalhado
- ✅ Limpa ambiente após execução

### **2. run-all-tests.sh**
Script abrangente para executar TODOS os tipos de teste.

```bash
# Executar todos os testes (Unitários + Integração + E2E)
./run-all-tests.sh all

# Executar apenas testes unitários
./run-all-tests.sh unit

# Executar apenas testes de integração
./run-all-tests.sh integration

# Executar apenas testes E2E automatizados
./run-all-tests.sh e2e

# Executar apenas testes E2E funcionais
./run-all-tests.sh functional
```

## 🚀 Como Usar

### **Execução Simples (Recomendado)**
```bash
# 1. Tornar scripts executáveis (se necessário)
chmod +x *.sh

# 2. Executar testes E2E funcionais
./run-e2e-tests.sh
```

### **Execução Completa**
```bash
# Executar todos os tipos de teste
./run-all-tests.sh all
```

## 📊 O que Cada Script Executa

### **run-e2e-tests.sh**
| Tipo | Descrição | Status |
|------|-----------|---------|
| **Happy Path** | Cenários de sucesso (criar user, produto, pedido) | ✅ Automatizado |
| **Sad Path** | Cenários de erro (estoque insuficiente, acesso negado) | ✅ Automatizado |
| **Integração** | Comunicação assíncrona via RabbitMQ | ✅ Automatizado |
| **Relatório** | Geração automática de relatório | ✅ Automatizado |

### **run-all-tests.sh**
| Tipo | Descrição | Projetos |
|------|-----------|----------|
| **Unitários** | Testes de unidade isolados | AuthService, StockService, SalesService, ApiGateway |
| **Integração** | Testes de integração entre componentes | Todos os serviços |
| **E2E** | Testes end-to-end automatizados | Todos os serviços |
| **Funcionais** | Cenários E2E reais com API calls | Ambiente completo |

## 🎯 Cenários de Teste Executados

### **Cenários Happy Path:**
1. ✅ Criar usuário ADMIN
2. ✅ Criar usuário USER
3. ✅ Criar produto (via ADMIN)
4. ✅ Consultar produtos
5. ✅ Criar pedido com estoque suficiente
6. ✅ Verificar reserva automática de estoque
7. ✅ Consultar pedido criado
8. ✅ Cancelar pedido
9. ✅ Verificar liberação automática de estoque

### **Cenários Sad Path:**
1. ✅ Usuário duplicado rejeitado
2. ✅ Produto sem autorização rejeitado
3. ✅ Pedido com estoque insuficiente rejeitado
4. ✅ Pedido sem autenticação rejeitado
5. ✅ Produto inexistente rejeitado
6. ✅ Consulta de pedido de outro usuário rejeitada

## 📋 Pré-requisitos

### **Obrigatórios:**
- ✅ **Docker** instalado e rodando
- ✅ **.NET 8.0 SDK** instalado
- ✅ **Git** para controle de versão

### **Recomendados:**
- 🔧 **curl** para testes manuais
- 🔧 **jq** para processamento JSON
- 🔧 **tree** para visualização de estrutura

## 📊 Resultados e Relatórios

### **Saídas dos Scripts:**

#### **run-e2e-tests.sh**
```
🚀 EXECUTANDO TESTES E2E FUNCIONAIS
===================================

🔍 Status dos Serviços:
=======================
✅ RabbitMQ: Online (porta 15672)
✅ AuthService: Online (porta 5051)
✅ StockService: Online (porta 5048)
✅ SalesService: Online (porta 5047)
✅ ApiGateway: Online (porta 5219)

🧪 EXECUTANDO TESTES:
====================
✅ Cenários Happy Path passaram!
✅ Cenários Sad Path passaram!

===================================
⏱️  TEMPO TOTAL: 45s
🎉 TODOS OS TESTES E2E PASSARAM COM SUCESSO!

📊 Resumo dos Testes:
   ✅ Cenários Happy Path: OK
   ✅ Cenários Sad Path: OK
   ✅ Comunicação Assíncrona: OK
   ✅ Relatório: e2e-report.md
```

#### **Relatório Gerado (e2e-report.md):**
- 📈 Métricas de performance
- 🔧 Status dos serviços
- 📋 Detalhes de cada cenário
- 📊 Estatísticas de log
- 🎯 Taxa de sucesso

## 🔍 Debugging e Troubleshooting

### **Se os testes falharem:**

1. **Verificar serviços:**
   ```bash
   # Verificar se RabbitMQ está rodando
   docker ps | grep rabbitmq

   # Verificar health dos serviços
   curl http://localhost:5051/health
   curl http://localhost:5048/health
   curl http://localhost:5047/health
   curl http://localhost:5219/health
   ```

2. **Verificar logs:**
   ```bash
   # Logs do RabbitMQ
   docker logs rabbitmq-e2e

   # Verificar mensagens na fila
   docker exec rabbitmq-e2e rabbitmqctl list_queues
   ```

3. **Executar testes individuais:**
   ```bash
   # Testar apenas happy path
   ./run-complete-e2e-test.sh

   # Testar apenas sad path
   ./run-sad-path-e2e-test.sh
   ```

### **Limpeza Manual:**
```bash
# Parar e remover containers
docker stop rabbitmq-e2e
docker rm rabbitmq-e2e

# Limpar dados de teste
./cleanup-e2e-environment.sh
```

## 🎯 Recomendações

### **Para Desenvolvimento:**
- Use `./run-e2e-tests.sh` para validação rápida
- Execute antes de cada commit importante
- Monitore os logs para identificar problemas

### **Para CI/CD:**
- Integre `./run-all-tests.sh all` no pipeline
- Configure timeout de 10 minutos
- Use os relatórios gerados para dashboards

### **Para Debugging:**
- Execute `./run-all-tests.sh functional` para testes isolados
- Use os logs detalhados para identificar falhas
- Verifique conectividade RabbitMQ durante execução

## 📚 Documentação Relacionada

- 📖 **[TESTES_E2E.md](TESTES_E2E.md)** - Cenários detalhados
- 🚀 **[GUIA_EXECUCAO_E2E.md](GUIA_EXECUCAO_E2E.md)** - Scripts manuais
- ✅ **[VALIDACAO_E2E.md](VALIDACAO_E2E.md)** - Checklist de validação
- 🎯 **[RESUMO_E2E.md](RESUMO_E2E.md)** - Visão executiva

---

**💡 Dica**: Comece com `./run-e2e-tests.sh` para uma execução completa e rápida dos cenários E2E mais importantes!</content>
<parameter name="filePath">/home/leandro/Imagens/micro/SCRIPTS_TESTE.md
