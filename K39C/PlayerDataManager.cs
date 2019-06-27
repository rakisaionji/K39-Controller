using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace K39C
{
    class PlayerDataManager : Component
    {
        Manipulator Manipulator;
        private Thread thread;
        private bool stopFlag;
        // private int consoleY;

        private const long PLAYER_DATA_ADDRESS = 0x0000000140E6E9B0L;
        private const long PLAYER_NAME_ADDRESS = PLAYER_DATA_ADDRESS + 0x0E0L;
        private const long PLAYER_LEVEL_NAME_ADDRESS = PLAYER_DATA_ADDRESS + 0x100L;
        private const long PLAYER_LEVEL_ADDRESS = PLAYER_DATA_ADDRESS + 0x120L;
        private const long PLAYER_SKIN_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x548L;
        private const long PLAYER_PLATE_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x124L;
        private const long PLAYER_PLATE_EFF_ADDRESS = PLAYER_DATA_ADDRESS + 0x128L;
        private const long PLAYER_VP_ADDRESS = PLAYER_DATA_ADDRESS + 0x12CL;
        private const long PLAYER_HP_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x130L;
        private const long PLAYER_ACT_TOGGLE_ADDRESS = PLAYER_DATA_ADDRESS + 0x134L;
        private const long PLAYER_ACT_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x138L;
        private const long PLAYER_ACT_SLVOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x13CL;
        private const long PLAYER_PV_SORT_KIND_ADDRESS = PLAYER_DATA_ADDRESS + 0x494L;
        private const long PLAYER_PWD_STAT_ADDRESS = PLAYER_DATA_ADDRESS + 0x560L;
        private const long PLAYER_CLEAR_BORDER_ADDRESS = PLAYER_DATA_ADDRESS + 0x95CL; // clear_border_disp_bit
        private const long PLAYER_RANK_DISP_ADDRESS = PLAYER_DATA_ADDRESS + 0x9A4L; // interim_ranking_disp_flag

        private const long PLAYER_PLAY_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D0L; // play_data_id
        private const long PLAYER_ACCEPT_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D4L; // accept_index
        private const long PLAYER_START_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D8L; // start_index

        private const long SET_DEFAULT_PLAYER_DATA_ADDRESS = 0x000000014033F5F0L;
        private const long MODSELECTOR_SET_MODULE_ADDRESS = 0x00000001403F27CEL;
        private const long MODSELECTOR_SET_ITEM_A_ADDRESS = 0x00000001403F2BC4L;
        private const long MODSELECTOR_SET_ITEM_B_ADDRESS = 0x00000001403F3AA3L;
        private const long MODSELECTOR_SET_ITEM_C_ADDRESS = 0x00000001403F3594L;
        private const long MODSELECTOR_SET_ITEM_D_ADDRESS = 0x00000001403F3842L;
        private const long MODSELECTOR_MISC_CHECK_ADDRESS = 0x00000001403F2332L;
        private const long MODSELECTOR_CLOSE_AFTER_MODULE = 0x00000001403F3F99L;
        private const long MODSELECTOR_CLOSE_AFTER_CUSTOMIZE = 0x00000001403F3E53L;

        private const long MODULE_TABLE_START = PLAYER_DATA_ADDRESS + 0x140;
        private const long ITEM_TABLE_START = PLAYER_DATA_ADDRESS + 0x290;

        private const long CURRENT_SUB_STATE = 0x0000000140CEFABCL;

        private readonly string PLAYER_DATA_PATH = Assembly.GetSaveDataPath("PlayerData.xml");

        private int step = 0;
        private PlayerData playerData;
        private byte[] PlayerNameValue;
        private byte[] LevelNameValue;
        private long PlayerNameAddress;
        private long LevelNameAddress;

        public PlayerDataManager(Manipulator manipulator)
        {
            Manipulator = manipulator;
        }

        private void InjectPatches()
        {
            // Prevent the PlayerData from being reset so we don't need to keep updating the PlayerData struct
            Manipulator.WritePatch(SET_DEFAULT_PLAYER_DATA_ADDRESS, new byte[] { 0xC3 }); // ret
            // Allow player to select the module
            Manipulator.WritePatch(MODSELECTOR_SET_MODULE_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            Manipulator.WritePatchNop(MODSELECTOR_SET_MODULE_ADDRESS + 0x2L, 0x34); // nop
            // Allow player to select extra item
            Manipulator.WritePatch(MODSELECTOR_SET_ITEM_A_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            Manipulator.WritePatchNop(MODSELECTOR_SET_ITEM_A_ADDRESS + 0x2L, 0x32); // nop
            Manipulator.WritePatch(MODSELECTOR_SET_ITEM_B_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            Manipulator.WritePatchNop(MODSELECTOR_SET_ITEM_B_ADDRESS + 0x2L, 0x32); // nop
            Manipulator.WritePatch(MODSELECTOR_SET_ITEM_C_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            Manipulator.WritePatchNop(MODSELECTOR_SET_ITEM_C_ADDRESS + 0x2L, 0x35); // nop
            Manipulator.WritePatch(MODSELECTOR_SET_ITEM_D_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            Manipulator.WritePatchNop(MODSELECTOR_SET_ITEM_D_ADDRESS + 0x2L, 0x34); // nop
            // Some miscellaneous check
            Manipulator.WritePatch(MODSELECTOR_MISC_CHECK_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            Manipulator.WritePatchNop(MODSELECTOR_MISC_CHECK_ADDRESS + 0x2L, 0x32); // nop
            // Fix annoying behavior of closing after changing module or item
            Manipulator.WritePatch(MODSELECTOR_CLOSE_AFTER_MODULE, new byte[] { 0x85 }); // je --> jne
            Manipulator.WritePatch(MODSELECTOR_CLOSE_AFTER_CUSTOMIZE, new byte[] { 0x85 }); // je --> jne
        }

        private void ReadPlayerData()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PlayerData));
                using (var fs = new FileStream(PLAYER_DATA_PATH, FileMode.Open))
                {
                    playerData = (PlayerData)serializer.Deserialize(fs);
                    if (playerData == null) playerData = new PlayerData();
                    fs.Close();
                }
            }
            catch (Exception)
            {
                playerData = new PlayerData();
            }
        }

        private void WritePlayerData()
        {
            Manipulator.Write(PlayerNameAddress, PlayerNameValue);
            Manipulator.Write(LevelNameAddress, LevelNameValue);
            Manipulator.WriteInt64(PLAYER_LEVEL_NAME_ADDRESS, LevelNameAddress);
            Manipulator.WriteByte(PLAYER_DATA_ADDRESS + 0x110L, 0xFF); // thanks @vladkorotnev
            Manipulator.WriteByte(PLAYER_DATA_ADDRESS + 0x118L, 0x1F); // thanks @vladkorotnev
            Manipulator.WriteInt32(PLAYER_SKIN_EQUIP_ADDRESS, playerData.SkinEquip);
            Manipulator.WriteInt32(PLAYER_LEVEL_ADDRESS, playerData.Level);
            Manipulator.WriteInt32(PLAYER_PLATE_ID_ADDRESS, playerData.PlateId);
            Manipulator.WriteInt32(PLAYER_PLATE_EFF_ADDRESS, playerData.PlateEff);
            Manipulator.WriteInt32(PLAYER_VP_ADDRESS, playerData.VocaloidPoint);
            Manipulator.WriteByte(PLAYER_ACT_TOGGLE_ADDRESS, playerData.ActToggle);
            Manipulator.WriteInt32(PLAYER_ACT_VOL_ADDRESS, playerData.ActVol);
            Manipulator.WriteInt32(PLAYER_ACT_SLVOL_ADDRESS, playerData.ActSlideVol);
            Manipulator.WriteInt32(PLAYER_HP_VOL_ADDRESS, playerData.HpVol);
            Manipulator.WriteInt32(PLAYER_PWD_STAT_ADDRESS, playerData.PasswordStatus);
            Manipulator.WriteInt32(PLAYER_PV_SORT_KIND_ADDRESS, playerData.PvSortKind);
            if (playerData.SetPlayData)
            {
                Manipulator.WriteUInt32(PLAYER_PLAY_ID_ADDRESS, playerData.PlayDataId);
                Manipulator.WriteUInt32(PLAYER_ACCEPT_ID_ADDRESS, playerData.PlayDataId);
            }
        }

        private void SavePlayerData()
        {
            // Manipulator.WriteInt32(GetPlayerNameFAddress(), 0x10);
            playerData.VocaloidPoint = Manipulator.ReadInt32(PLAYER_VP_ADDRESS);
            playerData.ActToggle = Manipulator.ReadByte(PLAYER_ACT_TOGGLE_ADDRESS);
            playerData.ActVol = Manipulator.ReadInt32(PLAYER_ACT_VOL_ADDRESS); ;
            playerData.ActSlideVol = Manipulator.ReadInt32(PLAYER_ACT_SLVOL_ADDRESS);
            playerData.HpVol = Manipulator.ReadInt32(PLAYER_HP_VOL_ADDRESS);
            playerData.PvSortKind = Manipulator.ReadInt32(PLAYER_PV_SORT_KIND_ADDRESS);
            if (playerData.SetPlayData) playerData.PlayDataId = Manipulator.ReadUInt32(PLAYER_PLAY_ID_ADDRESS);
            // Write to file
            var serializer = new XmlSerializer(typeof(PlayerData));
            using (var writer = new StreamWriter(PLAYER_DATA_PATH))
            {
                serializer.Serialize(writer, playerData);
                writer.Close();
            }
            // First write of play start id, only once per session
            if (playerData.SetPlayData) Manipulator.WriteUInt32(PLAYER_START_ID_ADDRESS, playerData.PlayDataId);
        }

        public void Initialize()
        {
            InjectPatches();
            ReadPlayerData();
            PlayerNameValue = new byte[21];
            var b_name = Encoding.UTF8.GetBytes(playerData.PlayerName);
            Buffer.BlockCopy(b_name, 0, PlayerNameValue, 0, b_name.Length);
            PlayerNameAddress = Manipulator.ReadInt64(PLAYER_NAME_ADDRESS);
            LevelNameValue = new byte[29];
            var c_name = Encoding.UTF8.GetBytes(playerData.LevelName);
            Buffer.BlockCopy(c_name, 0, LevelNameValue, 0, c_name.Length);
            LevelNameAddress = (long)Marshal.UnsafeAddrOfPinnedArrayElement(LevelNameValue, 0);
            if (playerData.Level < 1) playerData.Level = 1;
            if (playerData.ActVol < 0 || playerData.ActVol > 100) playerData.ActVol = 100;
            if (playerData.HpVol < 0 || playerData.HpVol > 100) playerData.HpVol = 100;
            // use_card = 1 // Required to allow for PV Mode and module selection
            Manipulator.WriteInt32(PLAYER_DATA_ADDRESS, 1);
            // Allow player to select the module and extra items
            for (long i = 0; i < 128; i++) Manipulator.WriteByte(MODULE_TABLE_START + i, 0xFF);
            for (long i = 0; i < 128; i++) Manipulator.WriteByte(ITEM_TABLE_START + i, 0xFF);
            // Display interim rank (despite it is not yet fully functional)
            Manipulator.WriteByte(PLAYER_RANK_DISP_ADDRESS, 1);
            // Display clear borders on the progress bar
            Manipulator.WriteByte(PLAYER_CLEAR_BORDER_ADDRESS, playerData.ClearBorder.ToByte());
            // First write of play start id, only once per starup
            if (playerData.SetPlayData) Manipulator.WriteUInt32(PLAYER_START_ID_ADDRESS, playerData.PlayDataId);
            WritePlayerData();
        }

        public void Update()
        {
            var currentSubState = (SubGameState)Manipulator.ReadInt32(CURRENT_SUB_STATE);
            // 4       .. save all when flag = 3 / flag = 0
            // 12 + 14 .. refresh->set flag = 1
            // 13      .. if flag = 1 , increase game id; set flag = 2
            // 15      .. set flag = 3
            switch (currentSubState)
            {
                case SubGameState.SUB_LOGO: // 4
                    if (step == 2 && playerData.SetPlayData) playerData.PlayDataId--;
                    if (step == 3) SavePlayerData();
                    step = 0;
                    break;
                case SubGameState.SUB_SELECTOR: // 11
                case SubGameState.SUB_GAME_SEL: // 13
                    if (step == 2 && playerData.SetPlayData) playerData.PlayDataId--;
                    if (step == 3) { SavePlayerData(); step = 0; }
                    if (step == 0) WritePlayerData();
                    step = 1;
                    break;
                case SubGameState.SUB_GAME_MAIN: // 12
                    if (step == 1 && playerData.SetPlayData)
                    {
                        if (playerData.PlayDataId < uint.MaxValue) playerData.PlayDataId++;
                        Manipulator.WriteUInt32(PLAYER_PLAY_ID_ADDRESS, playerData.PlayDataId);
                        Manipulator.WriteUInt32(PLAYER_ACCEPT_ID_ADDRESS, playerData.PlayDataId);
                    }
                    step = 2;
                    break;
                case SubGameState.SUB_STAGE_RESULT: // 14
                    step = 3;
                    break;
                default:
                    break;
            }
        }

        public void Start()
        {
            if (thread != null) return;
            stopFlag = false;
            Initialize();
            thread = new Thread(new ThreadStart(ThreadCallback));
            thread.Start();
            // consoleY = Console.CursorTop;
            Console.WriteLine("    PLAYER DATA      : OK");
        }

        public void Stop()
        {
            stopFlag = true;
            thread = null;
            // Console.CursorTop = consoleY;
            // Console.WriteLine("    PLAYER DATA      : EXITED");
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
