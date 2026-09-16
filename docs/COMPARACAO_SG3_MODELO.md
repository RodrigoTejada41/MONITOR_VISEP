# Comparacao do documento SG3 com o projeto e o modelo

Data: 2026-09-16. Analise estatica dos arquivos locais. Nenhuma conexao ao equipamento, alteracao operacional ou homologacao de protocolo realizada.

Fontes: `Inventario/Documentacao_Integracao_SurGard_SG3_Substituicao_BYKOM.docx` (v0.1) e `Inventario/Modelo/ioBroker.sia-master` (package.json 2.0.4).

## Resultado

O documento e compativel com a direcao do projeto: manter o SG3 e substituir a automacao BYKOM. A base C#/WinForms pode ser aproveitada, mas o receptor atual e apenas um consumidor de arquivos XML de simulacao. O modelo ioBroker implementa SIA DC-09; nao demonstra compatibilidade com a interface de automacao CPM3.

## Comparacao

| Requisito do documento | Evidencia atual | Avaliacao |
|---|---|---|
| Driver separado da interface | `src/Receiver/ReceiverProgram.cs`: console e ServiceBase | Estrutura aproveitavel; falta transporte TCP/serial SG3 |
| Registro bruto de todas as mensagens | `src/Core/Store.cs:180`: ReceiveSimulation monta Raw como SIMULATION e campos concatenados | Nao conserva bytes recebidos nem mensagens de controle; criar journal proprio |
| Framing, heartbeat e ACK | Receiver processa XML da pasta inbox | Ausentes para SG3 |
| Fila duravel | inbox, processed e quarantine no Receiver | Base conceitual de retentativa; nao existe fila de captura real independente do banco |
| Idempotencia | Store compara MessageId | Funciona para identificador de simulacao; criterio real depende do protocolo |
| Parser versionado | Nao existe parser SG3 | Criar modulo e fixtures separadas por versao/formato |
| Modelo de evento | `src/Core/Models.cs`: Incident possui conta, codigo, zona, particao, Raw e UTC | Faltam receptor/linha, protocolo, qualificador, zona versus usuario, versao do parser e estado de entrega/ACK |
| Evento separado de atendimento | ReceiveSimulation cria Incident diretamente | Separar evento tecnico de ocorrencia operacional; teste periodico e restauracao precisam de regras proprias |
| Atendimento e auditoria | Store Claim, AddAction, Close e Audit; Desktop atualiza a cada 3 segundos | Reaproveitar regras e UI; auditoria no XML nao e inviolavel contra alteracao do arquivo |
| Independencia da UI | Receptor executavel separado | Estrutura existe; launcher de teste encerra receptor ao fechar UI, conforme TESTE_BYKOM.md |
| Persistencia | XmlRepository usa lock, Flush(true) e substituicao atomica | Adequada ao demonstrador; carrega/regrava documento inteiro, sem escala multiestacao |
| Monitoramento | Sem estado SG3, heartbeat ou metricas de fila | Implementar alarmes de desconexao, atraso e armazenamento |
| Migracao BYKOM | import_legacy.py, sql_dump.py e TESTE_BYKOM.md | Reaproveitar importacao isolada; contas BYKOM-ORDER_ID nao sao contas homologadas do receptor |
| Paralelo e retorno | Integracao atual proibe presumir segundo consumidor | Falta procedimento de corte, reconciliacao e rollback SG3 validado |

## O que aproveitar do modelo

- `src/lib/sia.ts`: separacao de parseSIA, createACK/createNACK, validacao de comprimento/CRC, campos de sequencia/conta e tratamento de timestamp. Servem como referencia para um adaptador DC-09, se esse protocolo entrar no escopo.
- Estruturas de eventos e separacao entre transporte TCP/UDP e processamento podem orientar contratos C#; nao transportar constantes, framing ou ACK de DC-09 para SG3 sem especificacao.
- Exemplos do README podem originar fixtures de DC-09; nao sao capturas reais da automacao SG3.
- `LICENSE` e package.json identificam MIT. Eventual copia/adaptacao deve preservar os avisos do arquivo de licenca. Nenhum codigo foi copiado nesta analise.

Nao incorporar o aplicativo ioBroker inteiro. Ele depende de Node >=20 e do ambiente ioBroker, enquanto o projeto usa C#/.NET Framework 4.7.2. Isso acrescentaria outra plataforma sem resolver a compatibilidade SG3.

## Limitacoes encontradas no modelo

1. `src/lib/sia.ts:644`: o callback TCP passa cada bloco recebido diretamente a parseSIA. Nao ha acumulador incremental nesse caminho para mensagens fragmentadas ou varias mensagens no mesmo bloco.
2. `src/lib/sia.ts:650`: emite data, interpreta e escreve ACK antes de emitir sia, sem contrato de confirmacao de gravacao duravel. Nao reutilizar essa ordem como garantia contra perda de eventos.
3. `src/lib/sia.ts:379`: debug serializa a configuracao da conta, que inclui senha AES. Remover ou mascarar dados sensiveis em eventual adaptacao.
4. `src/main.test.ts`: teste de exemplo compara 5 com 5. Nao constitui validacao do parser, de retransmissao ou de confiabilidade.
5. Encerramento de socket e timeout de 30 segundos sao comportamentos do modelo; nao sao parametros comprovados da sessao SG3.

## Ajustes necessarios no documento recebido

- A secao 7 coloca resposta de protocolo antes da gravacao duravel; a secao 8 exige entrega duravel. Especificar quando o ACK confirma recebimento e posicionar o journal antes dessa confirmacao, conforme contrato real e janela de tempo do receptor.
- Os dados de firmware, serial e portas sao observacoes documentadas, nao verificacoes realizadas nesta analise. Resolvem parte do inventario, mas nao substituem o protocolo de automacao.
- Nao usar Alarm/Account Port nem portas Console/Printer como porta de automacao por inferencia.
- Execucao em paralelo precisa de topologia validada: nao presumir que duas automacoes possam consumir o mesmo fluxo simultaneamente.
- Os links de manuais citados sao referencias a obter/confirmar; nao foi encontrado manual do protocolo no inventario pesquisado.

## Sequencia recomendada

1. Confirmar manual aplicavel ao CPM3, interface de automacao, papel cliente/servidor, parametros seriais completos ou porta TCP, framing, ACK, heartbeat e retransmissao. Obter capturas autorizadas de laboratorio.
2. Definir contratos de transporte, journal duravel, parser e evento normalizado, preservando a UI e as regras de atendimento existentes.
3. Implementar journal/replay e simulador de transporte com testes de fragmentacao, mensagens agrupadas, queda de disco, reinicio e duplicidade. Esses trabalhos podem avancar sem conexao de producao.
4. Implementar o driver SG3 segundo a especificacao; associar contas por identificadores reais de receptor/linha/conta, preservando zeros iniciais.
5. Validar banco servidor e monitoramento; comparar eventos capturados com registros SG3, testar corte e retorno antes da substituicao operacional.

## Divergencia na documentacao atual

RETOMADA.md diz que nenhuma importacao foi realizada e BACKLOG.md ainda aponta importador como pendente. Entretanto, existem import_legacy.py, sql_dump.py e TESTE_BYKOM.md documentando a importacao de teste. Esses checkpoints precisam ser reconciliados com os relatorios efetivos antes de orientar a proxima execucao; a existencia do codigo nao comprova uma importacao executada.

Validacao desta entrega: leitura do DOCX via XML e inspecao de codigo/documentacao. Nao foram executados build, testes ou o modelo ioBroker; resultados historicos de testes nao foram revalidados.
