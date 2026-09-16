# Operacao do demonstrador

Execute na raiz E:\Projetos\VISEP_Monitoramento. Requer .NET Framework 4.7.2; compilacao requer Developer Pack. Interface exige sessao grafica. XML somente local.

## Compilar e validar

```powershell
.\scripts\Build.ps1 -Test
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\IntegrationTests.ps1
python .\tests\DesktopE2E.py
```

Teste desktop requer Python com pywinauto. Execute sequencialmente: backup/restauracao recusam processos VISEP abertos. Testes usam dados sinteticos isolados. Cobertura percentual nao medida.

## Executar sem instalar servico

Em um terminal:

```powershell
.\build\Visep.Receiver.exe --console "$PWD\data\data.xml"
```

Em outro terminal, use o mesmo arquivo:

```powershell
.\build\Visep.Desktop.exe "$PWD\data\data.xml"
```

No primeiro acesso, informe usuario, senha de 12 a 256 caracteres e confirmacao para criar o administrador. Em Clientes, cadastre nome e conta. No Simulador, informe essa conta, codigo, zona e particao. Em Ocorrencias, selecione a linha, clique Assumir, registre uma acao e encerre com justificativa. Administracao / Auditoria permite criar usuarios, ler auditoria e exportar CSV conforme autorizacao do nucleo.

O receptor processa inbox com a interface fechada. Entradas seguem para processed ou quarantine. Fluxo exclusivamente SIMULACAO; nao implementa SG-System III.

## Backup e restauracao

Feche a interface e encerre o receptor:

```powershell
.\scripts\Backup.ps1 -DataDirectory "$PWD\data" -Destination "$PWD\backups\copia-001"
.\scripts\Restore.ps1 -BackupDirectory "$PWD\backups\copia-001" -Destination "$PWD\restaurado-001"
```

Destinos precisam ser novos. Confira BACKUP_RESTAURACAO.md. Reaplique ACL do servico/operador antes de operar via servico.

scripts/Install.ps1 exige elevacao e instala como LocalService. Instalacao e reinicio via Service Control Manager nao homologados; validacao atual cobre console. Windows Server 2008 R2 SP1 pendente de VM.
