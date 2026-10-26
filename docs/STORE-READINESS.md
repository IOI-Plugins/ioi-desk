# Preparação para as lojas

## Microsoft Store

`tools/package-windows.ps1` gera e valida MSIX x64 com MakeAppx, incluindo
Deskflow original, licenças, fontes correspondentes e complemento do navegador.
`-Preview` usa uma identidade exclusiva de teste; não instalar nem submeter.
Para a loja, informar os valores reais de Name e Publisher da identidade do
produto no Partner Center. Não existe ainda esse produto cadastrado.

O pacote precisa de runFullTrust para organizar janelas e executar o componente
original de compartilhamento de entrada. A Microsoft analisa essa declaração
durante a certificação. O MSIX não instala drivers de monitor virtual; testar
esse recurso com o driver externo instalado separadamente.

Ainda faltam cadastro, identidade real, política de privacidade publicada,
imagens da loja e validação da instalação/atualização MSIX em sessão de teste.
Não declarar o pacote aprovado ou compatível com a loja só por compilar.

## Mac App Store

O código atual usa AXUIElementSetAttributeValue para mover janelas de outros
aplicativos e NSAppleScript para ajustar limites dos navegadores. A Apple lista
APIs de Acessibilidade assistiva e Apple Events para aplicativos arbitrários
como incompatíveis com App Sandbox, obrigatório para a Mac App Store.

Portanto, o pacote atual Developer ID não é um pacote Mac App Store.
Adicionar com.apple.security.app-sandbox sem redesenhar essas funções quebraria
a organização de janelas. A conta Apple já existente não elimina esse conflito.
Não retirar funcionalidades nem submeter uma edição limitada sem decidir seu
escopo com o proprietário. Uma eventual alternativa exige orientação técnica
da Apple e validação concreta, não apenas troca de certificado.

## Distribuição direta Mac

`notarize-macos.sh` submete o aplicativo Developer ID com Hardened Runtime.
`finish-notarization-macos.sh UUID` exige status Accepted, anexa o ticket,
valida stapler e Gatekeeper e só então gera ZIP e SHA-256.
Esse fluxo não altera permissões do usuário e não instala sobre o app em uso.

## Referências

- https://developer.apple.com/documentation/security/protecting-user-data-with-app-sandbox
- https://developer.apple.com/documentation/security/customizing-the-notarization-workflow
- https://learn.microsoft.com/en-us/windows/msix/package/create-app-package-with-makeappx-tool
- https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations
