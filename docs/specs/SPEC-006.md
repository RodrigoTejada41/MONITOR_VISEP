# SPEC-006 — Integração SG-System III

Versão: 0.1. Estado: RASCUNHO.
Responsável: protocolos. Revisor: revisor.

## Objetivo e limites
Adaptador interno SIMULAÇÃO; real condicionado ao manual. Não inclui alegação de produção pronta.

## Evidências disponíveis
Planos fornecidos e inspeção inicial de projeto vazio. Compatibilidade documental em COMPATIBILIDADE_WINDOWS.md. Implementação e testes ainda não constituem evidência nesta revisão inicial.

## Requisitos funcionais e não funcionais
Adaptador interno SIMULAÇÃO; real condicionado ao manual. Operação local, entradas validadas, erros explícitos, preservação de dados e separação de responsabilidades.

## Regras de negócio
Não gravar no BYKOM. Não confundir evento, ocorrência, atendimento e ACK. Nenhum fluxo marcado real quando simulado. Autorizar ações na aplicação.

## Dados e interfaces
Modelos próprios e contratos descritos em ARQUITETURA.md. Nomes/campos legados somente após inspeção autorizada. Arquivos de responsabilidade e vínculo com código em BACKLOG.md e RASTREABILIDADE.md.

## Cenários de falha
Frames parciais, retransmissões e ACK incorreto. Registrar erro sem dados pessoais e preservar estado anterior válido.

## Critérios de aceite verificáveis
Homologação documentada antes de ativar adaptador real. Registrar comando, resultado e limitações. Não concluir a spec enquanto critérios dependentes de ambiente estiverem pendentes.

## Estratégia de teste
Dados sintéticos em armazenamento isolado; cenários positivos e negativos do PLANO_TESTES.md; revisão de outro autor quando disponível.

## Dependências, dúvidas e bloqueios
Manual, interface e amostras. Ausências externas não impedem o demonstrador independente.

Fluxo permitido: RASCUNHO → PRONTA PARA IMPLEMENTAÇÃO → EM IMPLEMENTAÇÃO → EM VALIDAÇÃO → CONCLUÍDA.
