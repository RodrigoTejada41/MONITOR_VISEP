# SPEC-011 — Testes e validação operacional

Versão: 0.1. Estado: RASCUNHO.
Responsável: qualidade. Revisor: revisor.

## Objetivo e limites
Unidade, integração e fluxo desktop baseado em risco. Não inclui alegação de produção pronta.

## Evidências disponíveis
Planos fornecidos e inspeção inicial de projeto vazio. Compatibilidade documental em COMPATIBILIDADE_WINDOWS.md. Implementação e testes ainda não constituem evidência nesta revisão inicial.

## Requisitos funcionais e não funcionais
Unidade, integração e fluxo desktop baseado em risco. Operação local, entradas validadas, erros explícitos, preservação de dados e separação de responsabilidades.

## Regras de negócio
Não gravar no BYKOM. Não confundir evento, ocorrência, atendimento e ACK. Nenhum fluxo marcado real quando simulado. Autorizar ações na aplicação.

## Dados e interfaces
Modelos próprios e contratos descritos em ARQUITETURA.md. Nomes/campos legados somente após inspeção autorizada. Arquivos de responsabilidade e vínculo com código em BACKLOG.md e RASTREABILIDADE.md.

## Cenários de falha
Teste verde sem homologação real. Registrar erro sem dados pessoais e preservar estado anterior válido.

## Critérios de aceite verificáveis
Evidências e pendências explícitas; medir cobertura. Registrar comando, resultado e limitações. Não concluir a spec enquanto critérios dependentes de ambiente estiverem pendentes.

## Estratégia de teste
Dados sintéticos em armazenamento isolado; cenários positivos e negativos do PLANO_TESTES.md; revisão de outro autor quando disponível.

## Dependências, dúvidas e bloqueios
Implementação integrada e VM. Ausências externas não impedem o demonstrador independente.

Fluxo permitido: RASCUNHO → PRONTA PARA IMPLEMENTAÇÃO → EM IMPLEMENTAÇÃO → EM VALIDAÇÃO → CONCLUÍDA.

## Atualizacao 2026-09-16
Estado atual: EM VALIDACAO. Core, integracao PowerShell 5 e E2E desktop passaram; comandos/resultados em RETOMADA.md. Cobertura percentual nao medida e homologacao de SO/receptor reais pendentes. Esta atualizacao substitui a ausencia de evidencia descrita na revisao inicial.
