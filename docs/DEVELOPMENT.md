# Desenvolvimento

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
