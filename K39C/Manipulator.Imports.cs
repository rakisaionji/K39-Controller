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

        private const uint PAGE_EXECUTE_READWRITE = 0x40;

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

        [DllImport(KERNEL32_DLL, SetLastError = true)]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport(KERNEL32_DLL, SetLastError = true)]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport(KERNEL32_DLL, SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport(KERNEL32_DLL, SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport(USER32_DLL)]
        static extern bool ScreenToClient(IntPtr hWnd, out POINT lpPoint);

        [DllImport(KERNEL32_DLL)]
        static extern bool VirtualProtectEx(IntPtr hProcess, long lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport(KERNEL32_DLL)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport(KERNEL32_DLL)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport(KERNEL32_DLL)]
        public static extern bool GetExitCodeProcess(IntPtr hProcess, out int lpExitCode);

        [DllImport(USER32_DLL)]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport(KERNEL32_DLL)]
        public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, CreateProcessFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport(KERNEL32_DLL, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, AllocType flAllocType, MemoryProtection flProtect);

        [DllImport(KERNEL32_DLL, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport(KERNEL32_DLL, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport(KERNEL32_DLL)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);
    }
}
