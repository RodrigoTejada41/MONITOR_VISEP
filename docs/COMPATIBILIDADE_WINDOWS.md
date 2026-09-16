# Compatibilidade Windows

Consulta oficial: 2026-09-15. Compatibilidade documental não é teste do aplicativo.

| Componente | Evidência | Estado |
|---|---|---|
| .NET Framework 4.7.2 / Server 2008 R2 SP1 x64 | Microsoft lista o sistema entre os destinos | Documental; execução pendente |
| Windows Forms | Exige instalação Windows com interface gráfica | Teste de instalação real pendente |
| Windows 10/11 | Necessário testar versão, arquitetura e framework presentes | Pendente |
| Windows Server posteriores | Não presumir execução em Server Core da interface | Pendente |
| Conector e servidor MySQL | Nenhuma versão fixada sem origem e matriz | Bloqueado |
| Compilador local | Desenvolvimento; não é instalador nem targeting pack | Verificar evidências de build |
| Instalador/serviço | Scripts e privilégios devem ser testados por SO | Pendente |

Pré-requisito mínimo documental: Windows Server 2008 R2 **SP1**. Confirmar atualizações de assinatura SHA-2, cadeia de certificados e pacotes exigidos pelo instalador específico antes da instalação; não foram inspecionados neste servidor. Não selecionar .NET moderno para este alvo.

Windows legado exige isolamento de rede, menor privilégio e política de atualização/aceitação de risco. Sua presença na matriz histórica do framework não implica suporte atual do sistema operacional.

Fontes oficiais:
- [Requisitos .NET Framework](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements)
- [Instalação e dependências](https://learn.microsoft.com/en-us/dotnet/framework/install/troubleshoot-blocked-installations-and-uninstallations)
- [Ciclo Windows Server 2008 R2](https://learn.microsoft.com/en-us/lifecycle/products/windows-server-2008-r2)

A aprovação exige VM/máquina SP1 com inventário de patches, versão Release do framework, execução dos binários, instalação/reinício do serviço e evidências dos testes.


Validacao local 2026-09-16: SO build 26200, compilacao contra targeting pack 4.7.2 e fluxo WinForms completo aprovados. Nao comprova compatibilidade com Server 2008 R2 SP1 nem instalacao/reinicio do servico.
