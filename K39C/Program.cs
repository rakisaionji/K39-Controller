﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly string K39C_VERSION = "0.20.00";
        private static readonly string K39C_RELDATE = "2019-06-30";
        private static readonly string APP_SETTING_PATH = "Settings.xml";
        private static readonly string DIVA_PROCESS_NAME = "diva";
        private static readonly string PLUGIN_LOADER_NAME = "DllInjector.exe";

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
            Console.WriteLine("          PDAFT 1.01.00 Controller for S39 and K39          ");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("   Minimal and less intrusive controller for real cabinet   ");
            Console.WriteLine("                       by rakisaionji                       ");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine(" > Codename : K39-COCO ");
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
                        Settings.TouchEmulator = true;
                        break;
                    case "p": // Player Data
                        Settings.PlayerDataManager = true;
                        break;
                    case "c": // Module Manager
                        Settings.PvModuleManager = true;
                        break;
                    case "f": // System Timer
                        Settings.SysTimer = true;
                        break;
                    case "i": // Plugin Load
                        if (arg.Length < 4) break;
                        var i = arg.Substring(3).Split(',');
                        foreach (var f in i) Settings.DivaPlugins.Add(f.Trim());
                        break;
                    case "k": // Keychip Id
                        if (arg.Length < 4) break;
                        Settings.KeychipId = arg.Substring(3).Trim().ToUpper();
                        break;
                    case "m": // Main Id
                        if (arg.Length < 4) break;
                        Settings.MainId = arg.Substring(3).Trim().ToUpper();
                        break;
                    default:
                        break;
                }
            }
        }

        static void StartDiva()
        {
            var pa = Settings.DivaPath.Trim();
            if (String.IsNullOrEmpty(pa) || !File.Exists(pa)) return;

            var fi = new FileInfo(pa);
            var ar = Settings.Arguments;
            var wd = fi.DirectoryName;
            if (!(Manipulator.CreateProcess(pa, ar, wd, out IntPtr ht))) return;
            if (!Manipulator.TryAttachToProcess(DIVA_PROCESS_NAME)) return;

            if (Settings.ApplyPatch)
            {
                var pt = new DivaPatcher(Manipulator, Settings);
                pt.ApplyPatches();
            }

            Manipulator.ResumeThread(ht);
            WaitForDiva();
        }

        static void WaitForDiva()
        {
            if (Settings.WaitTime < 0) Settings.WaitTime = 0;
            if (Settings.WaitTime > 60) Settings.WaitTime = 60;
            for (int i = 0; i < Settings.WaitTime; i++)
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
            args = new string[] { "-t", "-c", "-f", "-i:FastLoader", "-k:A61E-01A07376003", "-m:AAVE-01A03965611" };
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
            if (Settings.TouchEmulator) components.Add(new TouchEmulator(Manipulator));
            // if (Settings.ScaleComponent) components.Add(new ScaleComponent(Manipulator));
            if (Settings.PlayerDataManager)
            {
                components.Add(new PlayerDataManager(Manipulator));
                if (Settings.PvModuleManager) components.Add(new PvModuleManager(Manipulator));
            }

            foreach (var component in components)
            {
                component.Start();
            }

            foreach (var plugin in Settings.DivaPlugins)
            {
                if (!File.Exists(PLUGIN_LOADER_NAME)) break;
                var file = String.Format("{0}.dll", plugin);
                if (File.Exists(file))
                {
                    Process.Start(PLUGIN_LOADER_NAME, file);
                    Console.WriteLine(String.Format("    {0} : OK", GetPluginLabel(plugin)));
                }
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
