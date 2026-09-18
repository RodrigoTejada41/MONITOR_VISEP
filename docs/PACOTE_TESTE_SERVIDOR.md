# Pacote de teste para o servidor BYKOM

Este pacote executa o demonstrador VISEP em .NET Framework 4.7.2. A conexao SG-System III existe somente no comando explicito de captura descrito em `docs\TESTE_REAL_SG3.md`; ela nao inicia pelo servico nem converte sinais em ocorrencias.

## Menu no CMD

Abra scripts/Menu-Servidor.cmd. O painel mostra conexao TCP observada (incluindo outros consumidores), servico de simulacao, total de capturas e ultima gravacao local. Opcao 2 valida a captura offline; opcao 3 troca o caminho; opcao 4 inicia captura real de 90 segundos com confirmacao e framing explicitos. Durante a captura o painel atualiza a cada 2 segundos. Cada teste usa pasta nova em data/sg3-menu-<id>; console.txt e erro.txt ficam nessa pasta. Nao feche a janela durante captura: aguarde os 90 segundos e confira o resultado. A janela nao para servicos automaticamente.

TCP conectado sozinho nao comprova recebimento; capturas devem aumentar e a analise deve terminar sem invalidos. Unknown nao significa falha de transporte. Ultima gravacao e metadata do arquivo, nao horario do evento.

## Teste recomendado nesta versao: captura preservada

Nao reinstale nem pare o servico existente para analisar a captura. Extraia o ZIP, abra a pasta extraida que contem build/scripts e execute no Prompt:

```cmd
scripts\Analisar-SG3.cmd "C:\ProgramData\Visep\sg3-active-test\data.xml"
```

O caminho deve ter inbox/captures e inbox/journal ao lado do data.xml; o data.xml nao precisa existir para analise. Resultado esperado na captura preservada: 230 validas, 54 payloads unicos, zero invalidas, 48 Unknown. Categorias e limites em docs/CLASSIFICACAO_SG3.md. Salve o resultado:

```cmd
scripts\Analisar-SG3.cmd "C:\ProgramData\Visep\sg3-active-test\data.xml" > resultado-analise.txt 2>&1
```

Nenhum sinal vira ocorrencia neste teste. Teste de captura ao vivo exige janela operacional separada conforme TESTE_REAL_SG3.md. O servico instalado continua sendo receptor de simulacao.

Scripts Backup/Restore incluidos requerem ambiente PowerShell compativel com seus comandos (nao homologados em PowerShell 2); nao os use como garantia de backup no servidor antigo. Preserve copia integral dos dados antes de instalacao/rollback.

## Pre-requisitos

- Windows Server 2008 R2 SP1 x64 com interface grafica.
- .NET Framework 4.7.2 instalado.
- Windows PowerShell 2 ou posterior; scripts de instalacao e captura evitam recursos posteriores no caminho operacional.
- Copia de seguranca do servidor e janela de teste isolada.
- Nao interromper nem alterar o Printer Log/SG3 para este teste.

## Teste sem instalar

1. Extraia o ZIP em `C:\VISEP-Teste`.
2. Confira `SHA256SUMS.txt` antes de executar.
3. Execute `scripts\Verificar-Ambiente.cmd`. Se algum item falhar, nao instale.
4. Execute `scripts\Abrir-Teste-Servidor.cmd`.
5. No primeiro acesso, crie o administrador com senha de 12 a 256 caracteres.
6. Cadastre um cliente, envie um evento pelo Simulador e conclua o atendimento em Ocorrencias.
7. Feche a interface; o receptor temporario sera encerrado. Os dados ficam em `data\teste-servidor` dentro da pasta extraida.

## Instalacao controlada do servico

Abra PowerShell como administrador na pasta extraida:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Install.ps1
```

O instalador copia binarios para `C:\Program Files\VISEP`, usa `C:\ProgramData\Visep\data.xml`, cria o servico `VisepReceiver` como LocalService e cria atalho publico. Abra o Desktop e crie o administrador antes de iniciar o servico:

No Windows Server 2008 R2, a criação usa `Win32_Service.Create` por WMI para evitar diferenças de passagem de argumentos do `sc.exe` entre versões do PowerShell.

```powershell
Start-Service VisepReceiver
Get-Service VisepReceiver
```

Verificacao somente leitura:

```powershell
& 'C:\Program Files\VISEP\Visep.Receiver.exe' --health 'C:\ProgramData\Visep\data.xml' 1024 5
```

## Rollback

Execute como administrador a partir da pasta extraida:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Uninstall.ps1
```

O rollback remove servico, atalho e binarios conhecidos. `C:\ProgramData\Visep` e preservado. Arquive essa pasta antes de qualquer remocao manual.

## Registrar resultado

Registre versao do Windows/SP, valor Release do .NET 4 Full, resultado da abertura, criacao do administrador, simulacao, atendimento, health, instalacao/reinicio do servico e rollback. A homologacao SG3 continua separada e depende do protocolo confirmado.

## Captura SG3 controlada

Use somente em janela acompanhada, depois de conferir **B32 Headers** no CPM3 e parar o consumidor anterior. Siga `docs\TESTE_REAL_SG3.md`.

## Controles operacionais do menu

Opcao 6 reinicia apenas VisepReceiver, exige administrador e confirmacao, aguarda estados Stopped/Running por ate 30 segundos e informa falha. Nao modifica BYKOM, Printer ou SG3.

Opcao 7 abre a interface do pacote usando C:/ProgramData/Visep/data.xml. Opcao 8 fecha normalmente e reabre somente a interface iniciada por este menu; exige salvar o trabalho e confirmar. Se houver dialogo impedindo fechamento, cancela sem matar o processo. Reiniciar Windows nao esta incluido.
