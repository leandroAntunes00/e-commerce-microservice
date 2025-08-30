# 🐳 LIMPEZA DO DOCKER - Guia para Iniciantes

## 📋 O que é este arquivo?

Este é um **script educacional completo** para limpeza profunda do Docker, especialmente criado para quem está começando a aprender Docker. Ele não apenas executa os comandos, mas **explica cada passo** de forma detalhada.

## 🚀 Como usar:

### Opção 1: Executar o script completo
```bash
cd /home/leandro/Documentos/micro
./LIMPEZA_DOCKER
```

### Opção 2: Executar comandos individualmente
Abra o arquivo `LIMPEZA_DOCKER` e execute cada comando separadamente no terminal para aprender passo a passo.

## 📚 O que você vai aprender:

### 1. **Monitoramento de Espaço**
- `docker system df` - Ver espaço ocupado
- `docker image ls` - Listar imagens
- `docker ps -a` - Ver containers

### 2. **Limpeza de Imagens**
- `docker image prune` - Remove imagens não utilizadas
- `docker image prune -a` - Remove todas as imagens não utilizadas

### 3. **Limpeza de Containers**
- `docker container prune` - Remove containers parados
- `docker rm <container>` - Remove container específico

### 4. **Limpeza de Volumes**
- `docker volume prune` - Remove volumes órfãos
- `docker volume ls` - Lista volumes

### 5. **Limpeza de Redes**
- `docker network prune` - Remove redes não utilizadas
- `docker network ls` - Lista redes

### 6. **Limpeza de Cache**
- `docker builder prune` - Remove cache de builds
- `docker system prune` - Limpeza geral

## ⚠️ **IMPORTANTE - Leia antes de executar:**

### ✅ **Quando FAZER limpeza:**
- Após muitos builds de desenvolvimento
- Quando o disco está cheio
- Antes de fazer deploy em produção
- Regularmente (semanalmente)

### ❌ **Quando NÃO fazer limpeza:**
- Se tiver containers importantes rodando
- Se precisar de dados em volumes
- Durante desenvolvimento ativo
- Se não tiver backup de dados importantes

### 🛡️ **Precauções:**
1. **Sempre faça backup** de dados importantes
2. **Verifique containers em execução** com `docker ps`
3. **Confirme volumes importantes** com `docker volume ls`
4. **Teste em ambiente de desenvolvimento primeiro**

## 📊 **O que cada comando faz:**

| Comando | O que remove | Segurança | Frequência |
|---------|-------------|-----------|------------|
| `docker image prune` | Imagens dangling | 🔒 Seguro | Semanal |
| `docker container prune` | Containers parados | 🔒 Seguro | Diária |
| `docker volume prune` | Volumes órfãos | ⚠️ Cuidado | Mensal |
| `docker network prune` | Redes não usadas | 🔒 Seguro | Semanal |
| `docker builder prune` | Cache de build | 🔒 Seguro | Semanal |
| `docker system prune -a` | TUDO não usado | ⚠️ Perigoso | Rara |

## 🎯 **Cenários de Uso:**

### **Cenário 1: Desenvolvimento Diário**
```bash
# Limpeza rápida e segura
docker container prune -f
docker image prune -f
```

### **Cenário 2: Disco Cheio**
```bash
# Limpeza completa
docker system prune -a
docker volume prune -f
docker builder prune -f
```

### **Cenário 3: Manutenção Semanal**
```bash
# Limpeza de rotina
docker system prune
docker builder prune -f
```

## 🔧 **Comandos de Verificação:**

```bash
# Ver espaço ocupado
docker system df

# Ver containers ativos
docker ps

# Ver todas as imagens
docker images -a

# Ver volumes
docker volume ls

# Ver redes
docker network ls
```

## 📈 **Resultados Esperados:**

Após executar a limpeza completa, você pode esperar:
- **Liberar 50-80%** do espaço ocupado por Docker
- **Melhor performance** do Docker
- **Menos conflitos** de nomes e portas
- **Sistema mais organizado**

## 🎓 **Próximos Passos para Aprender:**

1. **Pratique** os comandos individualmente
2. **Leia** a documentação oficial do Docker
3. **Experimente** criar seus próprios containers
4. **Aprenda** sobre Docker Compose para múltiplos containers
5. **Estude** melhores práticas de Docker

## 💡 **Dicas Profissionais:**

- Use `.dockerignore` para builds menores
- Nomeie containers e imagens claramente
- Use volumes nomeados para dados persistentes
- Monitore recursos com `docker stats`
- Faça backup regular de volumes importantes

---

**Lembre-se:** Docker é uma ferramenta poderosa, mas com grande poder vem grande responsabilidade. Sempre teste em ambientes seguros primeiro! 🐳</content>
<parameter name="filePath">/home/leandro/Documentos/micro/README_LIMPEZA_DOCKER.md
