# Plano de testes

Atualizacao 2026-09-17. Comandos e contagens finais em RETOMADA.md.

| ID | Cenario | Resultado |
|---|---|---|
| T-001 | Cadastro/validacao | Cadastro Core/UI passou; matriz completa de entradas invalidas pendente |
| T-002 | Evento simulado | Persistencia e metadados passaram; origem temporal nao nula ainda sem caso dedicado |
| T-003 | Atendimento completo | PASS Core e E2E: assumir, acao e encerramento |
| T-004 | Corrida de operadores | CoreTests: dois operadores sincronizados, um vencedor |
| T-005 | Permissoes | PASS perfis, bloqueio de login, auditoria e sessao invalida |
| T-006 | Reinicio | PASS reabertura e preservacao de estado |
| T-007 | Falha persistencia | CoreTests: backup de substituicao bloqueado, erro explicito e estado preservado |
| T-008 | Backup/restauracao | PASS copia isolada, manifesto simples/multiplo, hash e XML invalido |
| T-009 | CSV | CoreTests: formulas =+-@, aspas, virgula e multilinha |
| T-010 | SQL em fluxo | PASS fixture sintetica sem valores; dump real somente inventario textual |
| T-011 | Recepcao sem UI | PASS processo console independente; instalacao/reinicio SCM pendentes |
| T-012 | SO minimo | BLOQUEADO: VM Server 2008 R2 SP1 |
| T-013 | SG real e falhas | PARCIAL PASS: Primary Active, plain/1025 por 600 s recebeu 230 frames/5383 bytes, 54 únicos, health íntegro; parser/reconciliação e falhas prolongadas pendentes |
| T-014 | Migracao | Importador e testes sinteticos existentes; versoes/mapeamento real e reconciliacao pendentes |
| T-015 | Journal e replay | PASS Journal 20 e Integration 44: bytes, hash, conflitos, falha de disco simulada, estado corrompido, restart/replay e backup/restauracao; RED 14cf669 |
| T-016 | Envelope de captura | PASS Capture 29, Journal 23 e Integration 56: origem/UTC/hash, retransmissao distinta, estados, falha de envelope bloqueia Store, replay e backup/restauracao |
| T-017 | Supervisao | PASS Monitoring 23: espaco inclusive limite igual, pending antigo, captura SG3, integridade, capture/raw ausente/orfao, relogio futuro e CLI |
| T-018 | Retencao | PASS Retention 17 e Integration 60: previa sem mutacao, backup real/manifesto BOM, apply, pending/captured, familias compartilhadas, lock, divergencia, backup fora do data dir e restauracao |
| T-019 | Pacote Server 2008 R2 r2 | PASS local: verificador de ambiente, ZIP/hash interno e externo, carga de assemblies, ausencia de testes/dados privados e health extraido; execucao no servidor pendente |
| T-020 | Transporte SG3 | PASS 21: plain/B32, DC4, fragmentacao, multiplos frames, limite, parcial recusado, socket local, persistencia antes do ACK e ausencia de ACK em falha de disco |

Dados sinteticos em diretorios isolados. Captura final desktop inspecionada: tools/validation/desktop-completed.png. Testes nao substituem homologacao do receptor ou SO minimo. Cobertura minima solicitada de 80% ainda nao medida; nao declarar atingida.
