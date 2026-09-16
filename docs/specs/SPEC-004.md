# SPEC-004 — Cadastros

Versão: 0.2. Estado: EM IMPLEMENTAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de cadastros com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

Store.AddClient autoriza Admin, valida nome/conta, rejeita conta duplicada e limita campos. Desktop cadastra e consulta. Core testa cadastro e associação de ocorrência.

## Critérios de aceite pendentes e riscos

Testar explicitamente duplicatas, campos vazios e limites. Implementar edição/inativação e entidades estruturadas para locais, contatos, equipamentos, partições e zonas. Hoje contatos/equipamentos/zonas são texto, não CRUD relacionado completo.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

src/Core/Models.cs; src/Core/Store.cs; src/Desktop/Program.cs; tests/CoreTests.cs.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
