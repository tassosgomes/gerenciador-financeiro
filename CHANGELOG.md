# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [1.0.0] - 2026-02-15

### 🎉 Release Inicial - MVP Completo

Primeira versão estável do GestorFinanceiro, sistema self-hosted de gestão financeira pessoal/familiar.

### ✨ Funcionalidades Principais

#### Autenticação e Controle de Acesso
- Sistema de autenticação JWT com login seguro
- Suporte a múltiplos perfis de usuário (Administrador e Membro)
- Gestão de usuários com criação, edição e desativação
- Controle de acesso baseado em roles (RBAC)
- Seed automático de usuário administrador no primeiro uso
- Indicador de troca de senha recomendada no primeiro login

#### Gestão de Contas Bancárias
- Cadastro de múltiplas contas bancárias
- Tipos de conta configuráveis (Corrente, Poupança, Investimento)
- Marcação de conta principal
- Ativação/desativação de contas
- Visualização de saldos atualizados em tempo real

#### Categorias de Transações
- Sistema de categorias para Receitas e Despesas
- Categorias padrão do sistema (não editáveis):
  - **Despesas**: Alimentação, Transporte, Moradia, Saúde, Educação, Lazer, Vestuário, Serviços, Impostos, Outros
  - **Receitas**: Salário, Freelance, Investimentos, Outros
- Criação de categorias personalizadas
- Ativação/desativação de categorias
- Proteção contra edição/exclusão de categorias do sistema

#### Transações Financeiras
- Registro de receitas e despesas
- Categorização obrigatória de transações
- Suporte a transações recorrentes (automação futura)
- Anexo de descrição e observações
- Filtros avançados (período, categoria, conta, tipo)
- Visualização consolidada de extrato
- Edição e exclusão de transações existentes

#### Orçamentos
- Definição de orçamentos mensais por categoria
- Acompanhamento de limites vs gastos reais
- Alertas visuais de ultrapassagem de orçamento
- Flexibilidade para ajustar orçamentos ao longo do tempo
- Indicadores percentuais de consumo do orçamento

#### Dashboard e Relatórios
- Visão consolidada de saldos totais
- Gráficos de despesas por categoria (pizza)
- Evolução mensal de receitas e despesas (linha/barra)
- Indicadores de saúde financeira
- Filtros de período para análise histórica
- Resumo de transações recentes

#### Interface de Usuário
- Design responsivo para desktop, tablet e mobile (mínimo 320px)
- Menu de navegação lateral colapsável
- Menu hambúrguer em dispositivos móveis
- Temas de cores profissionais
- Componentes acessíveis (WCAG 2.1 Level AA)
- Touch targets adequados para mobile (mínimo 44x44px)
- Feedback visual para ações do usuário (loading, erros, sucessos)

### 🏗️ Arquitetura e Infraestrutura

#### Backend (.NET 8)
- Arquitetura limpa (Clean Architecture) com separação de camadas
- CQRS (Command Query Responsibility Segregation) com MediatR
- Entity Framework Core 8 para persistência
- PostgreSQL 15 como banco de dados
- Migrations automáticas na inicialização
- Seed de dados inicial (admin + categorias)
- Logging estruturado com Serilog (formato ECS)
- Health checks para monitoramento
- Validações com FluentValidation
- Tratamento global de exceções com RFC 9457 (Problem Details)
- Auditoria automática de entidades (Created/Updated timestamps e usuários)

#### Frontend (React 18)
- React 18 + TypeScript
- Vite para build e dev server otimizado
- Empacotamento de rotas por feature (code splitting)
- TanStack Query (React Query) para gerenciamento de estado assíncrono
- Tailwind CSS para estilização
- shadcn/ui como biblioteca de componentes base
- Recharts para visualizações de dados
- React Router para navegação
- Axios para comunicação com API
- Configuração de runtime via variáveis de ambiente (sem rebuild por ambiente)

#### DevOps e Containerização
- Docker multi-stage builds para imagens otimizadas
- Docker Compose para orquestração dos serviços:
  - PostgreSQL 15 (Alpine)
  - API .NET 8 (backend)
  - Web React + Nginx (frontend)
- Nginx configurado como reverse proxy e SPA server
- Health checks em todos os serviços
- Volume persistente para dados do PostgreSQL
- Configuração via variáveis de ambiente (.env)
- Porta configurável (padrão: 8080)

### 🔒 Segurança

- Autenticação JWT com secret configurável
- Senhas hasheadas com algoritmo seguro (BCrypt/PBKDF2)
- Validação de entrada em todas as requisições
- Tokens com expiração configurável
- Proteção contra SQL Injection (EF Core parametrizado)
- CORS configurado para origens permitidas
- Secrets configuráveis via variáveis de ambiente
- Usuário administrador com senha obrigatoriamente alterável

### 📚 Documentação

- README completo com Quick Start
- Arquivo `.env.example` com todas as variáveis documentadas
- DOCKER.md com comandos de uso detalhados
- Instruções de backup e restore
- Troubleshooting e FAQ
- Comentários inline em código complexo
- Licença MIT incluída

### 🧪 Testes

- Testes unitários com JUnit 5 e xUnit
- Testes de integração com Testcontainers
- Testes de integração HTTP com WebApplicationFactory
- Testes End-to-End com Playwright
- Cobertura de código com JaCoCo (≥90%)
- Testes de componentes React com Vitest + Testing Library

### 🎯 Fases de Desenvolvimento (Detalhamento)

#### Fase 1: Autenticação e Usuários
- Setup inicial do projeto (.NET + React)
- Sistema de autenticação JWT
- Gestão de usuários (CRUD)
- Controle de perfis (Admin/Membro)

#### Fase 2: Core Financeiro
- CRUD de Contas Bancárias
- CRUD de Categorias
- CRUD de Transações
- Cálculo de saldos

#### Fase 3: Dashboard e Orçamentos
- Dashboard com gráficos e métricas
- Sistema de orçamentos mensais
- Indicadores visuais de consumo
- Filtros e período de análise

#### Fase 4: API Completa
- Consolidação de endpoints REST
- Versionamento de API (v1)
- Documentação OpenAPI/Swagger
- Paginação e filtros avançados
- Tratamento unificado de erros

#### Fase 5: Polimento e Release
- Seed de dados inicial (admin + categorias)
- Responsividade mobile completa
- Docker Compose production-ready
- Documentação de instalação
- Release v1.0.0

### 🚀 Como Atualizar

Para atualizar de versões anteriores (se aplicável no futuro):

```bash
# 1. Backup dos dados
docker compose exec db pg_dump -U postgres gestorfinanceiro > backup.sql

# 2. Pare os serviços
docker compose down

# 3. Atualize o código
git pull origin main

# 4. Baixe as novas imagens
docker compose pull

# 5. Rebuild
docker compose build

# 6. Suba novamente (migrations automáticas)
docker compose up -d
```

### ⚠️ Breaking Changes

Nenhum (versão inicial).

### 🐛 Problemas Conhecidos

Nenhum conhecido no momento do release.

---

## [Unreleased]

### Planejado para Próximas Versões

- Exportação de relatórios (PDF/Excel)
- Notificações de ultrapassagem de orçamento
- Projeções financeiras (fluxo de caixa futuro)
- Suporte a múltiplas moedas
- Modo escuro (dark mode)
- PWA com funcionalidade offline
- Gráficos avançados (heatmaps, sunburst)
- Importação de extratos bancários (OFX/CSV)
- API pública para integrações
- Automação de transações recorrentes
- Tags personalizadas para transações
- Anexos de comprovantes (imagens/PDFs)

---

## Histórico de Versões

- **[1.0.0]** - 2026-02-15 - Release inicial (MVP completo)
