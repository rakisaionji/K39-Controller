using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace K39C
{
    public partial class Manipulator
    {
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        private const long RESOLUTION_WIDTH_ADDRESS = 0x0000000140EDA8BC;
        private const long RESOLUTION_HEIGHT_ADDRESS = 0x0000000140EDA8C0;

        private const ProcessAccess PROCESS_ACCESS = ProcessAccess.PROCESS_VM_READ | ProcessAccess.PROCESS_VM_WRITE | ProcessAccess.PROCESS_VM_OPERATION;

        public bool IsAttached => ProcessHandle != IntPtr.Zero;

        public IntPtr ProcessHandle { get; private set; }

        public Process AttachedProcess { get; private set; }

        public Manipulator()
        {
            return;
        }

        public Manipulator(Process proc)
        {
            AttachedProcess = proc;
            ProcessHandle = OpenProcess(PROCESS_ACCESS, false, AttachedProcess.Id);
        }

        public POINT GetMouseRelativePos(POINT pos)
        {
            if (!IsAttached)
            {
                return new POINT(0, 0);
            }
            else
            {
                float xoffset;
                float scale;

                ScreenToClient(AttachedProcess.MainWindowHandle, out pos);
                RECT hWindow;
                GetClientRect(AttachedProcess.MainWindowHandle, out hWindow);

                var gameHeight = ReadInt32(RESOLUTION_HEIGHT_ADDRESS);
                var gameWidth = ReadInt32(RESOLUTION_WIDTH_ADDRESS);
                // var fbWidth = ReadInt32(FB_WIDTH_ADDRESS);
                // var fbHeight = ReadInt32(FB_HEIGHT_ADDRESS);

                // if ((fbWidth != gameWidth) && (fbHeight != gameHeight))
                // {
                xoffset = ((float)16 / (float)9) * (hWindow.Bottom - hWindow.Top);
                if (xoffset != (hWindow.Right - hWindow.Left))
                {
                    scale = xoffset / (hWindow.Right - hWindow.Left);
                    xoffset = ((hWindow.Right - hWindow.Left) / 2) - (xoffset / 2);
                }
                else
                {
                    xoffset = 0;
                    scale = 1;
                }
                pos.X = (int)(((pos.X - Math.Round(xoffset)) * gameWidth / (hWindow.Right - hWindow.Left)) / scale);
                pos.Y = pos.Y * gameHeight / (hWindow.Bottom - hWindow.Top);
                // }

                return pos;
            }
        }

        public bool TryAttachToProcess(string processName)
        {
            var processes = Process.GetProcessesByName(processName);

            if (processes.Length > 0)
            {
                AttachedProcess = processes[0];

                ProcessHandle = OpenProcess(PROCESS_ACCESS, false, AttachedProcess.Id);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryAttachToProcess(int processId)
        {
            var process = Process.GetProcessById(processId);

            if (process != null)
            {
                AttachedProcess = process;

                ProcessHandle = OpenProcess(PROCESS_ACCESS, false, AttachedProcess.Id);

                return true;
            }
            else
            {
                return false;
            }
        }

        public void SuspendAttachedProcess()
        {
            if (!IsAttached)
                return;

            foreach (ProcessThread thread in AttachedProcess.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);

                if (pOpenThread != IntPtr.Zero)
                    SuspendThread(pOpenThread);
            }
        }

        public void ResumeAttachedProcess()
        {
            if (!IsAttached)
                return;

            foreach (ProcessThread thread in AttachedProcess.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);

                if (pOpenThread != IntPtr.Zero)
                    ResumeThread(pOpenThread);
            }
        }

        public bool IsProcessRunning()
        {
            if (!IsAttached)
                return false;

            return WaitForSingleObject(ProcessHandle, 0) != 0x0;
        }

        public void CloseHandles()
        {
            if (!IsAttached)
                return;

            foreach (ProcessThread thread in AttachedProcess.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);

                if (pOpenThread != IntPtr.Zero)
                    CloseHandle(pOpenThread);
            }
            CloseHandle(ProcessHandle);
        }

        public byte[] Read(long address, int length)
        {
            if (!IsAttached || address <= 0)
                return new byte[byte.MaxValue];

            int bytesRead = 0;
            byte[] buffer = new byte[length];

            ReadProcessMemory((int)ProcessHandle, address, buffer, buffer.Length, ref bytesRead);

            return buffer;
        }

        public byte ReadByte(long address)
        {
            if (!IsAttached || address <= 0)
                return byte.MaxValue;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(byte)];

            ReadProcessMemory((int)ProcessHandle, address, buffer, buffer.Length, ref bytesRead);

            return buffer[0];
        }

        public short ReadInt16(long address)
        {
            if (!IsAttached || address <= 0)
                return -1;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(short)];

            ReadProcessMemory((int)ProcessHandle, address, buffer, buffer.Length, ref bytesRead);

            return BitConverter.ToInt16(buffer, 0);
        }

        public int ReadInt32(long address)
        {
            if (!IsAttached || address <= 0)
                return -1;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(int)];

            ReadProcessMemory((int)ProcessHandle, address, buffer, buffer.Length, ref bytesRead);

            return BitConverter.ToInt32(buffer, 0);
        }

        public long ReadInt64(long address)
        {
            if (!IsAttached || address <= 0)
                return -1;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(long)];

            ReadProcessMemory((int)ProcessHandle, address, buffer, buffer.Length, ref bytesRead);

            return BitConverter.ToInt64(buffer, 0);
        }

        public float ReadSingle(long address)
        {
            if (!IsAttached || address <= 0)
                return -1;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(float)];

            ReadProcessMemory((int)ProcessHandle, address, buffer, buffer.Length, ref bytesRead);

            return BitConverter.ToSingle(buffer, 0);
        }

        public double ReadDouble(long address)
        {
            if (!IsAttached || address <= 0)
                return -1;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(double)];

            ReadProcessMemory((int)ProcessHandle, address, buffer, buffer.Length, ref bytesRead);

            return BitConverter.ToDouble(buffer, 0);
        }

        public string ReadAsciiString(long address)
        {
            if (!IsAttached || address <= 0)
                return string.Empty;

            int length = GetAsciiStringLength(address);

            int bytesRead = 0;
            byte[] buffer = new byte[length];

            ReadProcessMemory((int)ProcessHandle, address, buffer, length, ref bytesRead);

            return Encoding.ASCII.GetString(buffer);
        }

        public void Write(long address, byte[] value)
        {
            if (!IsAttached || address <= 0)
                return;

            int bytesWritten = 0;

            WriteProcessMemory((int)ProcessHandle, address, value, value.Length, ref bytesWritten);
        }

        public void WritePatch(long address, byte[] value)
        {
            if (!IsAttached || address <= 0)
                return;

            uint oldProtect, bck;
            int bytesWritten = 0;

            VirtualProtect((IntPtr)address, (uint)value.Length, PAGE_EXECUTE_READWRITE, out oldProtect);
            WriteProcessMemory((int)ProcessHandle, address, value, value.Length, ref bytesWritten);
            VirtualProtect((IntPtr)address, (uint)value.Length, oldProtect, out bck);
        }

        public void WriteByte(long address, byte value)
        {
            if (!IsAttached || address <= 0)
                return;

            // int bytesWritten = 0;
            byte[] buffer = { value };

            Write(address, buffer);
        }

        public void WriteInt16(long address, short value)
        {
            if (!IsAttached || address <= 0)
                return;

            // int bytesWritten = 0;
            byte[] buffer = BitConverter.GetBytes(value);

            Write(address, buffer);
        }

        public void WriteInt32(long address, int value)
        {
            if (!IsAttached || address <= 0)
                return;

            // int bytesWritten = 0;
            byte[] buffer = BitConverter.GetBytes(value);

            Write(address, buffer);
        }

        public void WriteInt64(long address, long value)
        {
            if (!IsAttached || address <= 0)
                return;

            // int bytesWritten = 0;
            byte[] buffer = BitConverter.GetBytes(value);

            Write(address, buffer);
        }

        public void WriteSingle(long address, float value)
        {
            if (!IsAttached || address <= 0)
                return;

            // int bytesWritten = 0;
            byte[] buffer = BitConverter.GetBytes(value);

            Write(address, buffer);
        }

        public void WriteDouble(long address, double value)
        {
            if (!IsAttached || address <= 0)
                return;

            // int bytesWritten = 0;
            byte[] buffer = BitConverter.GetBytes(value);

            Write(address, buffer);
        }

        public int GetAsciiStringLength(long address)
        {
            int length = 0;

            for (int i = 0; i < 4096; i++)
            {
                if (ReadByte(address + i) == 0x0)
                    break;

                length++;
            }

            return length;
        }

        public static Process GetForegroundProcessObject()
        {
            IntPtr foregroundWindow = GetForegroundWindow();

            if (foregroundWindow == IntPtr.Zero)
                return null;

            GetWindowThreadProcessId(foregroundWindow, out int activeProcId);

            return Process.GetProcessById(activeProcId);
        }

        public void WriteNop(long address, int length)
        {
            if (!IsAttached || address <= 0)
                return;

            Write(address, Assembly.GetNopInstructions(length));
        }

        public void WritePatchNop(long address, int length)
        {
            if (!IsAttached || address <= 0)
                return;

            uint oldProtect, bck;

            VirtualProtect((IntPtr)address, (uint)length, PAGE_EXECUTE_READWRITE, out oldProtect);
            Write(address, Assembly.GetNopInstructions(length));
            VirtualProtect((IntPtr)address, (uint)length, oldProtect, out bck);
        }

        public static bool VirtualProtect(IntPtr lpAddress, uint dwSize)
        {
            uint oldProtect;
            return VirtualProtect((IntPtr)lpAddress, dwSize, PAGE_EXECUTE_READWRITE, out oldProtect);
        }

        public static bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect)
        {
            uint oldProtect;
            return VirtualProtect((IntPtr)lpAddress, dwSize, flNewProtect, out oldProtect);
        }
    }
}
