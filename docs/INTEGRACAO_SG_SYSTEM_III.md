# Integração SG-System III

Estado: transporte e parser estrutural validados com captura real; serviço contínuo e conversão em ocorrência ainda não implementados.

## Evidencia de rede no inventario (revisada em 2026-09-16)

O snapshot `Inventario/05_Conexoes.txt:40` registra TCP estabelecido de `192.168.1.250:49178` para `192.168.1.249:1025`, PID 3792. `Inventario/06_Processos_Servicos.txt:71` identifica esse PID como `daemon1.exe` do BYKOM.

Assim, o endpoint historico da conexao BYKOM com o SG3 e **192.168.1.249:1025/TCP**. O servidor Windows aparece como **192.168.1.250**. A porta local 49178 e a porta de origem observada, nao uma porta fixa a configurar.

A combinacao de porta de origem alta e destino 1025 indica BYKOM como cliente TCP e SG3 como servidor. E uma inferencia forte do snapshot; nao foi capturado handshake SYN nem confirmada configuracao explicita de papel cliente/servidor.

Separadamente, `05_Conexoes.txt:37-38` registra conexoes para o mesmo SG3 nas portas 1027 e 1024, PID 1116. `06_Processos_Servicos.txt:55` identifica `SGC-WinService.exe` / `SGC-Server-V2.1`. Portanto, essas conexoes pertencem ao Console, nao ao daemon BYKOM.

Esses dados sao do inventario, nao de uma verificacao ao vivo. O protocolo de payload, framing, ACK, heartbeat e retransmissao continuam pendentes. Nenhuma conexao foi aberta ao equipamento nesta analise.

Nota de inspecao: as pastas SG_Dados/SG_Programa presentes nao forneceram arquivos na enumeracao local. Server.rar lista dois arquivos receiver.xml, mas a tentativa de leitura de um deles nao produziu XML valido; nao foi usado para confirmar parametros. SISTEMA.INI do Daemon1 contem outro IP, sem evidencia suficiente para atribui-lo ao SG3. A conclusao acima usa a correlacao verificavel entre conexoes e processos.

Necessário obter: fabricante/modelo/firmware, manual autorizado do protocolo, porta/interface serial ou TCP/IP, configuração do receptor e software atual, parâmetros e amostras autorizadas. Não presumir SIA, Contact ID, formato, enquadramento, ACK, heartbeat ou timeout.

O simulador exercita persistência e atendimento com protocolo interno explicitamente próprio. Não é emulador homologado do SG-System III e não deve enviar sinais à rede do receptor.

Captura controlada implementada em `src/Receiver/Sg3Transport.cs`: cliente TCP, parser incremental plain/B32, mensagens parciais/múltiplas, journal e envelope antes do ACK e falha sem ACK quando a persistência não conclui. `src/Receiver/Sg3Parser.cs` reconhece as três famílias presentes na captura real e `--sg3-analyze` valida envelopes sem exibir dados. Resultado: 230/230 capturas e 54/54 payloads únicos válidos. Conversão semântica em ocorrência, reconexão contínua, retransmissões, fila durante falha de banco e alarme de comunicação continuam pendentes.

ACK técnico difere de assumir ocorrência pelo operador. UI fechada não pode parar serviço. Segundo consumidor em produção depende de validação explícita do comportamento do receptor.

## Extracao de campos observados (2026-09-18)

NumericFields fornece Account (quatro digitos, zeros preservados), EventCode (tres), Field2 (dois) e Field3 (tres) somente para o layout observado 501001/18. Qualifier continua literal E/R. Outros layouts retornam NumericFields null sem perder o parse estrutural. Os campos originais Receiver/Signal continuam disponiveis; o journal conserva os bytes.

Confronto com Printer sustenta segmentacao textual, nao identifica cada evento nem prova significado dos sufixos. Nao deduplicar por hash ou Sequence; nao gerar ocorrencias automaticamente. Fixtures usam contas sinteticas. Capturas e logs reais permanecem privados em Inventario.

Classificacao offline v1 implementada: veja [tabela e limites](CLASSIFICACAO_SG3.md). O analisador mostra categorias por captura; isso nao implementa politica de ocorrencias.
