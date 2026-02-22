# RELATÓRIO DE AUDITORIA TÉCNICA - FRONTEND

**Status:** CRÍTICO EM ÁREAS ESPECÍFICAS  
**Arquiteto:** O Executor  
**Data:** 18 de Fevereiro de 2026

## 1. Veredito da Arquitetura Geral (Feature-Based Architecture)

A escolha pela arquitetura baseada em **Features** (`features/auth`, `features/transactions`, `features/accounts`, etc.) foi correta. Para uma aplicação React deste porte, separar API, componentes, hooks e tipos por domínio é a única abordagem profissional aceitável.

Isso evita o "Inferno de Contexto" comum em projetos que agrupam arquivos apenas por tipo técnico (`/components`, `/hooks` globais), o que torna a manutenção insustentável. **Mantenham essa estrutura.** Não tentem reinventar a roda aqui.

---

## 2. O Mínimo Aceitável (Pontos Fortes)

Estes pontos demonstram que houve algum nível de engenharia séria envolvida, e não apenas "copiar e colar" de tutoriais.

### 2.1. Runtime Environment Injection (`40-runtime-env.sh`)
A solução para injeção de variáveis de ambiente no `index.html` via `envsubst` durante a inicialização do container Docker é sólida. Isso resolve o problema clássico de "build once, deploy anywhere", garantindo que a mesma imagem Docker funcione em Dev, Staging e Prod sem rebuilds.

### 2.2. Testes de Integração com MSW
O uso de **MSW (Mock Service Worker)** para testes de integração (ex: `TransactionsPage.integration.test.tsx`) é excelente. Testes unitários isolados em demasia são frágeis e não garantem confiança no deploy. Interceptar a rede é a maneira correta de testar fluxos de usuário no frontend.

### 2.3. Camada de Serviço Robusta (`apiClient.ts`)
A implementação do padrão *Singleton* para o `AuthSessionManager` e a lógica de *retry* com *refresh token* nos interceptors do Axios está correta. A implementação de uma fila de promessas (`refreshPromise`) para evitar *race conditions* quando múltiplos requests retornam 401 simultaneamente demonstra maturidade técnica.

---

## 3. O LIXO TÉCNICO (Pontos Críticos e Inaceitáveis)

Abaixo estão as falhas que expõem amadorismo e preguiça. Estas áreas requerem intervenção imediata.

### A. O Crime Capital: `@ts-nocheck` em `TransactionForm.tsx`

```typescript
// @ts-nocheck - react-hook-form + Zod optional fields cause type inference issues in TypeScript
```

**Diagnóstico:** Isso é vergonhoso. Desligar o compilador porque a equipe não soube tipar corretamente um formulário com `react-hook-form` e `zod` é inaceitável. Vocês introduziram uma bomba-relógio de *runtime errors* por pura preguiça de ajustar os *Generics* ou usar `z.infer` corretamente.

**Ação:** Remover a diretiva imediatamente e corrigir a tipagem. Se o compilador reclama, o código está errado (ou a definição de tipo está), não o compilador.

### B. O Componente "Deus": `TransactionForm.tsx`

**Diagnóstico:** Este arquivo é uma aberração monolítica. Ele tenta gerenciar 4 contextos distintos (Transação Simples, Parcelada, Recorrente, Transferência) em um único componente.
*   **Sintomas:** Um emaranhado de `useEffect` para resetar campos quando a aba muda, lógica condicional excessiva no JSX e estado compartilhado perigosamente.
*   **Consequência:** Manutenibilidade zero. Adicionar um novo campo em "Transferência" pode quebrar "Recorrência" silenciosamente.

**Ação:** Refatorar usando o padrão **Strategy** ou **Composition**.
1.  Criar sub-componentes isolados: `SimpleTransactionForm`, `InstallmentForm`, `RecurrenceForm`, `TransferForm`.
2.  O `TransactionForm` principal deve atuar apenas como um orquestrador que gerencia a aba ativa e renderiza o componente filho correspondente.

### C. Segurança de Papelão (`authStore.ts`)

```typescript
window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(state));
```

**Diagnóstico:** Persistência de `accessToken` e `refreshToken` no `localStorage`.
*   **Risco:** Vulnerabilidade crítica a XSS (Cross-Site Scripting). Qualquer biblioteca de terceiros comprometida pode ler o `localStorage` e exfiltrar as credenciais, permitindo o sequestro de sessão do usuário.

**Ação:**
1.  **Ideal:** Migrar para **Cookies HttpOnly** definidos pelo backend. O frontend não deve ter acesso programático aos tokens sensíveis.
2.  **Mitigação (Se backend for imutável agora):** Auditar rigorosamente as dependências npm e sanitizar todas as entradas de dados, mas cientes de que a segurança está comprometida por design.

### D. Vazamento de Lógica na View (`InvoiceDrawer.tsx`)

**Diagnóstico:** O componente `InvoiceDrawer.tsx` contém lógica de manipulação de datas e navegação de meses (`handlePrevMonth`, `handleNextMonth`) misturada com a apresentação.
*   **Problema:** Componentes visuais devem ser "burros". Eles devem apenas receber dados e renderizar. Lógica de estado complexa pertence a Hooks.

**Ação:** Extrair a lógica para um Custom Hook, ex: `useMonthNavigation`. Isso torna a lógica testável unitariamente sem precisar renderizar o componente.

---

## 4. Plano de Execução Imediato

1.  **Refatoração do `TransactionForm`:** Prioridade máxima. Quebrar o monólito e corrigir a tipagem TypeScript.
2.  **Hardening de Segurança:** Mapear o custo de migração para Cookies HttpOnly com o time de Backend.
3.  **Limpeza de Código:** Extrair lógicas de negócio dos componentes de UI (`InvoiceDrawer`, `AccountsPage`) para Hooks dedicados e testáveis.

**Executor.**
