# Distribuição

O workflow Build and test compila e testa Windows e macOS ARM64 a cada
alteração em main e pull request. O Mac usa assinatura ad hoc somente no
runner descartável; esses binários não são oferecidos como atualização.
As Actions estão fixadas por SHA. Não há certificado ou chave no repositório.

O pipeline de testes não é um atualizador automático do aplicativo.
Publicação assinada e notarizada ainda exige configurar o ambiente de release.
A chave Apple existente permanece no chaveiro do proprietário. Não exportar
essa chave nem publicar builds ad hoc como atualização do app autorizado.

Windows: tools/build-windows.ps1 e tools/test-windows.ps1.
O executável produzido não possui assinatura Authenticode. Para um pacote
portável completo, incluir Deskflow original com -IncludeRuntime -DeskflowRoot.

Mac: tools/build-macos.sh com IOI_CODESIGN_IDENTITY e IOI_DESKFLOW_APP.
Depois tools/notarize-macos.sh, usando credencial IOIDesk no Keychain.
Aceitação pela Apple, stapling e validação Gatekeeper são etapas obrigatórias
antes de promover um pacote a release. O script de submissão atual não espera
o resultado; uma submissão enviada não significa notarização aprovada.

Não empacotar work/, dados de usuário, layouts pessoais, logs de uso,
certificados ou credenciais. Preservar third-party/ em todos os pacotes.
