# MONITOR_VISEP

Central de monitoramento em C# / WinForms (.NET Framework 4.7.2), com receptor de simulacao e ferramentas de migracao BYKOM. Integracao SG3 real ainda pendente de implementacao e homologacao.

## Retomada por outra IA ou desenvolvedor

Leia primeiro [docs/RETOMADA.md](docs/RETOMADA.md). Depois consulte [integracao SG3](docs/INTEGRACAO_SG_SYSTEM_III.md), [comparacao do modelo](docs/COMPARACAO_SG3_MODELO.md) e [teste BYKOM](docs/TESTE_BYKOM.md).

Confira `git status --short` e `git log -1` antes de alterar arquivos. O endpoint historico identificado e 192.168.1.249:1025/TCP; isso nao significa que o driver ja esteja implementado. Nao conectar ao equipamento de producao automaticamente.

Build e testes C#: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Build.ps1 -Test`.

Testes de importacao: `python -m unittest discover -s tests -p 'test_*.py'`.

Inventario, tools, data e build sao ignorados no Git. Um clone nao inclui backups BYKOM, documentos originais, bases locais nem capturas de validacao. Preserve essas pastas separadamente e com acesso restrito.
