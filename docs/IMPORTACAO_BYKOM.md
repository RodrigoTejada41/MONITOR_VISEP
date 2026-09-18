# Previa de importacao BYKOM

O backup confirmado no servidor e `C:\BACKUP_SQL\bykom.sql`. A previa le esse arquivo como texto: nao conecta nem executa SQL. Ela cria uma pasta nova em `C:\ProgramData\Visep-Imports` com `data.xml` isolado e `import-report.json`; nao altera a base VISEP ativa.

Execute como administrador:

```powershell
& 'C:\Program Files\VISEP\scripts\Preparar-Importacao-BYKOM.ps1' -SqlFile 'C:\BACKUP_SQL\bykom.sql'
```

O servidor precisa ter Python 3. O importador importa cadastros de `abmacodigos`, contatos, zonas e sensores. A conta gerada e `BYKOM-ORDER_ID`; ela e um identificador legado, nao uma confirmação de correspondência com o identificador SG3. Nao aplicar a previa à base ativa até reconciliar `ORDER_ID`, `ID_RC`, `ID_CL` e as contas SG3 reais.

Depois da previa, registrar `clients_imported`, `orphan_relations`, hash da origem e amostra de contas do relatório. A proxima etapa sera uma aplicacao transacional que preserva usuarios, ocorrencias e journal da base ativa, somente depois da validacao desse mapeamento.
