# Mapa do banco legado

Estado: backup disponível; inventário textual de estrutura realizado. Restauração e migração não validadas.

Verificação em 2026-09-16: `Inventario/bykom.sql` e `tools/legacy/extracted/bykom.sql` existem, ambos com 663.570.312 bytes e SHA-256 recalculado `FC375F7D2DB0411072B5580E5CBB5BE4F34FD931C63A76BEC1BC33E14AE6712F`, igual aos registros `tools/legacy/hashes.json` e `tools/legacy/extracted-hash.json`. O registro do RAR original e da cópia aponta SHA-256 `690BDD34A1ABC5DDAC11AFDAFE00D9CA58FDCB53CBA4D4C6E75C387E0AEE6075`; esse hash do RAR não foi recalculado nesta verificação.

Evidência estrutural: `tools/legacy/structure.json`, produzido por `tools/legacy/inspect_structure.py`. O inventário contém 173 tabelas (172 InnoDB e 1 MyISAM), colunas e linhas de índices/constraints, incluindo 178 linhas com `FOREIGN KEY`. Detectou charsets `latin1` e `utf8` e collation `utf8_spanish_ci`. A versão do servidor não foi identificada.

| Detecção textual | Contagem |
|---|---:|
| TRIGGER | 140 |
| PROCEDURE | 66 |
| FUNCTION | 1 |
| EVENT | 1 |
| Linhas com DEFINER | 274 |
| USE / DROP / CALL | 1 / 31 / 41 |
| UPDATE / DELETE | 122 / 22 |
| Linhas iniciadas por INSERT ou REPLACE | 9.082 |

Essas contagens são ocorrências reconhecidas pelo script, não objetos únicos ou número de registros. Ausência de uma categoria, como VIEW, não comprova ausência no SQL. O script usa expressões regulares, considera apenas os primeiros 65.536 bytes de cada linha e interpreta texto como latin1; pode perder comandos multilinha, qualificadores de tipos e outras construções. A leitura é por linha, portanto linhas muito grandes ainda podem consumir memória. O inventário não comprova integridade referencial, compatibilidade com uma versão de MySQL ou segurança de execução.

Tabelas presentes relevantes para investigação: `abmacodigos`, `abrltelefonos`, `abrlsensores`, `abrlzonas`, `evmahistorico` e `evrlhistorico`. A semântica de negócio e os relacionamentos inferidos ainda precisam ser validados; linhas de FK detectadas não equivalem a relacionamentos testados.

Próximos passos: revisar SQL administrativo, rotinas e DEFINER; confirmar versão/compatibilidade, chaves, cardinalidades e encoding; preparar restauração em instância isolada. Não executar o SQL durante análise textual nem registrar INSERTs, dados pessoais ou segredos. O script `inspect_artifacts.py` inspeciona artefatos auxiliares e não valida a migração do banco.
