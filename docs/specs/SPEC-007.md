# SPEC-007 — Atendimento de ocorrências

Versão: 0.2. Estado: EM VALIDAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de atendimento de ocorrências com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

Claim/AddAction/Close conectados à UI, autorizados e auditados. Core confirma fluxo, ocorrência encerrada e exatamente um vencedor na corrida de operadores.

## Critérios de aceite pendentes e riscos

Testar motivo vazio, ação antes de assumir e operador diferente do responsável. Reexecutar E2E e homologar com evento real após SPEC-006. Concorrência local não comprova múltiplas estações.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

src/Core/Store.cs; src/Desktop/Program.cs; tests/CoreTests.cs; tests/DesktopE2E.py.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
