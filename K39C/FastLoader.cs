using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace K39C
{
    public class FastLoader
    {
        Manipulator Manipulator;
        Thread stThread;

        public FastLoader(Manipulator manipulator)
        {
            Manipulator = manipulator;
        }

        private const long UPDATE_TASKS_ADDRESS = 0x000000014019B980L;
        private const long CURRENT_GAME_STATE_ADDRESS = 0x0000000140EDA810L;
        private const long DATA_INIT_STATE_ADDRESS = 0x0000000140EDA7A8L;
        private const long SYSTEM_WARNING_ELAPSED_ADDRESS = 0x00000001411A1430L;
        private const long SYSTEM_WARNING_ELAPSED_FRAME_ADDRESS = (SYSTEM_WARNING_ELAPSED_ADDRESS + 0x68L);

        private GameState currentGameState;
        private GameState previousGameState;
        const int updatesPerFrame = 39;
        bool dataInitialized = false;
        // private bool _stopFlag = false;

        private delegate void UpdateTask();

        public void Start()
        {
            if (stThread != null) return;
            stThread = new Thread(new ThreadStart(this.StateThread));
            stThread.Start();
        }

        public void Stop()
        {
            dataInitialized = true;
        }

        public void StateThread()
        {
            byte[] buf = new byte[16];
            IntPtr read = IntPtr.Zero;
            if (dataInitialized) return;

            previousGameState = currentGameState;
            currentGameState = (GameState)Manipulator.ReadInt32(CURRENT_GAME_STATE_ADDRESS);

            if (currentGameState == GameState.GS_STARTUP)
            {
                var updateTask = Marshal.GetDelegateForFunctionPointer<UpdateTask>((IntPtr)UPDATE_TASKS_ADDRESS);

                // Speed up TaskSystemStartup
                for (int i = 0; i < updatesPerFrame; i++) updateTask();

                // Skip most of TaskDataInit
                Manipulator.WriteInt32(DATA_INIT_STATE_ADDRESS, 3);
                // DATA_INITIALIZED = 3;

                // Skip the 600 frames of TaskWarning
                Manipulator.WriteInt32(SYSTEM_WARNING_ELAPSED_FRAME_ADDRESS, 3939);
            }
            else if (previousGameState == GameState.GS_STARTUP)
            {
                dataInitialized = true;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct TouchData
        {
            public float pos_x;
            public float pos_y;
            public float pos_ch;
            public int detect;
        }
    }
}
