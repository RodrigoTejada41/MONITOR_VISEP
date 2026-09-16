# SPEC-001 — Ambiente e compatibilidade Windows

Versão: 0.1. Estado: RASCUNHO.
Responsável: arquitetura. Revisor: qualidade.

## Objetivo e limites
Build Framework4.7.2 com referências explícitas e inventário de SO. Não inclui alegação de produção pronta.

## Evidências disponíveis
Planos fornecidos e inspeção inicial de projeto vazio. Compatibilidade documental em COMPATIBILIDADE_WINDOWS.md. Implementação e testes ainda não constituem evidência nesta revisão inicial.

## Requisitos funcionais e não funcionais
Build Framework4.7.2 com referências explícitas e inventário de SO. Operação local, entradas validadas, erros explícitos, preservação de dados e separação de responsabilidades.

## Regras de negócio
Não gravar no BYKOM. Não confundir evento, ocorrência, atendimento e ACK. Nenhum fluxo marcado real quando simulado. Autorizar ações na aplicação.

## Dados e interfaces
Modelos próprios e contratos descritos em ARQUITETURA.md. Nomes/campos legados somente após inspeção autorizada. Arquivos de responsabilidade e vínculo com código em BACKLOG.md e RASTREABILIDADE.md.

## Cenários de falha
Incompatibilidade runtime/instalador. Registrar erro sem dados pessoais e preservar estado anterior válido.

## Critérios de aceite verificáveis
Compilar e executar no SO mínimo. Registrar comando, resultado e limitações. Não concluir a spec enquanto critérios dependentes de ambiente estiverem pendentes.

## Estratégia de teste
Dados sintéticos em armazenamento isolado; cenários positivos e negativos do PLANO_TESTES.md; revisão de outro autor quando disponível.

## Dependências, dúvidas e bloqueios
VM Server2008 R2 SP1. Ausências externas não impedem o demonstrador independente.

Fluxo permitido: RASCUNHO → PRONTA PARA IMPLEMENTAÇÃO → EM IMPLEMENTAÇÃO → EM VALIDAÇÃO → CONCLUÍDA.
