# Review da Tarefa 2.0 — PRD Polimento

## 1) Validação da definição da tarefa

### Requisitos da tarefa x implementação
- **`IsSystem` no domínio (default false):** implementado em `Category` com `bool IsSystem { get; private set; } = false`.
- **Bloqueio de alteração em categoria de sistema:** implementado em `Category.UpdateName(...)` com `SystemCategoryCannotBeChangedException`.
- **Persistência em banco (`is_system`) via migration incremental:** implementado em `AddIsSystemToCategories` + mapeamento em `CategoryConfiguration`.
- **Mapeamento para Problem Details na API:** implementado em `GlobalExceptionHandler` com retorno `400` e `application/problem+json`.

### Escopo efetivo da Task 2
Conforme contexto informado, a maior parte do requisito funcional já estava implementada na Task 1. A Task 2 concentrou-se em **cobertura de testes** e **ajustes de compatibilidade**, e isso foi confirmado:
- 4 testes unitários no `UpdateCategoryCommandHandler` (válido, sistema, inexistente, validação);
- teste HTTP de integração para `PUT` com erro quando categoria é de sistema;
- compatibilidade em backup/mapeamentos (`CategoryBackupDto` com `IsSystem`, `MappingConfig`, testes de backup).

## 2) Conformidade com PRD e Tech Spec

### PRD
- Alinhado ao requisito de categorias seed protegidas (`system: true`) e impossibilidade de alteração de categorias de sistema.

### Tech Spec
- Alinhado ao bloco **Backend — Categorias do sistema** (`IsSystem` + exceção de domínio).
- Alinhado ao bloco de **Endpoints** (`PUT /categories/{id}` retornando Problem Details para violação de regra de negócio).

## 3) Análise de regras aplicáveis (`rules/*.md`)

### Regras verificadas
- `rules/dotnet-architecture.md`
- `rules/dotnet-testing.md`
- `rules/restful.md`

### Resultado
- **Arquitetura/CQRS:** regra no domínio + propagação no handler aplicadas corretamente.
- **REST/erros:** retorno padronizado em Problem Details está conforme esperado.
- **Testes:** cobertura de cenários críticos da regra de negócio atendida.

## 4) Revisão de código e problemas encontrados

### Problema crítico identificado
1. **Migração quebrando testes HTTP em runtime**
   - Sintoma: `InvalidOperationException: There is no entity type mapped to the table 'categories'...`
   - Causa: migration `20260215000002_UpdateSystemCategories` usava `InsertData/DeleteData` e o arquivo designer correspondente estava sem modelo.
   - Correção aplicada: substituição de `InsertData/DeleteData` por SQL explícito (`INSERT ... ON CONFLICT DO NOTHING` e `DELETE ... WHERE id IN (...)`) na migration.

### Problema de robustez de teste identificado
2. **Teste HTTP dependente de seed específico**
   - Sintoma: falha ao procurar categoria por nome fixo/flag no seed do ambiente HTTP tests.
   - Causa: `ResetDatabaseAsync` dos HTTP tests semeia categorias não-sistema.
   - Correção aplicada: teste passou a criar explicitamente uma categoria `IsSystem=true` no banco de teste antes do `PUT`, tornando-o determinístico.

## 5) Build e testes

### Build
- `dotnet build GestorFinanceiro.Financeiro.sln` ✅ sem erros.

### Testes
- `dotnet test ...UnitTests.csproj --no-build` ✅ **290/290** passando.
- `dotnet test ...HttpIntegrationTests.csproj --filter UpdateCategory_WhenIsSystemCategory_ReturnsBadRequestProblemDetails` ✅ **1/1** passando.

## 6) Conclusão

### Parecer
✅ **APROVADO**

### Justificativa
- Requisitos da Tarefa 2 validados e atendidos.
- Conformidade com PRD/Tech Spec confirmada.
- Problemas críticos encontrados durante a revisão foram corrigidos e revalidados.
- Checklist da tarefa foi atualizado para concluído.

## 7) Feedback e recomendações

- Recomenda-se, em follow-up técnico, **regenerar o designer** da migration `20260215000002` para manter consistência histórica do artefato EF (apesar da correção SQL já eliminar o impacto funcional atual).
- Recomenda-se manter o padrão de testes de integração **determinísticos e independentes do seed global**.
