using System;
using System.IO;
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
        private DivaScore divaScore;
        // private int consoleY;

        ////////////////////////////////////////////////////////////////////////////////
        // ===== PATCH.TXT DESCRIPTIONS =====
        // // Return early before resetting to the default PlayerData so we don't need to keep updating the PlayerData struct
        // 0x00000001404A7370 : 0x5 : 48 89 5C 24 08 : C3 90 90 90 90 
        // // Allow player to select the module and extra items (by vladkorotnev)
        // 0x00000001405869AD : 0x2 : 32 C0 : B0 01 
        // // Fix annoying behavior of closing after changing module or item (by vladkorotnev)
        // 0x0000000140583B45 : 0x1 : 84 : 85 
        // 0x0000000140583C8C : 0x1 : 84 : 85 
        ////////////////////////////////////////////////////////////////////////////////

        private const long PLAYER_DATA_ADDRESS = 0x00000001411A8850L;
        private const long PLAYER_NAME_ADDRESS = PLAYER_DATA_ADDRESS + 0x0E0L;
        private const long PLAYER_LEVEL_NAME_ADDRESS = PLAYER_DATA_ADDRESS + 0x100L;
        private const long PLAYER_LEVEL_ADDRESS = PLAYER_DATA_ADDRESS + 0x120L;
        private const long PLAYER_SKIN_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x54CL; // skin_equip_cmn
        private const long PLAYER_SKIN_USEPV_ADDRESS = PLAYER_DATA_ADDRESS + 0x550L; // use_pv_skin_equip
        private const long PLAYER_BTSE_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x558L; // btn_se_equip_cmn
        private const long PLAYER_BTSE_USEPV_ADDRESS = PLAYER_DATA_ADDRESS + 0x55CL; // se_pv_btn_se_equip
        private const long PLAYER_SLSE_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x564L; // slide_se_equip_cmn
        private const long PLAYER_SLSE_USEPV_ADDRESS = PLAYER_DATA_ADDRESS + 0x568L; // use_pv_slide_se_equip
        private const long PLAYER_CSSE_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x570L; // chainslide_se_equip_cmn
        private const long PLAYER_CSSE_USEPV_ADDRESS = PLAYER_DATA_ADDRESS + 0x574L; // use_pv_chainslide_se_equip
        private const long PLAYER_STSE_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x57CL; // slidertouch_se_equip_cmn
        private const long PLAYER_STSE_USEPV_ADDRESS = PLAYER_DATA_ADDRESS + 0x580L; // use_pv_slidertouch_se_equip
        private const long PLAYER_PLATE_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x124L;
        private const long PLAYER_PLATE_EFF_ADDRESS = PLAYER_DATA_ADDRESS + 0x128L;
        private const long PLAYER_VP_ADDRESS = PLAYER_DATA_ADDRESS + 0x12CL;
        private const long PLAYER_HP_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x130L;
        private const long PLAYER_ACT_TOGGLE_ADDRESS = PLAYER_DATA_ADDRESS + 0x134L;
        private const long PLAYER_ACT_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x138L;
        private const long PLAYER_ACT_SLVOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x13CL;
        private const long PLAYER_PV_SORT_KIND_ADDRESS = PLAYER_DATA_ADDRESS + 0x584L;
        private const long PLAYER_PWD_STAT_ADDRESS = PLAYER_DATA_ADDRESS + 0x668L;
        private const long PLAYER_CLEAR_BORDER_ADDRESS = PLAYER_DATA_ADDRESS + 0xD94L; // clear_border_disp_bit
        private const long PLAYER_RANK_DISP_ADDRESS = PLAYER_DATA_ADDRESS + 0xE34L; // interim_ranking_disp_flag
        private const long PLAYER_OPTION_DISP_ADDRESS = PLAYER_DATA_ADDRESS + 0xE35L; // rhythm_game_opt_disp_flag
        private const long PLAYER_USE_PV_MODULE_ADDRESS = PLAYER_DATA_ADDRESS + 0x2B0L; // use_pv_module_equip

        private const long PLAYER_PLAY_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D0L; // play_data_id
        private const long PLAYER_ACCEPT_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D4L; // accept_index
        private const long PLAYER_START_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D8L; // start_index

        private const long SET_DEFAULT_PLAYER_DATA_ADDRESS = 0x00000001404A7370L;
        private const long MODSELECTOR_CHECK_FUNCTION_ERRRET_ADDRESS = 0x00000001405869ADL;
        private const long MODSELECTOR_CLOSE_AFTER_MODULE = 0x0000000140583B45L;
        private const long MODSELECTOR_CLOSE_AFTER_CUSTOMIZE = 0x0000000140583C8CL;

        private const long MODULE_TABLE_START = PLAYER_DATA_ADDRESS + 0x140;
        private const long ITEM_TABLE_START = PLAYER_DATA_ADDRESS + 0x2B8;

        private const long CURRENT_SUB_STATE = 0x0000000140EDA82CL;
        private const long CURRENT_PVID_ADDRESS = 0x00000001418054C4L;

        private readonly string PLAYER_DATA_PATH = Assembly.GetSaveDataPath("PlayerData.xml");

        private int step = 0;
        private PlayerData playerData;
        private byte[] PlayerNameValue;
        private byte[] LevelNameValue;
        private long PlayerNameAddress;
        private long LevelNameAddress;
        private int lastPvId = 0;
        private Random rnd = new Random();
        private int acceptIdx;
        private int startIdx;
        // private uint playIdx;

        public PlayerDataManager(Manipulator manipulator)
        {
            Manipulator = manipulator;
        }

        private void InjectPatches()
        {
            // Prevent the PlayerData from being reset so we don't need to keep updating the PlayerData struct
            Manipulator.WritePatch(SET_DEFAULT_PLAYER_DATA_ADDRESS, new byte[] { 0xC3 }); // ret
            // Allow player to select the module and extra item (by vladkorotnev)
            Manipulator.WritePatch(MODSELECTOR_CHECK_FUNCTION_ERRRET_ADDRESS, new byte[] { 0xB0, 0x01 }); // xor al,al -> ld al,1
            // Fix annoying behavior of closing after changing module or item  (by vladkorotnev)
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
            Manipulator.WriteInt32(PLAYER_LEVEL_ADDRESS, playerData.Level);
            Manipulator.WriteInt32(PLAYER_PLATE_ID_ADDRESS, playerData.PlateId);
            Manipulator.WriteInt32(PLAYER_PLATE_EFF_ADDRESS, playerData.PlateEff);
            Manipulator.WriteInt32(PLAYER_VP_ADDRESS, playerData.VocaloidPoint);
            Manipulator.WriteInt32(PLAYER_PWD_STAT_ADDRESS, playerData.PasswordStatus);
            Manipulator.WriteInt32(PLAYER_PV_SORT_KIND_ADDRESS, playerData.PvSortKind);
            if (playerData.SkinEquip > 0)
            {
                Manipulator.WriteInt32(PLAYER_SKIN_EQUIP_ADDRESS, playerData.SkinEquip);
                Manipulator.WriteInt32(PLAYER_SKIN_USEPV_ADDRESS, 1);
            }
            if (playerData.BtnSeEquip > 0)
            {
                Manipulator.WriteInt32(PLAYER_BTSE_EQUIP_ADDRESS, playerData.BtnSeEquip);
                Manipulator.WriteInt32(PLAYER_BTSE_USEPV_ADDRESS, 1);
            }
            if (playerData.SlideSeEquip > 0)
            {
                Manipulator.WriteInt32(PLAYER_SLSE_EQUIP_ADDRESS, playerData.SlideSeEquip);
                Manipulator.WriteInt32(PLAYER_SLSE_USEPV_ADDRESS, 1);
            }
            if (playerData.ChainSeEquip > 0)
            {
                Manipulator.WriteInt32(PLAYER_CSSE_EQUIP_ADDRESS, playerData.ChainSeEquip);
                Manipulator.WriteInt32(PLAYER_CSSE_USEPV_ADDRESS, 1);
            }
            if (playerData.TouchSeEquip > 0)
            {
                Manipulator.WriteInt32(PLAYER_STSE_EQUIP_ADDRESS, playerData.TouchSeEquip);
                Manipulator.WriteInt32(PLAYER_STSE_USEPV_ADDRESS, 1);
            }
            Manipulator.WriteByte(PLAYER_ACT_TOGGLE_ADDRESS, playerData.ActToggle);
            Manipulator.WriteInt32(PLAYER_ACT_VOL_ADDRESS, playerData.ActVol);
            Manipulator.WriteInt32(PLAYER_ACT_SLVOL_ADDRESS, playerData.ActSlideVol);
            Manipulator.WriteInt32(PLAYER_HP_VOL_ADDRESS, playerData.HpVol);
            if (playerData.SetPlayData)
            {
                Manipulator.WriteUInt32(PLAYER_PLAY_ID_ADDRESS, 1);
                // Manipulator.WriteUInt32(PLAYER_PLAY_ID_ADDRESS, playIdx);
                Manipulator.WriteInt32(PLAYER_ACCEPT_ID_ADDRESS, acceptIdx);
                Manipulator.WriteInt32(PLAYER_START_ID_ADDRESS, startIdx);
            }
        }

        private void SavePlayerData()
        {
            playerData.VocaloidPoint = Manipulator.ReadInt32(PLAYER_VP_ADDRESS);
            playerData.ActToggle = Manipulator.ReadByte(PLAYER_ACT_TOGGLE_ADDRESS);
            playerData.ActVol = Manipulator.ReadInt32(PLAYER_ACT_VOL_ADDRESS); ;
            playerData.ActSlideVol = Manipulator.ReadInt32(PLAYER_ACT_SLVOL_ADDRESS);
            playerData.HpVol = Manipulator.ReadInt32(PLAYER_HP_VOL_ADDRESS);
            playerData.PvSortKind = Manipulator.ReadInt32(PLAYER_PV_SORT_KIND_ADDRESS);
            // if (playerData.SetPlayData) playerData.PlayDataId = playIdx;
            // Write to file
            var serializer = new XmlSerializer(typeof(PlayerData));
            using (var writer = new StreamWriter(PLAYER_DATA_PATH))
            {
                serializer.Serialize(writer, playerData);
                writer.Close();
            }
            if (divaScore != null) divaScore.SavePlayerScoreData();
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
            LevelNameAddress = Manipulator.ReadInt64(PLAYER_LEVEL_NAME_ADDRESS);
            Manipulator.WriteByte(PLAYER_LEVEL_NAME_ADDRESS + 0x10L, 0xFF); // thanks @vladkorotnev
            Manipulator.WriteByte(PLAYER_LEVEL_NAME_ADDRESS + 0x18L, 0x1F); // thanks @vladkorotnev
            if (playerData.Level < 1) playerData.Level = 1;
            if (playerData.ActVol < 0 || playerData.ActVol > 100) playerData.ActVol = 100;
            if (playerData.HpVol < 0 || playerData.HpVol > 100) playerData.HpVol = 100;
            // use_card = 1 // Required to allow for module selection
            if (playerData.UseCard) Manipulator.WriteInt32(PLAYER_DATA_ADDRESS, 1);
            // Allow player to select the module and extra items (by vladkorotnev)
            for (long i = 0; i < 128; i++) Manipulator.WriteByte(MODULE_TABLE_START + i, 0xFF);
            for (long i = 0; i < 128; i++) Manipulator.WriteByte(ITEM_TABLE_START + i, 0xFF);
            // Display interim rank and rhythm options
            Manipulator.WriteByte(PLAYER_RANK_DISP_ADDRESS, 1);
            // Display custom pv module options
            Manipulator.WriteByte(PLAYER_USE_PV_MODULE_ADDRESS, 1);
            // Discovered by vladkorotnev, improved by rakisaionji
            if (playerData.OptionDisp)
            {
                // Allow to use without use_card (by somewhatlurker)
                if (!playerData.UseCard)
                {
                    Manipulator.WritePatchNop(0x00000001405CB14A, 6);
                    Manipulator.WritePatchNop(0x0000000140136CFA, 6);
                }
                Manipulator.WriteByte(PLAYER_OPTION_DISP_ADDRESS, 1);
                Manipulator.WritePatchNop(0x00000001405CA0F5, 2); // Allow it to be displayed
                Manipulator.WritePatchNop(0x00000001405CB1B3, 13); // Allow it to be set and used
                if (playerData.KeepOption)
                {
                    // It was reset when changing level or song, annoying so prevent it
                    Manipulator.WritePatchNop(0x00000001405C84EE, 6); // Prevent it to be reset
                    Manipulator.WritePatchNop(0x00000001405C84F9, 3); // Prevent it to be reset
                }
            }
            // Enable module selection without card (by lybxlpsv and crash5band) [WIP / NG]
            // if (!playerData.UseCard)
            // {
            //     Manipulator.WritePatch(0x00000001405C513B, new byte[] { 0x01 });
            //     Manipulator.WritePatch(0x000000014010523F, new byte[] { 0x30, 0xC0, 0x90 });
            // }
            // Display clear borders on the progress bar (by vladkorotnev)
            Manipulator.WriteByte(PLAYER_CLEAR_BORDER_ADDRESS, playerData.ClearBorder.ToByte());
            // First write of play start id, only once per starup
            // if (playerData.SetPlayData)
            // {
            //     playIdx = playerData.PlayDataId;
            //     if (playIdx < 10001 || playIdx == uint.MaxValue) playIdx = 10001;
            //     Manipulator.WriteUInt32(PLAYER_PLAY_ID_ADDRESS, playIdx);
            // }
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
                    if (step == 3) SavePlayerData();
                    step = 0;
                    break;
                case SubGameState.SUB_SELECTOR: // 12
                case SubGameState.SUB_GAME_SEL: // 14
                    if (step == 3) { SavePlayerData(); WritePlayerData(); }
                    if (step == 0)
                    {
                        acceptIdx = rnd.Next(1000, 1500);
                        startIdx = rnd.Next(1500, 2000);
                        WritePlayerData();
                    }
                    step = 1;
                    var pvId = Manipulator.ReadInt32(CURRENT_PVID_ADDRESS);
                    if (pvId != lastPvId)
                    {
                        if (divaScore != null) divaScore.SaveCurrentPvSetting(lastPvId);
                        lastPvId = pvId; SavePlayerData();
                    }
                    break;
                case SubGameState.SUB_GAME_MAIN: // 13
                    if (step == 1 && divaScore != null) divaScore.SaveCurrentPvSetting(Manipulator.ReadInt32(CURRENT_PVID_ADDRESS));
                    step = 2;
                    break;
                case SubGameState.SUB_STAGE_RESULT: // 15
                    if (step == 2 && divaScore != null) { divaScore.GetScoreResults(); /* playIdx++; SavePlayerData(); */ }
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
            divaScore = new DivaScore(Manipulator);
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
