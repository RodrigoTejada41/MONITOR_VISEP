# Retomada exata

## Correcao r12 - resultado de falha do instalador

O r10/r11 foram executados no Server 2008 R2 e recusaram corretamente atualizar enquanto `Visep.Desktop.exe` estava aberto no PID 4544. Nenhum arquivo foi atualizado. Eles mostraram "Instalacao concluida" indevidamente porque Windows PowerShell 2 interrompeu o tratador que usava `Write-Error`. O r12 escreve a falha diretamente no console e retorna código 1. Use somente `artifacts/VISEP-Setup-20260918-r12.exe`; r10/r11 estao supersedidos.

## Atualizacao de instalador e validacao no servidor - 2026-09-18

O teste presencial do r8 passou no Server 2008 R2 SP1: análise da captura preservada retornou 230 válidas, 54 payloads distintos, zero inválidas e 48 Unknown; verificador aprovou SO x64, PowerShell 2 e .NET 4.7.2; interface abriu, criou cliente TEST001, simulou evento 130/001/01, assumiu, registrou ação e encerrou a ocorrência. O evento foi preservado em uma base extraída `C:\CVISEP-Teste\...\data\teste-servidor\data.xml`, não na base do serviço `C:\ProgramData\Visep\data.xml`. A causa foi abrir o teste temporário e depois o menu/serviço em outra base. Não houve perda: a base antiga contém 1 cliente e 1 ocorrência fechada.

Correção entregue localmente: `artifacts/VISEP-Setup-20260918-r10.exe`, SHA-256 `39C1903426FD02F58F7047B33C6B6889D7FA2622DF5605DFD493447464E43CE4`. É executável com elevação, valida requisitos, detecta serviço instalado, lista bases antigas por contagens e exige seleção explícita; copia backup hash-verificado para `C:\ProgramData\Visep-Installer-Backups`; não mescla journals e preserva `sg3-active-test`. Atualiza binários na pasta do serviço e inicia o serviço. Em modo `sg3`, solicita IP/porta/framing, recusa outro consumidor TCP e exige confirmação `CAPTURAR`; o receptor reconecta e grava raw/envelope antes do ACK, sem criar ocorrências. O login agora apresenta `Entrar` para bases já inicializadas e mostra o caminho da base.

Validação local r10: Bootstrap 6, SG3 contínuo 19, parser 77, transporte 21, retenção 17, supervisão 23, captura 29, journal 23, Core 46, histórico 27, integração 60, Python 16 e DesktopE2E passaram. Extração isolada do r10 e hashes internos passaram. Não executar r10/SCM ou conexão contínua SG3 automaticamente: falta janela acompanhada no servidor, consumidor anterior parado e framing B32 confirmado. Não declarar eventos SG3 como ocorrências.

Próxima ação: copiar apenas r10 ao servidor, fechar VISEP/receptor console, executar como administrador, selecionar explicitamente a base de `C:\CVISEP-Teste` com 1 cliente/1 ocorrência quando ela for listada, escolher simulação para validar migração e serviço. Só depois, em outra janela aprovada, executar nova atualização/mudança para modo SG3 com o consumidor BYKOM parado.

## LEIA PRIMEIRO - parada solicitada em 2026-09-18

Estado preservado localmente. HEAD verificado: `14cf6690a6074b651b73c9d1a2d8f3386a6b492a`. Ha alteracoes locais ainda sem commit; este HEAD isolado NAO contem as entregas atuais. Nao executar checkout/reset/clean para retomar. Nenhum push realizado nesta continuacao.

### Acao exata ao retornar

1. Usar o pacote **r8-menu**, hash conferido abaixo. Copiar ZIP para servidor e extrair em pasta nova. Abrir scripts/Menu-Servidor.cmd como administrador.
2. Conferir opcao 1 e caminho da captura preservada C:/ProgramData/Visep/sg3-active-test/data.xml; ajustar pela opcao 3 se necessario.
3. Executar opcao 2. Esperado: 230 validas, 54 payloads distintos, zero invalidas, 48 Unknown. Isso valida leitura/classificacao offline; nao confirma recepcao ao vivo.
4. Validar opcao 5 no Server 2008 R2/PS2. Opcao 6 reinicia somente servico de simulacao VisepReceiver, com confirmacao; opcoes 7/8 abrem/reiniciam interface rastreada. Esses caminhos ainda nao foram executados no servidor nesta etapa.
5. Somente em janela acompanhada, com consumidor anterior/servico parados e B32 conferido, usar opcao 4. Exige CAPTURAR, dura 90 segundos, grava pasta nova data/sg3-menu-<id>, atualiza painel a cada 2s. TCP pode pertencer a outro consumidor; conferir aumento de capturas e analise final. Preservar console.txt/erro.txt e journal/envelopes.

Usuario pediu reinicio de servicos e sistema. Entregue reinicio de VisepReceiver e aplicativo VISEP. Pergunta sobre reiniciar Windows ficou sem resposta; reboot do servidor NAO foi implementado.

### Artefatos e limites

- Pacote: `artifacts/VISEP_Server2008R2_Teste_20260918-r8-menu.zip`; SHA256 `469AEF99AB4B1713FD00017B68B217302264D69EC1FAAFF748CE1227C4D264AB`.
- Dados privados: Inventario/captures (230 XML), Inventario/journal (54 raw), Inventario/Printer (160 logs). Ignorados pelo Git; devem permanecer locais.
- Evidencias privadas: tools/validation/sg3-reconciliacao/RELATORIO.md, PRINTER_CONFRONTO.md e timeline.csv. Pacote extraido validado em tools/validation/package-r8.
- Codigo: Sg3Parser/Sg3Classifier e menu CMD/PowerShell. Classificacao descritiva nao gera ocorrencias; desconhecidos preservados. Nao inferir restauracao E/R nem deduplicar por hash/Sequence.
- Testes: build 263 assertions C#, integracao 60; ZIP 28 arquivos com hash correto, menu abre/sai com codigo 0, painel local leu 230 capturas. Cobertura percentual, live/SCM/UI deste pacote e PS2 real continuam pendentes.
- Depois do teste: resolver eventuais falhas de ambiente/menu e seguir politica de atendimento, retransmissao/idempotencia, servico continuo com reconexao, UI e homologacao. Nao declarar central substituta do BYKOM pronta.

As secoes abaixo sao historico; prevalece este checkpoint e o pacote r8.


## Pacote atual r8 com menu CMD - 2026-09-18

artifacts/VISEP_Server2008R2_Teste_20260918-r8-menu.zip substitui r6/r7. SHA256 469AEF99AB4B1713FD00017B68B217302264D69EC1FAAFF748CE1227C4D264AB. Menu em scripts/Menu-Servidor.cmd: conexao TCP observada, capturas/ultima gravacao, analise offline, captura acompanhada 90s, ambiente, reinicio confirmado apenas de VisepReceiver e interface rastreada pelo menu. Nao reinicia Windows. Menu nao libera comandos enquanto captura filha segue ativa, inclusive em erro de painel.

Validado localmente: 28 arquivos do ZIP conferidos por hash, menu extraido executado/encerrado com codigo 0, painel com captura real 230 arquivos. Reinicio SCM, interface e captura live nao executados nesta rodada; PS2/Server2008R2 requerem teste no servidor. Nao reinstalar servico para usar analise/menu. Primeiro abrir menu e analisar captura preservada; novas capturas exigem janela acompanhada. Dados privados nao incluidos.


## Pacote servidor r6 - 2026-09-18

Gerado artifacts/VISEP_Server2008R2_Teste_20260918-r6-classificacao.zip. SHA-256 F4FE9B1FDC948DCFAEAFAD38028C68F32B482EBB2E59AEE264ECF83F5CCE81E0. Validado ZIP extraido com 26 arquivos no manifesto; Analisar-SG3.cmd do pacote retornou 230 capturas validas/54 payloads/zero invalidos/48 Unknown usando copia isolada real. Nao executado no Server 2008 R2 nesta rodada.

Inclui classificacao, comando de analise offline e documentos. Launcher remove dependencia PSScriptRoot para PS2; verificador passa a aceitar PS2. Primeiro teste: scripts/Analisar-SG3.cmd com caminho C:/ProgramData/Visep/sg3-active-test/data.xml. Nao reinstalar/parar servico para analisar. Captura ao vivo segue procedimento de janela acompanhada; servico continua simulacao. Backup/Restore nao homologados em PS2. Pacote nao contem capturas, Printer ou credenciais.


## Atualizacao 2026-09-18: classificacao offline

Implementada tabela em CLASSIFICACAO_SG3.md e Sg3Classifier.cs, integrada ao resumo --sg3-analyze. Qualificadores desconhecidos nao sao inferidos; TroubleTopic nao determina falha ativa/restauracao. Nenhuma geracao de ocorrencia ou deduplicacao.

Validacao: parser 77 assertions, build C# 263, integracao 60. Copia privada da captura: 230 validas, 54 payloads unicos, zero invalidas; 48 Unknown. Totais completos e limites em CLASSIFICACAO_SG3.md. Proximo passo: definir politica de atendimento e contrato de retransmissao/qualificadores antes da ligacao com ocorrencias. Sem conexao ao SG3.


## Atualizacao 2026-09-18: campos numericos SG3

Printer completo recebido em Inventario/Printer (privado). Confronto local em tools/validation/sg3-reconciliacao/PRINTER_CONFRONTO.md: 8 de 13 combinacoes numericas presentes nos dois fluxos; 162/169 capturas possuem campos correspondentes. Nao e reconciliacao individual: ordem, horarios e repeticoes divergem.

Sg3Message.NumericFields extrai Account, EventCode, Field2 e Field3 apenas no layout observado com prefixo 501001 e bloco iniciado por 18. Outros layouts permanecem validos estruturalmente, com NumericFields null. Receiver, Signal, Qualifier e journal originais preservados. Field2/Field3 nao recebem semantica de particao/zona/usuario sem contrato confirmado. Sequence nao deve ser usado como identificador unico.

Testes sinteticos adicionados antes da implementacao: RED por NumericFields ausente; GREEN build completo com 221 assertions C# e integracao com 60 assertions. Revisao independente sem problemas acionaveis. Cobertura percentual nao medida; UI e equipamento real nao reexecutados. Nao ha classificacao, deduplicacao ou criacao automatica de ocorrencias. Proximo passo: validar contrato dos qualificadores e campos, politica de ocorrencias e retransmissoes. Nenhuma conexao SG3 nesta rodada.


Checkpoint: 2026-09-17, America/Sao_Paulo. Diretorio: E:\Projetos\VISEP_Monitoramento. Repositorio: git@github.com:RodrigoTejada41/MONITOR_VISEP.git, branch main. Historico remoto inicial 908dc55 preservado. Consultar `git log -1 --format=fuller` para o commit exato e `git status --short` para alteracoes posteriores.

Teste real no servidor Windows 6.1.7601 concluído: CPM3 B32 Off, `192.168.1.249:1025 plain`, Primary v2.04 e Secondary desconectado. Reset Fallback Successful; `NSC0003 Switching To Active Mode` às 15:42:25. Em 600 s: 230 frames/5383 bytes, 230 envelopes e 54 raws únicos. Health healthy, captured=230, corrupção/missing/unreferenced zero. Serviço de simulação reiniciado. Preservar `C:\ProgramData\Visep\sg3-active-test`.

Captura copiada por canal restrito e analisada offline. Parser SG3 v1 e `--sg3-analyze` implementados: 230/230 capturas válidas, 54/54 payloads únicos válidos, 0 inválidos; 2 keepalives, 59 bracket events, 169 numeric events. O parser preserva campos sem atribuir semântica não comprovada e o analisador não imprime conteúdo. Próxima etapa: reconciliar um alarme conhecido com o Printer Log, definir quais códigos criam ocorrência e só então ligar parser ao serviço contínuo/UI.

## Leia primeiro ao retomar

### Continuacao atual: supervisao e retencao segura

Receiver oferece `--health datafile [minimumFreeMiB] [stalePendingMinutes]`: leitura sem mutacao de espaco livre, inbox, estados, pending antigo, corrupcao, correlacao capture/raw e relogio futuro. Codigo 1 sinaliza condicao critica. Raw sem envelope agora degrada health para evitar falso OK operacional.

`--retention datafile backupDirectory cutoffUtc [--apply]` usa previa por padrao. Exige lock exclusivo e backup externo: cada candidato precisa constar no manifesto, existir na copia e manter o mesmo SHA-256. Somente familias em que todas as capturas sao delivered/invalid e anteriores ao corte UTC sao removidas. Pending, familias recentes, raw orfao e capture sem raw permanecem. Metadata corrompida bloqueia tudo. Nao ha limpeza automatica.

TDD RED: ReceiverHealth/ReceiverMonitor ausentes; propriedades de correlacao/clock ausentes; RetentionResult/JournalRetention ausentes; manifesto com BOM do Windows PowerShell 5 rejeitado; backup igual ao diretorio de dados aceito; raw sem envelope com health saudavel; previa sem inbox criava diretorio. GREEN: Retention 16, Monitoring 22, Capture 29, Journal 23, Core 46 e LegacyHistory 27; IntegrationTests 60; Python 16. UI nao mudou; E2E desktop nao reexecutado. Cobertura percentual de 80% ainda nao demonstrada.

Operacao e limites: JOURNAL_REPLAY.md e BACKUP_RESTAURACAO.md. Evidencia TDD: testing/monitoramento-retencao.tdd.md. Alteracoes locais preservadas, sem commit/push.

Pacote atual: `artifacts/VISEP_Server2008R2_Teste_20260917-r5-sg3-ps2.zip`, SHA-256 `D76EF8AF73649CBA814329FB68FC539F1CB5F678FADFDF870903CD1670E02A5C`. Mantém a correção WMI do instalador e corrige `Testar-SG3.ps1` para PowerShell 2 (`$MyInvocation.MyCommand.Path`, sem `$PSScriptRoot` no parâmetro). Falha TCP controlada confirmou execução do script até o cliente; 24 arquivos verificados diretamente no ZIP. O r5 substitui r4/r3/r2.

Próxima entrega: obter um alarme de teste com horário e linha exatos no Printer Log, localizá-lo entre as capturas já preservadas e definir o mapeamento para ocorrência. Depois transformar o cliente de captura em serviço contínuo com reconexão e estado exibido na UI.

### Historico: envelopes de captura

CaptureJournal conserva cada tentativa em inbox/captures/<guid>.xml com origem, capturedUtc, payloadSha256 e estado pending/captured/delivered/invalid. `captured` identifica frame SG3 durável e confirmado, ainda sem parser/ocorrência. Envelope precede Parse/Store ou ACK; falha impede confirmação. Bytes idênticos compartilham .raw, mas possuem capturas distintas.

TDD RED: CaptureTests falhou por envelope ausente; integracao tambem falhou por inbox/captures ausente antes do novo build. Teste obsoleto do journal corrigido apos reproduzir falha: journal inexistente retorna 1, nao excecao.

Revisao adicional: indice de capturas por hash evita varredura completa por payload. Metadata malformada ou com esquema invalido e isolada/reportada; payloads validos continuam. Teste com envelope bloqueado comprovou persistencia no Store com estado pending e posterior replay idempotente para delivered. Pending indica falta de confirmacao no envelope, nao ausencia garantida de ocorrencia.

Validacao em 2026-09-17: Build -Test passou (Capture 29, Journal 23, Core 46, LegacyHistory 27); IntegrationTests passou com 56 assertions; Python passou com 16 testes. Dados sinteticos isolados. UI nao alterada; E2E desktop, SCM, SG3 e Server 2008 R2 nao reexecutados. Cobertura de 80% ainda nao demonstrada. Alteracoes locais preservadas, sem novo commit/push.

Supervisao e retencao foram implementadas na continuacao acima. Classificacao de eventos teste/restauro/falha depende do contrato real. Relogio de captura e local, nao do equipamento; contagem inclui tentativas locais. Nao conectar ao SG3 sem protocolo confirmado e laboratorio.

### Historico anterior: journal de simulacao (2026-09-16)

Receiver agora grava payload XML original em inbox/journal (.raw por SHA-256) antes de Parse/Store; grava temporario com Flush(true), verifica hash e limita leitura a 1 MiB. --replay datafile recupera journal sem inbox original, deduplica via MessageId e rejeita conflitos de payload/ID normalizado. Erros do estado XML mantem entrada pendente, nao a classificam como mensagem invalida. Arquivo receiver.lock exclui ciclos concorrentes.

Validacao desta continuacao: teste RED 14cf669 falhou por journal ausente; suite JournalTests 20 assertions e IntegrationTests 44 assertions passaram. Build inclui testes de Core e LegacyHistory. Ver JOURNAL_REPLAY.md para operacao/limites. UI nao mudou; E2E desktop nao reexecutado. Sem homologacao SG3, SCM ou SO minimo.

Envelope, supervisao e retencao foram implementados. Transporte SG3 agora suporta framing plain/B32 explícito e ACK ativo depois da persistência; parser/classificacao e homologacao real continuam pendentes. Nao ligar --replay a um endpoint de rede.

Ancora do codigo e da analise SG3: `5b4a104b37579c37128af75582ad68eb030e2a2c`, publicada em origin/main. Revisoes documentais posteriores devem preservar essa referencia.

1. Este arquivo, INTEGRACAO_SG_SYSTEM_III.md e COMPARACAO_SG3_MODELO.md.
2. TESTE_BYKOM.md, scripts/Build.ps1 e scripts/Import-Legacy.ps1 para fluxo existente.
3. ORGANOGRAMA.md, specs/README.md e BACKLOG.md: revisados contra o codigo; 5 specs em implementacao, 6 em validacao, nenhuma concluida.
4. evidencias/README.md e evidencias/manifesto.json: referencias preservadas e hashes de insumos privados.

Usuario confirmou: Windows Server 2008 R2; ligacao SG3/BYKOM por rede; Printer Log continua recebendo eventos. Objetivo: manter receptora SG3 e concluir a central de monitoramento substituta do BYKOM.

Descoberta confirmada no inventario: `05_Conexoes.txt:40` registra `192.168.1.250:49178 -> 192.168.1.249:1025`, PID 3792; `06_Processos_Servicos.txt:71` identifica daemon1.exe. Endpoint historico da automacao: **192.168.1.249:1025/TCP**. BYKOM como cliente e SG3 como servidor e inferencia forte da conexao, ainda sem captura SYN/configuracao explicita. Portas 1024/1027 pertencem ao SGC-WinService, PID 1116. Nao pedir novamente IP/porta como se fossem desconhecidos.

O modelo Inventario/Modelo/ioBroker.sia-master implementa DC-09, nao comprova protocolo de automacao SG3. Nao copiar ACK/framing para SG3. Nosso Receiver continua apenas simulacao por XML. Nenhuma conexao ao SG3 foi realizada nesta analise.

Proximo passo da integracao: obter a especificacao aplicavel e/ou capturas autorizadas para framing, ACK, heartbeat e retransmissoes. Preparar journal duravel, replay e contratos de transporte/parser sem alterar producao. Depois implementar cliente TCP segundo protocolo confirmado, com persistencia antes de confirmacao quando exigido pelo contrato. Usuario pediu neste turno preservacao/commit, nao conexao ao equipamento.

Preservacao: repositorio PUBLIC verificado. DOCX SG3 e modelo MIT de Inventario agora sao versionados por excecoes explicitas; configuracoes BYKOM, SQL, RAR, planilhas, logs integrais, tools, data e build continuam locais. Conhecimento tecnico de tools consolidado em ARQUITETURA.md. O manifesto registra hashes/tamanhos, nao e backup. Outra maquina precisa receber insumos privados por canal restrito; nenhuma credencial ou dado de cliente deve ir ao Git publico.

Ultimo pedido: continuar projeto e pendencias. Implementado journal/replay do simulador; sem conexao SG3/alteracao de producao. Originais privados preservados. O README e a entrada de retomada para outra IA.

Validacao do checkpoint: scripts/Build.ps1 -Test passou com 46 assertions Core e 27 de historico legado; python -m unittest discover -s tests -p 'test_*.py' passou com 16 testes. E2E desktop, integracao PowerShell e receptor real nao reexecutados nesta rodada.

## Estado atual

Base demonstravel integrada: Core, WinForms, receptor de simulacao, inspector SQL e scripts. Persistencia XML local; nao e banco servidor nem produto homologado.

Validacao historica registrada neste Windows local, build do SO 26200 (nao confundir com homologacao no Server 2008 R2):
- `scripts/Build.ps1 -Test`: compilacao C# 5 contra referencias .NET Framework 4.7.2, warnings como erros; PASS 46 assertions, incluindo corrida de operadores, falha de gravacao e CSV adversarial.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/IntegrationTests.ps1`: PASS 32 assertions em Windows PowerShell 5. Receptor independente, deduplicacao, quarentena, DTD, inspector sintetico, backup/restauracao, hash e formato invalido.
- `python tests/DesktopE2E.py`: PASS administrador inicial, cliente, simulacao via receptor, assumir, acao e encerramento. Dados sinteticos em diretorio temporario proprio. Captura final inspecionada e preservada em tools/validation/desktop-completed.png.
- Revisao independente confirmou correcoes de raiz/schema e unicidade das secoes no Core/Restore.

Correcoes: enumeracao de manifesto no PowerShell 5; validacao de XML restaurado antes de criar destino; rejeicao de secoes duplicadas no nucleo; localizador de modal e clique assincrono no teste desktop.

## Legado

Backup localizado em Inventario e copia isolada em tools/legacy. Registro historico: SQL de 663.570.312 bytes, hash correspondente ao original; 173 tabelas. Ver MAPA_BANCO_LEGADO.md e MAPEAMENTO_MIGRACAO.md. Existem import_legacy.py, sql_dump.py, testes de importacao e TESTE_BYKOM.md. A afirmacao anterior de que o importador ainda nao existia ficou desatualizada. Nao foi confirmada nesta rodada a existencia da base importada; conferir destino e import-report.json antes de importar novamente. Nenhum SQL executado nesta rodada.

## Proxima acao

Expandir matriz de entradas invalidas e origem temporal; medir cobertura (80% ainda nao demonstrados). Depois fixar versao/conector de banco servidor e validar semantica do mapeamento em ambiente isolado. Nao importar o dump diretamente sem revisar comandos administrativos e rotinas.

Pendencias externas: protocolo/manual e receptor SG-System III de laboratorio; VM Server 2008 R2 SP1; versao MySQL/conector, carga e retencao. Instalacao/reinicio do servico via SCM nao testados. XML nao habilita multiestacao/LAN.

## Preservacao

Nenhum servico instalado nesta rodada. Receptores/interfaces de teste encerrados pelo teste; dados de producao preservados. Backup/restauracao devem usar destinos novos; ACL do servico precisa ser reaplicada antes de operar um estado restaurado. Comandos em OPERACAO.md.
