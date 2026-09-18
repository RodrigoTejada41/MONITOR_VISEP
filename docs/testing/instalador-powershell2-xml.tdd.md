# Instalador PowerShell 2: tipos XML

## Jornada

Como operador do Windows Server 2008 R2, quero executar o instalador no PowerShell 2 para atualizar o VISEP e disponibilizar a importação BYKOM.

## Evidência TDD

| Garantia | Teste | Tipo | Resultado | Evidência |
|---|---|---|---|---|
| Scripts usados no servidor não contêm tipos XML abreviados incompatíveis | `tests/InstallerTests.ps1` | Compatibilidade | PASS | RED detectou `Installer.Common.ps1`; GREEN: `Installer pure-function tests passed.` |
| Leitura segura do XML e funções do instalador continuam operacionais | `tests/InstallerTests.ps1` | Integração | PASS | Resumo, validação, parsing do serviço e backup verificado passaram |
| Fluxos gerais não regrediram | `scripts/Build.ps1 -Test` e `tests/IntegrationTests.ps1` | Regressão | PASS | Build completo e 60 assertions de integração |
| Importador continua válido | `python -m unittest discover -s tests -p "test_*.py"` | Regressão | PASS | 17 testes |

## Pacote

`VISEP-Setup-20260918-r17.exe` foi gerado com 54 arquivos no payload. SHA-256: `B6D6A73D426D3A3CD947C2A8E45C9D1873938FC4EB77A268C1F202E73BDA0F8A`.

Cobertura percentual não foi medida. A execução final do instalador no PowerShell 2 depende do servidor alvo.
