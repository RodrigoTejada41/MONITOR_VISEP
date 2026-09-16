# SPEC-005 — Recepção e persistência

Versão: 0.2. Estado: EM IMPLEMENTAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de recepção e persistência com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

InboxReceiver processa simulation, quarentena inválidos e move para processed após persistência. Core testa deduplicação por MessageId e preservação do estado após falha de escrita.

## Critérios de aceite pendentes e riscos

Criar journal bruto durável e replay; separar evento de ocorrência e classificar teste/restauro/falha. Testar interrupções antes/depois da gravação e confirmação, sem perda nas contagens. Hoje Raw é texto SIMULATION reconstruído e toda mensagem gera Incident; não existe ACK real.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

src/Receiver/ReceiverProgram.cs; src/Core/Store.cs; tests/CoreTests.cs; tests/IntegrationTests.ps1.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
