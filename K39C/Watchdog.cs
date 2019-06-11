using System.Threading;

namespace K39C
{
    class Watchdog : Component
    {
        Manipulator Manipulator;
        private Thread thread;
        private bool stopFlag;

        public Watchdog(Manipulator manipulator)
        {
            Manipulator = manipulator;
        }

        public void Start()
        {
            if (thread != null) return;
            stopFlag = false;
            thread = new Thread(new ThreadStart(ThreadCallback));
            thread.Start();
        }

        public void Stop()
        {
            stopFlag = true;
            thread = null;
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
                Thread.Sleep(10);
            }
            stopFlag = false;
        }
    }
}
