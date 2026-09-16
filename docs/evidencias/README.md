# Evidencias e preservacao dos insumos

Revisao 2026-09-16. Repositorio publico; contexto tecnico preservado sem dados de clientes ou configuracoes secretas.

## Conexao historica SG3

Trechos selecionados do inventario local; demais conexoes/processos nao publicados.

| Fonte local | Linha | Evidencia |
|---|---|---|
| Inventario/05_Conexoes.txt | 40 | TCP 192.168.1.250:49178 -> 192.168.1.249:1025 ESTABLISHED PID 3792 |
| Inventario/06_Processos_Servicos.txt | 71 | daemon1.exe PID 3792 |
| Inventario/05_Conexoes.txt | 37 | TCP 192.168.1.250:49157 -> 192.168.1.249:1027 ESTABLISHED PID 1116 |
| Inventario/05_Conexoes.txt | 38 | TCP 192.168.1.250:49158 -> 192.168.1.249:1024 ESTABLISHED PID 1116 |
| Inventario/06_Processos_Servicos.txt | 55 | SGC-WinService.exe PID 1116 SGC-Server-V2.1 |

Conclusao: daemon BYKOM usava 192.168.1.249:1025/TCP. Papel cliente BYKOM inferido pela origem efemera/destino fixo; sem handshake SYN. Usuario confirmou Windows Server 2008 R2, rede e eventos novos no Printer Log. Nenhuma verificacao ao vivo feita pela IA.

## Conteudo preservado

| Grupo | Destino | Limite |
|---|---|---|
| Codigo, scripts, testes, specs e decisoes | Git | Sem driver SG3 real |
| Documento SG3 e modelo MIT | Inventario no Git | Referencias; nao comprovam compatibilidade |
| Resultados e comandos | docs/RETOMADA.md e specs | Distinguir historico de reexecucao |
| Contratos uteis antes apenas em tools | docs/ARQUITETURA.md | Consolidacao; notas temporarias ficam locais |
| Dump/configuracoes/planilhas/clientes | Inventario e tools privados | Nao enviados ao GitHub publico |
| Integridade de insumos principais | manifesto.json | SHA-256, tamanho e caminho; sem dados dos registros |
| Binarios de build | build local | Recriar com scripts/Build.ps1 |

O manifesto registra insumos principais e contagens agregadas das pastas privadas. Nao lista nomes de arquivos de clientes nem logs integrais. Nao e backup completo. Nenhum original privado foi apagado ou alterado.

## Retomada em outra maquina

1. Clonar e ler docs/RETOMADA.md, docs/ORGANOGRAMA.md e docs/specs/README.md.
2. Compilar e executar testes sinteticos pelos comandos do README.
3. Para legado real, obter copia privada e comparar tamanho/SHA-256 dos insumos; nao executar o SQL.
4. Antes de importar, verificar destino com data.xml e import-report.json; nao sobrescrever usuarios/testes.
5. Para SG3 real, obter protocolo/capturas autorizadas; nao presumir DC-09.

SG3 disponivel em producao nao comprova laboratorio isolado. Integracao real exige testes de framing, ACK, falhas, retransmissao e recuperacao.

## Verificacao desta publicacao

Links locais Markdown verificados sem destinos ausentes. Diff dos documentos proprios sem erros de whitespace; modelo externo preservado com whitespace original. Inspecao de candidatos nao encontrou dumps/configuracoes privadas no staging. Segredos do modelo sao placeholders/referencias a secrets, e a chave no README e explicitamente exemplo. Nao foi executado o modelo nem instalada sua cadeia de dependencias. A revisao independente de publicacao foi interrompida por limite de uso; verificacoes finais realizadas pelo coordenador, sem alegacao de auditoria independente concluida.
