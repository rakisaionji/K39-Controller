using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace K39C
{
    class Watchdog : Component
    {
        Manipulator Manipulator;
        private Thread thread;
        private bool stopFlag;

        // DISP MAIN ID    : 0x14CC94728 - 0x16
        // SAVE MAIN ID    : 0x14CC94739 - 0x11
        // DISP KEYCHIP ID : 0x14CC94E0C - 0x16
        // SAVE KEYCHIP ID : 0x14CC94E1D - 0x11

        private const long DISP_MAIN_ID = 0x000000014CC94728L; // 16
        private const long SAVE_MAIN_ID = 0x000000014CC94739L; // 11
        private const long TASK_MAIN_ID = 0x000000014CC93D19L; // 16
        private const long DISP_KEYCHIP_ID = 0x000000014CC94E0CL; // 16
        private const long SAVE_KEYCHIP_ID = 0x000000014CC94E1DL; // 11
        private const long AIME_KEYCHIP_ID = 0x0000000140ED5B05L; // 11
        private const long TASK_KEYCHIP_ID = 0x0000000140CDC431L; // 11
        private const string DEFAULT_FALLBACK_ID = "AAAA0000000";

        private Regex REGEX_MAIN_ID = new Regex(@"A[A-Z]{2}E\-\d{2}A\d{8}", RegexOptions.CultureInvariant);
        private Regex REGEX_KEYCHIP_ID = new Regex(@"A[0-9]{2}E\-\d{2}A\d{8}", RegexOptions.CultureInvariant);
        private Regex REGEX_ALLOW_ID = new Regex(@"[^A-Z0-9]", RegexOptions.CultureInvariant);

        private const int SYS_TIME_FACTOR = 60;
        private const int SEL_PV_FREEZE_TIME = 39;
        private const int SYS_TIMER_TIME = SEL_PV_FREEZE_TIME * SYS_TIME_FACTOR;
        private const long SEL_PV_TIME_ADDRESS = 0x000000014CC12498L;

        public Watchdog(Manipulator manipulator)
        {
            Manipulator = manipulator;
        }

        public void SysTimer_Start()
        {
            Manipulator.WriteInt32(SEL_PV_TIME_ADDRESS, SYS_TIMER_TIME);

            // 0x00000001405C5143:  mov qword ptr [rsi+0B38h], 3600
            Manipulator.WriteInt32(0x1405C514AL, SYS_TIMER_TIME);

            // 0x00000001405BDFBF:  dec dword ptr [rbx+0B38h]
            Manipulator.Write(0x1405BDFBFL, Assembly.GetNopInstructions(6));

            // 0x00000001405C517A:  mov [rsi+0B38h], ecx
            Manipulator.Write(0x1405C517AL, Assembly.GetNopInstructions(6));
        }

        public void SysTimer_Stop()
        {
            // Manipulator.WriteInt32(SEL_PV_TIME_ADDRESS, 0xE10);

            // 0x00000001405C5143:  mov qword ptr [rsi+0B38h], 3600
            // Manipulator.WriteInt32(0x1405C514AL, 0xE10);

            // 0x00000001405BDFBF:  dec dword ptr [rbx+0B38h]
            // Manipulator.Write(0x1405BDFBFL, new byte[] { 0xFF, 0x8B, 0x38, 0x0B, 0x00, 0x00 });

            // 0x00000001405C517A:  mov [rsi+0B38h], ecx
            // Manipulator.Write(0x1405C517AL, new byte[] { 0x89, 0x8E, 0x38, 0x0B, 0x00, 0x00 });
        }

        public string MainId
        {
            get { return Manipulator.ReadAsciiString(DISP_MAIN_ID); }
            set
            {
                Manipulator.WriteAsciiString(DISP_MAIN_ID, 16, value);
                Manipulator.WriteAsciiString(TASK_MAIN_ID, 16, value);
                if (REGEX_MAIN_ID.IsMatch(value))
                    Manipulator.WriteAsciiString(SAVE_MAIN_ID, 11, REGEX_ALLOW_ID.Replace(value, ""));
                else
                    Manipulator.WriteAsciiString(SAVE_MAIN_ID, 11, DEFAULT_FALLBACK_ID);
            }
        }

        public string KeychipId
        {
            get { return Manipulator.ReadAsciiString(DISP_KEYCHIP_ID); }
            set
            {
                Manipulator.WriteAsciiString(DISP_KEYCHIP_ID, 16, value);
                if (REGEX_KEYCHIP_ID.IsMatch(value))
                {
                    Manipulator.WriteAsciiString(AIME_KEYCHIP_ID, 11, REGEX_ALLOW_ID.Replace(value, ""));
                    Manipulator.WriteAsciiString(SAVE_KEYCHIP_ID, 11, REGEX_ALLOW_ID.Replace(value, ""));
                    Manipulator.WriteAsciiString(TASK_KEYCHIP_ID, 11, REGEX_ALLOW_ID.Replace(value, ""));
                }
                else
                {
                    Manipulator.WriteAsciiString(AIME_KEYCHIP_ID, 11, DEFAULT_FALLBACK_ID);
                    Manipulator.WriteAsciiString(SAVE_KEYCHIP_ID, 11, DEFAULT_FALLBACK_ID);
                    Manipulator.WriteAsciiString(TASK_KEYCHIP_ID, 11, DEFAULT_FALLBACK_ID);
                }
            }
        }

        public void Start()
        {
            if (thread != null) return;
            stopFlag = false;
            thread = new Thread(new ThreadStart(ThreadCallback));
            thread.Start();
            Console.WriteLine("    SYSTEM MANAGER   : OK");
        }

        public void Stop()
        {
            stopFlag = true;
            thread = null;
        }

        private void ThreadCallback()
        {
            while (!stopFlag)
            {
                if (!Manipulator.IsProcessRunning())
                {
                    Program.Stop();
                    break;
                }
                Thread.Sleep(10);
            }
            stopFlag = false;
        }
    }
}
