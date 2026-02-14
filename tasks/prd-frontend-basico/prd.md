# PRD — Frontend Básico (Fase 3)

## Visão Geral

A Fase 3 entrega a interface web do GestorFinanceiro em React, conectando-se à API da Fase 2. Inclui tela de login, CRUDs de contas/categorias/transações e um dashboard com cards de resumo e gráficos básicos. O objetivo é tornar o sistema usável para membros da família sem conhecimento técnico.

**Problema**: O backend da Fase 2 só é acessível via API/curl. Para que a família use o sistema no dia a dia, é necessária uma interface web simples e funcional.

**Para quem**: Todos os membros da família que precisam registrar transações, consultar saldos e acompanhar finanças.

**Valor**: Interface funcional que transforma o motor financeiro em um produto utilizável, com dashboard visual para acompanhamento rápido.

## Objetivos

- Entregar interface web funcional para todas as operações do Core Financeiro
- Implementar dashboard com visão consolidada das finanças da família
- Garantir que o sistema seja usável em desktop (responsividade mobile é Fase 5)
- Manter a interface simples e objetiva — funcionalidade sobre estética
- Integrar com todos os endpoints da API (Fase 2) via chamadas autenticadas

## Histórias de Usuário

- Como **membro da família**, quero fazer login na interface web para acessar minhas finanças
- Como **membro da família**, quero ver o dashboard ao entrar no sistema para ter uma visão rápida das finanças
- Como **membro da família**, quero cadastrar, editar e inativar contas pela interface
- Como **membro da família**, quero cadastrar e editar categorias pela interface
- Como **membro da família**, quero registrar transações (simples, parcelada, recorrente, transferência) pela interface
- Como **membro da família**, quero filtrar transações por conta, categoria, período e status
- Como **membro da família**, quero cancelar ou ajustar uma transação pela interface
- Como **membro da família**, quero ver o histórico de auditoria de uma transação
- Como **admin da família**, quero acessar a gestão de usuários pela interface
- Como **admin da família**, quero exportar e importar backup pela interface

## Funcionalidades Principais

### F1 — Tela de Login

**Requisitos funcionais:**

1. A tela de login deve solicitar e-mail e senha
2. Após login bem-sucedido, o token JWT deve ser armazenado de forma segura (httpOnly cookie ou secure storage)
3. Tentativas de login com erro devem exibir mensagem genérica ("Credenciais inválidas")
4. O sistema deve redirecionar para o dashboard após login
5. Sessão expirada deve redirecionar para a tela de login com mensagem informativa
6. Botão de logout deve estar acessível em todas as telas

### F2 — Dashboard

Tela principal com visão consolidada das finanças.

**Requisitos funcionais:**

7. Card: **Saldo total** — soma dos saldos de todas as contas ativas
8. Card: **Total receitas do mês** — soma das transações `Credit` com status `Paid` no mês de competência atual
9. Card: **Total despesas do mês** — soma das transações `Debit` com status `Paid` no mês de competência atual
10. Card: **Dívida de cartão total** — soma dos saldos negativos das contas do tipo `Cartão`
11. Gráfico: **Receita vs Despesa por competência** — gráfico de barras com os últimos 6 meses
12. Gráfico: **Despesa por categoria** — gráfico de pizza/donut com o mês atual
13. O dashboard deve carregar dados resumidos via endpoints específicos (evitar carregar todas as transações)
14. O mês de referência deve ser configurável (seletor de mês/ano)

### F3 — CRUD de Contas

**Requisitos funcionais:**

15. Tela de listagem de contas com: nome, tipo, saldo atual, status (ativa/inativa)
16. Formulário de criação de conta com: nome, tipo (dropdown), saldo inicial, flag "permitir saldo negativo"
17. Formulário de edição de conta (nome, flag saldo negativo)
18. Botão para ativar/inativar conta com confirmação
19. Indicação visual do tipo de conta (ícones ou cores)
20. O saldo deve ser exibido formatado em Real brasileiro (R$)

### F4 — CRUD de Categorias

**Requisitos funcionais:**

21. Tela de listagem de categorias com: nome e tipo (Receita/Despesa)
22. Filtro por tipo (Receita / Despesa / Todas)
23. Formulário de criação com: nome e tipo
24. Formulário de edição (apenas nome)
25. Indicação visual do tipo (cor ou ícone diferenciado para Receita vs Despesa)

### F5 — CRUD de Transações

**Requisitos funcionais:**

26. Tela de listagem de transações com: data de competência, descrição, categoria, conta, valor, status
27. Filtros: conta, categoria, tipo (Debit/Credit), status, período de competência (de-até)
28. Paginação na listagem
29. Formulário de criação com abas ou seletor para: Simples, Parcelada, Recorrente, Transferência
30. **Transação simples**: conta, tipo, valor, categoria, data de competência, data de vencimento (opcional), descrição
31. **Transação parcelada**: mesmos campos + número de parcelas; exibir preview das parcelas antes de confirmar
32. **Transação recorrente**: mesmos campos + indicação de recorrência mensal
33. **Transferência**: conta de origem, conta de destino, valor, data de competência, descrição
34. Ação de **cancelar** transação com confirmação e campo de motivo (opcional)
35. Ação de **ajustar** transação: formulário com novo valor e justificativa
36. Detalhe da transação exibindo: dados, status, se é ajuste, se é parcela (X de Y), se é recorrente, se é transferência, histórico de auditoria
37. Valores devem ser exibidos formatados em R$ com cores: verde para Credit, vermelho para Debit
38. Transações canceladas devem ter indicação visual clara (riscado ou badge)

### F6 — Gestão de Usuários (Admin)

**Requisitos funcionais:**

39. Tela acessível apenas pelo admin
40. Listagem de usuários com: nome, e-mail, papel, status (ativo/inativo)
41. Formulário de criação de usuário: nome, e-mail, senha temporária, papel (admin/membro)
42. Botão para desativar/reativar usuário

### F7 — Backup (Admin)

**Requisitos funcionais:**

43. Botão "Exportar Backup" que faz download do JSON completo
44. Botão "Importar Backup" com upload de arquivo JSON e confirmação antes de aplicar
45. Exibir mensagem de sucesso ou erro após operação
46. Aviso claro de que o import substitui dados existentes

## Experiência do Usuário

### Personas
- **Membro da família**: usa diariamente para registrar gastos e consultar saldos
- **Admin da família**: usa periodicamente para gerenciar usuários e fazer backup

### Fluxos Principais

1. **Login → Dashboard**: entrada natural no sistema com visão geral
2. **Dashboard → Nova Transação**: atalho rápido para registrar gasto/receita
3. **Listagem Transações → Detalhe → Ajuste/Cancelamento**: fluxo de correção
4. **Contas → Detalhe da Conta → Transações filtradas**: navegação por conta

### Diretrizes de UI/UX

- Layout simples com navegação lateral (sidebar) ou superior (navbar)
- Formulários com validação inline (antes de enviar ao servidor)
- Feedbacks claros: toasts para sucesso, alertas para erros
- Confirmação em ações destrutivas (cancelamento, import)
- Loading states em todas as chamadas à API
- Formatação brasileira: R$, datas dd/MM/yyyy, separador decimal com vírgula

### Acessibilidade

- Labels em todos os campos de formulário
- Navegação por teclado funcional
- Contraste mínimo WCAG AA
- Textos alternativos em ícones informativos

## Restrições Técnicas de Alto Nível

- **Stack**: React (com TypeScript)
- **Gerenciamento de estado**: solução leve (Context API ou Zustand — evitar Redux no MVP)
- **Gráficos**: biblioteca leve (Recharts, Chart.js ou similar)
- **HTTP client**: Axios ou fetch nativo com interceptors para JWT
- **Roteamento**: React Router
- **Formatação**: Intl.NumberFormat para moeda, date-fns ou similar para datas
- **Build**: Vite
- **Testes**: testes de componentes críticos (formulários, dashboard)
- **Comunicação com API**: proxy ou variável de ambiente para URL do backend

## Não-Objetivos (Fora de Escopo)

- Responsividade mobile completa (→ Fase 5)
- Mode escuro
- PWA / app mobile
- Projeção financeira (→ Fase 4)
- Gráfico anual comparativo
- Drag and drop
- Notificações em tempo real
- Internacionalização (apenas pt-BR)
- Design system elaborado (Material UI, Ant Design) — manter simples

## Questões em Aberto

1. **Biblioteca de componentes**: usar uma UI library (Shadcn/UI, Mantine) para acelerar ou componentes puros para manter leve?
2. **Dashboard endpoints**: a API precisa de endpoints específicos de agregação ou o frontend calcula a partir das transações?
3. **Navegação**: sidebar fixa ou navbar superior? (sugestão: sidebar colapsável)
4. **Tema visual**: definir paleta de cores e tipografia base antes de iniciar o frontend?
5. **Formulário de transação**: uma página com abas (Simples/Parcelada/Recorrente/Transferência) ou telas separadas?
