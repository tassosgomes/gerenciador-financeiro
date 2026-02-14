---
status: pending
parallelizable: false
blocked_by: []
---

<task_context>
<domain>frontend/infra</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>medium</complexity>
<dependencies>none</dependencies>
<unblocks>"2.0", "3.0"</unblocks>
</task_context>

# Tarefa 1.0: Scaffold do Projeto React e Infraestrutura

## Visão Geral

Criar o projeto React + TypeScript + Vite no diretório `frontend/` na raiz do monorepo. Configurar todas as ferramentas base: Tailwind CSS, Shadcn/UI, path aliases, runtime config, Axios client, Zustand store base, TanStack Query provider e setup de testes (Vitest + RTL + MSW). Esta tarefa entrega a fundação sobre a qual todas as features serão construídas.

## Requisitos

- Projeto Vite com React 18 e TypeScript em `frontend/`
- Tailwind CSS configurado com tokens customizados extraídos dos mockups (cores, fontes, border-radius)
- Shadcn/UI inicializado com `components.json`
- Path aliases configurados (`@/` → `src/`)
- Runtime config via `window.RUNTIME_ENV` conforme `rules/react-containers.md`
- Axios instance base (`apiClient.ts`) com baseURL configurável
- Zustand instalado e pronto para uso
- TanStack Query provider configurado com defaults sensatos
- Vitest + React Testing Library + MSW configurados
- ESLint configurado para TypeScript + React
- Estrutura de pastas conforme techspec (feature-based)

## Subtarefas

- [ ] 1.1 Criar projeto Vite + React + TypeScript (`npm create vite@latest frontend -- --template react-ts`)
- [ ] 1.2 Instalar dependências: `tailwindcss`, `postcss`, `autoprefixer`, `@tanstack/react-query`, `zustand`, `axios`, `react-router-dom`, `react-hook-form`, `zod`, `@hookform/resolvers`, `recharts`, `date-fns`, `lucide-react`
- [ ] 1.3 Configurar Tailwind CSS com tokens customizados (cores: primary `#137fec`, success `#10b981`, danger `#ef4444`, warning `#f59e0b`, background, surface) e fonte Inter
- [ ] 1.4 Inicializar Shadcn/UI (`npx shadcn-ui@latest init`) com estilo "default" e Tailwind
- [ ] 1.5 Configurar path aliases no `tsconfig.json` e `vite.config.ts` (`@/` → `src/`)
- [ ] 1.6 Criar `src/shared/config/runtimeConfig.ts` com leitura de `window.RUNTIME_ENV` e fallback para `import.meta.env`
- [ ] 1.7 Criar `public/runtime-env.template.js` para injeção em container
- [ ] 1.8 Criar `src/shared/services/apiClient.ts` — Axios instance com baseURL do runtime config, timeout 30s, header Content-Type
- [ ] 1.9 Criar `src/app/providers/QueryProvider.tsx` — QueryClientProvider com defaults (staleTime, retry, refetchOnWindowFocus)
- [ ] 1.10 Criar `src/main.tsx` e `src/App.tsx` com provider wrapper básico
- [ ] 1.11 Criar `src/index.css` com diretivas Tailwind (`@tailwind base/components/utilities`) e importação da fonte Inter
- [ ] 1.12 Criar estrutura de pastas: `src/app/`, `src/shared/`, `src/features/` (auth, dashboard, accounts, categories, transactions, admin)
- [ ] 1.13 Configurar Vitest (`vitest.config.ts`) com jsdom, setup file, path aliases
- [ ] 1.14 Criar `src/shared/test/setup.ts` com cleanup do RTL e MSW server setup
- [ ] 1.15 Criar `src/shared/test/mocks/server.ts` e `handlers.ts` (MSW mock server base)
- [ ] 1.16 Configurar ESLint (`.eslintrc.cjs`) para TypeScript + React
- [ ] 1.17 Criar `Dockerfile` multi-stage (build com Node, serve com Nginx) e `docker/40-runtime-env.sh`
- [ ] 1.18 Validar: `npm run dev` inicia sem erros, `npm run build` compila, `npm run test` executa

## Sequenciamento

- Bloqueado por: Nenhum
- Desbloqueia: 2.0, 3.0
- Paralelizável: Sim, com 4.0 (ajustes backend)

## Detalhes de Implementação

### Runtime Config (`runtimeConfig.ts`)

```typescript
declare global {
  interface Window {
    RUNTIME_ENV?: {
      API_URL?: string;
      OTEL_ENDPOINT?: string;
    };
  }
}

export const API_URL = window.RUNTIME_ENV?.API_URL
  ?? import.meta.env.VITE_API_URL
  ?? 'http://localhost:5000';
```

### Tailwind Config (`tailwind.config.ts`)

```typescript
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: { DEFAULT: '#137fec', dark: '#0e62b6' },
        success: '#10b981',
        danger: '#ef4444',
        warning: '#f59e0b',
        background: { light: '#f6f7f8', dark: '#101922' },
        surface: { light: '#ffffff', dark: '#182430' },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [require('@tailwindcss/forms')],
};
```

### Vite Config (`vite.config.ts`)

```typescript
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5000', changeOrigin: true },
    },
  },
});
```

### Dependências Principais

| Pacote | Versão | Propósito |
|--------|--------|-----------|
| `react` / `react-dom` | ^18 | Framework UI |
| `typescript` | ^5 | Tipagem estática |
| `vite` | ^5 | Build tool |
| `tailwindcss` | ^3 | Estilização utility-first |
| `@tanstack/react-query` | ^5 | Cache e estado de servidor |
| `zustand` | ^4 | Estado global leve |
| `axios` | ^1 | HTTP client com interceptors |
| `react-router-dom` | ^6 | Roteamento SPA |
| `react-hook-form` + `zod` | latest | Formulários e validação |
| `recharts` | ^2 | Gráficos (BarChart, PieChart) |
| `date-fns` | ^3 | Manipulação de datas |
| `msw` | ^2 | Mock de API em testes |

## Critérios de Sucesso

- `npm run dev` inicia o servidor de desenvolvimento em `localhost:5173` sem erros
- `npm run build` gera bundle de produção sem erros TypeScript ou warnings
- `npm run test` executa o setup do Vitest com sucesso (pode ter 0 testes)
- Estrutura de pastas segue fielmente a techspec (`src/app/`, `src/shared/`, `src/features/`)
- Tailwind CSS renderiza estilos com tokens customizados corretamente
- Path alias `@/` resolve corretamente no editor e no build
- `apiClient` faz request para a baseURL configurada
- Dockerfile constrói imagem funcional
