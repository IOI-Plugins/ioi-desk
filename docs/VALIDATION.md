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
- Notarização ainda não executada: perfil IOIDesk ausente no Keychain.
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

