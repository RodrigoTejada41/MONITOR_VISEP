# Mapeamento de migração

Estado: RASCUNHO; backup disponível e estrutura inventariada, sem restauração ou importação validada. Evidências e limites em `MAPA_BANCO_LEGADO.md`.

| Entidade destino própria | Tabela/coluna origem | Transformação | Estado |
|---|---|---|---|
| Cliente/local | Candidata: `abmacodigos` (`ORDER_ID`, `ID_RC`, `ID_CL`, `NOMBRE`) | Confirmar identificação e separação cliente/local | Pendente de validação semântica |
| Conta/contato | Candidatas: `abmacodigos` e `abrltelefonos` (`ORDER_ID`, `ORDER_RL`, `CODIGO_ID`) | Confirmar vínculos, significado e cardinalidade | Pendente de validação semântica |
| Equipamento/zona/partição | Candidatas: `abrlsensores` (`SENSOR_ID`), `abrlzonas` (`N_ZONA`) e `abmacodigos` (`PARTICION`) | Distinguir sensor, equipamento, zona e partição | Pendente de validação semântica |
| Evento/ocorrência | Candidatas: `evmahistorico` (`EVENTO`, `FECHAHORA`) e `evrlhistorico` (`ORDER_RL`, `TIPO`) | Validar códigos, horários e vínculo do histórico; sem presumir equivalência | Pendente de validação semântica |

Os nomes acima existem no inventário estrutural; são hipóteses de origem, não mapeamentos aprovados. Não foi feita análise de valores de registros. A existência de `ORDER_RL` não comprova a entidade referenciada nem a cardinalidade.

Pendências reais: confirmar chaves e relações, regras de exclusão/inativação, encoding, fuso horário, campos obrigatórios, duplicidades e códigos de evento; revisar rotinas antes de restaurar; implementar transformação e testar idempotência, reconciliação de contagens e tratamento de inconsistências em ambiente isolado. Usar o SHA-256 do SQL verificado no mapa como identificação do arquivo de entrada, não como prova de correção da migração.

Cada importação terá ID da execução, hash da cópia, chave de origem e destino, versão do mapeamento, contagens, inconsistências e relatório. Usar chave única de origem para reexecução idempotente. Não descartar linhas inválidas silenciosamente. Senhas legadas não serão promovidas a credenciais válidas sem análise; provisionar/redefinir acessos.

Restauração somente em instância isolada após revisão do SQL. Nenhuma conexão ao banco de produção.
