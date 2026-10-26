# IOI Desk · refatoração 0.3.0

Base única de código do IOI Desk para Windows e macOS. Organiza aplicações em zonas, integra o componente original Deskflow e inclui um complemento de navegador para tela cheia dentro da janela.

## O que mudou

- **Um aplicativo no Mac:** configurações, zonas, seleção de janelas e conexão pertencem ao mesmo processo IOI Desk. Camadas.app e a interface antiga de conexão foram eliminados da nova estrutura.
- **Configurações fora dos binários:** atualizar o executável ou o aplicativo não exige copiar layouts manualmente. A migração inicial copia dados antigos sem apagar os originais e sem substituir configurações novas já existentes.
- **Responsabilidades separadas:** interface, organização de janelas, captura/entrada Windows, conexão, diagnóstico e armazenamento têm módulos próprios.
- **Layouts Windows centralizados:** início, Aplicar e organização de painéis usam a mesma política de monitor e preservam a zona atribuída à apresentação.
- **Versão única de produto:** release.json gera os metadados Windows e Mac. O complemento do navegador mantém sua versão de componente declarada no manifesto.
- **Assinatura com verificação:** compilações normais do Mac exigem identidade persistente. Assinatura provisória só é permitida explicitamente em testes isolados.

O Deskflow continua como componente original de terceiros, em processo independente. Não é outro aplicativo IOI a ser configurado pelo usuário. Seu código e licenças não foram incorporados nem renomeados como código próprio.

## Estrutura

```text
release.json             versão e identidade do produto
src/windows/             interface e serviços Windows
src/macos/               aplicativo único AppKit e serviços internos
src/browser/             complemento de apresentação dentro da janela
tests/windows/           regressões de zonas, entrada, migração e layouts
tests/macos/             geometria, migração, layouts e ciclo de vida
tools/                   compilação e validação reproduzíveis
assets/                  ícones, manifesto e metadados
third-party/             avisos e licenças preservados
dist/                    artefatos de desenvolvimento, sem dados pessoais
docs/                    arquitetura, migração e resultados
```

## Dados do usuário

Windows: `%LOCALAPPDATA%/IOI Desk/Workspace`, `Connection` e `Logs`.

Mac: `~/Library/Application Support/IOI Desk/Workspace` e `Connection`. O antigo arquivo `Camadas/workspace.json` é importado apenas se o destino não existir. A identidade principal `com.ioi.desk.settings` foi preservada; não foi criado outro identificador de produto.

Os nomes antigos das sessões de terminal são reconhecidos na migração para conservar as sessões abertas. Nenhuma sessão é encerrada pelo importador.

## Compilação e testes

Windows: execute `tools/build-windows.ps1` e `tools/test-windows.ps1`. Para incluir a dependência original, informe `-IncludeRuntime -DeskflowRoot CAMINHO_DO_COMPONENTE_ORIGINAL`.

Mac: execute `tools/build-macos.sh`, informando `IOI_CODESIGN_IDENTITY` e `IOI_DESKFLOW_APP`. O primeiro é o hash da identidade de assinatura disponível no chaveiro; o segundo é o caminho do componente original Deskflow.app. O script gera o ícone e os metadados e compila um único executável IOIDesk.

`IOI_ALLOW_ADHOC=1` permite somente uma compilação de teste. **Não instalar essa compilação sobre o aplicativo autorizado.** A migração final usa assinatura persistente. Não há comandos para redefinir bases de permissões, confiar em certificados ou remover autorizações.

`tools/check-source.py` verifica os limites arquiteturais. Consulte [VALIDATION.md](docs/VALIDATION.md) para o resultado dos testes e as verificações ainda pendentes.

## Atualizações

O processo de compilação e validação está unificado no projeto. **Um atualizador automático entre computadores ainda não está implementado.** `tools/verify-update.sh` bloqueia assinaturas provisórias ou identidade diferente antes da substituição. A transição do pacote antigo, composto por dois aplicativos, para o novo pacote único precisa ser validada como migração inicial.

A instalação Windows ainda pode usar um caminho histórico. Esse caminho representa uma implantação, não uma segunda base de desenvolvimento. Novas alterações partem somente deste projeto.

## Integração contínua e marca

O GitHub Actions executa compilação e testes nos dois sistemas. Consulte
[DISTRIBUTION.md](docs/DISTRIBUTION.md) para os limites de publicação e assinatura.
O símbolo original IOI foi preservado e recebeu o azul aprovado para o Desk;
origem e decisão estão em [BRAND.md](assets/BRAND.md).
