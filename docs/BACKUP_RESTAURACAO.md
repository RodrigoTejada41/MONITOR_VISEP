# Backup e restauração

Procedimento demonstrador. Scripts Backup.ps1/Restore.ps1 validados em 2026-09-16 por tests/IntegrationTests.ps1 com dados sintéticos:
1. Interromper serviço e fechar todas as instâncias da interface para cópia offline consistente.
2. Identificar diretório de dados efetivo; copiar arquivo de estado e configuração para destino separado com ACL restrita.
3. Registrar SHA-256, instante, versão do programa e tamanho, sem conteúdo pessoal.
4. Testar restauração em diretório isolado. Nunca sobrescrever original para testar.
5. Abrir cópia com versão compatível; conferir clientes, ocorrências, ações e auditoria.
6. Para retorno operacional, preservar estado atual antes de substituição e verificar recepção após reinício.

Backup automático de substituição não substitui cópia em outro disco. RPO/RTO, retenção, criptografia, teste de falha de disco e backup de banco servidor: pendentes. Backup BYKOM original preservado; cópia isolada conferida conforme MAPA_BANCO_LEGADO.md.

Journal de simulacao: inbox/journal acompanha a copia recursiva de inbox e entra no manifesto SHA-256. Nao copiar apenas data.xml ao transferir uma instalacao com journal. Apos restaurar, o comando --replay permite reprocessar entradas preservadas; nao recupera acoes de operador posteriores ao backup. Procedimento e limites em JOURNAL_REPLAY.md.

Retencao: use somente backup externo gerado por Backup.ps1. O comando `--retention` confere manifesto, copia e hash de cada candidato antes da previa ou aplicacao. Manifestos UTF-8 com BOM do Windows PowerShell 5 sao aceitos; tamanho maximo 16 MiB. O backup permanece intacto depois da limpeza e pode ser restaurado em destino novo. Nao use uma pasta dentro/acima do diretorio de dados como backup de retencao.

A restauração rejeita hash divergente, raiz/versão incompatível e seções principais ausentes ou duplicadas antes de criar o destino. O destino deve ser novo. A ACL restaurada permite somente usuário atual, administradores e SYSTEM; reaplicar explicitamente a permissão do serviço LocalService e do operador antes de operação pelo serviço.
