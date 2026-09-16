# SPEC-011 — Testes e validação operacional

Versão: 0.2. Estado: EM VALIDAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de testes e validação operacional com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

Neste checkpoint Build -Test passou: Core 46 assertions e LegacyHistory 27; unittest Python 16 testes. IntegrationTests 32 assertions e DesktopE2E têm evidência histórica, não nova execução nesta revisão.

## Critérios de aceite pendentes e riscos

Medir cobertura e demonstrar 80%; assertions não medem cobertura. Reexecutar integração/E2E com logs e ambiente. Homologar SG3, SO, SCM, carga/retenção/recuperação. Definir taxa e latência esperadas. ImportedDesktopE2E existe, mas sua presença não comprova execução aprovada.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

tests/CoreTests.cs; tests/LegacyHistoryTests.cs; tests/test_import_legacy.py; tests/test_sql_dump.py; tests/IntegrationTests.ps1; tests/DesktopE2E.py; tests/ImportedDesktopE2E.py.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
