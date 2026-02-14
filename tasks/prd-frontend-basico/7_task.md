---
status: pending
parallelizable: true
blocked_by: ["3.0"]
---

<task_context>
<domain>frontend/categories</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>low</complexity>
<dependencies>tanstack-query, react-hook-form, zod</dependencies>
<unblocks>"8.0", "10.0"</unblocks>
</task_context>

# Tarefa 7.0: CRUD de Categorias

## Visão Geral

Implementar a feature de gestão de categorias: listagem com filtro por tipo (Receita/Despesa), formulário de criação (nome + tipo) e edição (apenas nome), com indicação visual diferenciada entre Receita e Despesa. É a feature mais simples do sistema, mas essencial para que o formulário de transações tenha o select de categorias disponível.

## Requisitos

- PRD F4 req. 21: Listagem com nome e tipo (Receita/Despesa)
- PRD F4 req. 22: Filtro por tipo (Receita / Despesa / Todas)
- PRD F4 req. 23: Formulário de criação com nome e tipo
- PRD F4 req. 24: Formulário de edição (apenas nome)
- PRD F4 req. 25: Indicação visual do tipo (cor ou ícone diferenciado)

## Subtarefas

- [ ] 7.1 Criar `src/features/categories/types/category.ts` — enum `CategoryType` (Income=1, Expense=2), interfaces `CategoryResponse` (id, name, type, createdAt, updatedAt), `CreateCategoryRequest` (name, type), `UpdateCategoryRequest` (name)
- [ ] 7.2 Criar `src/features/categories/api/categoriesApi.ts` — funções: `getCategories(type?)`, `createCategory(data)`, `updateCategory(id, data)` usando apiClient
- [ ] 7.3 Criar `src/features/categories/hooks/useCategories.ts` — hooks TanStack Query: `useCategories(type?)`, `useCreateCategory()`, `useUpdateCategory()` com mutations e invalidação de cache
- [ ] 7.4 Criar `src/features/categories/components/CategoryFilter.tsx` — barra de filtro com tabs/botões: "Todas", "Receitas", "Despesas"; controla o filtro via estado local ou query param
- [ ] 7.5 Criar `src/features/categories/components/CategoryList.tsx` — lista/tabela de categorias com: nome, badge de tipo (verde "Receita" / vermelho "Despesa"), botão de edição. Usar `Badge` do Shadcn/UI com variantes de cor
- [ ] 7.6 Criar schema Zod: `createCategorySchema` — nome (obrigatório, min 2 chars), tipo (obrigatório). `updateCategorySchema` — nome (obrigatório, min 2 chars)
- [ ] 7.7 Criar `src/features/categories/components/CategoryForm.tsx` — modal com formulário (react-hook-form + zod): campo nome + select tipo (somente em criação). Loading state no botão
- [ ] 7.8 Criar `src/features/categories/pages/CategoriesPage.tsx` — composição: header com título "Categorias" + botão "Nova Categoria", CategoryFilter, CategoryList; modal de criação/edição
- [ ] 7.9 Criar `src/features/categories/index.ts` — barrel export
- [ ] 7.10 Criar MSW handlers: mock de GET/POST/PUT para `/api/v1/categories`
- [ ] 7.11 Testes: CategoryList (renderização, filtro), CategoryForm (validação, criação, edição), CategoriesPage (fluxo completo)

## Sequenciamento

- Bloqueado por: 3.0 (Auth — rota protegida)
- Desbloqueia: 8.0 (Transações — select de categorias no formulário), 10.0 (Polimento)
- Paralelizável: Sim, com 5.0 (Dashboard), 6.0 (Contas), 9.0 (Admin)

## Detalhes de Implementação

### Indicação Visual por Tipo

| Tipo | Badge | Ícone | Cor |
|------|-------|-------|-----|
| Receita (Income) | `bg-green-100 text-green-800` | `arrow_upward` | Verde |
| Despesa (Expense) | `bg-red-100 text-red-800` | `arrow_downward` | Vermelho |

### CategoryList — Estrutura

```
┌──────────────────────────────────────────────────────────┐
│ [Todas] [Receitas] [Despesas]        [+ Nova Categoria] │
├──────────────────────────────────────────────────────────┤
│ Nome                          Tipo            Ações      │
│──────────────────────────────────────────────────────────│
│ Alimentação                   🔴 Despesa      ✏️        │
│ Transporte                    🔴 Despesa      ✏️        │
│ Salário                       🟢 Receita      ✏️        │
│ Freelance                     🟢 Receita      ✏️        │
│ Moradia                       🔴 Despesa      ✏️        │
└──────────────────────────────────────────────────────────┘
```

### CategoryForm — Schema

```typescript
const createCategorySchema = z.object({
  name: z.string().min(2, 'Nome deve ter no mínimo 2 caracteres'),
  type: z.nativeEnum(CategoryType, {
    errorMap: () => ({ message: 'Selecione o tipo' }),
  }),
});

const updateCategorySchema = z.object({
  name: z.string().min(2, 'Nome deve ter no mínimo 2 caracteres'),
});
```

### Hook com Filtro

```typescript
function useCategories(type?: CategoryType) {
  return useQuery({
    queryKey: ['categories', type],
    queryFn: () => getCategories(type),
  });
}
```

## Critérios de Sucesso

- Listagem exibe todas as categorias com badges de tipo coloridos
- Filtro por tipo funciona: "Todas" mostra tudo, "Receitas" filtra Income, "Despesas" filtra Expense
- Botão "Nova Categoria" abre modal com formulário validado
- Criação de categoria: formulário com nome + tipo, toast de sucesso, lista atualizada
- Edição de categoria: modal preenchido com nome atual, tipo não editável (desabilitado ou oculto)
- Validação inline: nome obrigatório e mínimo 2 caracteres
- Testes unitários e de integração passam
