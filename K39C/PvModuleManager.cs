using System;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace K39C
{
    class PvModuleManager : Component
    {
        Manipulator Manipulator;
        private Thread thread;
        private bool stopFlag;

        private readonly string MODULE_LIST_PATH = Assembly.GetSaveDataPath("PvCostume.xml");

        private const long PLAYER_MODULE_ADDRESS = 0x00000001410F2820L + 0x1C0L;
        private const long SEL_PVID_BYFRAME_ADDRESS = 0x0000000141136094L;
        private const long CURRENT_SUB_STATE = 0x0000000140E8E09CL;

        private PvModules PvModules;
        private int lastPvId;

        public PvModuleManager(Manipulator manipulator)
        {
            Manipulator = manipulator;
        }

        private void LoadPvModules()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PvModules));
                using (var fs = new FileStream(MODULE_LIST_PATH, FileMode.Open))
                {
                    PvModules = (PvModules)serializer.Deserialize(fs);
                    if (PvModules == null) stopFlag = true;
                    fs.Close();
                }
            }
            catch (Exception)
            {
                stopFlag = true;
            }
        }

        public void Update()
        {
            var currentSubState = (SubGameState)Manipulator.ReadInt32(CURRENT_SUB_STATE);
            if (currentSubState == SubGameState.SUB_SELECTOR || currentSubState == SubGameState.SUB_GAME_SEL)
            {
                var pvId = Manipulator.ReadInt32(SEL_PVID_BYFRAME_ADDRESS);
                if (pvId == lastPvId || pvId == -1) return;
                var pvMd = PvModules.Get(pvId);
                if (pvMd == null) return;
                var i = 0;
                foreach (var md in pvMd.Costumes)
                {
                    var addr = PLAYER_MODULE_ADDRESS + (i * 0x4L);
                    Manipulator.WriteInt32(addr, md); i++;
                }
                while (i <= 6)
                {
                    var addr = PLAYER_MODULE_ADDRESS + (i * 0x4L);
                    Manipulator.WriteInt32(addr, 0); i++;
                }
                lastPvId = pvId;
            }
        }

        public void Start()
        {
            if (thread != null) return;
            stopFlag = false;
            lastPvId = 0;
            LoadPvModules();
            thread = new Thread(new ThreadStart(ThreadCallback));
            thread.Start();
            Console.WriteLine("    MODULE MANAGER   : OK");
        }

        public void Stop()
        {
            stopFlag = true;
            thread = null;
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
