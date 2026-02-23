---
status: pending
parallelizable: false
blocked_by: ["6.0"]
---

<task_context>
<domain>frontend</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>http_server</dependencies>
<unblocks>""</unblocks>
</task_context>

# Tarefa 7.0: Frontend — Página de Importação e Integração UI

## Visão Geral

Implementar toda a interface visual do recurso de Importação de Cupom Fiscal. Esta é a tarefa final do projeto e inclui: a página de importação com wizard de 3 etapas, o componente de preview com tabela de itens, a seção de itens do cupom na página de detalhe da transação, o botão de importação na página de listagem, o badge indicador de cupom fiscal, e a nova rota no router. Usa os hooks e tipos criados na Task 6.0.

## Requisitos

- Criar `ImportReceiptPage` com wizard de 3 steps (Input → Preview → Confirmação)
- Criar componente `ReceiptPreview` com tabela de itens do cupom
- Criar componente `ReceiptItemsSection` para exibição na página de detalhe da transação
- Atualizar `TransactionDetailPage` para exibir seção de itens quando `hasReceipt` é true
- Atualizar `TransactionsPage` com botão "Importar Cupom Fiscal"
- Adicionar badge/indicador visual de cupom fiscal na listagem de transações
- Adicionar rota `/transactions/import-receipt` no router
- Seguir padrões visuais existentes: shadcn/ui, TailwindCSS, layout consistente
- Acessibilidade: labels, tabela semântica, navegação por teclado, anúncio de erros
- Feedback de loading durante consulta SEFAZ

## Subtarefas

- [ ] 7.1 Adicionar rota no router
  - Nova rota: `/transactions/import-receipt` → `ImportReceiptPage` (lazy-loaded)
  - Dentro do layout protegido existente (ProtectedRoute)

- [ ] 7.2 Criar `ImportReceiptPage` com wizard de 3 steps
  - **Step 1 — Input da NFC-e:**
    - Campo de texto para chave de acesso (44 dígitos) ou URL da NFC-e
    - Detecção automática: se contém `http` ou `sefaz` → URL; senão → chave de acesso
    - Máscara/formatação opcional para a chave (grupos de 4 dígitos com espaço)
    - Botão "Consultar" que chama `useReceiptLookup`
    - Loading spinner durante a consulta (pode levar 2-10 segundos)
    - Mensagens de erro claras: chave inválida, NFC-e não encontrada, SEFAZ indisponível
    - Aviso de duplicidade se `alreadyImported === true` (bloqueia prosseguir)
  - **Step 2 — Preview e Configuração:**
    - Exibição dos dados do cupom usando `ReceiptPreview`
    - Seletor de **Conta** (accounts ativas — reusa componente existente do projeto)
    - Seletor de **Categoria de Despesa** (reusa componente existente)
    - Campo **Descrição** (pré-preenchido com nome do estabelecimento, editável)
    - Campo **Data** (pré-preenchido com data de emissão do cupom, editável)
    - Validação com schema Zod da Task 6.0
    - Botão "Voltar" para step 1, "Importar" para confirmar
  - **Step 3 — Confirmação:**
    - Loading durante a importação
    - Sucesso: toast de sucesso + redirecionamento para detalhe da transação criada
    - Erro: toast de erro + permanece no step 2 para tentar novamente

- [ ] 7.3 Criar componente `ReceiptPreview`
  - Exibe dados do estabelecimento: nome (razão social), CNPJ formatado (XX.XXX.XXX/XXXX-XX)
  - Exibe data da compra formatada
  - Tabela semântica (`<table>`) com itens:
    - Colunas: #, Descrição, Qtd, Unidade, Valor Unitário (R$), Valor Total (R$)
    - Cada linha é um item do cupom
    - Footer com totais: subtotal, desconto (se houver), **total pago** (destaque)
  - Se houver desconto, exibir linha de desconto com destaque visual
  - Responsivo: tabela com scroll horizontal em telas pequenas

- [ ] 7.4 Criar componente `ReceiptItemsSection` para detalhe da transação
  - Usa hook `useTransactionReceipt` para buscar dados
  - Seção/aba "Itens do Cupom Fiscal" na página de detalhe
  - Dados do estabelecimento: nome, CNPJ formatado
  - Chave de acesso da NFC-e (formato com espaços para legibilidade)
  - Tabela de itens (mesmo layout do `ReceiptPreview`)
  - Loading skeleton enquanto carrega
  - Exibido apenas quando `hasReceipt === true`

- [ ] 7.5 Atualizar `TransactionDetailPage`
  - Verificar `hasReceipt` na transação
  - Se true, renderizar `ReceiptItemsSection` abaixo dos dados existentes
  - Exibir badge "Cupom Fiscal" no header do detalhe

- [ ] 7.6 Atualizar `TransactionsPage` com botão de importação
  - Adicionar botão "Importar Cupom" no header da página (ao lado dos filtros/ações existentes)
  - Ícone: `Receipt` ou `FileText` do Lucide
  - Navega para `/transactions/import-receipt`

- [ ] 7.7 Adicionar indicador de cupom fiscal na listagem de transações
  - Na tabela de transações, adicionar badge/ícone quando `hasReceipt === true`
  - Badge discreto (ex: ícone de recibo pequeno ou badge "NFC-e") na coluna de descrição ou status
  - Tooltip: "Importado via Cupom Fiscal"

- [ ] 7.8 Testes frontend
  - **ImportReceiptPage:**
    - Renderiza step 1 inicialmente
    - Input de chave de acesso e clique em "Consultar" chama lookup
    - Após lookup bem-sucedido, avança para step 2 com preview
    - Alerta de duplicidade quando `alreadyImported === true`
    - Seleção de conta, categoria e confirmação chama import
    - Redirecionamento após import bem-sucedido
  - **ReceiptPreview:**
    - Renderiza tabela com itens corretos
    - Exibe desconto quando presente
    - Formata valores monetários corretamente
  - **ReceiptItemsSection:**
    - Renderiza dados do estabelecimento e itens
    - Mostra skeleton durante loading
  - **TransactionDetailPage (extensão):**
    - Renderiza seção de cupom quando `hasReceipt === true`
    - Não renderiza seção quando `hasReceipt === false`
  - **TransactionsPage (extensão):**
    - Botão "Importar Cupom" é renderizado
    - Badge de cupom exibido para transações com `hasReceipt === true`

## Sequenciamento

- Bloqueado por: 6.0 (Tipos, API, Hooks)
- Desbloqueia: Nenhum (tarefa final)
- Paralelizável: Não (depende da infra frontend da Task 6.0)

## Detalhes de Implementação

### Localização dos Arquivos

| Arquivo | Caminho |
|---------|---------|
| `ImportReceiptPage.tsx` | `frontend/src/features/transactions/pages/` |
| `ReceiptPreview.tsx` | `frontend/src/features/transactions/components/` |
| `ReceiptItemsSection.tsx` | `frontend/src/features/transactions/components/` |
| Rota | `frontend/src/app/router/routes.tsx` |
| Testes | `frontend/src/features/transactions/test/` |

### Padrões a Seguir

- Consultar páginas existentes (ex: `TransactionsPage`, `TransactionDetailPage`) para layout e padrão
- Consultar componentes do formulário de transação existente para reutilizar seletores de conta/categoria
- Usar componentes shadcn/ui: `Card`, `Table`, `Button`, `Input`, `Select`, `Badge`, `Skeleton`, `Sonner` (toast)
- Usar ícones do Lucide React
- Seguir padrão de formatação monetária existente (`formatCurrency` utils)
- Seguir padrão de formatação de data existente
- Testes com Vitest + Testing Library + MSW

### Wireframe do Wizard

```
┌──────────────────────────────────────────────────┐
│  Importar Cupom Fiscal                           │
│                                                  │
│  Step: ① Consultar  ② Revisar  ③ Confirmar       │
│  ─────────────────────────────────────────       │
│                                                  │
│  [Step 1]                                        │
│  Chave de acesso ou URL da NFC-e:                │
│  ┌──────────────────────────────────────┐        │
│  │ 1234 5678 9012 3456 7890 1234 5678...│        │
│  └──────────────────────────────────────┘        │
│                                                  │
│  [Consultar]                                     │
│                                                  │
│  ─────────────────────────────────────────       │
│  [Step 2 - após consulta]                        │
│  ┌─ Dados do Cupom ─────────────────────┐        │
│  │ Estabelecimento: SUPERMERCADO X LTDA  │        │
│  │ CNPJ: 12.345.678/0001-90             │        │
│  │ Data: 20/02/2026 14:30               │        │
│  ├───────────────────────────────────────┤        │
│  │ #  Descrição        Qtd  Unit  Total  │        │
│  │ 1  ARROZ TIPO 1 5KG  2   UN   51,80  │        │
│  │ 2  FEIJÃO CARIOCA    1   UN   12,90  │        │
│  │ ...                                   │        │
│  ├───────────────────────────────────────┤        │
│  │ Subtotal:                   R$ 150,00 │        │
│  │ Desconto:                  -R$   5,00 │        │
│  │ Total Pago:                 R$ 145,00 │        │
│  └───────────────────────────────────────┘        │
│                                                  │
│  Conta:     [▼ Selecione uma conta     ]         │
│  Categoria: [▼ Selecione uma categoria ]         │
│  Descrição: [SUPERMERCADO X LTDA       ]         │
│  Data:      [20/02/2026                ]         │
│                                                  │
│  [◀ Voltar]                    [Importar ▶]      │
└──────────────────────────────────────────────────┘
```

### Formatação de CNPJ

```typescript
function formatCnpj(cnpj: string): string {
  return cnpj.replace(
    /^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/,
    '$1.$2.$3/$4-$5'
  );
}
```

### Formatação da Chave de Acesso

```typescript
function formatAccessKey(key: string): string {
  return key.replace(/(\d{4})/g, '$1 ').trim();
}
```

## Critérios de Sucesso

- Wizard de importação funciona end-to-end: input → preview → confirmação → redirecionamento
- Step 1 aceita chave de acesso (44 dígitos) e URL, detectando formato automaticamente
- Step 2 exibe preview completo com tabela de itens, totais e descontos
- Step 2 permite seleção de conta, categoria, edição de descrição e data
- Importação bem-sucedida redireciona para detalhe da transação
- Erros são exibidos com mensagens claras em português (SEFAZ indisponível, NFC-e não encontrada, duplicidade)
- Loading feedback durante consulta à SEFAZ (2-10 segundos)
- `TransactionDetailPage` exibe seção "Itens do Cupom Fiscal" para transações importadas
- `TransactionsPage` tem botão "Importar Cupom" e badge de cupom fiscal na listagem
- Rota `/transactions/import-receipt` funciona e é protegida
- Tabela de itens é semanticamente correta (`<table>`) e responsiva
- CNPJ formatado corretamente (XX.XXX.XXX/XXXX-XX)
- Valores monetários formatados no padrão brasileiro (R$ X.XXX,XX)
- Acessibilidade: labels, navegação por teclado, anúncios para leitores de tela
- Mínimo 8 testes frontend passando
- Nenhum teste existente quebrado
- Projeto frontend compila sem erros TypeScript
