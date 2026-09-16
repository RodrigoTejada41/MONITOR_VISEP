# SPEC-006 — Integração SG-System III

Versão: 0.2. Estado: EM IMPLEMENTAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de integração sg-system iii com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

Inventário histórico: 192.168.1.250:49178 conecta a 192.168.1.249:1025/TCP, daemon1.exe PID 3792. BYKOM cliente é inferência forte, sem captura SYN. Receiver é somente simulação XML.

## Critérios de aceite pendentes e riscos

Confirmar direção, framing, ACK, heartbeat e retransmissão por manual/captura autorizada. Implementar cliente TCP e testar frames parciais/múltiplos, duplicação, reconexão e falha de disco. Comparar eventos com Printer Log em laboratório. Modelo ioBroker DC-09 não comprova automação CPM3; não houve conexão real nesta revisão.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

docs/INTEGRACAO_SG_SYSTEM_III.md; docs/COMPARACAO_SG3_MODELO.md; Inventario/05_Conexoes.txt:40; Inventario/06_Processos_Servicos.txt:71.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
