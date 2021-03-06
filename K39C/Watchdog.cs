﻿using System;
using System.IO;
using System.Text;
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

        private readonly string ANNOUNCE_PATH = Assembly.GetSaveDataPath("BannerData.txt");

        internal Watchdog(Manipulator manipulator, Settings settings)
        {
            Manipulator = manipulator;
            Settings = settings;
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

        private string Announcement
        {
            get
            {
                var txtAddr = Manipulator.ReadInt64(0x140CD9E00);
                if (txtAddr == 0) return String.Empty;
                return Manipulator.ReadUtf8String(txtAddr);
            }
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                var txtUtf8 = Encoding.UTF8.GetBytes(value);
                var txtAddr = Manipulator.AllocateMemory(txtUtf8.Length).ToInt64();
                Manipulator.Write(txtAddr, txtUtf8);
                Manipulator.WriteInt64(0x140CD9E00, txtAddr);
                // Force display annoucement, thanks vladkorotnev
                Manipulator.WriteByte(0x140CD9E18, 0x1F);
                Manipulator.WriteByte(0x140CD9E10, 0x1);
            }
        }

        private void Announce()
        {
            if (File.Exists(ANNOUNCE_PATH))
            {
                Announcement = File.ReadAllText(ANNOUNCE_PATH).Trim();
            }
            else
            {
                var sb = new StringBuilder();
#if DEBUG
                sb.Append(">>>> DEBUG BUILD <<<< ");
#endif
                sb.Append("PDAFT Loader for S39 and K39 by Team Shimapan");
                sb.AppendFormat(" - Codename : {0}", Program.K39C_CODEVER);
                sb.AppendFormat(" - Version : {0}", Program.K39C_VERSION);
                sb.AppendFormat(" - Date : {0}", Program.K39C_RELDATE);
                sb.Append(" - Thank you for your support!");
                Announcement = sb.ToString();
            }
        }

        public void Start()
        {
            KeychipId = Settings.System.KeychipId.Trim();
            MainId = Settings.System.MainId.Trim();

            // Additional patch for annoucement display, by rakisaionji
            Manipulator.WritePatchNop(0x000000014001F680, 4);  //  mov  [rax+10h], rdi
            Manipulator.WritePatchNop(0x000000014001F68E, 3);  //  mov  [rcx], dil
            new Thread(new ThreadStart(Announce)).Start();

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

        internal void Update()
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
