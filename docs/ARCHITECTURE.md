# Arquitetura

## macOS

`main.swift` seleciona testes ou inicia um único `NSApplication`, com `AppController` como único delegado. Ele possui `WorkspaceController` e `DeskConnection`; nenhum deles é um aplicativo separado.

`AppController` é responsável pelas configurações, abertura e reabertura da janela, inicialização, permissões e encerramento. `WorkspaceController` organiza os monitores e as zonas, mantém o catálogo de janelas em segundo plano e gerencia seleção e arrastar. `WindowSystem` encapsula a comunicação de Acessibilidade e o ajuste de navegadores. `DeskConnection` possui exclusivamente o processo Deskflow criado por ele, o estado da conexão e os registros limitados. `AppPaths` importa o arquivo antigo e mantém dados fora do pacote. `Diagnostics` preserva a retenção dos eventos.

Aplicar layouts modifica o modelo em memória. Apenas as atribuições da tela alterada são removidas; as referências das demais telas são conservadas. Não é necessário encerrar um segundo aplicativo, esperar por outro processo ou procurar Camadas em Aplicativos.

## Windows

`Program` controla entrada, instância única e testes. `WorkspaceContext` possui o ciclo de vida das janelas e a bandeja. `WorkspaceScreens` é a única política de escolha dos monitores físicos. `ZoneWorkspace` coordena os painéis; `ZoneBoard` controla a geometria e as atribuições de uma tela. O fundo permanece atrás das aplicações em vez de ser inserido no grupo de janelas de outro aplicativo.

`Viewer`, `ViewMath`, `MouseNative`, `InputBroker` e `InputSafety` separam apresentação virtual, geometria, entrada nativa e saída de emergência. `WindowCatalog`, `WindowPicker` e `WindowLimits` separam descoberta, seleção e limites das janelas. `WorkspaceProfile`, `AppPaths` e `TraceLog` separam persistência, migração e diagnóstico.

`ComputerSharing` mantém a dependência Deskflow externa. O material de pareamento permanece em Connection, fora do executável e do pacote distribuído. `TerminalGrid` reconhece nomes de sessão antigos para não forçar reabertura de terminais.

## Limites

Não foi prometida equivalência de implementação entre Windows e Mac: a apresentação virtual permanece exclusiva do Windows. O Mac organiza janelas nativas. O complemento do navegador continua separado porque atua dentro da página, sem depender da geometria nativa do sistema.

Não há servidor de atualizações nem inscrição automática de máquinas. A assinatura de distribuição e a preservação real das autorizações entre duas versões assinadas são uma etapa posterior, dependente do certificado do proprietário.
