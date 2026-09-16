# SPEC-008 — Usuários, permissões e auditoria

Versão: 0.2. Estado: EM VALIDAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de usuários, permissões e auditoria com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

Login precede MainForm. PBKDF2 com salt aleatório e 100000 iterações; Admin/Operator/Viewer e bloqueio persistido. Core testa acessos negados, auditoria e bloqueio após reinício do Store.

## Critérios de aceite pendentes e riscos

Definir/testar troca, recuperação e revogação de credenciais/sessões; verificar ACLs em instalação real. Auditoria compartilha XML e não é inviolável contra escrita direta. Não importar credenciais BYKOM nem documentar senhas.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

src/Core/PasswordSecurity.cs; src/Core/Store.cs; src/Desktop/Program.cs; scripts/Install.ps1; tests/CoreTests.cs.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
