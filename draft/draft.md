🎯 1️⃣ Princípios do MVP

O MVP do GestAuto precisa:

Ser financeiramente correto

Ter modelo sólido de contas

Ter projeção básica

Ter saldo consistente

Ser simples de usar

Ser estável para self-hosted

Não precisa:

Ser bonito demais

Ter mil gráficos

Ter automações avançadas

🧱 2️⃣ Escopo Oficial do MVP v1.0
🔹 Módulo 1 — Autenticação

Login

Admin cria usuários

JWT ou cookie seguro

Logout

🔹 Módulo 2 — Contas
Funcionalidades:

Criar conta

Editar conta

Tipo (Corrente, Cartão, Investimento, Carteira)

Saldo inicial

Permitir saldo negativo

Ativar/Inativar conta

Regra:

Saldo materializado.

🔹 Módulo 3 — Categorias

Criar categoria

Editar

Tipo (Receita / Despesa)

Categorias padrão no seed inicial

🔹 Módulo 4 — Transações
Criar transação:

Conta

Tipo (Debit / Credit)

Valor

Categoria

CompetenceDate

DueDate (opcional)

Status automático inteligente

Descrição

Permitir:

Parcelamento (gera InstallmentGroup)

Recorrência mensal simples

Transferência entre contas

NÃO permitir:

Exclusão física

Edição de parcela isolada

Permitir:

Ajuste (Adjustment)

Cancelamento lógico

🔹 Módulo 5 — Projeção Financeira (Simples)

Tela nova:

“Projeção”

Mostrar:

Saldo atual

Saldo projetado próximos 3 meses

Parcelas futuras

Recorrências futuras

Despesas pendentes

Cálculo:

Saldo atual

transações futuras Paid

transações Pending que vencem no período

Sem IA. Sem previsão estatística. Apenas matemática determinística.

🔹 Módulo 6 — Dashboard
Cards:

Saldo total

Total receitas mês

Total despesas mês

Dívida cartão total

Gráficos:

Receita vs despesa por competência

Despesa por categoria

🔹 Módulo 7 — Histórico & Auditoria

Em cada transação mostrar:

Criado por

Criado em

Se é ajuste

Se está cancelado

Nada complexo, mas presente.

🔹 Módulo 8 — Backup Manual

Export JSON completo

Import JSON validado

Sem backup automático.

🗺 3️⃣ Roadmap Técnico Sugerido
🟢 Fase 1 — Core Financeiro (Sem UI bonita)

Entidades

Migration

Regras de saldo

Transação + ajuste

Transferência

Parcelamento

Objetivo: motor funcionando.

🟢 Fase 2 — API completa

Endpoints

Validações

Testes de consistência

🟢 Fase 3 — Frontend básico

CRUD contas

CRUD categorias

CRUD transações

Dashboard simples

🟢 Fase 4 — Projeção

Endpoint de projeção

Tela simples

🟢 Fase 5 — Polimento

Seed inicial

Responsividade

Docker final

Versão 1.0.0

📦 4️⃣ Fora do MVP (Mesmo Que Dê Vontade)

Múltiplas moedas

Modo escuro

Gráfico anual comparativo

API pública

Notificações

Controle de orçamento mensal por categoria

Limite de cartão automático

Anexo de comprovante

Se entrar nisso, o prazo explode.

🧠 5️⃣ Diferencial Competitivo Já no Lançamento

Com o que definimos, já teremos:

✔ Engine contábil correta
✔ Cartão modelado corretamente
✔ Projeção futura
✔ Imutabilidade com ajuste
✔ Self-hosted profissional
✔ PostgreSQL

Isso já é nível “produto sério open source”.

🎯 Nome do MVP

Gerenciador Financeiro v1.0
Tagline possível:

Controle absoluto do seu dinheiro. No seu servidor.