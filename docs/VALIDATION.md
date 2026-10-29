# Correções de controle e reconexão · 0.4.5

- Treze testes Windows aprovados em dados isolados. O teste novo de ciclo de vida inicia e encerra duas vezes um processo próprio usando uma porta de loopback, verificando sua liberação antes da substituição.
- Regressões de entrada cobrem botões, arrasto/captura nativos, coordenadas reais após arrasto, origem negativa, limite superior e descarte de saída atrasada após reentrada.
- O build de teste Mac passou geometria, layout, migração e configuração Sparkle, sem substituir o app instalado e sem alterar permissões.
- O travamento intermitente durante uma apresentação longa ainda não foi reproduzido de forma determinística. Estas verificações não equivalem a uma reunião real de 15 minutos. Os novos eventos locais permitem identificar falha de captura, interação nativa e saída de segurança.

# Validação de distribuição · 0.4.4

- Release pública: https://github.com/IOI-Plugins/ioi-desk/releases/tag/v0.4.4
- Build and test 40438864354 e CodeQL 40438864096 aprovados no commit da release.
- Publish signed release 40438886575 aprovado: Windows, Mac e publicação.
- Windows: onze testes, geração NSIS e instalação/remoção em ambiente isolado aprovados, com dados preservados.
- Mac: assinatura Developer ID real, notarização aceita, ticket anexado, validação do ticket e Gatekeeper aprovados no runner.
- Sparkle/WinSparkle: feeds e pacotes assinados e verificados pelas ferramentas originais antes da publicação.
- Downloads e feeds públicos responderam HTTP 200 sem autenticação; SHA256SUMS coincide com os digests dos assets no GitHub.
- Não foi simulada uma atualização entre duas releases com updater: esta é a primeira versão pública que o inclui. A primeira instalação continua necessária.

A entrega do ticket Apple perdeu a conexão na primeira tentativa; repetir o job no mesmo commit concluiu com sucesso. O script de finalização passa a repetir essa operação até três vezes para próximas releases, mantendo todas as validações obrigatórias.

## Histórico da base anterior

# Validação da refatoração · 26/10/2026

## Distribuição: verificações adicionais

- GitHub Actions, execução 40421069767: Windows e macOS aprovados.
- Windows: dez testes e geração MSIX por MakeAppx aprovados. O artefato
  `windows-msix-validation-only` usa identidade de preview e não foi instalado,
  submetido ou certificado pela Microsoft.
- MacBook Pro: build com símbolo IOI azul, recursos do navegador e avisos
  de terceiros, assinado com Developer ID, Team 5723C964BL e Hardened Runtime.
  Geometria, layout e migração passaram. Nenhum app instalado foi substituído.
- O acesso SSH do Windows ao MacBook foi validado por chave dedicada.
- Notarização aceita pela Apple: submissão
  `9f3a5355-e766-43fc-ba98-f3f4e17796fb`, enviada em 26/10/2026.
  Credencial IOIDesk validada e salva somente no Keychain. Stapling e
  `stapler validate` passaram; Gatekeeper retornou `accepted` com origem
  `Notarized Developer ID`. ZIP final gerado por finish-notarization-macos.sh.
- Compatibilidade do pacote atual com Mac App Store bloqueada pelos recursos
  de Acessibilidade/Apple Events fora do sandbox; detalhes em STORE-READINESS.md.

As observações abaixo registram a validação inicial da refatoração.

## Executado

Windows compilado com o compilador .NET Framework disponível na máquina. Dez verificações passaram, usando diretório de dados isolado:

1. Migração: copia configurações suportadas, conserva originais e alterações posteriores, exclui arquivos alheios.
2. Persistência do layout: aplicar repetidamente mantém a apresentação na zona; painéis livres usam o monitor configurado.
3. Geometria e coordenadas do ponteiro, formato nativo de resolução e botão central.
4. Persistência de atribuições com validação de identificador de janela e processo.
5. Divisão de terminais, áreas sem sobreposição e limpeza de janela fechada.
6. Abrir, cancelar e reabrir o seletor sem deixar a janela proprietária bloqueada.
7. Shift + arrastar, transferência, cancelamento por Esc e recusa de tamanho incompatível.
8. Seleção de apresentação virtual e liberação da origem ao transferir.
9. Fundo atrás das aplicações, recuperação de janela oculta e transferência entre zonas. A falha anterior desse teste foi corrigida na versão refatorada.
10. Saída de emergência independente da captura da interface e mapeamento de botão central.

Relatório gerado pelo executor: `work/test-results/windows/results.json`.

Mac compilado no Mac mini autorizado, em `Library/Caches/IOIDeskRefactor`, sem substituir `/Applications/IOI Desk.app`. Passaram:

- geometria de duas telas, origens negativas e serialização do perfil;
- mudança de layout limitada à tela escolhida;
- migração com atribuições preservadas, sem apagar originais ou sobrescrever alterações;
- ciclo de vida do aplicativo único: configurações, zonas e conexão sob um único proprietário, início repetido sem duplicação, encerramento e reinício;
- permanência das zonas após perda de foco e recuperação ao fechar o seletor.

`tools/check-source.py` passou: um único delegado de aplicativo no Mac, ausência de Camadas.app e da interface de conexão duplicada no código de execução, dados Windows fora da pasta de binários e ausência de chaves/certificados privados nos fontes.

## Ainda não validado para implantação

- Notarização e Hardened Runtime. A assinatura Developer ID já foi validada; veja SIGNING.md.
- Continuidade de outras permissões além de Acessibilidade. A migração para aplicativo único e uma atualização posterior preservando Acessibilidade já foram verificadas no Mac mini; veja SIGNING.md.
- Teste de uso completo da versão unificada com aplicações reais, mouse entre computadores, modos de tela cheia e reinício de sessão. Os resultados da instalação anterior não são apresentados como teste da nova versão.
- O teste Windows de múltiplos painéis/hotkeys exige execução exclusiva em uma sessão de teste; não foi executado com o aplicativo de produção aberto.
- Atualização automática e distribuição comercial permanecem fora do que foi validado nesta refatoração.

A compilação Mac de teste usa assinatura ad hoc explicitamente habilitada, somente no cache de desenvolvimento. Ela não foi instalada e não deve ser usada como atualização do aplicativo autorizado. Dados pessoais, permissões e sessões atuais foram preservados.
