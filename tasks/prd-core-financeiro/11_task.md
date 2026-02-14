---
status: pending
parallelizable: true
blocked_by: ["7.0"]
---

<task_context>
<domain>infra/persistência</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>low</complexity>
<dependencies>database</dependencies>
<unblocks></unblocks>
</task_context>

# Tarefa 11.0: Seed de Categorias Padrão

## Visão Geral

Criar o mecanismo de seed para categorias padrão conforme PRD F2 req 11. As categorias iniciais permitem ao usuário começar a classificar transações imediatamente, sem necessidade de cadastro manual.

O seed pode ser implementado via migration (data seed do EF Core) ou via `IDataSeeder` invocado na inicialização da aplicação.

## Requisitos

- PRD F2 req 11: conjunto de categorias padrão para seed inicial (Alimentação, Transporte, Salário, Moradia, Lazer, Saúde, Educação)
- PRD F9 req 40: categorias devem ter `CreatedBy = "system"` e `CreatedAt` em UTC

## Subtarefas

- [ ] 11.1 Definir a lista completa de categorias padrão (Despesa + Receita) conforme techspec
- [ ] 11.2 Implementar o seed via EF Core data seed (`HasData` na configuration) ou migration dedicada
- [ ] 11.3 Gerar migration de seed (se usando migration): `dotnet ef migrations add SeedDefaultCategories`
- [ ] 11.4 Testar que o seed cria as categorias corretas (pode ser validado no teste de integração 10.14)

## Sequenciamento

- Bloqueado por: 7.0 (DbContext e configurations devem existir)
- Desbloqueia: Nenhum (mas seed é validado em 10.14)
- Paralelizável: Sim — pode ser executada em paralelo com 8.0

## Detalhes de Implementação

### Lista de categorias padrão (conforme techspec)

| Nome | Tipo |
|------|------|
| Alimentação | Despesa |
| Transporte | Despesa |
| Moradia | Despesa |
| Lazer | Despesa |
| Saúde | Despesa |
| Educação | Despesa |
| Vestuário | Despesa |
| Outros | Despesa |
| Salário | Receita |
| Freelance | Receita |
| Investimento | Receita |
| Outros | Receita |

### Opção A — EF Core Data Seed (recomendada)

Na `CategoryConfiguration` (Fluent API):

```csharp
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // ... existing configuration ...

        builder.HasData(
            CreateSeedCategory("guid-fixo-1", "Alimentação",  CategoryType.Despesa),
            CreateSeedCategory("guid-fixo-2", "Transporte",   CategoryType.Despesa),
            CreateSeedCategory("guid-fixo-3", "Moradia",      CategoryType.Despesa),
            CreateSeedCategory("guid-fixo-4", "Lazer",        CategoryType.Despesa),
            CreateSeedCategory("guid-fixo-5", "Saúde",        CategoryType.Despesa),
            CreateSeedCategory("guid-fixo-6", "Educação",     CategoryType.Despesa),
            CreateSeedCategory("guid-fixo-7", "Vestuário",    CategoryType.Despesa),
            CreateSeedCategory("guid-fixo-8", "Outros",       CategoryType.Despesa),
            CreateSeedCategory("guid-fixo-9", "Salário",      CategoryType.Receita),
            CreateSeedCategory("guid-fixo-10", "Freelance",   CategoryType.Receita),
            CreateSeedCategory("guid-fixo-11", "Investimento",CategoryType.Receita),
            CreateSeedCategory("guid-fixo-12", "Outros",      CategoryType.Receita)
        );
    }

    // HasData requer anonymous objects ou Guid fixos (não factory methods)
    private static object CreateSeedCategory(string guidSuffix, string name, CategoryType type)
    {
        return new
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{guidSuffix.PadLeft(12, '0')}"),
            Name = name,
            Type = type,
            IsActive = true,
            CreatedBy = "system",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }
}
```

### Observações

- **GUIDs fixos**: o EF Core data seed requer IDs conhecidos e estáveis entre migrations. Usar GUIDs determinísticos
- **CreatedBy**: `"system"` para identificar dados de seed
- **CreatedAt**: data fixa (ex: `2025-01-01 UTC`) para estabilidade do seed
- **Não usar factory methods** no `HasData` — o EF Core requer anonymous objects ou valores explícitos
- Se preferir `IDataSeeder` em vez de `HasData`, criar interface + implementação e invocar na inicialização

### Opção B — IDataSeeder (alternativa)

```csharp
public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}

public class CategorySeeder : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // Verificar se categorias já existem
        // Se não, criar via Category.Create("nome", tipo, "system")
        // Salvar via repository
    }
}
```

## Critérios de Sucesso

- 12 categorias padrão criadas no banco após migration/seed
- 8 categorias do tipo `Despesa` e 4 do tipo `Receita`
- Todas as categorias com `CreatedBy = "system"`
- Todas as categorias com `IsActive = true`
- Migration gerada sem erros
- Seed é idempotente — executar múltiplas vezes não cria duplicatas
- Validado pelo teste de integração (10.14)
