# Histórico de versões

## 0.4.4 · Seu ambiente sempre atualizado

- Atualizações pelo próprio aplicativo, com Sparkle no Mac e WinSparkle no Windows.
- Instalador Windows com atalhos e remoção que preserva dados e layouts.
- Distribuição Mac assinada e notarizada pela Apple.
- Canal público de downloads com pacotes e feeds assinados.


## 0.3.0 · Base unificada

- **Um aplicativo no Mac:** configurações, zonas, seleção de janelas e conexão pertencem ao mesmo processo IOI Desk. Camadas.app e a interface antiga de conexão foram eliminados da nova estrutura.
- **Configurações fora dos binários:** atualizar o executável ou o aplicativo não exige copiar layouts manualmente. A migração inicial copia dados antigos sem apagar os originais e sem substituir configurações novas já existentes.
- **Responsabilidades separadas:** interface, organização de janelas, captura/entrada Windows, conexão, diagnóstico e armazenamento têm módulos próprios.
- **Layouts Windows centralizados:** início, Aplicar e organização de painéis usam a mesma política de monitor e preservam a zona atribuída à apresentação.
- **Versão única de produto:** release.json gera os metadados Windows e Mac. O complemento do navegador mantém sua versão de componente declarada no manifesto.
- **Assinatura com verificação:** compilações normais do Mac exigem identidade persistente. Assinatura provisória só é permitida explicitamente em testes isolados.

O Deskflow continua como componente original de terceiros, em processo independente. Não é outro aplicativo IOI a ser configurado pelo usuário. Seu código e licenças não foram incorporados nem renomeados como código próprio.
