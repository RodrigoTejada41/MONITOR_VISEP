# Plano de testes

Atualizacao 2026-09-16. Comandos e contagens finais em RETOMADA.md.

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
| T-013 | SG real e falhas | BLOQUEADO: protocolo/receptor |
| T-014 | Migracao | BLOQUEADO: versoes e mapeamento validado/importador; backup disponivel |

Dados sinteticos em diretorios isolados. Captura final desktop inspecionada: tools/validation/desktop-completed.png. Testes nao substituem homologacao do receptor ou SO minimo. Cobertura minima solicitada de 80% ainda nao medida; nao declarar atingida.
