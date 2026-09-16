# SPEC-001 — Ambiente e compatibilidade Windows

Versão: 0.2. Estado: EM VALIDAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de ambiente e compatibilidade windows com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

Build C# 5/.NET Framework 4.7.2 com referências explícitas e warnings como erros implementado. Build, Core (46 assertions) e LegacyHistory (27) passaram neste checkpoint.

## Critérios de aceite pendentes e riscos

Homologar instalação, login e reinício em VM Server 2008 R2 SP1, registrando runtime e versão do SO. Compilar localmente não comprova compatibilidade no servidor. Python da migração é dependência separada.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

scripts/Build.ps1; docs/COMPATIBILIDADE_WINDOWS.md.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
