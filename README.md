# VISEP

Central de monitoramento de alarmes para Windows Server 2008 R2 SP1 x64, desenvolvida em C# / WinForms (.NET Framework 4.7.2).

## Componentes

- Interface operacional para autenticação, clientes, ocorrências, atendimento, ações e encerramento.
- Persistência local em XML, com bloqueio de escrita, backup e trilha de auditoria.
- Receptor de simulação independente da interface.
- Receptor SG3 contínuo por TCP, com reconexão, journal bruto, envelopes de captura e ACK após persistência.
- Analisador SG3 offline para validação estrutural das capturas sem expor payloads.
- Instalador com atualização de instalação existente, backup e inicialização do serviço `VisepReceiver`.
- Prévia de importação BYKOM: clientes, endereços, contatos, zonas, sensores, receptor, conta e partição. A prévia é isolada e não altera a base ativa.

## Limites atuais

O receptor SG3 já captura e preserva sinais continuamente. A conversão automática desses sinais em ocorrências permanece bloqueada até a reconciliação homologada entre os identificadores BYKOM e as contas SG3 reais.

A persistência atual é local em XML. Uso multiestação e homologação operacional completa ainda não foram liberados.

## Instalação e operação

Execute como administrador o instalador mais recente fornecido para a instalação. O pacote é distribuído fora do clone público; ele preserva a base selecionada, atualiza os binários e inicia o serviço.

- [Instalador](docs/INSTALADOR.md)
- [Recepção e validação SG3](docs/INTEGRACAO_SG_SYSTEM_III.md)
- [Prévia de importação BYKOM](docs/IMPORTACAO_BYKOM.md)
- [Retomada técnica](docs/RETOMADA.md)

## Desenvolvimento

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Build.ps1 -Test
```

A suíte de importação é executada pelo runtime portátil distribuído no instalador. No ambiente de desenvolvimento com Python disponível:

```powershell
python -m unittest discover -s tests -p 'test_*.py'
```

## Dados privados

Backups BYKOM, dados de clientes, configurações, capturas completas, binários e diretórios de trabalho locais não fazem parte do repositório público. Consulte [Inventario/README.md](Inventario/README.md) e [docs/evidencias/README.md](docs/evidencias/README.md) para os insumos que precisam ser transferidos por canal privado.
