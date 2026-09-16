# SPEC-003 — Banco próprio e legado

Versão: 0.2. Estado: EM IMPLEMENTAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de banco próprio e legado com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

XML schema 1, lock local, Flush(true), substituição de arquivo e backup. Importador SQL existe, não executa SQL e gera XML com relatório, hashes, contagens e exclusões. Testes Python e LegacyHistory passaram.

## Critérios de aceite pendentes e riscos

Reconciliar entidades e rejeições da migração final. Definir banco/conector e provar transações com duas estações. Importação com ImportMode=BYKOM_TEST usa contas BYKOM-ORDER_ID e amostra padrão de 1000 eventos; fuso não confirmado. XML carrega/regrava o estado inteiro e limita leitura a 64 * 1024 * 1024 caracteres; o importador limita o payload a 60 MiB.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

src/Migration/import_legacy.py; src/Migration/sql_dump.py; src/Core/XmlRepository.cs; docs/MAPEAMENTO_MIGRACAO.md.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
