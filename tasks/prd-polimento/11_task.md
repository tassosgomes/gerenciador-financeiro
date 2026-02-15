---
status: pending # Opcoes: pending, in-progress, completed, excluded
parallelizable: false # Se pode executar em paralelo
blocked_by: ["10.0"] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>docs/release</domain>
<type>documentation</type>
<scope>configuration</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>""</unblocks>
</task_context>

# Tarefa 11.0: Docs/Release: README Quick Start + CHANGELOG + LICENSE + tag v1.0.0

## Visão Geral

Atualizar a documentação mínima de self-hosting e preparar artefatos de release v1.0.0.

## Requisitos

- Atualizar README com:
  - descrição do projeto
  - requisitos (Docker/Compose)
  - Quick Start (clonar, configurar `.env`, subir)
  - primeiro acesso (login admin) e orientação para troca de senha
  - configuração (tabela de env vars)
  - backup/restore (instruções)
  - screenshots (se já existirem no repo; caso contrário, indicar placeholder/como gerar)
- Adicionar `CHANGELOG.md` cobrindo funcionalidades do MVP.
- Definir licença (MIT sugerida no PRD) via `LICENSE`.
- Instruir processo de tag `v1.0.0` (passo manual do maintainer).

## Subtarefas

- [ ] 11.1 Atualizar README na raiz (Quick Start + Config + Backup/Restore)
- [ ] 11.2 Criar `CHANGELOG.md` com resumo por fase (MVP)
- [ ] 11.3 Criar `LICENSE` (MIT) conforme decisão do produto
- [ ] 11.4 Documentar tags/versões de imagens (ex.: `gestorfinanceiro-api:1.0.0`, `gestorfinanceiro-web:1.0.0`)

## Sequenciamento

- Bloqueado por: 10.0
- Desbloqueia: Nenhum
- Paralelizável: Não (depende de compose final para comandos consistentes)

## Detalhes de Implementação

Referências:
- PRD F4 (requisitos 23–27)
- PRD F5 (requisitos 28–31)
- Spec: "Documentação" e "Release v1.0.0 (processo)"

## Critérios de Sucesso

- Um usuário consegue instalar e acessar o sistema apenas seguindo o README.
- Variáveis do `.env.example` batem com o compose.
- Artefatos de release (CHANGELOG/LICENSE) presentes.
