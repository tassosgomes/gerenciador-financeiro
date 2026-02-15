# Docker - Comandos de Uso

Este documento lista os comandos mínimos para executar o GestorFinanceiro usando Docker Compose.

## Pré-requisitos

- Docker 20.10+
- Docker Compose v2+

## Configuração Inicial

1. **Copie o arquivo de exemplo de variáveis de ambiente:**
   ```bash
   cp .env.example .env
   ```

2. **Configure as variáveis obrigatórias no arquivo `.env`:**
   - `JWT_SECRET`: Gere uma chave segura com no mínimo 32 bytes
     - Linux/Mac: `openssl rand -base64 32`
     - PowerShell: `[Convert]::ToBase64String((1..32|%{Get-Random -Maximum 256}))`
   - `POSTGRES_PASSWORD`: Altere a senha padrão do PostgreSQL
   - `ADMIN_PASSWORD`: Defina a senha do administrador inicial

## Comandos Básicos

### Subir a aplicação
```bash
docker compose up -d
```

### Verificar status dos serviços
```bash
docker compose ps
```

### Ver logs de todos os serviços
```bash
docker compose logs -f
```

### Ver logs de um serviço específico
```bash
docker compose logs -f api
docker compose logs -f web
docker compose logs -f db
```

### Parar a aplicação
```bash
docker compose down
```

### Parar e remover volumes (⚠️ apaga dados do banco)
```bash
docker compose down -v
```

### Rebuild de imagens (após mudanças no código)
```bash
docker compose build
docker compose up -d
```

### Rebuild de um único serviço
```bash
docker compose build api
docker compose up -d api
```

## Verificação de Health

### Verificar se todos os serviços estão healthy
```bash
docker compose ps --format json | jq '.[] | {Name: .Name, Health: .Health}'
```

### Acessar a aplicação
- Frontend: `http://localhost:8080` (ou porta configurada em `WEB_PORT`)
- API Health Check: `http://localhost:8080/api/health`

## Primeiro Acesso

1. Aguarde todos os serviços ficarem com status `healthy` (pode levar até 30-60 segundos)
2. Acesse `http://localhost:8080`
3. Faça login com as credenciais configuradas em `.env`:
   - Email: valor de `ADMIN_EMAIL` (padrão: `admin@gestorfinanceiro.local`)
   - Senha: valor de `ADMIN_PASSWORD` (padrão: `mudar123`)
4. **⚠️ IMPORTANTE**: Altere a senha do administrador após o primeiro login

## Troubleshooting

### Verificar se há algum erro nos serviços
```bash
docker compose logs --tail=50
```

### Reiniciar apenas a API (após mudanças)
```bash
docker compose restart api
```

### Acessar o shell do container da API
```bash
docker compose exec api sh
```

### Acessar o PostgreSQL
```bash
docker compose exec db psql -U postgres -d gestorfinanceiro
```

### Verificar health check manualmente
```bash
# API
docker compose exec api wget -qO- http://localhost:8080/health

# Web
docker compose exec web wget -qO- http://localhost/

# Database
docker compose exec db pg_isready -U postgres -d gestorfinanceiro
```

## Backup e Restore

### Backup do banco de dados
```bash
docker compose exec db pg_dump -U postgres gestorfinanceiro > backup.sql
```

### Restore do banco de dados
```bash
docker compose exec -T db psql -U postgres gestorfinanceiro < backup.sql
```

## Limpeza

### Remover imagens não utilizadas
```bash
docker image prune -a
```

### Remover todos os containers parados, volumes não utilizados e redes
```bash
docker system prune -a --volumes
```
