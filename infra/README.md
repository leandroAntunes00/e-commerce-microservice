# Infraestrutura - Microserviços E-commerce

Esta pasta contém a configuração da infraestrutura para o projeto de microserviços, utilizando Docker Compose para orquestrar os serviços de banco de dados e mensageria.

## Serviços Configurados

### Bancos de Dados PostgreSQL
- **postgres-auth**: Banco para o Auth Service
  - Porta externa: 5432
  - Database: authdb
  - Usuário: postgres
  - Senha: password123

- **postgres-stock**: Banco para o Stock Service
  - Porta externa: 5433
  - Database: stockdb
  - Usuário: postgres
  - Senha: password123

- **postgres-sales**: Banco para o Sales Service
  - Porta externa: 5434
  - Database: salesdb
  - Usuário: postgres
  - Senha: password123

### RabbitMQ
- **rabbitmq**: Servidor de mensageria para comunicação assíncrona
  - Porta AMQP: 5672
  - Porta Management UI: 15672
  - Usuário: admin
  - Senha: password123
  - Management UI: http://localhost:15672

## Como Usar

### 1. Subir a Infraestrutura
```bash
cd infra
docker-compose up -d
```

### 2. Verificar Status dos Containers
```bash
docker-compose ps
```

### 3. Ver Logs
```bash
# Todos os serviços
docker-compose logs -f

# Serviço específico
docker-compose logs -f rabbitmq
```

### 4. Parar a Infraestrutura
```bash
docker-compose down
```

### 5. Parar e Remover Volumes (dados)
```bash
docker-compose down -v
```

## Conexões dos Microserviços

### Auth Service
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=password123"
  }
}
```

### Stock Service
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=stockdb;Username=postgres;Password=password123"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "admin",
    "Password": "password123"
  }
}
```

### Sales Service
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5434;Database=salesdb;Username=postgres;Password=password123"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "admin",
    "Password": "password123"
  }
}
```

## Volumes Persistentes

Os dados dos bancos PostgreSQL e RabbitMQ são persistidos em volumes Docker:
- `postgres_auth_data`
- `postgres_stock_data`
- `postgres_sales_data`
- `rabbitmq_data`

## Rede

Todos os serviços estão conectados à rede `microservices-net` para comunicação interna entre containers.

## Próximos Passos

1. **Configurar appsettings.json** de cada microserviço com as strings de conexão acima
2. **Criar migrations** do Entity Framework para cada serviço
3. **Testar conexões** dos microserviços com os bancos
4. **Configurar comunicação RabbitMQ** entre Stock e Sales services

## Troubleshooting

### Verificar se PostgreSQL está rodando
```bash
docker exec -it postgres-auth psql -U postgres -d authdb -c "SELECT version();"
```

### Acessar Management UI do RabbitMQ
- URL: http://localhost:15672
- Usuário: admin
- Senha: password123

### Verificar portas em uso
```bash
netstat -tulpn | grep -E '(5432|5433|5434|5672|15672)'
```</content>
<parameter name="filePath">/home/leandro/Imagens/micro/infra/README.md
