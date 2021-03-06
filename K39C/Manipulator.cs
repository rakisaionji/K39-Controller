﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace K39C
{
    public partial class Manipulator
    {
        private const long RESOLUTION_WIDTH_ADDRESS = 0x0000000140EDA8BC;
        private const long RESOLUTION_HEIGHT_ADDRESS = 0x0000000140EDA8C0;

        private const ProcessAccess PROCESS_ACCESS = ProcessAccess.PROCESS_ALL_ACCESS;

        private static readonly Dictionary<IntPtr, int> ProcessIdCache = new Dictionary<IntPtr, int>(16);

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
            if (!IsAttached) return new POINT(0, 0);

            float xoffset;
            float scale;

            ScreenToClient(AttachedProcess.MainWindowHandle, out pos);
            RECT hWindow;
            GetClientRect(AttachedProcess.MainWindowHandle, out hWindow);

            var gameHeight = ReadInt32(RESOLUTION_HEIGHT_ADDRESS);
            var gameWidth = ReadInt32(RESOLUTION_WIDTH_ADDRESS);

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

            return pos;
        }

        public bool IsAttachedProcessActive()
        {
            IntPtr foregroundHandle = GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero) return false;

            // GetWindowThreadProcessId can sometimes have massive spikes in performance leading to micro stutters
            if (!ProcessIdCache.TryGetValue(foregroundHandle, out int foregroundProcessId))
            {
                GetWindowThreadProcessId(foregroundHandle, out foregroundProcessId);
                ProcessIdCache.Add(foregroundHandle, foregroundProcessId);
            }
            return foregroundProcessId == AttachedProcess.Id;
        }

        public bool IsProcessRunning(string processName)
        {
            return (Process.GetProcessesByName(processName).Length > 0);
        }

        public bool CreateProcess(string fileName, string arguments, string workingDirectory, out IntPtr hThread)
        {
            var si = new STARTUPINFO();
            var ps = CreateProcess(null, fileName + " " + arguments, IntPtr.Zero, IntPtr.Zero, false, CreateProcessFlags.CREATE_SUSPENDED, IntPtr.Zero, workingDirectory, ref si, out PROCESS_INFORMATION pi);
            hThread = pi.hThread;
            return ps;
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
            else return false;
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
            else return false;
        }

        public void SuspendAttachedProcess()
        {
            if (!IsAttached) return;
            foreach (ProcessThread thread in AttachedProcess.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread != IntPtr.Zero) SuspendThread(pOpenThread);
            }
        }

        public void ResumeAttachedProcess()
        {
            if (!IsAttached) return;
            foreach (ProcessThread thread in AttachedProcess.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread != IntPtr.Zero) ResumeThread(pOpenThread);
            }
        }

        public bool IsProcessRunning()
        {
            if (!IsAttached) return false;
            int num; // STILL_ACTIVE = 259
            return !(GetExitCodeProcess(ProcessHandle, out num) && (num != 0x103));
        }

        public void CloseHandles()
        {
            if (!IsAttached || !IsProcessRunning()) return;
            foreach (ProcessThread thread in AttachedProcess.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread != IntPtr.Zero) CloseHandle(pOpenThread);
            }
            CloseHandle(ProcessHandle);
        }

        public byte[] Read(long address, int length)
        {
            if (!IsAttached || address <= 0) return new byte[byte.MaxValue];
            byte[] buffer = new byte[length];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return buffer;
        }

        public byte ReadByte(long address)
        {
            if (!IsAttached || address <= 0) return byte.MaxValue;
            byte[] buffer = new byte[sizeof(byte)];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return buffer[0];
        }

        public short ReadInt16(long address)
        {
            if (!IsAttached || address <= 0) return -1;
            byte[] buffer = new byte[sizeof(short)];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return BitConverter.ToInt16(buffer, 0);
        }

        public int ReadInt32(long address)
        {
            if (!IsAttached || address <= 0) return -1;
            byte[] buffer = new byte[sizeof(int)];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return BitConverter.ToInt32(buffer, 0);
        }

        public long ReadInt64(long address)
        {
            if (!IsAttached || address <= 0) return -1;
            byte[] buffer = new byte[sizeof(long)];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return BitConverter.ToInt64(buffer, 0);
        }

        public ushort ReadUInt16(long address)
        {
            if (!IsAttached || address <= 0) return ushort.MaxValue;
            byte[] buffer = new byte[sizeof(ushort)];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return BitConverter.ToUInt16(buffer, 0);
        }

        public uint ReadUInt32(long address)
        {
            if (!IsAttached || address <= 0) return uint.MaxValue;
            byte[] buffer = new byte[sizeof(uint)];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public ulong ReadUInt64(long address)
        {
            if (!IsAttached || address <= 0) return ulong.MaxValue;
            byte[] buffer = new byte[sizeof(ulong)];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public float ReadSingle(long address)
        {
            if (!IsAttached || address <= 0) return -1;
            byte[] buffer = new byte[sizeof(float)];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return BitConverter.ToSingle(buffer, 0);
        }

        public double ReadDouble(long address)
        {
            if (!IsAttached || address <= 0) return -1;
            byte[] buffer = new byte[sizeof(double)];
            ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out int bytesRead);
            return BitConverter.ToDouble(buffer, 0);
        }

        public string ReadAsciiString(long address)
        {
            if (!IsAttached || address <= 0) return string.Empty;
            int length = GetStringLength(address);
            byte[] buffer = new byte[length];
            ReadProcessMemory(ProcessHandle, address, buffer, length, out int bytesRead);
            return Encoding.ASCII.GetString(buffer);
        }

        public string ReadUtf8String(long address)
        {
            if (!IsAttached || address <= 0) return string.Empty;
            int length = GetStringLength(address);
            byte[] buffer = new byte[length];
            ReadProcessMemory(ProcessHandle, address, buffer, length, out int bytesRead);
            return Encoding.UTF8.GetString(buffer);
        }

        public void Write(long address, byte[] value)
        {
            if (!IsAttached || address <= 0) return;
            WriteProcessMemory(ProcessHandle, address, value, value.Length, out int bytesWritten);
        }

        public void WriteByte(long address, byte value)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = { value };
            Write(address, buffer);
        }

        public void WriteInt16(long address, short value)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = BitConverter.GetBytes(value);
            Write(address, buffer);
        }

        public void WriteInt32(long address, int value)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = BitConverter.GetBytes(value);
            Write(address, buffer);
        }

        public void WriteInt64(long address, long value)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = BitConverter.GetBytes(value);
            Write(address, buffer);
        }

        public void WriteUInt16(long address, ushort value)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = BitConverter.GetBytes(value);
            Write(address, buffer);
        }

        public void WriteUInt32(long address, uint value)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = BitConverter.GetBytes(value);
            Write(address, buffer);
        }

        public void WriteUInt64(long address, ulong value)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = BitConverter.GetBytes(value);
            Write(address, buffer);
        }

        public void WriteSingle(long address, float value)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = BitConverter.GetBytes(value);
            Write(address, buffer);
        }

        public void WriteDouble(long address, double value)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = BitConverter.GetBytes(value);
            Write(address, buffer);
        }

        public void WritePatch(long address, byte[] value)
        {
            if (!IsAttached || address <= 0) return;
            uint oldProtect, bck;
            VirtualProtectEx(ProcessHandle, address, value.Length, PAGE_EXECUTE_READWRITE, out oldProtect);
            WriteProcessMemory(ProcessHandle, address, value, value.Length, out int bytesWritten);
            VirtualProtectEx(ProcessHandle, address, value.Length, oldProtect, out bck);
        }

        public void WritePatchNop(long address, int length)
        {
            if (!IsAttached || address <= 0) return;
            uint oldProtect, bck;
            VirtualProtectEx(ProcessHandle, address, length, PAGE_EXECUTE_READWRITE, out oldProtect);
            Write(address, Assembly.GetNopInstructions(length));
            VirtualProtectEx(ProcessHandle, address, length, oldProtect, out bck);
        }

        public void WriteAsciiString(long address, int length, string value)
        {
            if (!IsAttached || address <= 0) return;
            if (value.Length > length) value = value.Substring(0, length);
            var buffer = new byte[length];
            var data = Encoding.ASCII.GetBytes(value);
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            Write(address, buffer);
        }

        public int GetStringLength(long address)
        {
            int length = 0;
            for (int i = 0; i < Byte.MaxValue; i++)
            {
                if (ReadByte(address + i) == 0x0) break;
                length++;
            }
            return length;
        }

        public static Process GetForegroundProcessObject()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return null;
            GetWindowThreadProcessId(foregroundWindow, out int activeProcId);
            return Process.GetProcessById(activeProcId);
        }

        public void SetMainWindowActive()
        {
            if (!IsAttached) return;
            SetForegroundWindow(AttachedProcess.MainWindowHandle);
        }

        public bool InjectDll(string dllPath)
        {
            if (!IsAttached) return false;

            // Thanks vladkorotnev for this solution
            int pathLen = Encoding.Default.GetByteCount(dllPath) + 1;
            byte[] pathBytes = new byte[pathLen];
            Encoding.Default.GetBytes(dllPath).CopyTo(pathBytes, 0);

            IntPtr remoteMemory = VirtualAllocEx(ProcessHandle, IntPtr.Zero, new IntPtr(pathLen), AllocType.Commit | AllocType.Reserve, MemoryProtection.ReadWrite);
            if (remoteMemory.ToInt32() == 0) return false;

            bool didWrite = WriteProcessMemory(ProcessHandle, (long)remoteMemory, pathBytes, pathLen, out int written);
            if (!didWrite) return false;

            IntPtr threadId;
            IntPtr loadLibraryPtr = GetProcAddress(GetModuleHandle(KERNEL32_DLL), "LoadLibraryA");
            IntPtr threadRslt = CreateRemoteThread(ProcessHandle, IntPtr.Zero, 0, loadLibraryPtr, remoteMemory, 0, out threadId);
            if (threadRslt.ToInt32() == 0) return false;

            return true;
        }

        public IntPtr AllocateMemory(int length)
        {
            if (!IsAttached) return IntPtr.Zero;
            return VirtualAllocEx(ProcessHandle, IntPtr.Zero, new IntPtr(length), AllocType.Commit | AllocType.Reserve, MemoryProtection.ReadWrite);
        }

        public int[] ReadInt32Array(long address, int length)
        {
            if (!IsAttached || address <= 0) return null;
            var ret = new int[length];
            for (int i = 0; i < length; i++)
            {
                ret[i] = ReadInt32(address + i * 4);
            }
            return ret;
        }

        public void WriteInt32Array(long address, int[] value, int length)
        {
            if (!IsAttached || address <= 0 || value == null) return;
            if (value.Length < length) length = value.Length;
            for (int i = 0; i < length; i++)
            {
                byte[] buffer = BitConverter.GetBytes(value[i]);
                Write(address + i * 4, buffer);
            }
        }

        public void WriteBytes(long address, byte value, int length)
        {
            if (!IsAttached || address <= 0) return;
            byte[] buffer = new byte[length];
            for (int i = 0; i < length; i++) buffer[i] = value;
            Write(address, buffer);
        }
    }
}
