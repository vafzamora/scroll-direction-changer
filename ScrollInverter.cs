using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static ScrollFlip.NativeMethods;

namespace ScrollFlip;

/// <summary>
/// Instala um hook global de mouse e reinjeta os eventos de roda com o delta invertido.
/// </summary>
internal sealed class ScrollInverter : IDisposable
{
    private readonly LowLevelMouseProc _proc;
    private readonly BlockingCollection<INPUT> _injectionQueue = new();
    private readonly Thread _injectionThread;
    private IntPtr _hookHandle = IntPtr.Zero;

    public ScrollInverter()
    {
        _proc = HookCallback;
        _injectionThread = new Thread(ProcessInjections)
        {
            IsBackground = true,
            Name = "ScrollFlip.Injector"
        };
        _injectionThread.Start();
    }

    private void ProcessInjections()
    {
        foreach (INPUT input in _injectionQueue.GetConsumingEnumerable())
        {
            SendInput(1, [input], Marshal.SizeOf<INPUT>());
        }
    }

    public bool Enabled { get; set; }

    public bool InvertHorizontal { get; set; }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        IntPtr module = GetModuleHandle(null);
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _proc, module, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Não foi possível instalar o hook de mouse (erro {Marshal.GetLastWin32Error()}).");
        }
    }

    public void Stop()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || !Enabled)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        int message = (int)wParam;
        bool isVertical = message == WM_MOUSEWHEEL;
        bool isHorizontal = message == WM_MOUSEHWHEEL;

        if (!isVertical && (!isHorizontal || !InvertHorizontal))
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
        if (data.dwExtraInfo == InjectedMarker)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        short delta = unchecked((short)(data.mouseData >> 16));
        if (delta == 0)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        int inverted = -delta;
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                mouseData = unchecked((uint)inverted),
                dwFlags = isVertical ? MOUSEEVENTF_WHEEL : MOUSEEVENTF_HWHEEL,
                time = 0,
                dwExtraInfo = InjectedMarker
            }
        };

        // A injeção é feita fora do callback: chamar SendInput dentro do hook faz o
        // Windows entregar também o evento original, mesmo retornando 1.
        _injectionQueue.Add(input);

        // Bloqueia o evento original: só o invertido segue adiante.
        return 1;
    }

    public void Dispose()
    {
        Stop();
        _injectionQueue.CompleteAdding();
        _injectionThread.Join(TimeSpan.FromSeconds(1));
        _injectionQueue.Dispose();
    }
}
