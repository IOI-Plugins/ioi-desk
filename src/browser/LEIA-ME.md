# IOI Desk · Tela cheia na zona

Este complemento faz o botão de tela cheia de um site preencher a janela do navegador. Não altera F11 nem o botão verde de tela cheia do Mac. O navegador precisa estar encaixado em uma zona.

## Instalação local de desenvolvimento

1. No Chrome ou Brave, abra o gerenciamento de extensões.
2. Ative o modo de desenvolvedor e escolha **Carregar sem compactação**.
3. Selecione esta pasta, que contém `manifest.json`.
4. Recarregue a página que já estava aberta antes da instalação.

YouTube e Power BI recebem o recurso automaticamente. Para outro site, clique no complemento IOI Desk antes de usar o botão de tela cheia do site. Clique novamente no complemento para desativá-lo na página. Esc ou **Sair da zona** retorna ao tamanho anterior.

O complemento não envia conteúdo de páginas, não grava teclas e não usa servidor. A permissão de acesso aos sites deve ser revisada na instalação. Nesta versão de desenvolvimento, ele modifica a API de fullscreen da página; sites com implementações diferentes podem exigir ajustes.

## Verificado

O teste próprio `test.html#test` passou no Chrome real do Mac em 25/10/2026, incluindo viewport, preservação da janela, restauração, Esc, remoção do conteúdo e iframe.

Instalado no Chrome do Mac mini. Em 26/10/2026, a versão 0.1.3 corrigiu um caso real do YouTube: o site solicitava fullscreen para a página inteira e o vídeo ficava com altura zero. Agora esse caso apresenta o player existente. O diagnóstico confirmou vídeo carregado e tocando em 1074 × 768, com o elemento de vídeo na camada visível. A conferência visual do usuário está pendente. Power BI real ainda não validado.

No Windows, os arquivos estão prontos em `%LOCALAPPDATA%\IOI Desk\BrowserExtension`. A instalação no Brave ainda depende de conclusão pela interface: a ferramenta de controle bloqueou a tentativa porque não identificou a URL atual com confiança. Não foi contornado o bloqueio.
