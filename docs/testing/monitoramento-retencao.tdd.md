# Evidencia TDD: monitoramento e retencao

Data: 2026-09-17. Jornadas derivadas da retomada do projeto, sem plano externo.

## Jornadas

- Operacao identifica espaco baixo, pending antigo, corrupcao, perda de correlacao e relogio divergente sem alterar dados.
- Operacao visualiza uma limpeza antes de aplica-la.
- Retencao remove somente familias terminais antigas recuperaveis por backup verificado.
- Pending, dados recentes, legado sem envelope e dados sem backup permanecem preservados.

## RED e GREEN

| Comportamento | RED confirmado | GREEN |
|---|---|---|
| Supervisao basica | Build falhou: ReceiverHealth/ReceiverMonitor ausentes | MonitoringTests 13 inicial |
| Correlacao e relogio | Build falhou: MissingPayloads, UnreferencedPayloads e FutureCaptures ausentes | MonitoringTests 19 |
| Retencao | Build falhou: RetentionResult/JournalRetention ausentes | RetentionTests 13 |
| Manifesto do Windows PowerShell 5 | DataContractJsonSerializer rejeitou BOM UTF-8 | RetentionTests 13 e IntegrationTests 60 |
| Health de raw sem envelope | Raw orfao deixava IsHealthy verdadeiro | MonitoringTests 21 |
| Backup igual ao diretorio de dados | Retencao verificava candidato contra o proprio arquivo e poderia excluir | RetentionTests 15 |
| Previa sem inbox | Comando criava inbox/receiver.lock em operacao somente leitura | RetentionTests 16 |
| Limite de espaco igual | Contrato de comparacao precisava evidencia de fronteira | MonitoringTests 22 |

## Garantias

| Garantia | Evidencia | Tipo | Resultado |
|---|---|---|---|
| Health classifica espaco, pending antigo, corrupcao e clock skew | tests/MonitoringTests.cs | unidade | PASS 22 |
| Capture sem raw e raw legado sao diferenciados | tests/MonitoringTests.cs | unidade | PASS |
| Previa nao altera arquivos; apply preserva pending e familia recente | tests/RetentionTests.cs | unidade | PASS 16 |
| Backup igual/contido/ancestral ao diretorio de dados e recusado antes de excluir | tests/RetentionTests.cs | unidade | PASS |
| Backup ausente/divergente e lock bloqueiam retencao | tests/RetentionTests.cs | unidade | PASS |
| Backup.ps1, manifesto com BOM, retencao e restore interoperam | tests/IntegrationTests.ps1 | integracao | PASS 60 |
| Suites anteriores permanecem verdes | scripts/Build.ps1 -Test | regressao | Capture29, Journal23, Core46, Legacy27 |
| Importacao Python permanece verde | python -m unittest discover -s tests -p 'test_*.py' | regressao | PASS 16 |

## Lacunas

Cobertura percentual nao foi medida; 80% permanece nao demonstrado. E2E desktop nao mudou e nao foi reexecutado. Driver SG3, SCM e Windows Server 2008 R2 continuam sem homologacao. Retencao de processed/quarantine e automacao externa do health permanecem fora desta entrega.
