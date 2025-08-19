using System.Runtime.InteropServices;

namespace WinHotKeyEx.Host;

internal class Program
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_WIN = 0x0008;
    private const uint VK_ESCAPE = 0x1B;
    private const int HOTKEY_ID = 9001;
    private const int SW_MINIMIZE = 6;

    private static IntPtr _hwnd;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    private struct MSG
    {
        public IntPtr hWnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    private struct POINT
    {
        public int x;
        public int y;
    }

    static void Main()
    {
        // 单实例互斥锁，防止重复运行
        bool createdNew;
        using var mutex = new Mutex(true, "Global\\WinHotKeyExHostAppMutex", out createdNew);
        if (!createdNew)
        {
            return; // 已有实例在运行
        }

        _hwnd = IntPtr.Zero;

        if (!RegisterHotKey(_hwnd, HOTKEY_ID, MOD_WIN, VK_ESCAPE))
        {
            Console.WriteLine("注册快捷键失败，可能已有其他程序占用！");
            return;
        }

        // 注册退出事件，确保资源释放
        AppDomain.CurrentDomain.ProcessExit += (_, __) =>
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);
        };

        Console.WriteLine("程序已启动并注册 Win + Esc 快捷键（最小化当前窗口）...");
        Console.WriteLine("可将其设为开机自启。按 Ctrl+C 可退出。");

        try
        {
            while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) != 0)
            {
                if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID)
                {
                    var foreground = GetForegroundWindow();
                    if (foreground != IntPtr.Zero)
                        ShowWindow(foreground, SW_MINIMIZE);
                }

                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        finally
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);
        }
    }
}
