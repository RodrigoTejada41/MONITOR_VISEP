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
| Migracao | Importador ausente | Backup identificado, implementacao e reconciliacao pendentes |
