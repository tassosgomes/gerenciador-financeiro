# Relatório de Teste Exploratório - Funcionalidade

## Informações do teste
- **Data:** 2026-02-16
- **Ambiente:** Local (`http://localhost:8080`)
- **Perfil utilizado:** `admin@gestorfinanceiro.local` (credencial do `.env`)
- **Tipo de teste:** Exploratório com foco em funcionalidade
- **Escopo navegado:** Login, Dashboard, Transações, Categorias, Contas, Admin

## Resumo executivo
Foram identificados **4 problemas funcionais** relevantes:
1. Classificação de categorias de **Receita** sendo exibida/salva como **Despesa**.
2. Fluxo administrativo permite **inativar o próprio usuário logado**.
3. Dashboard apresenta inconsistência visual/funcional no gráfico (legenda duplicada como "Despesa").
4. Tela de transações não disponibiliza ações de **buscar/aplicar** e **limpar** filtros.

---

## Bug 1 — Categoria de Receita salva/exibida como Despesa
- **Severidade:** Alta
- **Módulo:** Categorias
- **URL:** `/categories`

### Passos para reproduzir
1. Acessar `Categorias`.
2. Clicar em `Nova Categoria`.
3. Informar nome: `Teste Receita QA`.
4. No campo `Tipo`, selecionar `Receita`.
5. Clicar em `Criar`.
6. Visualizar aba `Receitas` (ou listagem de todas).

### Resultado esperado
A categoria criada com tipo `Receita` deve ser exibida como `Receita`.

### Resultado obtido
A categoria aparece com tipo `Despesa`.

### Evidência observada
- Registro recém-criado `Teste Receita QA` exibido com coluna Tipo = `Despesa`.
- Também ocorre com categorias de receita de sistema (ex.: `Salário`, `Freelance`) na aba `Receitas`.

---

## Bug 2 — Sistema permite inativar o próprio usuário logado
- **Severidade:** Alta
- **Módulo:** Admin > Usuários
- **URL:** `/admin`

### Passos para reproduzir
1. Logar com usuário administrador.
2. Acessar `Admin` > aba `Usuários`.
3. Na linha do próprio usuário logado (`Administrador`), clicar no botão `block`.
4. Confirmar em `Inativar`.

### Resultado esperado
O sistema deve bloquear a ação (ou exigir política de proteção) para impedir auto-inativação do usuário da sessão atual.

### Resultado obtido
A inativação é concluída com sucesso e o usuário fica `Inativo`.

### Evidência observada
- Toast: `Usuário inativado com sucesso!`
- Status da linha alterado para `Inativo`.
- Após logout, tentativa de login retorna `Credenciais inválidas` para o usuário inativado.

---

## Bug 3 — Legenda do gráfico no Dashboard incorreta/duplicada
- **Severidade:** Média
- **Módulo:** Dashboard
- **URL:** `/dashboard`

### Passos para reproduzir
1. Efetuar login.
2. Acessar o bloco `Receitas vs Despesas (6 meses)`.
3. Observar legenda do gráfico.

### Resultado esperado
A legenda deve conter duas séries distintas (`Receitas` e `Despesas`).

### Resultado obtido
A legenda exibe `Despesa` duplicado para as duas séries.

### Evidência observada
- Na UI do gráfico, ambos itens de legenda aparecem com o mesmo rótulo `Despesa`.

---

## Bug 4 — Ausência de botões para buscar e limpar filtros em Transações
- **Severidade:** Média
- **Módulo:** Transações
- **URL:** `/transactions`

### Passos para reproduzir
1. Acessar `Transações`.
2. Preencher qualquer combinação de filtros (Conta, Categoria, Tipo, Status, Data De, Data Até).
3. Observar ações disponíveis na área de filtros.

### Resultado esperado
Deve existir, no mínimo:
- ação para **buscar/aplicar** filtros explicitamente;
- ação para **limpar/resetar** filtros rapidamente.

### Resultado obtido
Não há botão de `Buscar` e não há botão de `Limpar` na área de filtros.

### Evidência observada
- A seção de filtros apresenta apenas campos de seleção/data, sem ações explícitas para aplicar ou limpar.

---

## Observações adicionais
- Fluxo de validação de formulário em `Nova Transação` respondeu corretamente para campos obrigatórios (valor, descrição, categoria e conta).
- Após a auto-inativação do usuário seed, o acesso com a credencial do `.env` deixou de funcionar (comportamento esperado para usuário inativo, mas causado por falha de regra administrativa citada no Bug 2).

## Recomendação de priorização
1. **Bug 1** (integridade de dados de categoria) — corrigir imediatamente.
2. **Bug 2** (governança/segurança administrativa) — corrigir imediatamente.
3. **Bug 3** (confiabilidade da visualização no dashboard) — corrigir em seguida.
4. **Bug 4** (usabilidade/eficiência de filtragem em transações) — corrigir em seguida.
