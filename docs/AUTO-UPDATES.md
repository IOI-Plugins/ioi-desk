# Atualizações pelo GitHub

O aplicativo usa componentes originais: Sparkle 2.10.1 no macOS e WinSparkle
0.9.4 no Windows. Não existe um mecanismo próprio de extrair e sobrescrever
o programa durante a sessão. Frameworks e compilador NSIS são baixados com
versão e SHA-256 fixos em tools/updater-dependencies.json.

## Comportamento

Mac: checagem automática, download em segundo plano e instalação quando
o aplicativo pode encerrar, usando a interface padrão do Sparkle.
As preferências posteriores do usuário são respeitadas. Tanto o appcast
quanto o arquivo de atualização exigem assinatura; o pacote também tem
Developer ID e notarização Apple. Perfil de sistema anônimo fica desativado.

Windows: WinSparkle verifica atualizações automaticamente e apresenta sua
interface padrão para instalar. O instalador NSIS aguarda o IOI Desk sair,
atualiza a instalação por usuário e reabre o app. Ele não encerra os apps
organizados. O pacote é validado com Ed25519 pelo WinSparkle. A versão inicial
ainda não tem certificado Authenticode Windows e pode mostrar SmartScreen.
Instalações MSIX usam o mecanismo de atualização do Windows, não WinSparkle.

Dados e layouts ficam nos caminhos AppPaths, fora dos binários. Atualizações
não instalam drivers nem complementos no navegador sem a ação necessária
nesses componentes. A primeira instalação da versão com updater precisa ser
feita uma vez; uma versão antiga sem updater não recebe esse recurso sozinha.

## Release automática

Após atualizar release.json, use uma tag vVERSAO no commit de main que passou
no CI. O workflow Publish signed release só aceita tag com a versão exata,
exige CI aprovado no mesmo SHA e gera os dois pacotes. No Mac ele exige
certificado persistente, notarização aceita, stapling e Gatekeeper.
No último job, o sign_update e generate_appcast oficiais produzem assinaturas
e os feeds. A publicação sai de draft somente depois desses passos.

Não se publica uma release a cada commit de desenvolvimento. Não instalar
artefatos de PR como atualização. Arquivos e feeds publicados nunca devem ser
editados depois de assinados; uma correção exige nova versão e build maior.

## Configuração única do ambiente release

GitHub Secrets, no ambiente release, sem guardar valores no código:

- MACOS_CERTIFICATE_BASE64: PKCS12 criptografado somente da identidade Developer ID.
- MACOS_CERTIFICATE_PASSWORD: senha de criptografia desse PKCS12.
- APPLE_ID e APPLE_APP_PASSWORD: conta e senha específica para notarização.
- SPARKLE_PRIVATE_KEY: chave Ed25519 do canal de atualização.

Somente a chave pública aparece em assets/updates.json. Credenciais são
importadas para chaveiro descartável no runner e a chave temporária é removida
ao fim do job. O ambiente deve permitir deployment somente das tags v*.
Sem segredos válidos o workflow falha; não produz uma release sem assinatura.

## Referências

- https://sparkle-project.org/documentation/
- https://sparkle-project.org/documentation/programmatic-setup/
- https://winsparkle.org/guides/integrating-winsparkle/
- https://winsparkle.org/guides/publishing-updates/
