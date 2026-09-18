# Backlog

Atualizado em 2026-09-17. Resultados e comandos em RETOMADA.md.

| ID | Escopo | Estado | Evidencia / pendencia |
|---|---|---|---|
| TASK-001 | Documentacao 001-011 | EM VALIDACAO | Specs v0.2 revisadas: 5 em implementacao, 6 em validacao; nenhuma concluida; matriz em specs/README.md |
| TASK-002 | Dominio/persistencia/autorizacao | EM VALIDACAO | src/Core, testes CoreTests; XML somente demonstrador |
| TASK-003 | Desktop | EM VALIDACAO | Fluxo completo DesktopE2E passou; inspecao visual da ocorrencia encerrada |
| TASK-004 | Recepcao simulada | EM VALIDACAO | Console, dedup e quarentena passaram; instalacao SCM pendente |
| TASK-005 | Build/scripts/inspector | EM VALIDACAO | Build e 32 assertions integradas passaram; instalador nao homologado |
| TASK-006 | Validacao integrada | EM VALIDACAO | Core, integracao e E2E executados; percentual de cobertura nao medido |
| TASK-007 | Manual e checkpoint | CONCLUIDA | OPERACAO.md, RETOMADA.md e matrizes atualizados nesta rodada |
| TASK-008 | Banco servidor/migracao | BLOQUEADO | Backup disponivel; importador inicial existe; falta fixar versoes, validar semantica e reconciliacao |
| TASK-009 | SG real | EM VALIDACAO | Primary Active/TCP; 230 frames/5383 bytes. Parser estrutural: 230/230 capturas e 54/54 raws válidos, 0 inválidos. Faltam semântica/reconciliação de alarme conhecido e serviço contínuo |
| TASK-010 | SO minimo | BLOQUEADO | VM Server 2008 R2 SP1 ausente |
| TASK-011 | Preservacao tecnica e organograma | CONCLUIDA | Organograma de modulos, evidencias selecionadas, manifesto e referencias publicaveis preparados; insumos privados preservados localmente |
| TASK-012 | Journal local e replay de simulacao | CONCLUIDA | RED 14cf669; Journal 20 e Integration 44 passaram na etapa inicial. Envelope evoluiu na TASK-013; retencao e SG3 seguem pendentes na SPEC-005 |
| TASK-013 | Envelope local de captura | CONCLUIDA | Origem/UTC/hash/estado por tentativa; Capture 29, Journal 23 e Integration 56 passaram. Retencao/supervisao evoluiram na TASK-014; eventos reais pendentes |
| TASK-014 | Supervisao e retencao segura | CONCLUIDA | Health read-only e retencao explicita com backup/lock; Monitoring 22, Retention 16 e Integration 60 passaram. Sem limpeza automatica |
| TASK-015 | Pacote de teste Server 2008 R2 | EM VALIDACAO | r8 validou análise, ambiente e atendimento simulado no Server 2008 R2/PS2. Instalador único r10, atualização preservando base e serviço SG3 contínuo foram validados localmente; execução r10/SCM/SG3 contínuo no servidor pendente |

Aprovacao do demonstrador nao conclui specs de producao, migracao ou receptor real.

## Prioridade ao retomar - 2026-09-18

1. Testar r8-menu no servidor: analise preservada, ambiente, controles de servico/interface; captura ao vivo somente em janela acompanhada.
2. TASK-015: pacote r8 pronto e validado localmente, homologacao Server2008R2/PS2 pendente.
3. TASK-009: parser e classificacao offline implementados; ocorrencias, deduplicacao, reconexao e servico continuo pendentes. Classificacao nao equivale a homologacao.
4. Reinicio Windows: pedido ambiguo, nao implementado. Menu reinicia apenas VisepReceiver e interface VISEP.
