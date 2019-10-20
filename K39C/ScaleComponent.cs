using System;
using System.Threading;

namespace K39C
{
    // From @lybxlpsv
    class ScaleComponent : Component
    {
        Manipulator Manipulator;
        private Thread thread;
        private bool stopFlag;
        // private int consoleY;
        private RECT lhWindow;

        ////////////////////////////////////////////////////////////////////////////////
        // UI_CRAP_STRUCT_ADDRESS = 0x000000014CC611E8
        // UI_ASPECT_RATIO = UI_CRAP_STRUCT_ADDRESS + 0xFE8
        // UI_WIDTH_ADDRESS = UI_CRAP_STRUCT_ADDRESS + 0xFFC
        // UI_HEIGHT_ADDRESS = UI_CRAP_STRUCT_ADDRESS + 0x1000
        ////////////////////////////////////////////////////////////////////////////////

        private const long FB1_WIDTH_ADDRESS = 0x00000001411AD5F8;
        private const long FB1_HEIGHT_ADDRESS = 0x00000001411AD5FC;

        private const long UI_WIDTH_ADDRESS = 0x000000014CC621E4;
        private const long UI_HEIGHT_ADDRESS = 0x000000014CC621E8;

        private const long FB_ASPECT_RATIO = 0x0000000140FBC2E8;
        private const long UI_ASPECT_RATIO = 0x000000014CC621D0;

        internal ScaleComponent(Manipulator manipulator)
        {
            Manipulator = manipulator;
        }

        private void InjectPatches()
        {
            Manipulator.WritePatch(0x00000001404ACD24, new byte[] { 0x44, 0x8B, 0x0D, 0xD1, 0x08, 0xD0, 0x00 }); // mov r9d, [rbx+63Ch] --> mov r9d, cs:FB1_HEIGHT
            Manipulator.WritePatch(0x00000001404ACD2B, new byte[] { 0x44, 0x8B, 0x05, 0xC6, 0x08, 0xD0, 0x00 }); // mov r8d, [rbx+638h] --> mov r8d, cs:FB1_WIDTH
            Manipulator.WritePatchNop(0x00000001405030A0, 6); // Whatever shitty checking flag, only Froggy knows
        }

        internal void Update()
        {
            Manipulator.GetClientRect(Manipulator.AttachedProcess.MainWindowHandle, out RECT hWindow);

            if (hWindow.Equals(lhWindow)) return;
            if (hWindow.Bottom - hWindow.Top == 0) return;

            Manipulator.WriteSingle(UI_ASPECT_RATIO, (float)(hWindow.Right - hWindow.Left) / (float)(hWindow.Bottom - hWindow.Top));
            Manipulator.WriteDouble(FB_ASPECT_RATIO, (double)(hWindow.Right - hWindow.Left) / (double)(hWindow.Bottom - hWindow.Top));
            Manipulator.WriteSingle(UI_WIDTH_ADDRESS, hWindow.Right - hWindow.Left);
            Manipulator.WriteSingle(UI_HEIGHT_ADDRESS, hWindow.Bottom - hWindow.Top);
            Manipulator.WriteInt32(FB1_WIDTH_ADDRESS, hWindow.Right - hWindow.Left);
            Manipulator.WriteInt32(FB1_HEIGHT_ADDRESS, hWindow.Bottom - hWindow.Top);

            Manipulator.WriteInt32(0x00000001411AD608, 0); // Set that fucking whatever shitty checking flag to 0
            Manipulator.WriteInt32(0x0000000140EDA8E4, Manipulator.ReadInt32(0x0000000140EDA8BC)); // RESOLUTION_WIDTH
            Manipulator.WriteInt32(0x0000000140EDA8E8, Manipulator.ReadInt32(0x0000000140EDA8C0)); // RESOLUTION_HEIGHT

            Manipulator.WriteSingle(0x00000001411A1900, 0); // WTF FROGGY? 0x00000001411A1870 + 0x90
            Manipulator.WriteSingle(0x00000001411A1904, (float)Manipulator.ReadInt32(0x0000000140EDA8BC)); // RESOLUTION_WIDTH
            Manipulator.WriteSingle(0x00000001411A1908, (float)Manipulator.ReadInt32(0x0000000140EDA8C0)); // RESOLUTION_HEIGHT

            lhWindow = hWindow;
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
