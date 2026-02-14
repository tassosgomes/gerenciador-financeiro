# PRD — Polimento e Release v1.0 (Fase 5)

## Visão Geral

A Fase 5 é a etapa final do MVP: preparar o GestorFinanceiro para uso real como produto self-hosted. Inclui seed de dados iniciais, responsividade mobile do frontend, empacotamento Docker definitivo e versionamento 1.0.0. Ao final desta fase, qualquer família deve conseguir subir o GestorFinanceiro com `docker compose up` e começar a usar.

**Problema**: As fases anteriores entregam funcionalidade completa, mas a experiência de instalação e primeiro uso ainda exigem configuração manual e conhecimento técnico.

**Para quem**: Famílias técnicas (ou com um membro técnico) que querem instalar o sistema em servidor próprio e começar a usar imediatamente.

**Valor**: Produto pronto para release — instalável em minutos, com dados iniciais, responsivo em qualquer dispositivo e documentação mínima para self-hosting.

## Objetivos

- Fornecer dados iniciais (seed) para que o sistema esteja utilizável imediatamente após instalação
- Tornar o frontend responsivo para uso em celulares e tablets
- Entregar Docker Compose final com configuração production-ready
- Versionar o produto como v1.0.0 com changelog completo
- Documentar instalação e primeiro uso

## Histórias de Usuário

- Como **admin da família**, quero instalar o GestorFinanceiro com um único comando Docker para não perder tempo com configuração
- Como **admin da família**, quero que o sistema já venha com categorias padrão para não precisar criar tudo do zero
- Como **membro da família**, quero usar o sistema no celular para registrar gastos no momento em que acontecem
- Como **admin da família**, quero documentação clara de como instalar, configurar e fazer primeiro acesso
- Como **membro da família**, quero que o sistema funcione bem em qualquer tamanho de tela

## Funcionalidades Principais

### F1 — Seed Inicial

**Requisitos funcionais:**

1. Na primeira execução, o sistema deve criar automaticamente um usuário admin padrão com credenciais configuráveis via variáveis de ambiente (padrão: `admin@GestorFinanceiro.local` / `mudar123`)
2. O sistema deve popular categorias padrão de Despesa: Alimentação, Transporte, Moradia, Saúde, Educação, Lazer, Vestuário, Serviços, Impostos, Outros
3. O sistema deve popular categorias padrão de Receita: Salário, Freelance, Investimentos, Outros
4. O seed só deve executar se o banco estiver vazio (idempotente)
5. As categorias padrão devem ser marcadas como `system: true` (não editáveis/removíveis)
6. Após o seed, o admin deve ser orientado a alterar a senha no primeiro login

### F2 — Responsividade Mobile

**Requisitos funcionais:**

7. Todas as telas devem ser utilizáveis em dispositivos com largura mínima de 320px
8. O menu de navegação deve colapsar em menu hambúrguer em telas pequenas
9. Tabelas devem ter scroll horizontal ou layout em cards em telas pequenas
10. Formulários devem empilhar campos verticalmente em telas pequenas
11. O dashboard deve reorganizar cards em coluna única em telas pequenas
12. Gráficos devem redimensionar proporcionalmente
13. Botões de ação devem ter área de toque mínima de 44x44px
14. Não é necessário ser PWA nem ter funcionalidade offline

### F3 — Docker Compose Final

**Requisitos funcionais:**

15. Arquivo `docker-compose.yml` na raiz do repositório com: backend (.NET), frontend (React/Nginx) e PostgreSQL
16. Variáveis de ambiente configuráveis: porta da aplicação, credenciais do banco, credenciais do admin inicial, secret do JWT
17. Volumes persistentes para dados do PostgreSQL
18. Health checks para todos os serviços
19. O backend deve aplicar migrations automaticamente na inicialização
20. O frontend deve ser servido via Nginx com configuração de proxy reverso para a API
21. Arquivo `.env.example` com todas as variáveis documentadas
22. O sistema deve iniciar com `docker compose up -d` sem passos adicionais

### F4 — Documentação de Instalação

**Requisitos funcionais:**

23. `README.md` atualizado com: descrição do projeto, requisitos (Docker), instalação, primeiro acesso, screenshots
24. Seção "Quick Start" com comandos para clonar, configurar e subir
25. Seção de configuração com tabela de variáveis de ambiente
26. Seção de backup/restore com instruções de uso
27. Licença definida (sugestão: MIT)

### F5 — Versionamento e Release

**Requisitos funcionais:**

28. Tag de versão `v1.0.0` no repositório
29. `CHANGELOG.md` com resumo de todas as funcionalidades do MVP
30. Dockerfile otimizado com multi-stage build para imagens pequenas
31. Imagens Docker tagueadas com versão (`GestorFinanceiro-api:1.0.0`, `GestorFinanceiro-web:1.0.0`)

## Experiência do Usuário

### Primeiro Uso (Onboarding)

1. Admin executa `docker compose up -d`
2. Acessa `http://localhost:8080` (ou porta configurada)
3. Faz login com credenciais padrão
4. Sistema sugere alterar a senha
5. Categorias padrão já estão disponíveis
6. Admin cria contas e começa a usar — ou cadastra membros da família primeiro

### Mobile

- Navegação por menu hambúrguer
- Dashboard com cards empilhados
- Formulários em tela cheia
- Tabelas com scroll horizontal ou modo card

### Acessibilidade

- Manter todos os requisitos de acessibilidade da Fase 3
- Touch targets adequados para mobile
- Fontes legíveis em telas pequenas (mínimo 16px para corpo de texto)

## Restrições Técnicas de Alto Nível

- **Docker**: Docker Compose v2+
- **Nginx**: para servir o frontend e fazer proxy reverso
- **PostgreSQL**: versão 15+ com volume persistente
- **Migrations**: executadas automaticamente pelo backend na inicialização
- **Imagens Docker**: multi-stage build para minimizar tamanho
- **Segurança**: credenciais padrão devem ser alteráveis, JWT secret configurável, sem portas desnecessárias expostas
- **Compatibilidade**: testado em Docker Desktop (Windows/Mac) e Docker Engine (Linux)

## Não-Objetivos (Fora de Escopo)

- Kubernetes / Helm charts
- CI/CD pipeline (pode ser futuro)
- Monitoramento e alertas (Prometheus, Grafana)
- Backup automático / agendado
- Atualização automática de versão
- App mobile nativo
- PWA com funcionalidade offline
- Modo escuro
- Internacionalização (apenas pt-BR)
- Publicação em Docker Hub (pode ser futuro)
- SSL/TLS automático (o usuário configura via reverse proxy externo se necessário)

## Questões em Aberto

1. **Credenciais padrão**: forçar alteração de senha no primeiro login ou apenas recomendar?
2. **Porta padrão**: usar 8080, 3000 ou outra porta para o serviço web?
3. **Licença**: MIT, Apache 2.0 ou GPL?
4. **Atualizações futuras**: como o usuário atualiza de v1.0 para v1.1? (sugestão: documentar `docker compose pull && docker compose up -d`)
5. **SSL**: incluir configuração opcional de SSL com Let's Encrypt no Docker Compose ou deixar para o usuário?
