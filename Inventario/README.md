# Inventario e preservacao

Repositorio GitHub verificado como PUBLIC em 2026-09-16. Entram no Git somente os itens liberados pelo .gitignore:

- Documentacao_Integracao_SurGard_SG3_Substituicao_BYKOM.docx: documento de arquitetura analisado; nao e manual homologado do protocolo.
- Modelo/ioBroker.sia-master: referencia MIT, com LICENSE e package-lock originais. Nao integra o aplicativo e nao deve ser iniciado contra a receptora automaticamente.

Permanecem privados: dump SQL, RAR BYKOM, Config_Backup, planilhas de clientes/equipamentos e relatorios completos da maquina. Podem conter dados pessoais, credenciais, licencas e informacoes operacionais. Binarios proprietarios nao sao fonte do VISEP.

Manifesto e evidencias publicaveis: [docs/evidencias/README.md](../docs/evidencias/README.md). Hash identifica bytes; nao substitui backup nem reconstroi arquivo ausente.

Para outra maquina, copiar os insumos privados por canal restrito e conferir hashes. Um clone isolado permite estudar, compilar e testar com dados sinteticos, mas nao reproduz a migracao dos clientes reais.
