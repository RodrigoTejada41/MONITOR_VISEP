# Mapeamento de migração

Estado: EM IMPLEMENTACAO; importador isolado de teste existente e testes sinteticos aprovados. Migracao de producao/reconciliacao nao homologadas. Evidencias em `MAPA_BANCO_LEGADO.md` e mapeamento implementado em `TESTE_BYKOM.md`.

| Entidade destino própria | Tabela/coluna origem | Transformação | Estado |
|---|---|---|---|
| Cliente/local | Candidata: `abmacodigos` (`ORDER_ID`, `ID_RC`, `ID_CL`, `NOMBRE`) | Confirmar identificação e separação cliente/local | Pendente de validação semântica |
| Conta/contato | Candidatas: `abmacodigos` e `abrltelefonos` (`ORDER_ID`, `ORDER_RL`, `CODIGO_ID`) | Confirmar vínculos, significado e cardinalidade | Pendente de validação semântica |
| Equipamento/zona/partição | Candidatas: `abrlsensores` (`SENSOR_ID`), `abrlzonas` (`N_ZONA`) e `abmacodigos` (`PARTICION`) | Distinguir sensor, equipamento, zona e partição | Pendente de validação semântica |
| Evento/ocorrência | Candidatas: `evmahistorico` (`EVENTO`, `FECHAHORA`) e `evrlhistorico` (`ORDER_RL`, `TIPO`) | Validar códigos, horários e vínculo do histórico; sem presumir equivalência | Pendente de validação semântica |

As candidatas acima orientaram o importador em src/Migration/import_legacy.py. O mapeamento de teste usa ORDER_ID para conta BYKOM-ORDER_ID, contatos por ORDER_RL/CODIGO_ID e amostra de evmahistorico. A existencia de ORDER_RL nao comprova sozinha semantica/cardinalidade; o mapeamento de producao continua pendente.

Pendencias reais: validar chaves/relacoes, exclusao/inativacao, encoding, fuso, campos obrigatorios, duplicidades e codigos; revisar rotinas antes de restaurar; reconciliar contagens e inconsistencias reais. Transformacao inicial ja implementada. Importador recusa destino existente; nao implementa merge incremental/idempotencia de importacao em banco servidor. SHA-256 identifica entrada, nao prova correcao semantica.

Cada importação terá ID da execução, hash da cópia, chave de origem e destino, versão do mapeamento, contagens, inconsistências e relatório. Usar chave única de origem para reexecução idempotente. Não descartar linhas inválidas silenciosamente. Senhas legadas não serão promovidas a credenciais válidas sem análise; provisionar/redefinir acessos.

Restauração somente em instância isolada após revisão do SQL. Nenhuma conexão ao banco de produção.
