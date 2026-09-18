# Teste real SG-System III

Endpoint historico confirmado no inventario: SG3 `192.168.1.249:1025/TCP`; o cliente anterior era `daemon1.exe` no servidor `192.168.1.250`.

## Antes da janela

1. No SG-System Console, registre o valor de **B32 Headers** da automacao CPM3: use `plain` quando desabilitado e `b32` quando habilitado.
2. Confirme com a operacao que o BYKOM/daemon1 pode permanecer parado durante a janela.
3. Pare `daemon1.exe` e o servico `VisepReceiver`. Nao encerre processos de console/printer das portas 1024/1027.
4. Confirme no Printer Log que os eventos continuam visiveis e mantenha operador acompanhando a janela.

O script recusa executar se encontrar outra conexao estabelecida para `192.168.1.249:1025`. Ele nao altera configuracao do SG3.

## Executar no servidor antigo do BYKOM

Abra Prompt de Comando como administrador dentro do pacote extraido:

```cmd
scripts\Testar-SG3.cmd plain 90
```

Troque `plain` por `b32` somente conforme o valor conferido no CPM3. Durante ate 90 segundos, o cliente recebe frames, grava cada payload em `data\sg3-capture\inbox\journal`, grava o envelope em `captures` e somente entao envia ACK. O conteúdo do sinal nao aparece no console e ainda nao vira ocorrencia.

Resultado esperado: pelo menos um heartbeat ou evento, `frames` maior que zero e codigo de saida 0. Codigo 2 significa conexao sem frame; 1 indica falha; 3 indica consumidor ja conectado. O resumo fica em `resultados-sg3\sg3-AAAAMMDD-HHMMSS.txt`.

## Encerrar e restaurar

O processo fecha a conexao ao terminar. Verifique o estado de automacao no SG3 e o Printer Log. Depois restaure apenas o consumidor definido pela operacao. Preserve a pasta `data\sg3-capture` para implementar e validar o parser sem reconectar ao equipamento.

O modo de captura reconhece frames terminados em DC4 e ACK `0x06`; em B32, reconhece o prefixo decimal de quatro bytes e envia `0005` seguido de `0x06`. Esses parâmetros devem corresponder à configuração conferida no CPM3.

## Análise offline

Em build que contém o parser v1, analise uma captura preservada sem imprimir payloads:

```cmd
"C:\Program Files\VISEP\Visep.Receiver.exe" --sg3-analyze "C:\ProgramData\Visep\sg3-active-test\data.xml"
```

O resumo separa capturas, payloads únicos e as três famílias observadas. Resultado real de 2026-09-17: 230 capturas válidas, 54 payloads únicos válidos, 2 keepalives, 59 eventos entre colchetes, 169 eventos numéricos e zero inválidos. Classificação estrutural não autoriza criar ocorrências antes da reconciliação semântica com o Printer Log.
