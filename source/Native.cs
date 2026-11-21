using System;
using System.Runtime.InteropServices;

namespace SmoothFlow {
internal static class Native {
    internal const int WM_WHEEL = 0x20A, WM_HWHEEL = 0x20E;
    internal static readonly UIntPtr Marker = new UIntPtr(0x53464C57);
    [StructLayout(LayoutKind.Sequential)] internal struct Point { internal int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseHook { internal Point Point; internal uint Data, Flags, Time; internal UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseInput { internal int X, Y; internal uint Data, Flags, Time; internal UIntPtr Extra; }
    [StructLayout(LayoutKind.Explicit, Size=32)] internal struct InputUnion { [FieldOffset(0)] internal MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] internal struct Input { internal uint Type; internal InputUnion Union; }
    internal delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError=true)] internal static extern IntPtr SetWindowsHookEx(int type, HookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("kernel32.dll", CharSet=CharSet.Auto)] internal static extern IntPtr GetModuleHandle(string name);
    [DllImport("user32.dll", SetLastError=true)] internal static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] internal static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] internal static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll")] internal static extern bool PostThreadMessage(uint thread, uint message, UIntPtr w, IntPtr l);
    [DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("advapi32.dll", SetLastError=true)] internal static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DllImport("advapi32.dll", SetLastError=true)] internal static extern bool GetTokenInformation(IntPtr token, int type, out int value, int size, out int length);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] internal static extern IntPtr CreateWaitableTimerEx(IntPtr attributes,string name,uint flags,uint access);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool SetWaitableTimer(IntPtr timer,ref long due,int period,IntPtr completion,IntPtr argument,bool resume);
    [DllImport("winmm.dll")] internal static extern uint timeBeginPeriod(uint period);
    [DllImport("winmm.dll")] internal static extern uint timeEndPeriod(uint period);
    internal static bool Down(int key) { return (GetAsyncKeyState(key) & 0x8000) != 0; }
    internal static bool Modifiers() { return Down(0x11) || Down(0x12) || Down(0x5B) || Down(0x5C); }
    internal static IntPtr RootAtCursor() { Point p; return GetCursorPos(out p) ? GetAncestor(WindowFromPoint(p), 2) : IntPtr.Zero; }
    internal static IntPtr LeafAtCursor() { Point p; return GetCursorPos(out p) ? WindowFromPoint(p) : IntPtr.Zero; }
    internal static bool CanInject(uint pid) {
        IntPtr process = OpenProcess(0x1000, false, pid), token = IntPtr.Zero;
        if (process == IntPtr.Zero) return false;
        try { int elevation, length; return OpenProcessToken(process, 8, out token) && GetTokenInformation(token, 20, out elevation, 4, out length) && elevation == 0; }
        finally { if (token != IntPtr.Zero) CloseHandle(token); CloseHandle(process); }
    }
    internal static bool Inject(int delta, bool horizontal) {
        Input i = new Input(); i.Union.Mouse.Data = unchecked((uint)delta);
        i.Union.Mouse.Flags = horizontal ? 0x1000u : 0x800u; i.Union.Mouse.Extra = Marker;
        return SendInput(1, new Input[] { i }, Marshal.SizeOf(typeof(Input))) == 1;
    }
    internal static bool InjectDemo(int delta) {
        Input i=new Input();i.Union.Mouse.Data=unchecked((uint)delta);i.Union.Mouse.Flags=0x800;i.Union.Mouse.Extra=new UIntPtr(0x53465453);
        return SendInput(1,new Input[]{i},Marshal.SizeOf(typeof(Input)))==1;
    }
}
}
