# SPEC-002 — Arquitetura e interfaces

Versão: 0.2. Estado: EM IMPLEMENTAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de arquitetura e interfaces com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

Core, Desktop, Receiver e Migration separados. Receiver consome inbox XML sem depender da UI; integração independente possui evidência histórica.

## Critérios de aceite pendentes e riscos

Formalizar contratos de transporte/parser/repositório servidor. Store depende do XmlRepository concreto. Demonstrar recepção real com desktop fechado e supervisionar falhas de I/O hoje repetidas sem diagnóstico suficiente.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

src/Core/Store.cs; src/Core/XmlRepository.cs; src/Receiver/ReceiverProgram.cs.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
