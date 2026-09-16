# Teste com o backup BYKOM

## Abrir

Execute `scripts/Abrir-Teste-BYKOM.cmd`. Ele inicia o receptor de simulacao e a interface usando `data/bykom-teste/data.xml`; fecha o receptor quando a interface termina. Nao instala servico.

No primeiro acesso, crie seu usuario administrador e uma senha de 12 a 256 caracteres, repetida na confirmacao. Nao existe senha padrao nem senha importada do BYKOM.

1. Clientes: clique **Carregar clientes** para ver os cadastros importados.
2. Historico BYKOM: clique **Carregar historico**. A consulta mostra uma amostra dos 1.000 eventos mais recentes pela data textual de origem; filtros por conta, cliente e codigo. Datas permanecem no horario original sem conversao de fuso.
3. Simulador: copie a conta `BYKOM-...` da aba Clientes; informe codigo, zona e particao. Envie a simulacao.
4. Ocorrencias: atualize, selecione a ocorrencia, assuma, registre acao e encerre com justificativa.

Historicos importados nunca entram na fila de alarmes ativos. O campo Account usa `BYKOM-ORDER_ID` para evitar colisao entre receptores/particoes; identificadores originais estao em Equipment. Essa conta e exclusiva do teste e nao e mapeamento homologado do receptor real.

## O que foi mapeado

- Clientes: `abmacodigos.ORDER_ID/NOMBRE`; sem excluir automaticamente registros por flags de status desconhecidas.
- Enderecos: `calles` e `ciudad` por chaves do cadastro, numero/piso/departamento.
- Contatos: `abrltelefonos.ORDER_RL -> abmacodigos.ORDER_ID`, pessoa via `CODIGO_ID -> tlmapersonas.ORDER_ID`, telefone via `tlrlpersonas.ORDER_RL`.
- Zonas/sensores: `abrlzonas` e `abrlsensores` por `ORDER_RL`.
- Historico: amostra de `evmahistorico`, com codigo de origem, zona e data textual; vinculo do historico por `ORDER_RL` avaliado por correspondencia de IDs, ainda sujeito a validacao semantica.

Fotos, operadores/senhas, credenciais de cameras, detalhe livre dos eventos, rotinas SQL e demais tabelas nao sao importados. Nenhum comando SQL e executado. A base XML local nao oferece multiestacao.

## Recriar em outro diretorio

```powershell
.\scripts\Import-Legacy.ps1 -Destination .\data\bykom-teste-02 -AllowControlSeparator
.\scripts\Abrir-Teste-BYKOM.ps1 -DataFile .\data\bykom-teste-02\data.xml
```

Requer Python 3.11+ na maquina de preparacao. O aplicativo continua em .NET Framework 4.7.2. Destino existente e recusado, preservando usuarios e testes anteriores. Para desfazer, feche a interface e arquive a pasta de teste; o SQL original permanece intacto.

O dump declara latin1. `AllowControlSeparator` permite exclusivamente o artefato 0x05 entre tuplas encontrado neste backup; e desabilitado por padrao e contabilizado em `import-report.json`. Nao altera bytes dentro de strings nem regrava o SQL original.

O relatorio registra hash origem/destino, versao do mapeamento, contagens por tabela, eventos lidos versus amostra, relacoes orfas e ajustes de texto XML. Restricao ACL e aplicada antes de escrever dados pessoais. Nao compartilhar a pasta nem o XML publicamente.
