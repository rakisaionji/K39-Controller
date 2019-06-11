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
        // static readonly string PICO_RELDATE = "2019-06-20";

        static readonly string DIVA_PROCESS_NAME = "diva";
        static Manipulator Manipulator = new Manipulator();

        // static FastLoader fastLoader;
        static List<Component> components;
        static bool stopFlag = false;

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
            // Console.WriteLine(" > Date     : {0} ", PICO_RELDATE);
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
            args = new string[] { "-t", "-s", "-p" };
#endif
            if (args == null || args.Length == 0) args = new string[] { "-p" };

            Console.Clear();
            PrintProgramInfo();

            if (Manipulator.TryAttachToProcess(DIVA_PROCESS_NAME)) Console.WriteLine("    DIVA HOOK        : OK");
            else { Console.WriteLine("    DIVA HOOK        : NG"); Thread.Sleep(5000); return; }

            // fastLoader = new FastLoader(Manipulator);
            // fastLoader.Start();

            components = new List<Component>();
            components.Add(new Watchdog(Manipulator));

            foreach (var arg in args.Select(a => a.ToLower().Trim()).Distinct())
            {
                switch (arg)
                {
                    case "-t":
                        components.Add(new TouchEmulator(Manipulator));
                        break;
                    case "-s":
                        components.Add(new ScaleComponent(Manipulator));
                        break;
                    case "-p":
                        components.Add(new PlayerDataManager(Manipulator));
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
            Console.WriteLine("    APPLICATION      : OK");

            while (!stopFlag && Manipulator.IsProcessRunning())
            {
                Application.DoEvents();
            }

            Console.CursorTop--;
            Console.WriteLine("    APPLICATION      : EXITED");

            foreach (var component in components)
            {
                component.Stop();
            }

            Manipulator.CloseHandles();
            Thread.Sleep(5000);
        }
    }
}
