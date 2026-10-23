using static ScrollFlip.NativeMethods;

namespace ScrollFlip;

/// <summary>
/// Janela invisível que recebe as mensagens do atalho global.
/// </summary>
internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int HotkeyId = 1;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    public event EventHandler? HotkeyPressed;

    /// <summary>Registra Ctrl+Alt+S. Retorna false se o atalho já estiver em uso.</summary>
    public bool Register() =>
        RegisterHotKey(Handle, HotkeyId, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, (uint)Keys.S);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && (int)m.WParam == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            UnregisterHotKey(Handle, HotkeyId);
            DestroyHandle();
        }
    }
}
