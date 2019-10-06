using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace K39C
{
    class Program
    {
        internal static readonly string K39C_CODEVER = "K39-PICO";
        internal static readonly string K39C_VERSION = "7.10.41";
        internal static readonly string K39C_RELDATE = "2019-10-01";

        private static readonly string APP_SETTING_PATH = Assembly.GetSaveDataPath("Settings.xml");
        private static readonly string DIVA_PROCESS_NAME = "diva";

        private static Manipulator Manipulator = new Manipulator();

        private static List<Component> components;
        private static bool stopFlag = false;
        private static int consoleY;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static Settings Settings { get; private set; }

        static void PrintProgramInfo()
        {
            Console.WriteLine("------------------------------------------------------------");
#if DEBUG
            Console.WriteLine("              ---- >>>> DEBUG_BUILD <<<< ----               ");
#endif
            Console.WriteLine("                PDAFT Loader for S39 and K39                ");
            Console.WriteLine("                      by Team Shimapan                      ");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine(" > Codename : {0} ", K39C_CODEVER);
            Console.WriteLine(" > Date     : {0} ", K39C_RELDATE);
            Console.WriteLine(" > Version  : {0} ", K39C_VERSION);
            Console.WriteLine("------------------------------------------------------------");
        }

        public static void Stop()
        {
            stopFlag = true;
            foreach (var component in components) component.Stop();
            Manipulator.CloseHandles();
        }

        private static void LockConsole()
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

        private static void LoadSettings()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Settings));
                using (var fs = new FileStream(APP_SETTING_PATH, FileMode.Open))
                {
                    Settings = (Settings)serializer.Deserialize(fs);
                    if (Settings == null) Settings = new Settings();
                    fs.Close();
                }
            }
            catch (Exception)
            {
                Settings = new Settings();
            }
        }

        private static void SaveSettings() // Write to file
        {
            var serializer = new XmlSerializer(typeof(Settings));
            using (var writer = new StreamWriter(APP_SETTING_PATH))
            {
                serializer.Serialize(writer, Settings);
                writer.Close();
            }
        }

        private static void SaveSettings(string[] args) // Read from args
        {
            if (args == null || args.Length == 0) return;
            Settings.Reset();
            foreach (var arg in args.Select(a => a.Trim()).Distinct())
            {
                if (arg.Length < 2) continue;
                var cmd = arg.Substring(1, 1).ToLower();
                switch (cmd)
                {
                    case "t": // Touch Emulator
                        Settings.Components.TouchEmulator = true;
                        break;
                    case "s": // Scale Component
                        Settings.Components.ScaleComponent = true;
                        break;
                    case "p": // Player Data
                        Settings.Components.PlayerDataManager = true;
                        break;
                    case "f": // System Timer
                        Settings.System.SysTimer = true;
                        break;
                    case "i": // Plugin Loader
                        if (arg.Length < 4) break;
                        var i = arg.Substring(3).Split(',');
                        foreach (var f in i) Settings.DivaPlugins.Add(f.Trim());
                        break;
                    case "k": // Keychip Id
                        if (arg.Length < 4) break;
                        Settings.System.KeychipId = arg.Substring(3).Trim().ToUpper();
                        break;
                    case "m": // Main Id
                        if (arg.Length < 4) break;
                        Settings.System.MainId = arg.Substring(3).Trim().ToUpper();
                        break;
                    default:
                        break;
                }
            }
        }

        static void StartDiva()
        {
            var pa = Settings.Executable.DivaPath.Trim();
            if (String.IsNullOrEmpty(pa) || !File.Exists(pa)) return;

            var fi = new FileInfo(pa);
            var ar = Settings.Executable.Arguments;
            var wd = fi.DirectoryName;
            if (!(Manipulator.CreateProcess(pa, ar, wd, out IntPtr ht))) return;
            if (!Manipulator.TryAttachToProcess(DIVA_PROCESS_NAME)) return;

            if (Settings.Executable.ApplyPatch)
            {
                var pt = new DivaPatcher(Manipulator, Settings);
                pt.ApplyPatches();
            }

            Manipulator.ResumeThread(ht);
            WaitForDiva();
        }

        static void WaitForDiva()
        {
            var waitTime = Settings.Executable.WaitTime;
            if (waitTime < 0) waitTime = 0;
            if (waitTime > 60) waitTime = 60;
            Settings.Executable.WaitTime = waitTime;
            for (int i = 0; i < waitTime; i++)
            {
                Console.CursorTop = consoleY;
                Console.WriteLine("    DIVA HOOK        : WAIT " + i);
                Thread.Sleep(1000);
            }
        }

        private static string GetPluginLabel(string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x.ToString() : x.ToString())).ToUpper().PadRight(16).Substring(0, 16);
        }

        static void Main(string[] args)
        {
            LockConsole();
            LoadSettings();
#if DEBUG
            args = new string[] { "-t", "-s", "-p", "-f", "-i:FastLoader", "-k:A61E-01A07376003", "-m:AAVE-01A03965611" };
            Settings.DivaPatches.GlutCursor = GlutCursor.RIGHT_ARROW;
            Settings.DivaPatches.FreePlay = true;
            Settings.DivaPatches.RamPathFix = true;
            Settings.DivaPatches.MdataPathFix = true;
            Settings.DivaPatches.CardIcon = StatusIcon.OK;
            Settings.DivaPatches.NetIcon = StatusIcon.OK;
#endif
            SaveSettings(args);

            Console.Clear();
            PrintProgramInfo();
            components = new List<Component>();

            consoleY = Console.CursorTop;
            if (!Manipulator.IsProcessRunning(DIVA_PROCESS_NAME)) StartDiva();
            Console.CursorTop = consoleY;
            if (Manipulator.TryAttachToProcess(DIVA_PROCESS_NAME)) Console.WriteLine("    DIVA HOOK        : OK      ");
            else { Console.WriteLine("    DIVA HOOK        : NG      "); Thread.Sleep(5000); return; }
            Manipulator.SetMainWindowActive();

            components.Add(new Watchdog(Manipulator, Settings));
            if (Settings.Components.TouchEmulator) components.Add(new TouchEmulator(Manipulator));
            if (Settings.Components.ScaleComponent) components.Add(new ScaleComponent(Manipulator));
            if (Settings.Components.PlayerDataManager) components.Add(new PlayerDataManager(Manipulator));

            foreach (var component in components)
            {
                component.Start();
            }

            foreach (var plugin in Settings.DivaPlugins)
            {
                var file = Assembly.GetSaveDataPath(String.Format("{0}.dll", plugin));
                if (File.Exists(file) && Manipulator.InjectDll(file))
                    Console.WriteLine(String.Format("    {0} : OK", GetPluginLabel(plugin)));
                else
                    Console.WriteLine(String.Format("    {0} : NG", GetPluginLabel(plugin)));
            }

            Thread.Sleep(1000);
            SaveSettings();

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
