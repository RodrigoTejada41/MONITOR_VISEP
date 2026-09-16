# MONITOR_VISEP

Central de monitoramento em C# / WinForms (.NET Framework 4.7.2), com receptor de simulacao e ferramentas de migracao BYKOM. Integracao SG3 real ainda pendente de implementacao e homologacao.

## Retomada por outra IA ou desenvolvedor

Leia primeiro [docs/RETOMADA.md](docs/RETOMADA.md). Depois consulte o [organograma](docs/ORGANOGRAMA.md), a [matriz das 11 specs](docs/specs/README.md), a [integracao SG3](docs/INTEGRACAO_SG_SYSTEM_III.md), a [comparacao do modelo](docs/COMPARACAO_SG3_MODELO.md) e o [teste BYKOM](docs/TESTE_BYKOM.md).

Estado revisado: 5 specs em implementacao, 6 em validacao, nenhuma concluida para producao. Base local funcional; driver SG3, banco multiestacao e homologacao operacional pendentes.

Confira `git status --short` e `git log -1` antes de alterar arquivos. O endpoint historico identificado e 192.168.1.249:1025/TCP; isso nao significa que o driver ja esteja implementado. Nao conectar ao equipamento de producao automaticamente.

Build e testes C#: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Build.ps1 -Test`.

Testes de importacao: `python -m unittest discover -s tests -p 'test_*.py'`.

Documento SG3 e modelo MIT estao versionados em [Inventario](Inventario/README.md). Backups BYKOM, configuracoes privadas, planilhas de clientes, tools, data e build permanecem ignorados. Um clone nao inclui esses dados. [Evidencias e manifesto](docs/evidencias/README.md) permitem identificar os insumos para uma copia privada separada. O repositorio e publico.
