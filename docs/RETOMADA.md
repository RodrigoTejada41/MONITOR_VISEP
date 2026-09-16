# Retomada exata

Checkpoint: 2026-09-16, America/Sao_Paulo. Diretorio: E:\Projetos\VISEP_Monitoramento. Repositorio: git@github.com:RodrigoTejada41/MONITOR_VISEP.git, branch main. Historico remoto inicial 908dc55 preservado. Consultar `git log -1 --format=fuller` para o commit exato e `git status --short` para alteracoes posteriores.

## Leia primeiro ao retomar

1. Este arquivo, INTEGRACAO_SG_SYSTEM_III.md e COMPARACAO_SG3_MODELO.md.
2. TESTE_BYKOM.md, scripts/Build.ps1 e scripts/Import-Legacy.ps1 para fluxo existente.
3. BACKLOG.md e specs sao planejamento historico; confrontar com o codigo atual e evidencias antes de repetir trabalho.

Usuario confirmou: Windows Server 2008 R2; ligacao SG3/BYKOM por rede; Printer Log continua recebendo eventos. Objetivo: manter receptora SG3 e concluir a central de monitoramento substituta do BYKOM.

Descoberta confirmada no inventario: `05_Conexoes.txt:40` registra `192.168.1.250:49178 -> 192.168.1.249:1025`, PID 3792; `06_Processos_Servicos.txt:71` identifica daemon1.exe. Endpoint historico da automacao: **192.168.1.249:1025/TCP**. BYKOM como cliente e SG3 como servidor e inferencia forte da conexao, ainda sem captura SYN/configuracao explicita. Portas 1024/1027 pertencem ao SGC-WinService, PID 1116. Nao pedir novamente IP/porta como se fossem desconhecidos.

O modelo Inventario/Modelo/ioBroker.sia-master implementa DC-09, nao comprova protocolo de automacao SG3. Nao copiar ACK/framing para SG3. Nosso Receiver continua apenas simulacao por XML. Nenhuma conexao ao SG3 foi realizada nesta analise.

Proximo passo da integracao: obter a especificacao aplicavel e/ou capturas autorizadas para framing, ACK, heartbeat e retransmissoes. Preparar journal duravel, replay e contratos de transporte/parser sem alterar producao. Depois implementar cliente TCP segundo protocolo confirmado, com persistencia antes de confirmacao quando exigido pelo contrato. Usuario pediu neste turno preservacao/commit, nao conexao ao equipamento.

Preservacao: Inventario, tools, data e build sao locais e ignorados no Git; contem originais, dados ou artefatos. O commit preserva codigo, scripts, testes e documentacao, mas nao substitui backup dessas pastas. Outra IA nesta maquina pode usa-las; em outra maquina sera necessario copiar o inventario e os dados de forma protegida. Usuario forneceu o remoto para guardar este checkpoint.

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
