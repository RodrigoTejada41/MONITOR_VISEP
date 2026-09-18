# Classificacao offline SG3 v1

Tabela local baseada nas descricoes do Printer Log recebido em 2026-09-18. Categorias descrevem assunto; nao sao politica de atendimento. Nenhuma categoria cria, encerra ou descarta ocorrencia. Qualificador E/R permanece literal.

| Regra | Categoria | Evidencia / limite |
|---|---|---|
| Keepalive estrutural | KeepAlive | Mensagem de supervisao reconhecida pelo parser |
| E602 | PeriodicTest | PERIODIC TEST |
| E351, R351 | TroubleTopic | TELCO 1 FAULT; estado atual nao inferido |
| E354, R354 | TroubleTopic | FAILURE TO COMMUNICATE; estado atual nao inferido |
| E608 | TroubleTopic | SYSTEM TROUBLE PRESENT |
| E402, R402 | Operational | GROUP O/C; nao define abertura/fechamento ou permissao |
| NSC0000, NSC0003 | ReceiverControl | Manual/Active no Printer; header 001000 e conta 0000 |
| NYY0000 | ReceiverControl | Reset SG-Fallback Initiated; header 001000 e conta 0000 |
| NYC com detalhe IP | TransmitterFailure | Transmitter Failure; header 001001 |
| NYK com detalhe IP | TransmitterRestoral | Transmitter Restoral; header 001001 |
| Demais | Unknown | Necessitam evidencia; nao sao descartados |

Regras numericas exigem NumericFields do layout observado 501001/18. Aplicam o assunto ao par qualificador/codigo, independentemente da conta e dos sufixos; isso nao homologa todas as combinacoes desses campos. Os sufixos continuam opacos. Nao generalizar a convencao E/R para restauracao: R351/R354 usam a mesma descricao de falha no Printer. R602, R608 e NSC0001 permanecem Unknown.

## Operacao

`Visep.Receiver.exe --sg3-analyze datafile` acrescenta totais por categoria ao resumo existente. Conta capturas validas, incluindo repeticoes; invalidas continuam nos contadores de erro. Nao exibe contas, enderecos nem payloads. Nao conecta ao SG3 nem altera o journal. Retorno zero indica validade estrutural, nao homologacao semantica: Unknown pode ser maior que zero.

Resultado na copia isolada da captura: 230 validas / 54 payloads unicos, zero invalidas. Unknown 48, KeepAlive 2, PeriodicTest 21, TroubleTopic 139, Operational 3, ReceiverControl 12, TransmitterFailure 3, TransmitterRestoral 2. Sao tentativas capturadas, nao eventos unicos.

## Validacao e proximos passos

TDD: RED por ausencia de Sg3Category/Sg3Classifier; GREEN parser 77 assertions, build completo 263 assertions C#, integracao 60. Fixtures sinteticas cobrem codigos desconhecidos, qualificadores nao mapeados, layout desconhecido, controle, transmissor e contagem de repeticoes. Cobertura percentual nao medida. Nenhum teste em equipamento ou UI nesta etapa.

Pendencias: contrato formal dos qualificadores/sufixos, politica de atendimento, retransmissao/idempotencia, reconexao e servico continuo, estado na UI e homologacao operacional. Os arquivos privados em Inventario e tools nao devem ser publicados.
