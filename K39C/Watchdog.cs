using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace K39C
{
    class Watchdog : Component
    {
        Settings Settings;
        Manipulator Manipulator;
        private Thread thread;
        private bool stopFlag;

        private const long DISP_MAIN_ID = 0x000000014C5C52D8L; // 16
        private const long SAVE_MAIN_ID = 0x000000014C5C52E9L; // 11
        private const long TASK_MAIN_ID = 0x000000014C5C48C9L; // 16
        private const long DISP_KEYCHIP_ID = 0x000000014C5C59BCL; // 16
        private const long SAVE_KEYCHIP_ID = 0x000000014C5C59CDL; // 11
        private const long AIME_KEYCHIP_ID = 0x0000000140E89375L; // 11
        private const long TASK_KEYCHIP_ID = 0x0000000140C8FCB1L; // 11
        private const string DEFAULT_FALLBACK_ID = "AAAA0000000";

        private Regex REGEX_MAIN_ID = new Regex(@"A[A-Z]{2}E\-\d{2}A\d{8}", RegexOptions.CultureInvariant);
        private Regex REGEX_KEYCHIP_ID = new Regex(@"A[0-9]{2}E\-\d{2}A\d{8}", RegexOptions.CultureInvariant);
        private Regex REGEX_ALLOW_ID = new Regex(@"[^A-Z0-9]", RegexOptions.CultureInvariant);

        private const int SYS_TIME_FACTOR = 60;
        private const int SEL_PV_FREEZE_TIME = 39;
        private const int SYS_TIMER_TIME = SEL_PV_FREEZE_TIME * SYS_TIME_FACTOR;

        public Watchdog(Manipulator manipulator, Settings settings)
        {
            Manipulator = manipulator;
            Settings = settings;
        }

        private void SysTimer_Start()
        {
            // 0x00000001405C5143:  mov qword ptr [rsi+0B38h], 3600
            Manipulator.WriteInt32(0x1405A1DA9L, SYS_TIMER_TIME);

            // 0x00000001405BDFBF:  dec dword ptr [rbx+0B38h]
            Manipulator.Write(0x14059B21FL, Assembly.GetNopInstructions(6));

            // 0x00000001405C517A:  mov [rsi+0B38h], ecx
            Manipulator.Write(0x1405A1DD9L, Assembly.GetNopInstructions(6));
        }

        private string MainId
        {
            get { return Manipulator.ReadAsciiString(DISP_MAIN_ID); }
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                Manipulator.WriteAsciiString(DISP_MAIN_ID, 16, value);
                Manipulator.WriteAsciiString(TASK_MAIN_ID, 16, value);
                if (REGEX_MAIN_ID.IsMatch(value))
                    Manipulator.WriteAsciiString(SAVE_MAIN_ID, 11, REGEX_ALLOW_ID.Replace(value, ""));
                else
                    Manipulator.WriteAsciiString(SAVE_MAIN_ID, 11, DEFAULT_FALLBACK_ID);
            }
        }

        private string KeychipId
        {
            get { return Manipulator.ReadAsciiString(DISP_KEYCHIP_ID); }
            set
            {
                if (String.IsNullOrEmpty(value)) return;
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
            if (Settings.SysTimer) SysTimer_Start();
            KeychipId = Settings.KeychipId.Trim();
            MainId = Settings.MainId.Trim();

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

        public void Update()
        {
            return;
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
                Update();
                Thread.Sleep(100);
            }
            stopFlag = false;
        }
    }
}
