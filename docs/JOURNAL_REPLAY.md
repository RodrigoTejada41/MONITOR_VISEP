# Journal local e replay

Escopo: preservar entradas do simulador para diagnostico e recuperacao. Nao define framing, heartbeat ou ACK SG3. Implementacao em src/Receiver; testes em tests/JournalTests.cs e tests/IntegrationTests.ps1.

## Contrato

O receptor conserva os bytes de entrada em inbox/journal antes de interpretar a mensagem e atualizar Store. Uma falha no journal impede a entrega ao Store; a entrada permanece pendente. Mensagens invalidas tambem ficam preservadas para investigacao. O journal nao substitui backup externo nem valida semantica de eventos reais.

O reprocessamento usa o identificador da simulacao para evitar nova ocorrencia quando o Store ja recebeu a mensagem. Payload diferente com mesmo identificador deve permanecer no journal; isso nao significa que o Store aceite alterar uma ocorrencia existente. O contrato de identificacao de retransmissoes do SG3 ainda precisa ser comprovado.

Payloads: arquivos .raw nomeados pelo SHA-256 do conteudo, com publicacao por temporario, Flush(true) e rename. Limite de 1 MiB por entrada antes de parse; entradas maiores permanecem na origem. Bytes identicos compartilham arquivo.

Capturas: inbox/captures/<id>.xml registra cada tentativa de processamento com id proprio, source (nome do arquivo), capturedUtc (relogio local em UTC), payloadSha256 e state. O envelope pending deve ser gravado antes do parser/Store. delivered indica persistencia aceita, inclusive deduplicacao; invalid indica rejeicao pelo parser. Falha no Store conserva pending. A captura e distinta da ocorrencia: duas entradas identicas possuem capturas diferentes e uma unica ocorrencia.

capturedUtc nao e horario de origem do equipamento nem timestamp autenticado. Uma nova tentativa do mesmo arquivo pendente tambem gera captura; nao se deve interpretar a contagem como numero de transmissoes fisicas. Replay nao inventa capturas para payloads legados sem envelope.

Falha ao atualizar estado depois do Store pode deixar pending mesmo com ocorrencia persistida; replay reconcilia apos nova entrega idempotente. Se o parser falha durante replay, capturas pending existentes permanecem pending e a falha e reportada. Portanto pending significa entrega ainda nao confirmada pelo envelope, nao prova ausencia no Store. A classificacao de eventos reais (teste/restauro/falha) continua pendente do protocolo.

Replay faz verificacao previa: identificador repetido com bytes diferentes e considerado conflito e suas entradas sao rejeitadas, sem escolher vencedor pela ordem dos arquivos. Outras mensagens validas continuam. Um lock de arquivo exclui processamento/replay simultaneo no mesmo inbox; nao protege contra alteracao manual por quem possui acesso ao disco.

Capturas pendentes sao indexadas por hash uma vez por replay. Envelope corrompido e reportado separadamente como falha de metadata, sem impedir a recuperacao de outros payloads/capturas validos. Falhas de payload ou metadata produzem codigo de saida 1; o resumo distingue as duas contagens.

## Operacao

Pare o receptor antes do replay e utilize uma copia de teste ao recuperar dados:

```powershell
.\build\Visep.Receiver.exe --replay "$PWD\data\data.xml"
```

O journal deve acompanhar o diretorio inbox do mesmo estado. Verifique o codigo de saida: zero significa que o replay terminou sem entradas rejeitadas; falhas exigem inspecao antes de considerar a recuperacao concluida. Mensagens invalidas preservadas podem causar falha no replay, mesmo que as validas tenham sido aplicadas.

Supervisao somente leitura:

```powershell
.\build\Visep.Receiver.exe --health "$PWD\data\visep.xml" 1024 5
```

Os argumentos opcionais sao espaco livre minimo em MiB e idade maxima de pending em minutos. A saida informa bytes livres, inbox, estados das capturas, corrupcao, payload ausente, payload sem envelope e horario futuro. Codigo 1 indica espaco abaixo do limite, pending antigo, corrupcao, captura sem payload, payload sem envelope ou divergencia de relogio. Inbox recente e invalid sao informativos.

Retencao exige receptor parado e backup externo criado por Backup.ps1. Primeiro execute a previa; aplique somente depois de conferir as contagens:

```powershell
.\build\Visep.Receiver.exe --retention "$PWD\data\visep.xml" "D:\Backup\VISEP-20260917" "2026-06-01T00:00:00Z"
.\build\Visep.Receiver.exe --retention "$PWD\data\visep.xml" "D:\Backup\VISEP-20260917" "2026-06-01T00:00:00Z" --apply
```

Uma familia por hash so e candidata quando todas as capturas sao delivered/invalid e anteriores ao corte UTC. Pending, familia recente, raw sem captura e captura sem raw permanecem. Antes de excluir, cada candidato deve constar no manifesto, existir no backup e ter o mesmo SHA-256 no backup e no estado atual. Metadata ilegivel ou lock ocupado bloqueia toda a operacao. Envelopes sao removidos antes do raw; falha intermediaria conserva o raw e o backup verificado.

Backup.ps1 ja copia inbox recursivamente e Restore.ps1 verifica os hashes do manifesto. A suite de integracao exige que journal seja copiado/restaurado e que o replay da copia nao duplique ocorrencias. ACL do diretorio de dados tambem deve proteger o journal, pois ele conserva os valores originais.

## Limites

- Nao ha retencao automatica: `--health` deve ser supervisionado externamente e `--retention --apply` e sempre explicito.
- processed e quarantine nao entram na primeira politica de retencao.
- Hash detecta alteracao acidental; nao e assinatura nem auditoria inviolavel contra administrador local.
- Captura local do simulador nao comprova ausencia de perda de sinal SG3.
- Recuperar evento nao recupera acoes de operador ausentes; o backup do estado XML continua necessario.
- Cobertura percentual ainda nao medida. Evidencias RED/GREEN e contagens finais devem constar na retomada.
