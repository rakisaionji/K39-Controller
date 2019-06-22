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

        private const long DISP_MAIN_ID = 0x0000000140EEAF28L; // 16
        private const long SAVE_MAIN_ID = 0x0000000140EEAF39L; // 11
        private const long TASK_MAIN_ID = 0x0000000140EEA519L; // 16
        private const long DISP_KEYCHIP_ID = 0x0000000140EEB60CL; // 16
        private const long SAVE_KEYCHIP_ID = 0x0000000140EEB61DL; // 11
        private const long AIME_KEYCHIP_ID = 0x0000000140CEAEF5L; // 11
        // private const long TASK_KEYCHIP_ID = 0x0000000140??????L; // 11
        private const string DEFAULT_FALLBACK_ID = "AAAA0000000";

        private Regex REGEX_MAIN_ID = new Regex(@"A[A-Z]{2}E\-\d{2}A\d{8}", RegexOptions.CultureInvariant);
        private Regex REGEX_KEYCHIP_ID = new Regex(@"A[0-9]{2}E\-\d{2}A\d{8}", RegexOptions.CultureInvariant);
        private Regex REGEX_ALLOW_ID = new Regex(@"[^A-Z0-9]", RegexOptions.CultureInvariant);

        private const int SYS_TIME_FACTOR = 60;
        private const int SEL_PV_FREEZE_TIME = 39;
        private const int SYS_TIMER_TIME = SEL_PV_FREEZE_TIME * SYS_TIME_FACTOR;
        // private const long SEL_PV_TIME_ADDRESS = 0x0000000140EA6630L;

        public Watchdog(Manipulator manipulator, Settings settings)
        {
            Manipulator = manipulator;
            Settings = settings;
        }

        private void SysTimer_Start()
        {
            // Manipulator.WriteInt32(SEL_PV_TIME_ADDRESS, SYS_TIMER_TIME);

            // 0x0000000140411F0F:  mov qword ptr [r12+8C8h], 3600
            Manipulator.WriteInt32(0x140411F17L, SYS_TIMER_TIME);

            // 0x000000014040BEAF:  dec dword ptr [rbx+8C8h]
            Manipulator.Write(0x14040BEAFL, Assembly.GetNopInstructions(6));

            // 0x0000000140407976:  mov [rcx+8C8h], eax
            Manipulator.Write(0x140407976L, Assembly.GetNopInstructions(6));
        }

        private void SysTimer_Stop()
        {
            // Manipulator.WriteInt32(SEL_PV_TIME_ADDRESS, 0xE10);

            // 0x0000000140411F0F:  mov qword ptr [r12+8C8h], 3600
            // Manipulator.WriteInt32(0x140411F17L, 0xE10);

            // 0x000000014040BEAF:  dec dword ptr [rbx+8C8h]
            // Manipulator.Write(0x14040BEAFL, new byte[] { 0xFF, 0x8B, 0xC8, 0x08, 0x00, 0x00 });

            // 0x0000000140407976:  mov [rcx+8C8h], eax
            // Manipulator.Write(0x140407976L, new byte[] { 0x89, 0x81, 0xC8, 0x08, 0x00, 0x00 });
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
                    // Manipulator.WriteAsciiString(TASK_KEYCHIP_ID, 11, REGEX_ALLOW_ID.Replace(value, ""));
                }
                else
                {
                    Manipulator.WriteAsciiString(AIME_KEYCHIP_ID, 11, DEFAULT_FALLBACK_ID);
                    Manipulator.WriteAsciiString(SAVE_KEYCHIP_ID, 11, DEFAULT_FALLBACK_ID);
                    // Manipulator.WriteAsciiString(TASK_KEYCHIP_ID, 11, DEFAULT_FALLBACK_ID);
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
