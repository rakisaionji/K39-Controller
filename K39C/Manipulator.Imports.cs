using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace K39C
{
    public partial class Manipulator
    {
        private const string USER32_DLL = "user32.dll";

        private const string KERNEL32_DLL = "kernel32.dll";

        [DllImport(USER32_DLL)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(USER32_DLL)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT rectangle);

        [DllImport(USER32_DLL)]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT rectangle);

        [DllImport(USER32_DLL)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport(KERNEL32_DLL)]
        public static extern IntPtr OpenProcess(ProcessAccess dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport(KERNEL32_DLL)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport(KERNEL32_DLL)]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport(KERNEL32_DLL)]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport(KERNEL32_DLL)]
        public static extern bool ReadProcessMemory(int hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport(KERNEL32_DLL)]
        public static extern bool WriteProcessMemory(int hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        [DllImport(USER32_DLL)]
        static extern bool ScreenToClient(IntPtr hWnd, out POINT lpPoint);

        [DllImport(KERNEL32_DLL)]
        public static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport(KERNEL32_DLL)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport(KERNEL32_DLL)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
    }
}
