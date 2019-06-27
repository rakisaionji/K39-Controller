using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace K39C
{
    class TouchEmulator : Component
    {
        Manipulator Manipulator;
        // private int consoleY;

        private const long TOUCH_PANEL_TASK_OBJECT = 0x000000014C5CF7E0L;
        private const long TOUCH_PANEL_CONNECTION_STATE = TOUCH_PANEL_TASK_OBJECT + 0x78L;
        private const long TOUCH_PANEL_CONTACT_TYPE = TOUCH_PANEL_TASK_OBJECT + 0xA0L;
        private const long TOUCH_PANEL_X_POSITION = TOUCH_PANEL_TASK_OBJECT + 0x94L;
        private const long TOUCH_PANEL_Y_POSITION = TOUCH_PANEL_TASK_OBJECT + 0x98L;
        private const long TOUCH_PANEL_PRESSURE = TOUCH_PANEL_TASK_OBJECT + 0x9CL;

        public TouchEmulator(Manipulator manipulator)
        {
            Manipulator = manipulator;
        }

        public void Start()
        {
            // consoleY = Console.CursorTop;
            // Console.WriteLine("    TOUCH PANEL      : WAIT");

            MouseHook.Start();
            MouseHook.MouseAction += new MouseEventHandler(MouseHook_MouseAction);
            // Thread.Sleep(5000);

            // Manipulator.WriteInt32(TOUCH_PANEL_CONNECTION_STATE, 1);

            // Console.CursorTop = consoleY;
            Console.WriteLine("    TOUCH PANEL      : OK  ");
        }

        void MouseHook_MouseAction(object sender, MouseEventArgs e)
        {
            if (!Manipulator.IsAttachedProcessActive()) return;
            _isTouching = (e.Button == MouseButtons.Left);
            SendTouch(e.X, e.Y, e.Button == MouseButtons.None ? 1 : 2);
        }

        public void Stop()
        {
            MouseHook.Stop();
            // Console.CursorTop = consoleY;
            // Console.WriteLine("    TOUCH PANEL      : EXITED  ");
        }

        private bool _isTouching = false;

        [StructLayout(LayoutKind.Sequential)]
        struct TouchData
        {
            public float pos_x;
            public float pos_y;
            public float pos_ch;
            public int detect;
        }

        private void SendTouch(float x, float y, int state)
        {
            try
            {
                if (Manipulator.ReadInt32(TOUCH_PANEL_CONNECTION_STATE) != 1) Manipulator.WriteInt32(TOUCH_PANEL_CONNECTION_STATE, 1);

                if (_isTouching)
                {
                    var mousePos = new POINT((int)x, (int)y);
                    var relPos = Manipulator.GetMouseRelativePos(mousePos);

                    Manipulator.WriteSingle(TOUCH_PANEL_X_POSITION, relPos.X);
                    Manipulator.WriteSingle(TOUCH_PANEL_Y_POSITION, relPos.Y);
                }
                Manipulator.WriteInt32(TOUCH_PANEL_CONTACT_TYPE, state);
                float pressure = state != 0 ? 1 : 0;
                Manipulator.WriteSingle(TOUCH_PANEL_PRESSURE, pressure);
            }
            catch (Exception)
            {
                Stop();
            }
        }
    }
}
