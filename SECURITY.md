# Segurança

Não publique senhas, tokens, certificados privados, arquivos de conexão ou logs
com dados pessoais em issues. Para reportar uma vulnerabilidade, use a opção
Report a vulnerability na aba Security quando habilitada. Se ela não estiver
disponível, abra somente um pedido de canal privado de contato, sem detalhes
da vulnerabilidade ou dados de exploração.

## Verificações automáticas

- Gitleaks examina todo o histórico Git disponível, incluindo branches e tags
  obtidos pelo checkout completo. Achados são ocultados nos logs e bloqueiam o CI.
- actionlint valida a sintaxe dos workflows e verifica os comandos shell.
- security-policy.py impede arquivos de credenciais, caminhos pessoais e Actions
  sem SHA fixo entre os arquivos rastreados. Não é um analisador de vulnerabilidades.
- CodeQL analisa C#, Swift, JavaScript, Python e Actions quando o repositório é
  público. Enquanto privado, o workflow é ignorado para não exigir licença paga.
- Dependabot propõe atualizações de Actions semanalmente. Scanners binários e
  Deskflow estão fixados por versão e checksum e precisam de revisão ao atualizar.

O código não envia os valores encontrados a um serviço externo de varredura.
As análises rodam nos runners do GitHub; os fontes já ficam hospedados no GitHub.

## Antes de abrir o repositório

Exigir CI aprovado na regra de proteção de main, revisão dos CODEOWNERS nas
mudanças de segurança, revisão por mantenedor de workflows de contribuidores
novos e proteção contra force-push. Essas opções exigem configuração na conta;
ter os arquivos no repositório não habilita automaticamente essas regras.

Habilitar secret scanning, push protection e comunicação privada de falhas na
aba Security quando disponíveis. Fazer a revisão de licença do produto e dos
componentes redistribuídos. Não basta apagar um segredo no último commit: caso
um scanner encontre credencial histórica, revogar/rotacionar antes de publicar
e revisar a remoção do histórico. Não reescrever o histórico automaticamente.

Nenhum scanner garante ausência de falhas ou substitui revisão de código.
O CI verifica commits; não oferece monitoramento de incidentes ou atualização
automática das instalações dos usuários.
