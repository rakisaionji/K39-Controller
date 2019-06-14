﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace K39C
{
    class PlayerDataManager : Component
    {
        Manipulator Manipulator;
        private Thread thread;
        private bool stopFlag;
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
        private const long PLAYER_LEVEL_ADDRESS = PLAYER_DATA_ADDRESS + 0x120L;
        private const long PLAYER_SKIN_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x548L;
        private const long PLAYER_PLATE_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x124L;
        private const long PLAYER_PLATE_EFF_ADDRESS = PLAYER_DATA_ADDRESS + 0x128L;
        private const long PLAYER_VP_ADDRESS = PLAYER_DATA_ADDRESS + 0x12CL;
        private const long PLAYER_HP_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x130L;
        private const long PLAYER_ACT_TOGGLE_ADDRESS = PLAYER_DATA_ADDRESS + 0x134L;
        private const long PLAYER_ACT_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x138L;
        private const long PLAYER_ACT_SLVOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x13CL;
        private const long PLAYER_PV_SORT_KIND_ADDRESS = PLAYER_DATA_ADDRESS + 0x584L;
        private const long PLAYER_PWD_STAT_ADDRESS = PLAYER_DATA_ADDRESS + 0x668L;
        private const long PLAYER_RANK_DISP_ADDRESS = PLAYER_DATA_ADDRESS + 0xE34L; // interim_ranking_disp_flag
        private const long PLAYER_OPTION_DISP_ADDRESS = PLAYER_DATA_ADDRESS + 0xE35L; // rhythm_game_opt_disp_flag

        private const long PLAYER_PLAY_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D0L; // play_data_id
        private const long PLAYER_ACCEPT_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D4L; // accept_index
        private const long PLAYER_START_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D8L; // start_index

        private const long SET_DEFAULT_PLAYER_DATA_ADDRESS = 0x00000001404A7370L;
        private const long MODSELECTOR_CHECK_FUNCTION_ERRRET_ADDRESS = 0x00000001405869ADL;
        private const long MODSELECTOR_CLOSE_AFTER_MODULE = 0x0000000140583B45L;
        private const long MODSELECTOR_CLOSE_AFTER_CUSTOMIZE = 0x0000000140583C8CL;

        private const long MODULE_TABLE_START = 0x00000001411A8990L;
        private const long MODULE_TABLE_END = 0x00000001411A8A0FL;
        private const long ITEM_TABLE_START = 0x00000001411A8B08L;
        private const long ITEM_TABLE_END = 0x00000001411A8B87L;

        private const long CURRENT_SUB_STATE = 0x0000000140EDA82CL;

        private const string PLAYER_DATA_PATH = "PlayerData.xml";

        private int step = 0;
        private PlayerData playerData;
        private byte[] PlayerNameValue;
        private Int32 PlayerNameAddress;

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
            // Display clear borders on the progress bar (by vladkorotnev)
            Manipulator.WriteByte(PLAYER_DATA_ADDRESS + 0xD94, 0x3);
            // Enable module selection without card (by lybxlpsv) [ WIP / NG ]
            // Manipulator.WritePatch(0x00000001405C5133, new byte[] { 0x74 });
            // Manipulator.WritePatch(0x00000001405BC8E7, new byte[] { 0x74 });
        }

        private void ReadPlayerData()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PlayerData));
                var fs = new FileStream(PLAYER_DATA_PATH, FileMode.Open);
                playerData = (PlayerData)serializer.Deserialize(fs);
            }
            catch (Exception)
            {
                playerData = new PlayerData();
            }
        }

        private void WritePlayerData()
        {
            Manipulator.Write(PlayerNameAddress, PlayerNameValue);
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
            Manipulator.WriteUInt32(PLAYER_PLAY_ID_ADDRESS, playerData.PlayIndex);
            Manipulator.WriteUInt32(PLAYER_ACCEPT_ID_ADDRESS, playerData.PlayIndex);
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
            playerData.PlayIndex = Manipulator.ReadUInt32(PLAYER_PLAY_ID_ADDRESS);
            // Write to file
            var serializer = new XmlSerializer(typeof(PlayerData));
            var writer = new StreamWriter(PLAYER_DATA_PATH);
            serializer.Serialize(writer, playerData);
            writer.Close();
            // First write of play start id, only once per session
            Manipulator.WriteUInt32(PLAYER_START_ID_ADDRESS, playerData.PlayIndex);
        }

        public void Initialize()
        {
            InjectPatches();
            ReadPlayerData();
            PlayerNameValue = new byte[21];
            var b_name = Encoding.UTF8.GetBytes(playerData.PlayerName);
            Buffer.BlockCopy(b_name, 0, PlayerNameValue, 0, b_name.Length);
            PlayerNameAddress = Manipulator.ReadInt32(PLAYER_NAME_ADDRESS);
            if (playerData.Level < 1) playerData.Level = 1;
            if (playerData.ActVol < 0 || playerData.ActVol > 100) playerData.ActVol = 100;
            if (playerData.HpVol < 0 || playerData.HpVol > 100) playerData.HpVol = 100;
            // use_card = 1 // Required to allow for module selection
            Manipulator.WriteInt32(PLAYER_DATA_ADDRESS, 1);
            // Allow player to select the module and extra items (by vladkorotnev)
            for (long i = MODULE_TABLE_START; i <= MODULE_TABLE_END; i++)
            {
                Manipulator.WriteByte(i, 0xFF);
            }
            for (long i = ITEM_TABLE_START; i <= ITEM_TABLE_END; i++)
            {
                Manipulator.WriteByte(i, 0xFF);
            }
            // Display interim rank and rhythm options (despite it is not yet fully functional)
            Manipulator.WriteByte(PLAYER_RANK_DISP_ADDRESS, 1);
            Manipulator.WriteByte(PLAYER_OPTION_DISP_ADDRESS, 1);
            // First write of play start id, only once per starup
            Manipulator.WriteUInt32(PLAYER_START_ID_ADDRESS, playerData.PlayIndex);
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
                    if (step == 2) playerData.PlayIndex--;
                    if (step == 3) SavePlayerData();
                    step = 0;
                    break;
                case SubGameState.SUB_SELECTOR: // 12
                case SubGameState.SUB_GAME_SEL: // 14
                    if (step == 2) playerData.PlayIndex--;
                    if (step == 3) { SavePlayerData(); step = 0; }
                    if (step == 0) WritePlayerData();
                    step = 1;
                    break;
                case SubGameState.SUB_GAME_MAIN: // 13
                    if (step == 1)
                    {
                        if (playerData.PlayIndex < uint.MaxValue) playerData.PlayIndex++;
                        Manipulator.WriteUInt32(PLAYER_PLAY_ID_ADDRESS, playerData.PlayIndex);
                        Manipulator.WriteUInt32(PLAYER_ACCEPT_ID_ADDRESS, playerData.PlayIndex);
                    }
                    step = 2;
                    break;
                case SubGameState.SUB_STAGE_RESULT: // 15
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