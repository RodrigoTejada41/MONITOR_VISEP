# SPEC-009 — Relatórios e exportações

Versão: 0.2. Estado: EM VALIDAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de relatórios e exportações com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

UI filtra ocorrências e histórico BYKOM. CSV de ocorrências exclusivo Admin; Core testa aspas, vírgulas, múltiplas linhas e prefixos de fórmula. Histórico legado é amostra somente leitura.

## Critérios de aceite pendentes e riscos

Automatizar filtros com resultados esperados; definir períodos e relatórios operacionais. Testar volume/paginação. CSV atual não exporta histórico legado nem acompanha filtro da tela; consultas carregam listas inteiras.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

src/Core/Store.cs; src/Desktop/Program.cs; tests/CoreTests.cs; tests/LegacyHistoryTests.cs.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
