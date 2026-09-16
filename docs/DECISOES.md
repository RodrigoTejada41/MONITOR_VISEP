# Decisões

## ADR-001 — Desktop .NET Framework4.7.2
Contexto: alvo obrigatório Server2008 R2, toolchain local com reference assemblies 4.7.2. Opções: .NET moderno (não atende alvo), 4.6.2 (sem targeting pack local), 4.7.2 (disponível). Decisão: WinForms/Framework4.7.2 e referências explícitas /nostdlib. Consequência: build local viável; homologação no SO mínimo continua pendente. Fonte oficial em COMPATIBILIDADE_WINDOWS.md.

## ADR-002 — Persistência demonstrável XML
Contexto: banco/conector compatíveis e esquema legado desconhecidos. Opções: aguardar banco; usar armazenamento próprio limitado. Decisão: repositório XML transacional por processo/máquina para demonstrar fluxo, sem dependência externa. Consequência: não satisfaz banco relacional/migrações SQL nem estações LAN; substituir por backend multiusuário compatível antes de operação real. Arquivo precisa ACL, backup e teste de concorrência; não colocá-lo em compartilhamento SMB.

## ADR-003 — Simulação explícita
Contexto: protocolo do receptor ausente. Decisão: adaptador interno de simulação sem fingir compatibilidade SG. Consequência: valida domínio e persistência; ACK e homologação real permanecem bloqueados.

## ADR-004 — Legado somente leitura e isolado
Contexto: plano menciona backup não localizado. Decisão: ferramenta de inspeção em fluxo sem execução SQL; mapeamento apenas após evidência. Consequência: não há importação real nesta etapa; nenhuma tabela origem inventada.

Atualização 2026-09-16: backup localizado e hashes conferidos; inventário textual identifica 173 tabelas. A decisão de não executar SQL permanece; mapeamento preliminar não equivale a migração homologada.
