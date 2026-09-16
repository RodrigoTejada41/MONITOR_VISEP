# SPEC-010 — Instalação, atualização e recuperação

Versão: 0.2. Estado: EM VALIDAÇÃO. Revisão: 2026-09-16.

## Objetivo e limites

Entregar o escopo de instalação, atualização e recuperação com evidência reproduzível. Estado refere-se ao escopo completo; não equivale a homologação de produção.

## Implementação e evidências

Install cria serviço LocalService e ACLs; Backup/Restore existem. Integração histórica registra backup/hash e rejeição de formato inválido. Receiver possui ServiceBase.

## Critérios de aceite pendentes e riscos

Instalar/iniciar/parar/reiniciar via SCM em VM; testar atualização/rollback e reexecutar restauração em destino novo com contagens/hash. Reaplicar ACL do serviço no restaurado. Serviço não foi instalado/testado via SCM nesta revisão e continua simulador.

Cada verificação pendente exige comando/cenário, ambiente, resultado esperado e resultado obtido. Só concluir após todos os critérios aplicáveis passarem; dependências externas continuam explícitas.

## Rastreabilidade

scripts/Install.ps1; scripts/Backup.ps1; scripts/Restore.ps1; tests/IntegrationTests.ps1; docs/OPERACAO.md.

Ver [matriz das specs](README.md) e [retomada](../RETOMADA.md). Preservar dados originais; executar testes com destinos isolados. Eventos simulados devem continuar identificados como simulação.
