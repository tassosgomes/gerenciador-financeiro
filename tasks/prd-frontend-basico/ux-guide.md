Para apoiar os desenvolvedores no frontend (React + TypeScript + Vite), preparei um guia técnico detalhado baseado nas telas geradas. Este guia foca na estrutura de componentes, estado e integração com a API da Fase 2.

1. Estrutura de Componentes (Atomic Design Simples)
Para manter o projeto leve (como sugerido no PRD), a estrutura de pastas deve seguir:

/components/ui: Componentes básicos (Botões, Inputs, Cards, Badges, Modais).
/components/layout: Sidebar, Topbar, AppShell (o container que envolve as páginas logadas).
/components/charts: Wrappers para Recharts (BarChart, PieChart).
/features: Componentes complexos por domínio (ex: TransactionForm, AccountCard, BackupActions).
2. Guia de Implementação por Tela
A. Dashboard Financeiro
Componentes Recharts:
BarChart para Receita vs Despesa (6 meses).
PieChart (Donut) para Despesas por Categoria.
Estado: Utilizar um hook useDashboardData que chama o endpoint de agregação.
Regra de Negócio: Os cards de resumo devem refletir o filtro de mês/ano selecionado no topo.
B. Histórico de Transações (Listagem)
Componente de Tabela: Criar uma tabela genérica que suporte:
Badges de Status: Verde (Pago), Amarelo (Pendente), Cinza/Riscado (Cancelado).
Formatação: Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).
Filtros: O estado dos filtros (conta, categoria, data) deve ser sincronizado com a URL (Query Params) para permitir compartilhamento de links.
C. Nova Transação (Formulário com Abas)
Lógica de Abas: Use o estado local para alternar entre os tipos (Simples, Parcelada, Recorrente, Transferência).
Campos Dinâmicos: Se for Parcelada, exibir o campo parcelas e calcular um preview das datas/valores.
Validação: Recomenda-se react-hook-form com zod para validação inline conforme o PRD.
D. Gestão de Contas
Cards de Conta: Componente que exibe saldo e tipo (ícone de banco ou cartão).
Ações: Botão de 'Switch' para Ativar/Inativar (disparar chamada PATCH na API).
E. Painel Administrativo
Gestão de Usuários: Tabela simples com ações de edição e toggle de status.
Backup:
Exportar: window.location.href = API_URL + '/backup/export'.
Importar: Componente de upload de arquivo com modal de confirmação "Atenção: Isso substituirá todos os dados".
3. Stack Técnica Recomendada
Estilização: Tailwind CSS (rápido e segue o estilo clean das telas).
Ícones: lucide-react.
Gerenciamento de Estado: Zustand para o estado global do usuário e tokens. React Query (TanStack Query) para cache de dados da API e loading states automáticos.
Datas: date-fns.
4. Checklist para o Dev
 Implementar o AuthContext para proteger as rotas.
 Criar o Axios Interceptor para injetar o token JWT no Header e tratar erro 401 (Logout automático).
 Criar componentes de Skeleton Loaders para as tabelas e dashboard.
 Implementar Toasts (ex: react-hot-toast) para feedback de sucesso/erro em todas as operações CRUD.
Deseja que eu gere a tela de Gestão de Categorias que falta para completar o guia de implementação?

<critical>Exemplos de tela estão na pasta ´tasks/prd-frontend-basico/screen-examples´ e devem ser consultadas como referencias das telas</critical>