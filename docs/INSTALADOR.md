# Instalador VISEP

Execute o pacote VISEP mais recente fornecido para a instalação como administrador. O instalador valida Windows/.NET, identifica o servico `VisepReceiver`, preserva a pasta de dados e atualiza os binarios no caminho ja registrado pelo servico. Nao e necessario extrair o pacote em pasta de teste.

Se encontrar bases antigas em `C:\CVISEP-Teste` ou `C:\CVISEP-r8`, ele mostra usuarios, clientes e ocorrencias e exige selecao explicita. Ele nunca escolhe por data/tamanho e nunca mescla journals/inbox de bases diferentes. Antes de alterar, cria uma copia com hash conferido em `C:\ProgramData\Visep-Installer-Backups`.

O instalador oferece dois modos:

- `simulacao`: inicia o receptor XML local.
- `sg3`: inicia o receptor continuo no endpoint informado, com reconexao e ACK apenas depois de gravar raw/envelope. Exige confirmar `CAPTURAR`, consumidor anterior parado e framing correto (`plain` ou `b32`). Nao cria ocorrencias a partir de sinais SG3.

O servico inicia automaticamente ao fim. Para verificar, abra o atalho VISEP e use `C:\Program Files\VISEP\scripts\Menu-Servidor.cmd` como administrador. A base exibida no login precisa ser a mesma configurada pelo servico.

Em caso de falha, o instalador tenta restaurar binarios, configuracao do servico e dados. Nao remova `C:\ProgramData\Visep-Installer-Backups` ate validar a atualizacao.
