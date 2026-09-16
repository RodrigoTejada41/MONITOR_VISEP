# Segurança

Requisitos: senha derivada com salt aleatório individual e função de derivação resistente; sem senha padrão embutida. Cadastro inicial de administrador antes da UI operacional. Autorização em serviços de aplicação, não apenas botões. Perfis separados para administração, operação e leitura; auditoria inclui ator, instante e ação sem credenciais.

Arquivo de dados e backup contêm dados pessoais e hashes: restringir ACL NTFS. Usuário com acesso de escrita ao XML pode alterar dados fora do aplicativo; a aplicação não constitui fronteira de segurança contra administrador local ou edição direta. Banco servidor e serviço autenticado são necessários para controle multiestação robusto.

Não registrar senhas ou conteúdo pessoal em logs de erro. CSV deve neutralizar células com início de fórmula e escapar aspas, separadores e quebras. SQL futuro parametrizado. Não tocar Hardlock/Hardkey.

CoreTests cobre autenticacao, negacao por perfil/sessao, bloqueio persistido, corrida de atendimento e CSV adversarial. Cobertura percentual e homologacao de instalacao continuam pendentes. Revisao estatica de publicacao nao equivale a auditoria integral de seguranca do produto.

Repositorio PUBLIC: nao versionar dumps de clientes, credenciais, licencas proprietarias ou configuracoes privadas. Referencias liberadas e manifesto em Inventario/README.md e docs/evidencias/README.md. Configurar repositorio privado no futuro nao substitui tratamento de segredos.
