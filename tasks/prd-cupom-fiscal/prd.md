# PRD — Importação de Cupom Fiscal (NFC-e)

## Visão Geral

O **GestorFinanceiro** atualmente permite registrar transações financeiras manualmente, mas não oferece uma forma de capturar a granularidade dos itens comprados em cada compra. O recurso de **Importação de Cupom Fiscal** resolve esse problema permitindo que o usuário importe dados de uma NFC-e (Nota Fiscal de Consumidor Eletrônica) diretamente da SEFAZ, criando automaticamente uma transação de despesa e armazenando cada item individual do cupom para análise detalhada e futura categorização.

Este recurso é valioso para usuários que desejam:
- Evitar digitação manual de transações de compras
- Ter visibilidade granular de cada item comprado (descrição, quantidade, valor unitário, valor total)
- Construir uma base de dados de itens de consumo para futura análise e categorização

## Objetivos

1. **Automatizar o registro de compras**: Reduzir esforço manual ao importar transações diretamente do cupom fiscal eletrônico
2. **Granularidade de dados**: Armazenar itens individuais de cada compra vinculados à transação, permitindo análises detalhadas futuras
3. **Base para categorização futura**: Criar a estrutura de dados necessária para que um PRD futuro implemente categorização inteligente de itens de consumo
4. **Precisão dos dados**: Garantir que valores importados são fidedignos ao cupom fiscal oficial

### Métricas de Sucesso
- Usuário consegue importar um cupom fiscal em menos de 1 minuto (do input da chave/URL até a transação criada)
- 100% dos itens do cupom são extraídos e armazenados corretamente
- Zero divergência entre o valor total da transação criada e o total do cupom fiscal

## Histórias de Usuário

### Primárias

1. **Como** usuário, **quero** informar a chave de acesso ou URL de uma NFC-e **para que** o sistema busque automaticamente os dados do cupom fiscal e eu não precise digitar manualmente
2. **Como** usuário, **quero** visualizar um preview dos itens e valor total do cupom **para que** eu possa conferir os dados antes de confirmar a importação
3. **Como** usuário, **quero** escolher a conta, categoria e descrição da transação gerada **para que** o registro fique organizado conforme minha estrutura financeira
4. **Como** usuário, **quero** ver os itens individuais de um cupom importado no detalhe da transação **para que** eu tenha visibilidade do que comprei em cada compra

### Secundárias

5. **Como** usuário, **quero** ser informado se um cupom já foi importado anteriormente **para que** eu não crie transações duplicadas
6. **Como** usuário, **quero** ver o nome do estabelecimento e data do cupom **para que** eu saiba de onde veio a compra

## Funcionalidades Principais

### F1 — Consulta de NFC-e

O sistema deve permitir que o usuário informe dados de uma NFC-e para consulta automática.

**Requisitos Funcionais:**

- **RF01**: O sistema deve aceitar como entrada a **chave de acesso** (44 dígitos) de uma NFC-e
- **RF02**: O sistema deve aceitar como entrada a **URL completa** da NFC-e (link do portal da SEFAZ)
- **RF03**: O sistema deve consultar os dados da NFC-e diretamente no portal da SEFAZ. Na primeira versão, apenas o estado da **Paraíba (PB)** será suportado
- **RF04**: O sistema deve extrair do cupom fiscal: nome do estabelecimento (razão social), CNPJ, data/hora da emissão, lista de itens, descontos (se houver) e valor total pago
- **RF05**: Para cada item do cupom, o sistema deve extrair: descrição do produto, código do produto (se disponível), quantidade, unidade de medida, valor unitário e valor total do item
- **RF06**: O sistema deve validar a chave de acesso (formato de 44 dígitos numéricos) antes de consultar a SEFAZ
- **RF07**: Em caso de **SEFAZ indisponível** (timeout, erro de conexão), o sistema deve exibir mensagem informando que a SEFAZ está fora do ar e orientar o usuário a tentar novamente mais tarde
- **RF07a**: Em caso de **NFC-e não encontrada** (nota expirada, inválida ou não disponível), o sistema deve informar que a nota não está disponível na SEFAZ
- **RF07b**: Em caso de **chave de acesso inválida**, o sistema deve informar o formato correto esperado

### F2 — Preview e Confirmação

Após a consulta bem-sucedida, o sistema apresenta os dados para revisão antes da criação da transação.

**Requisitos Funcionais:**

- **RF08**: O sistema deve exibir um preview com: nome do estabelecimento, CNPJ, data da compra, lista de itens (descrição, qtd, valor unitário, valor total), descontos aplicados (se houver) e valor total pago
- **RF09**: O sistema deve exigir que o usuário selecione uma **conta** (entre suas contas ativas do tipo Corrente, Cartão, Carteira ou Investimento) para vincular a transação
- **RF10**: O sistema deve exigir que o usuário selecione uma **categoria de despesa** para a transação
- **RF11**: O sistema deve permitir que o usuário informe uma **descrição** para a transação (com sugestão padrão: nome do estabelecimento)
- **RF12**: O sistema deve permitir que o usuário informe a **data da transação** (com default para a data de emissão do cupom)
- **RF13**: O sistema deve verificar se a chave de acesso já foi importada anteriormente e alertar o usuário em caso de duplicidade, impedindo a importação duplicada

### F3 — Criação da Transação e Armazenamento de Itens

Ao confirmar, o sistema cria a transação e armazena os itens individualmente.

**Requisitos Funcionais:**

- **RF14**: O sistema deve criar uma **transação de débito** com status **Paga** na conta selecionada, com o **valor efetivamente pago** (total líquido após descontos). Se houver descontos, incluir observação automática na descrição (ex: "Desconto de R$ X,XX aplicado. Valor original: R$ Y,YY")
- **RF15**: A transação criada deve armazenar a **chave de acesso da NFC-e** para rastreabilidade e detecção de duplicidade
- **RF16**: O sistema deve armazenar em uma entidade/tabela separada cada **item individual** do cupom fiscal, vinculado à transação criada
- **RF17**: Cada item armazenado deve conter: descrição do produto, código do produto (se disponível), quantidade, unidade de medida, valor unitário, valor total do item
- **RF18**: O sistema deve armazenar os **dados do estabelecimento** (razão social, CNPJ) em uma **entidade separada** vinculada à transação. Modelagem detalhada na Tech Spec
- **RF19**: O saldo da conta deve ser atualizado conforme a transação criada (seguindo as regras existentes do sistema para transações pagas)
- **RF20**: A operação de criação (transação + estabelecimento + itens) deve ser atômica — se falhar qualquer parte, nada é criado
- **RF21**: Ao **cancelar** uma transação que possui itens de cupom fiscal, os itens e dados do estabelecimento vinculados devem ser **excluídos em cascade**

### F4 — Visualização de Itens no Detalhe da Transação

O detalhe de uma transação importada via cupom fiscal exibe os itens associados.

**Requisitos Funcionais:**

- **RF22**: Na tela de detalhe de uma transação que possui itens de cupom fiscal, o sistema deve exibir uma seção/aba "Itens do Cupom" com a lista de itens (descrição, quantidade, valor unitário, valor total)
- **RF23**: O sistema deve exibir os dados do estabelecimento (razão social, CNPJ) no detalhe da transação
- **RF24**: O sistema deve indicar visualmente que a transação foi importada via cupom fiscal (badge, ícone ou indicador)
- **RF25**: O sistema deve exibir a chave de acesso da NFC-e no detalhe para referência

## Experiência do Usuário

### Fluxo Principal de Importação

1. Usuário acessa a funcionalidade de importação de cupom fiscal (via botão na tela de transações ou menu dedicado)
2. Usuário informa a chave de acesso (44 dígitos) ou cola a URL da NFC-e
3. Sistema consulta a SEFAZ e exibe loading/feedback durante a consulta
4. Sistema exibe preview com dados do cupom (estabelecimento, itens, total)
5. Usuário seleciona conta, categoria, descrição e data
6. Usuário confirma a importação
7. Sistema cria a transação e armazena os itens
8. Usuário é redirecionado para o detalhe da transação criada (ou recebe feedback de sucesso)

### Considerações de UI/UX

- O campo de entrada deve aceitar tanto chave de acesso quanto URL, detectando automaticamente o formato
- O preview deve ser claro e escanável, mostrando os itens em formato de tabela
- Campos de conta e categoria devem usar os mesmos componentes de seleção já existentes no sistema (consistência)
- A descrição deve ter sugestão automática preenchida com o nome do estabelecimento, mas editável
- Feedback claro em caso de erros (SEFAZ indisponível, cupom não encontrado, duplicidade)
- Indicador de loading durante a consulta na SEFAZ (pode levar alguns segundos)

### Acessibilidade

- Todos os campos de formulário devem ter labels acessíveis
- A tabela de preview dos itens deve ser semanticamente correta (`<table>`)
- Mensagens de erro e sucesso devem ser anunciadas por leitores de tela
- Navegação por teclado completa no fluxo de importação

## Restrições Técnicas de Alto Nível

- **Consulta SEFAZ**: O backend deve fazer a consulta à SEFAZ (não o frontend), evitando problemas de CORS e exposição de detalhes de scraping
- **Cobertura estadual**: Primeira versão suporta apenas **Paraíba (PB)**. A arquitetura deve ser extensível para adicionar outros estados futuramente
- **Disponibilidade da SEFAZ**: Os portais podem ficar indisponíveis temporariamente. O sistema deve tratar timeouts e erros de forma graciosa, exibindo mensagem amigável ao usuário
- **Novas entidades no domínio**: Serão necessárias novas entidades: `ReceiptItem` (itens individuais) e `Establishment` (dados do estabelecimento), com tabelas correspondentes no banco
- **Vínculo com transação**: `ReceiptItem` e `Establishment` devem ter foreign key para `Transaction`
- **Cascade delete**: Ao cancelar/excluir a transação, itens e dados do estabelecimento devem ser removidos em cascade
- **Chave de acesso única**: A chave de acesso da NFC-e deve ter constraint de unicidade para evitar importação duplicada
- **Performance**: A consulta à SEFAZ pode ter latência variável (2-10 segundos); o frontend deve tratar isso com feedback adequado
- **Dados sensíveis**: CNPJ e razão social do estabelecimento são dados públicos da NFC-e, sem restrições de privacidade adicionais

## Não-Objetivos (Fora de Escopo)

- **Categorização de itens**: Classificar itens individualmente (ex: "Arroz" → "Alimentos") será tratado em PRD futuro. Este PRD foca apenas no armazenamento
- **Entrada manual de itens**: Não haverá funcionalidade para digitar itens manualmente. A entrada é exclusivamente via importação NFC-e
- **OCR / foto do cupom**: Não haverá importação por foto ou imagem do cupom físico
- **Importação em lote**: Importar múltiplos cupons de uma vez não está no escopo
- **Edição de itens importados**: Não será possível editar itens individuais após a importação
- **Relatórios por item**: Dashboards ou relatórios baseados nos itens individuais ficam para PRD futuro
- **Suporte a NF-e (Nota Fiscal Eletrônica)**: Apenas NFC-e (consumidor) é suportada, não NF-e (modelo 55)
- **Integração com apps de cupom**: Não haverá integração com aplicativos como Nota Fiscal Paulista, Nota Paraná, etc.

## Decisões

1. **Cobertura estadual inicial**: O sistema é de uso pessoal (single-user). A primeira versão suportará apenas o estado da **Paraíba (PB)**. Suporte a outros estados pode ser adicionado incrementalmente no futuro
2. **SEFAZ indisponível**: Quando a SEFAZ estiver fora do ar ou com timeout, o sistema deve exibir mensagem clara informando que a SEFAZ está indisponível e orientando o usuário a tentar novamente mais tarde
3. **NFC-e não encontrada na SEFAZ**: Quando a NFC-e não for encontrada (expirada, inválida ou não disponível), o sistema deve informar que a nota não está disponível na SEFAZ
4. **Cupons com desconto**: A transação deve ser criada com o **valor efetivamente pago** (total líquido). Se houver descontos, incluir uma observação/descrição automática explicando o desconto (ex: "Desconto de R$ X,XX aplicado. Valor original: R$ Y,YY")
5. **Cancelamento cascade**: Ao cancelar uma transação que possui itens de cupom fiscal, os itens vinculados devem ser **excluídos em cascade**
6. **Dados do estabelecimento**: Serão armazenados em **entidade separada** (ex: `Establishment`). Detalhes de modelagem serão definidos na Tech Spec

## Questões em Aberto

- Nenhuma questão pendente. Todas as decisões foram tomadas acima
