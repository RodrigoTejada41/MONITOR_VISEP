# Rastreabilidade

Revisao 2026-09-16. Detalhes dos resultados em RETOMADA.md e PLANO_TESTES.md.

| Requisito | Codigo | Teste / resultado |
|---|---|---|
| Compatibilidade minima | scripts/Build.ps1 | Build local passou; VM minima pendente |
| Cadastro | src/Core/Store.cs, src/Desktop/Program.cs | CoreTests + DesktopE2E |
| Recepcao independente | src/Receiver/ReceiverProgram.cs | IntegrationTests, console sem UI aprovado |
| Atendimento | src/Core/Store.cs, src/Desktop/Program.cs | DesktopE2E completo aprovado |
| Controle de acesso | src/Core/PasswordSecurity.cs, Store.cs | CoreTests: perfis, bloqueio e auditoria |
| Durabilidade/recuperacao | XmlRepository.cs, Backup.ps1, Restore.ps1 | CoreTests + IntegrationTests |
| Relatorios | Store.cs, Desktop/Program.cs | Exportacao automatizada; ver PLANO_TESTES.md |
| Inspecao legado | src/Migration/InspectProgram.cs | Fixture SQL sintetica aprovada; mapa legado textual disponivel |
| Integracao SG | Adaptador real ausente | Bloqueado por protocolo/receptor |
| Migracao | src/Migration/import_legacy.py, sql_dump.py | 16 testes Python e 27 assertions LegacyHistory passaram; reconciliacao real pendente |

Matriz individual das 11 specs e criterios pendentes em [specs/README.md](specs/README.md). Insumos e conexao historica SG3 em [evidencias/README.md](evidencias/README.md). Resultados Core/Python/LegacyHistory foram reexecutados no checkpoint; IntegrationTests/DesktopE2E permanecem evidencias historicas.
