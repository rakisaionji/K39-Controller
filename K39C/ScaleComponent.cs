using System;
using System.Threading;

namespace K39C
{
    // Originally from @lybxlpsv, enforced for v1.01 by @rakisaionji
    class ScaleComponent : Component
    {
        Manipulator Manipulator;
        private Thread thread;
        private bool stopFlag;
        // private int consoleY;

        ////////////////////////////////////////////////////////////////////////////////
        // UI_CRAP_STRUCT_ADDRESS = 0x0000000140EB67F8
        // UI_ASPECT_RATIO = UI_CRAP_STRUCT_ADDRESS + 0xFE8
        // UI_WIDTH_ADDRESS = UI_CRAP_STRUCT_ADDRESS + 0xFFC
        // UI_HEIGHT_ADDRESS = UI_CRAP_STRUCT_ADDRESS + 0x1000
        ////////////////////////////////////////////////////////////////////////////////

        private const long FB1_WIDTH_ADDRESS = 0x0000000140E780F8;
        private const long FB1_HEIGHT_ADDRESS = 0x0000000140E780FC;

        private const long UI_WIDTH_ADDRESS = 0x0000000140EB73F4;
        private const long UI_HEIGHT_ADDRESS = 0x0000000140EB73F8;

        private const long FB_ASPECT_RATIO = 0x0000000140D9CD68;
        private const long UI_ASPECT_RATIO = 0x0000000140EB73E0;

        public ScaleComponent(Manipulator manipulator)
        {
            Manipulator = manipulator;
        }

        private void InjectPatches()
        {
            Manipulator.WritePatch(0x000000014034215A, new byte[] { 0x44, 0x8B, 0x0D, 0x9B, 0x5F, 0xB3, 0x00 }); // mov r9d, [rbx+63Ch] --> mov r9d, cs:FB1_HEIGHT
            Manipulator.WritePatch(0x0000000140342161, new byte[] { 0x44, 0x8B, 0x05, 0x90, 0x5F, 0xB3, 0x00 }); // mov r8d, [rbx+638h] --> mov r8d, cs:FB1_WIDTH
            Manipulator.WritePatchNop(0x000000014037FB70, 6); // Whatever shitty checking flag, only Froggy knows
        }

        public void Update()
        {
            Manipulator.GetClientRect(Manipulator.AttachedProcess.MainWindowHandle, out RECT hWindow);

            Manipulator.WriteSingle(UI_ASPECT_RATIO, (float)(hWindow.Right - hWindow.Left) / (float)(hWindow.Bottom - hWindow.Top));
            Manipulator.WriteDouble(FB_ASPECT_RATIO, (double)(hWindow.Right - hWindow.Left) / (double)(hWindow.Bottom - hWindow.Top));
            Manipulator.WriteSingle(UI_WIDTH_ADDRESS, hWindow.Right - hWindow.Left);
            Manipulator.WriteSingle(UI_HEIGHT_ADDRESS, hWindow.Bottom - hWindow.Top);
            Manipulator.WriteInt32(FB1_WIDTH_ADDRESS, hWindow.Right - hWindow.Left);
            Manipulator.WriteInt32(FB1_HEIGHT_ADDRESS, hWindow.Bottom - hWindow.Top);

            Manipulator.WriteInt32(0x0000000140E78108, 0); // Set that fucking whatever shitty checking flag to 0
            Manipulator.WriteInt32(0x0000000140CEFB74, Manipulator.ReadInt32(0x0000000140CEFB4C)); // RESOLUTION_WIDTH
            Manipulator.WriteInt32(0x0000000140CEFB78, Manipulator.ReadInt32(0x0000000140CEFB50)); // RESOLUTION_HEIGHT

            Manipulator.WriteSingle(0x0000000140E68240, 0); // WTF FROGGY? 0x0000000140E681B0 + 0x90
            Manipulator.WriteSingle(0x0000000140E68244, (float)Manipulator.ReadInt32(0x0000000140CEFB4C)); // RESOLUTION_WIDTH
            Manipulator.WriteSingle(0x0000000140E68248, (float)Manipulator.ReadInt32(0x0000000140CEFB50)); // RESOLUTION_HEIGHT
        }

        public void Start()
        {
            if (thread != null) return;
            stopFlag = false;
            InjectPatches();
            thread = new Thread(new ThreadStart(ThreadCallback));
            thread.Start();
            // consoleY = Console.CursorTop;
            Console.WriteLine("    SCALE COMPONENT  : OK");
        }

        public void Stop()
        {
            stopFlag = true;
            thread = null;
            // Console.CursorTop = consoleY;
            // Console.WriteLine("    SCALE COMPONENT  : EXITED");
        }

        private void ThreadCallback()
        {
            try
            {
                while (!stopFlag)
                {
                    Update();
                    Thread.Sleep(100);
                }
                stopFlag = false;
            }
            catch (Exception)
            {
                Stop();
            }
        }
    }
}
