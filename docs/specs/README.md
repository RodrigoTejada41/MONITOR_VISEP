# Matriz de especificações

Revisão 2026-09-16, versão 0.2. Substitui rascunhos genéricos que descreviam projeto vazio. Revisão confrontada com src, scripts, tests e docs/RETOMADA.md.

**Resultado: 5 specs em implementação, 6 em validação, nenhuma concluída.** A base local existe; produção continua dependente da integração SG3, banco servidor e homologação operacional.

| Spec | Escopo | Estado |
|---|---|---|
| [SPEC-001](SPEC-001.md) | Ambiente e compatibilidade Windows | EM VALIDAÇÃO |
| [SPEC-002](SPEC-002.md) | Arquitetura e interfaces | EM IMPLEMENTAÇÃO |
| [SPEC-003](SPEC-003.md) | Banco próprio e legado | EM IMPLEMENTAÇÃO |
| [SPEC-004](SPEC-004.md) | Cadastros | EM IMPLEMENTAÇÃO |
| [SPEC-005](SPEC-005.md) | Recepção e persistência | EM IMPLEMENTAÇÃO |
| [SPEC-006](SPEC-006.md) | Integração SG-System III | EM IMPLEMENTAÇÃO |
| [SPEC-007](SPEC-007.md) | Atendimento de ocorrências | EM VALIDAÇÃO |
| [SPEC-008](SPEC-008.md) | Usuários, permissões e auditoria | EM VALIDAÇÃO |
| [SPEC-009](SPEC-009.md) | Relatórios e exportações | EM VALIDAÇÃO |
| [SPEC-010](SPEC-010.md) | Instalação, atualização e recuperação | EM VALIDAÇÃO |
| [SPEC-011](SPEC-011.md) | Testes e validação operacional | EM VALIDAÇÃO |

## Evidência de validação

| Comando/suíte | Evidência neste checkpoint | Limite |
|---|---|---|
| scripts/Build.ps1 -Test | PASS: 46 assertions Core + 27 LegacyHistory | Build local, não Server 2008 R2 |
| python -m unittest discover -s tests -p 'test_*.py' | PASS: 16 testes | Importação/parsing sintéticos |
| tests/IntegrationTests.ps1 | Histórico: 32 assertions | Não reexecutado nesta revisão |
| tests/DesktopE2E.py | Histórico: fluxo completo simulado | Não reexecutado nesta revisão |
| tests/ImportedDesktopE2E.py | Arquivo existente | Execução não comprovada nesta revisão |
| Cobertura de código | Não medida | Meta 80% ainda não demonstrada |
| SG3 real / instalação SCM / VM alvo | Pendente | Sem homologação |

## Regras de atualização

- EM IMPLEMENTAÇÃO: partes do escopo funcional ainda ausentes.
- EM VALIDAÇÃO: base implementada, com verificações ou aceite pendentes.
- CONCLUÍDA: todos os critérios aplicáveis demonstrados; nenhum item neste estado hoje.
- Diferenciar evidência atual, histórica e inferência. Não transformar teste local em homologação de produção.
- A quantidade de testes não mede percentual concluído nem cobertura.

## Ordem de continuidade

1. Confirmar protocolo SG3 usando manual/captura autorizada; endpoint histórico já identificado, não pedir IP/porta novamente.
2. Implementar journal, replay, separação evento/ocorrência e contratos do driver.
3. Implementar/homologar cliente TCP em laboratório; comparar com Printer Log.
4. Definir banco servidor, completar cadastros e reconciliar migração sem sobrescrever originais.
5. Medir cobertura; validar instalação, carga e recuperação no ambiente escolhido.

Consulte [retomada](../RETOMADA.md), [integração SG3](../INTEGRACAO_SG_SYSTEM_III.md) e [plano de testes](../PLANO_TESTES.md).
