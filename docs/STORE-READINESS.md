# Preparação para as lojas

## Microsoft Store

O workflow `store.yml` gera o pacote de submissão por execução manual ou tag
de versão. Ele usa as variáveis `WINDOWS_STORE_IDENTITY_NAME`,
`WINDOWS_STORE_PUBLISHER` e `WINDOWS_STORE_PUBLISHER_DISPLAY_NAME`, que devem
ser copiadas da identidade oficial do produto no Partner Center. Ele não usa
SignPath e não instala, submete ou publica o aplicativo. A assinatura da Store
ocorre após a certificação. A quarta parte da versão MSIX fica em zero,
reservada pela Microsoft Store. A configuração de atualização do aplicativo já
detecta identidade de pacote e delega as atualizações ao Windows.

Validação atual desse fluxo: actionlint e análise de sintaxe PowerShell
aprovados. A identidade real, o pacote final e a instalação certificada ainda
dependem da conclusão do cadastro e dos testes no Partner Center.

Em 4/11/2026, o produto IOI Desk foi criado na conta Tecnovix, Store ID
`9P7S2J0CTNSL`. A identidade oficial está em `assets/store-identity.json` e é
usada pelo workflow quando não houver variáveis de substituição. O MSIX
0.4.5.0 x64 foi gerado com MakeAppx e aceito pela validação inicial de upload
do Partner Center; a seção Pacotes aparece como Completo/Validated.
Isso não é certificação nem publicação. A justificativa runFullTrust, as
propriedades e a listagem pt-BR foram cadastradas. O questionário IARC gerou
classificação Livre no Brasil; sua aceitação legal aguarda confirmação do
proprietário. Preço e verificação final da conta ainda precisam ser concluídos.
A política de privacidade está publicada em `docs/PRIVACY.md`.

`tools/package-windows.ps1` gera e valida MSIX x64 com MakeAppx, incluindo
Deskflow original, licenças, fontes correspondentes e complemento do navegador.
`-Preview` usa uma identidade exclusiva de teste; não instalar nem submeter.
Para a loja, informar os valores reais de Name e Publisher da identidade do
produto no Partner Center. `-MakeAppxPath` permite usar o SDK oficial obtido
por NuGet sem instalar globalmente as ferramentas de desenvolvimento.

O pacote precisa de runFullTrust para organizar janelas e executar o componente
original de compartilhamento de entrada. A Microsoft analisa essa declaração
durante a certificação. O MSIX não instala drivers de monitor virtual; testar
esse recurso com o driver externo instalado separadamente.

Ainda faltam concluir preço/classificação, confirmar a liberação da conta e
validar a instalação/atualização do pacote certificado em sessão de teste.
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
