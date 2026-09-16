# Escopo e evidências

Versão 0.2 — 2026-09-16. Estado: base demonstrável em validação integrada; resultados em RETOMADA.md.

## Origem e limites
Os dois planos fornecidos são referências de requisitos. As instruções neles contidas são aplicadas dentro do pedido de implementação; não comprovam disponibilidade do legado, receptor ou infraestrutura.

Confirmado: execução local desktop, Windows Server 2008 R2 como alvo obrigatório, operação offline, necessidade futura de estações LAN, separação entre recepção e atendimento. O projeto estava vazio na inspeção inicial.

Backup BYKOM localizado e hash conferido; inventário textual disponível em MAPA_BANCO_LEGADO.md. Pendente: validação semântica do esquema, versão do servidor/conector, interface e protocolo SG-System III, VM Windows Server 2008 R2 SP1, carga e retenção reais.

## Entrega incremental
Fluxo demonstrável: autenticação → cadastro → evento marcado SIMULAÇÃO → assumir ocorrência → registrar ação → encerrar com justificativa → histórico/CSV. Serviço independente da interface e ferramenta de inspeção estrutural do SQL.

Armazenamento XML próprio é apenas base local demonstrável. Não satisfaz o banco multiusuário de produção. Não habilitar armazenamento compartilhado SMB como substituto de servidor de banco.

## Fora da aprovação operacional
Integração real, importação BYKOM e liberação multiestação exigem evidências externas. Nenhuma modificação de produção, licença BYKOM ou receptor real está autorizada por este demonstrador.
