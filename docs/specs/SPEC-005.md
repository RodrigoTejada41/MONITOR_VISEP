# SPEC-005 — Recepção e persistência

Versão: 0.2. Estado: EM IMPLEMENTAÇÃO. Revisão: 2026-09-17.

## Objetivo e limites

Entregar o escopo de recepção e persistência com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

InboxReceiver processa simulation, quarentena inválidos e move para processed após persistência. Core testa deduplicação por MessageId e preservação do estado após falha de escrita.

## Critérios de aceite pendentes e riscos

### Etapa em execucao: journal local de simulacao

Implementado: bytes XML em inbox/journal antes do parser/Store, hash de integridade, replay sem duplicar MessageId, rejeicao de conflitos, invalidos preservados e erros de persistencia mantidos pendentes. Backup de inbox inclui journal. Sem socket, ACK ou protocolo SG3 presumido. JournalTests 20 assertions e IntegrationTests 44 passaram. Procedimento em ../JOURNAL_REPLAY.md.

RED em 2026-09-16: IntegrationTests.ps1 falhou com "Journal duravel ausente" no binario anterior. Teste exige journal apos recepcao, replay sem duplicacao e preservacao no backup/restauracao. Criterios restantes abaixo continuam pendentes mesmo apos esta etapa.

Envelope local implementado em 2026-09-17: identidade de captura distinta da ocorrencia, origem por nome do arquivo, instante UTC local, hash e estados pending/delivered/invalid. Retransmissoes identicas compartilham payload, mas conservam capturas distintas. Tentativas locais repetidas tambem geram captura; nao equivalem a transmissoes fisicas. Procedimento e limites em ../JOURNAL_REPLAY.md.

Pendente: separar modelo de evento real da ocorrencia e classificar teste/restauro/falha. Testar interrupcoes de processo/energia antes/depois da confirmacao real, sem perda nas contagens. Incident.Raw continua texto SIMULATION reconstruido; bytes originais estao no journal. Nao existe ACK real nem retencao automatica.

Supervisao e retencao explicita implementadas em 2026-09-17: `--health` verifica espaco, pending antigo, integridade, correlacao capture/raw e relogio; raw sem envelope degrada health. `--retention` planeja ou remove somente familias terminais antigas integralmente preservadas em backup externo verificado. Nao roda automaticamente no servico. Retention 16, Monitoring 22 e Integration 60 passaram.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

src/Receiver/ReceiverProgram.cs; src/Core/Store.cs; tests/CoreTests.cs; tests/IntegrationTests.ps1.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
