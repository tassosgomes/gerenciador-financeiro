# PRD — API Completa (Fase 2)

## Visão Geral

A Fase 2 expõe o Core Financeiro (Fase 1) como uma API REST segura, adicionando autenticação JWT, endpoints para todas as entidades, validações de entrada, histórico/auditoria consultável e backup manual por JSON. Ao final desta fase, o GestorFinanceiro terá um backend funcional e testável via ferramentas como Postman/curl, pronto para receber o frontend na Fase 3.

**Problema**: O motor financeiro da Fase 1 existe apenas como domínio interno. Sem uma API, não há como consumidores (frontend, scripts, testes de integração) interagirem com o sistema.

**Para quem**: Membros da família que precisam acessar o sistema via interface (futura) e admin que gerencia usuários e backups.

**Valor**: Backend completo, seguro e documentável que serve de contrato para o frontend e garante integridade via validações e autenticação.

## Objetivos

- Implementar autenticação e autorização com JWT para acesso seguro
- Expor endpoints RESTful para todas as operações do Core Financeiro
- Validar todas as entradas de dados na borda da API
- Fornecer endpoints de histórico e auditoria
- Implementar export/import JSON para backup manual
- Garantir cobertura de testes de integração para todos os endpoints críticos

## Histórias de Usuário

- Como **admin da família**, quero fazer login com credenciais seguras para acessar o sistema
- Como **admin da família**, quero cadastrar membros da família como usuários para que possam registrar suas transações
- Como **membro da família**, quero acessar os endpoints de contas, categorias e transações via API autenticada
- Como **membro da família**, quero receber mensagens de erro claras quando enviar dados inválidos
- Como **membro da família**, quero consultar o histórico de alterações de uma transação para entender o que aconteceu
- Como **admin da família**, quero exportar todos os dados em JSON para fazer backup manual
- Como **admin da família**, quero importar um backup JSON validado para restaurar dados em caso de necessidade
- Como **membro da família**, quero fazer logout para encerrar minha sessão de forma segura

## Funcionalidades Principais

### F1 — Autenticação e Gestão de Usuários

**Requisitos funcionais:**

1. O sistema deve permitir login com e-mail e senha, retornando um token JWT
2. O token JWT deve conter: id do usuário, nome, papel (admin/membro) e expiração
3. O tempo de expiração do token deve ser configurável (padrão sugerido: 24h)
4. O sistema deve suportar refresh token para renovação de sessão sem novo login
5. O admin da família deve poder criar novos usuários com: nome, e-mail, senha temporária e papel
6. O admin deve poder desativar um usuário (sem exclusão física)
7. Cada usuário deve poder alterar sua própria senha
8. O endpoint de logout deve invalidar o token atual (blacklist ou rotação)
9. Toda operação de gestão de usuários deve registrar auditoria (quem, quando)

### F2 — Endpoints de Contas

**Requisitos funcionais:**

10. `POST /api/contas` — criar conta (nome, tipo, saldo inicial, permitir saldo negativo)
11. `GET /api/contas` — listar contas do usuário (com filtro por status ativo/inativo)
12. `GET /api/contas/{id}` — detalhe da conta com saldo atual
13. `PUT /api/contas/{id}` — editar conta (nome, permitir saldo negativo)
14. `PATCH /api/contas/{id}/status` — ativar/inativar conta
15. Validações: nome obrigatório (3-100 chars), tipo deve ser um dos permitidos, saldo inicial ≥ 0

### F3 — Endpoints de Categorias

**Requisitos funcionais:**

16. `POST /api/categorias` — criar categoria (nome, tipo: Receita/Despesa)
17. `GET /api/categorias` — listar categorias (com filtro por tipo)
18. `PUT /api/categorias/{id}` — editar nome da categoria
19. Validações: nome obrigatório (2-100 chars), tipo deve ser Receita ou Despesa

### F4 — Endpoints de Transações

**Requisitos funcionais:**

20. `POST /api/transacoes` — criar transação simples
21. `POST /api/transacoes/parcelada` — criar transação parcelada (inclui número de parcelas)
22. `POST /api/transacoes/recorrente` — criar transação recorrente
23. `POST /api/transacoes/transferencia` — criar transferência entre contas
24. `GET /api/transacoes` — listar transações com filtros: conta, categoria, tipo, status, período de competência, período de vencimento
25. `GET /api/transacoes/{id}` — detalhe da transação com informações de auditoria
26. `POST /api/transacoes/{id}/ajuste` — criar ajuste para uma transação existente
27. `POST /api/transacoes/{id}/cancelar` — cancelar transação (com motivo opcional)
28. `POST /api/transacoes/grupo-parcelas/{groupId}/cancelar` — cancelar todas as parcelas pendentes de um grupo
29. Paginação obrigatória na listagem (padrão: 20 itens por página)
30. Validações: valor > 0, conta deve existir e estar ativa, categoria deve existir, datas válidas

### F5 — Histórico e Auditoria

**Requisitos funcionais:**

31. `GET /api/transacoes/{id}/historico` — retornar histórico completo da transação (criação, ajustes, cancelamento)
32. `GET /api/auditoria` — listar eventos de auditoria com filtros: entidade, período, usuário (acesso restrito ao admin)
33. Cada registro de auditoria deve conter: entidade, id da entidade, ação, usuário, timestamp, dados anteriores (quando aplicável)

### F6 — Backup Manual

**Requisitos funcionais:**

34. `GET /api/backup/exportar` — exportar todos os dados em formato JSON (acesso restrito ao admin)
35. `POST /api/backup/importar` — importar JSON validado, restaurando dados (acesso restrito ao admin)
36. O import deve validar a integridade dos dados antes de aplicar (referências entre entidades, formatos)
37. O import deve ser transacional — falha em qualquer registro reverte toda a operação
38. O export deve incluir: usuários (sem senhas), contas, categorias, transações, grupos de parcela, transferências

### F7 — Tratamento de Erros e Validação

**Requisitos funcionais:**

39. Erros de validação devem retornar HTTP 400 com corpo padronizado: `{ "errors": [{ "field": "...", "message": "..." }] }`
40. Erros de autenticação devem retornar HTTP 401
41. Erros de autorização devem retornar HTTP 403
42. Recurso não encontrado deve retornar HTTP 404
43. Erros internos devem retornar HTTP 500 com mensagem genérica (sem stack trace em produção)
44. Todos os endpoints devem validar o token JWT e rejeitar requisições sem autenticação (exceto login / Health Check)

## Experiência do Usuário

Nesta fase a interação é via API REST. A experiência foca em:

- **Respostas consistentes**: formato JSON padronizado para sucesso e erro
- **Mensagens de erro claras**: em português, indicando campo e problema
- **Paginação previsível**: cursor ou offset-based com metadados (total, página atual, próxima)
- **Documentação**: endpoints devem ser auto-documentáveis (OpenAPI/Swagger)

**Personas:**
- **Admin da família**: acesso total — gestão de usuários, backup, auditoria global
- **Membro da família**: acesso às suas contas, categorias compartilhadas, transações próprias

## Restrições Técnicas de Alto Nível

- **Stack**: ASP.NET Core Web API
- **Autenticação**: JWT Bearer Token
- **Banco de dados**: PostgreSQL (mesmo da Fase 1)
- **Documentação de API**: Swagger/OpenAPI via Swashbuckle ou equivalente
- **Padrão REST**: seguir convenções de `rules/restful.md`
- **Segurança**: senhas armazenadas com hash (bcrypt ou Argon2), nunca em texto plano
- **CORS**: configurável para ambiente self-hosted
- **Logging**: estruturado, seguindo `rules/dotnet-logging.md`
- **Testes**: integração com banco em memória ou Testcontainers para PostgreSQL

## Não-Objetivos (Fora de Escopo)

- Interface de usuário / frontend (→ Fase 3)
- Projeção financeira (→ Fase 4)
- API pública para terceiros
- OAuth2 / login social
- Rate limiting avançado
- Versionamento de API (v2, v3)
- Notificações push/e-mail
- Backup automático / agendado
- WebSockets ou Server-Sent Events

## Questões em Aberto

1. **Isolamento de dados**: membros da família veem todas as transações da família ou apenas as próprias? (sugestão: todos veem tudo, auditoria mostra quem criou)
2. **Refresh token**: armazenar em banco ou usar rotação de JWT sem estado?
3. **Rate limiting**: implementar rate limiting básico no MVP ou deixar para pós-MVP?
4. **Swagger UI**: expor Swagger UI em produção (self-hosted) ou apenas em desenvolvimento?
5. **Import destrutivo**: o import substitui todos os dados ou faz merge? (sugestão: substituição completa com confirmação)
