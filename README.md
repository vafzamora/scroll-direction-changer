# ScrollFlip

Aplicativo de bandeja (system tray) para Windows que inverte a direção da rolagem do mouse
instantaneamente, sem precisar de permissão de administrador nem reiniciar o dispositivo.

## Como funciona

Um hook global de mouse (`WH_MOUSE_LL`) intercepta os eventos da roda. Quando a inversão está
ativa, o evento original é bloqueado e um novo evento com o delta invertido é injetado via
`SendInput` (em uma thread separada, pois chamar `SendInput` dentro do próprio callback faz o
Windows entregar também o evento original).

## Uso

- **Ctrl+Alt+S** — alterna entre scroll normal e invertido (funciona em qualquer aplicativo).
- **Clique esquerdo no ícone da bandeja** — alterna também.
- **Menu do ícone (botão direito)**:
  - `Scroll invertido` — estado atual.
  - `Inverter também o scroll horizontal` — aplica a inversão à roda horizontal.
  - `Iniciar com o Windows` — registra/remove o app na inicialização do usuário atual.
  - `Sair`.

O ícone fica azul (seta para cima) no modo normal e laranja (seta para baixo) no modo invertido.
As preferências são salvas em `%APPDATA%\ScrollFlip\settings.json`.

## Compilar e executar

```powershell
dotnet build -c Release
.\bin\Release\net10.0-windows\ScrollFlip.exe
```

Para gerar um executável único:

```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
```

O resultado fica em `bin\Release\net10.0-windows\win-x64\publish\ScrollFlip.exe`.

## Observações

- Apenas uma instância roda por vez (mutex).
- Se o atalho Ctrl+Alt+S já estiver em uso por outro programa, o app avisa na bandeja e continua
  funcionando pelo clique no ícone.
- Janelas de aplicativos executados como administrador não são afetadas pelo hook, a menos que o
  ScrollFlip também seja executado como administrador.

## Estrutura

| Arquivo | Função |
| --- | --- |
| `Program.cs` | Ponto de entrada, instância única |
| `TrayAppContext.cs` | Ícone da bandeja, menu, persistência e inicialização automática |
| `ScrollInverter.cs` | Hook de mouse e reinjeção dos eventos invertidos |
| `HotkeyWindow.cs` | Janela invisível que recebe o atalho global |
| `IconFactory.cs` | Geração dos ícones em tempo de execução |
| `Settings.cs` | Leitura/gravação das preferências |
| `NativeMethods.cs` | Declarações P/Invoke |
