using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace K39C
{
    class Program
    {
        static readonly string PICO_VERSION = "2.00.00";
        static readonly string PICO_RELDATE = "2019-06-15";

        static readonly string DIVA_PROCESS_NAME = "diva";
        static Manipulator Manipulator = new Manipulator();
        static Watchdog system;

        static List<Component> components;
        static bool stopFlag = false;
        private static int consoleY;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        static void PrintProgramInfo()
        {
            Console.WriteLine("------------------------------------------------------------");
#if DEBUG
            Console.WriteLine("              ---- >>>> DEBUG_BUILD <<<< ----               ");
#endif
            Console.WriteLine("              PDAFT Controller for S39 and K39              ");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("   Minimal and less intrusive controller for real cabinet   ");
            Console.WriteLine("     by rakisaionji, vladkorotnev, samyuu and lybxlpsv      ");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine(" > Codename : K39-PICO ");
            Console.WriteLine(" > Date     : {0} ", PICO_RELDATE);
            Console.WriteLine(" > Version  : {0} ", PICO_VERSION);
            Console.WriteLine("------------------------------------------------------------");
        }

        public static void Stop()
        {
            stopFlag = true;
            foreach (var component in components) component.Stop();
            Manipulator.CloseHandles();
        }

        static void LockConsole()
        {
            const int STD_INPUT_HANDLE = -10;
            const uint ENABLE_QUICK_EDIT = 0x0040;
            const uint ENABLE_MOUSE_INPUT = 0x0010;
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
            uint mode;
            GetConsoleMode(consoleHandle, out mode);
            mode &= ~ENABLE_QUICK_EDIT;
            mode &= ~ENABLE_MOUSE_INPUT;
            SetConsoleMode(consoleHandle, mode);
        }

        static void Main(string[] args)
        {
            LockConsole();
#if DEBUG
            args = new string[] { "-t", "-s", "-p", "-f", "-k:A61E-01A07376003", "-m:AAVE-01A03965611" };
#endif
            if (args == null || args.Length == 0) args = new string[] { "-p" };

            Console.Clear();
            PrintProgramInfo();

            if (Manipulator.TryAttachToProcess(DIVA_PROCESS_NAME)) Console.WriteLine("    DIVA HOOK        : OK");
            else { Console.WriteLine("    DIVA HOOK        : NG"); Thread.Sleep(5000); return; }
            Manipulator.SetMainWindowActive();

            components = new List<Component>();
            components.Add(system = new Watchdog(Manipulator));

            foreach (var arg in args.Select(a => a.ToLower().Trim()).Distinct())
            {
                if (arg.Length < 2) continue;
                var cmd = arg.Substring(1, 1);
                switch (cmd)
                {
                    case "t": // Touch Emulator
                        components.Add(new TouchEmulator(Manipulator));
                        break;
                    case "s": // Scale Component
                        components.Add(new ScaleComponent(Manipulator));
                        break;
                    case "p": // Player Data
                        components.Add(new PlayerDataManager(Manipulator));
                        break;
                    case "f": // System Timer
                        system.SysTimer_Start();
                        break;
                    case "k": // Keychip Id
                        if (arg.Length < 4) break;
                        system.KeychipId = arg.Substring(3).Trim().ToUpper();
                        break;
                    case "m": // Main Id
                        if (arg.Length < 4) break;
                        system.MainId = arg.Substring(3).Trim().ToUpper();
                        break;
                    default:
                        break;
                }
            }

            foreach (var component in components)
            {
                component.Start();
            }

            Thread.Sleep(1000);
            consoleY = Console.CursorTop;
            Console.WriteLine("    APPLICATION      : OK");

            while (!stopFlag && Manipulator.IsProcessRunning())
            {
                Application.DoEvents();
            }

            Console.CursorTop = consoleY;
            Console.WriteLine("    APPLICATION      : EXITED");

            if (!stopFlag) Stop();
            Thread.Sleep(5000);
        }
    }
}
