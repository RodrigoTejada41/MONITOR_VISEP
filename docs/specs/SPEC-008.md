# SPEC-008 — Usuários permissões auditoria

Versão: 0.1. Estado: RASCUNHO.
Responsável: seguranca. Revisor: revisor.

## Objetivo e limites
Autenticação antes da UI; perfis e auditoria em operações. Não inclui alegação de produção pronta.

## Evidências disponíveis
Planos fornecidos e inspeção inicial de projeto vazio. Compatibilidade documental em COMPATIBILIDADE_WINDOWS.md. Implementação e testes ainda não constituem evidência nesta revisão inicial.

## Requisitos funcionais e não funcionais
Autenticação antes da UI; perfis e auditoria em operações. Operação local, entradas validadas, erros explícitos, preservação de dados e separação de responsabilidades.

## Regras de negócio
Não gravar no BYKOM. Não confundir evento, ocorrência, atendimento e ACK. Nenhum fluxo marcado real quando simulado. Autorizar ações na aplicação.

## Dados e interfaces
Modelos próprios e contratos descritos em ARQUITETURA.md. Nomes/campos legados somente após inspeção autorizada. Arquivos de responsabilidade e vínculo com código em BACKLOG.md e RASTREABILIDADE.md.

## Cenários de falha
Senha exposta ou escalada de privilégio. Registrar erro sem dados pessoais e preservar estado anterior válido.

## Critérios de aceite verificáveis
Senha derivada e ações não autorizadas negadas. Registrar comando, resultado e limitações. Não concluir a spec enquanto critérios dependentes de ambiente estiverem pendentes.

## Estratégia de teste
Dados sintéticos em armazenamento isolado; cenários positivos e negativos do PLANO_TESTES.md; revisão de outro autor quando disponível.

## Dependências, dúvidas e bloqueios
Contrato de sessão. Ausências externas não impedem o demonstrador independente.

Fluxo permitido: RASCUNHO → PRONTA PARA IMPLEMENTAÇÃO → EM IMPLEMENTAÇÃO → EM VALIDAÇÃO → CONCLUÍDA.
