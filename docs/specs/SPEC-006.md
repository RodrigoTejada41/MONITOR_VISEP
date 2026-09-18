# SPEC-006 — Integração SG-System III

Versão: 0.4. Estado: EM VALIDAÇÃO. Revisão: 2026-09-17.

## Objetivo e limites

Entregar o escopo de integração sg-system iii com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

Inventário e teste real confirmam 192.168.1.250 cliente de 192.168.1.249:1025/TCP. CPM3 B32 Off. Após Reset Fallback e Primary Active, cliente `plain` por 600 s recebeu 230 frames/5383 bytes, 230 envelopes e 54 raws únicos. Health íntegro; ACK ocorreu somente após journal/envelope duráveis. Captura fica `captured` e ainda não cria ocorrência.

A cópia restrita foi analisada offline pelo parser v1. Todas as 230 capturas e os 54 payloads únicos foram estruturalmente válidos: 2 keepalives, 59 eventos entre colchetes e 169 eventos numéricos E/R. O comando `--sg3-analyze` expõe somente contagens. Bytes, contas e sinais não são impressos.

## Critérios de aceite pendentes e riscos

O manual oficial confirma saída de automação TCP/IP/RS-232. Testes locais cobrem frames parciais/múltiplos, plain/B32, limite, persistência antes do ACK, ausência de ACK quando o journal falha e parser das três famílias observadas; a janela real confirmou endpoint, framing plain e compatibilidade estrutural. Faltam reconciliar um alarme conhecido com o Printer Log, definir semântica/criticidade dos códigos, implementar reconexão contínua e validar heartbeat/retransmissão/falhas prolongadas.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

docs/INTEGRACAO_SG_SYSTEM_III.md; docs/COMPARACAO_SG3_MODELO.md; docs/TESTE_REAL_SG3.md; src/Receiver/Sg3Transport.cs; src/Receiver/Sg3Parser.cs; tests/Sg3TransportTests.cs; tests/Sg3ParserTests.cs; Inventario/05_Conexoes.txt:40; Inventario/06_Processos_Servicos.txt:71.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
