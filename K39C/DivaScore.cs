using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Xml.Serialization;

namespace K39C
{
    internal class DivaScore
    {
        private Manipulator Manipulator;
        private PlayerScore playerScore;
        private ScoreHistory scoreHistory;
        private bool isInitialized;
        private long scoreArray;

        private const long PLAYER_DATA_ADDRESS = 0x00000001411A8850L;
        private const long PLAYER_NAME_ADDRESS = PLAYER_DATA_ADDRESS + 0x0E0L;
        private const long PLAYER_LEVEL_NAME_ADDRESS = PLAYER_DATA_ADDRESS + 0x100L;
        private const long PLAYER_LEVEL_ADDRESS = PLAYER_DATA_ADDRESS + 0x120L;
        private const long PLAYER_PLATE_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x124L;
        private const long PLAYER_PLATE_EFF_ADDRESS = PLAYER_DATA_ADDRESS + 0x128L;
        private const long PLAYER_MODULE_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x1C0L;

        private const long RESULTS_BASE_ADDRESS = 0x000000014CC93830L;
        private const long GAME_INFO_ADDRESS = 0x0000000141197E00L;
        private const long CURRENT_SONG_NAME_ADDRESS = 0x0000000140D0A578L;
        private const long SONG_CLEAR_COUNTS_ADDRESS = 0x00000001411A95E8L;

        private readonly string HI_SCORE_PATH = Assembly.GetSaveDataPath("PlayerScore.dat");
        private readonly string SCORE_LOG_PATH = Assembly.GetSaveDataPath(String.Format("ScoreData\\ScoreData_{0:yyyyMMdd}.dat", DateTime.UtcNow));
        private readonly string SCORE_DATA_PATH = Assembly.GetSaveDataPath("ScoreData");

        public DivaScore(Manipulator manipulator)
        {
            isInitialized = false;
            Manipulator = manipulator;
            scoreArray = Manipulator.AllocateMemory(4 * 1000 * 2 * 0xE4).ToInt64();
            for (int i = 0; i < 4; i++)
            {
                Manipulator.WriteInt64(PLAYER_DATA_ADDRESS + i * 0x18 + 0x5D0, scoreArray + i * 1000 * 2 * 0xE4);
                Manipulator.WriteInt64(PLAYER_DATA_ADDRESS + i * 0x18 + 0x5D8, scoreArray + (i + 1) * 1000 * 2 * 0xE4);
            }
            new Thread(new ThreadStart(Initialize)).Start();
        }

        private void Initialize()
        {
            if (!Directory.Exists(SCORE_DATA_PATH)) Directory.CreateDirectory(SCORE_DATA_PATH);
            ReadScoreData();
            UpdateScoreCache();
            UpdateClearCounts();
            ReadHistoryData();
            isInitialized = true;
        }

        private void ReadScoreData()
        {
            try
            {
                var s = new XmlSerializer(typeof(PlayerScore));
                using (var fs = new FileStream(HI_SCORE_PATH, FileMode.Open, FileAccess.Read))
                {
                    using (var gs = new GZipStream(fs, CompressionMode.Decompress))
                    {
                        playerScore = (PlayerScore)s.Deserialize(gs);
                        if (playerScore == null) playerScore = new PlayerScore();
                        gs.Close();
                    }
                    fs.Close();
                }
            }
            catch (Exception)
            {
                playerScore = new PlayerScore();
            }
        }

        private void ReadHistoryData()
        {
            try
            {
                var s = new XmlSerializer(typeof(ScoreHistory));
                using (var fs = new FileStream(SCORE_LOG_PATH, FileMode.Open, FileAccess.Read))
                {
                    using (var gs = new GZipStream(fs, CompressionMode.Decompress))
                    {
                        scoreHistory = (ScoreHistory)s.Deserialize(gs);
                        if (scoreHistory == null) scoreHistory = new ScoreHistory();
                        gs.Close();
                    }
                    fs.Close();
                }
            }
            catch (Exception)
            {
                scoreHistory = new ScoreHistory();
            }
        }

        internal void SaveScoreData()
        {
            if (!isInitialized) return;
            var s = new XmlSerializer(typeof(PlayerScore));
            using (var fs = new FileStream(HI_SCORE_PATH, FileMode.Create, FileAccess.Write))
            {
                using (var gs = new GZipStream(fs, CompressionMode.Compress))
                {
                    s.Serialize(gs, playerScore);
                    gs.Close();
                }
                fs.Close();
            }
        }

        internal void SaveHistoryData()
        {
            if (!isInitialized) return;
            var s = new XmlSerializer(typeof(ScoreHistory));
            using (var fs = new FileStream(SCORE_LOG_PATH, FileMode.Create, FileAccess.Write))
            {
                using (var gs = new GZipStream(fs, CompressionMode.Compress))
                {
                    s.Serialize(gs, scoreHistory);
                    gs.Close();
                }
                fs.Close();
            }
        }

        private CachedScore GetCachedScore(int pvId, int diff, int ed)
        {
            var d = scoreArray + diff * 1000 * 2 * 0xE4;
            var e = d + ed * 1000 * 0xE4;
            var f = e + pvId * 0xE4;
            return new CachedScore(Manipulator, f);
        }

        void UpdateSingleScoreCacheEntry(int pvId, int diff, int ed)
        {
            var entry = playerScore.GetScoreEntry(pvId, diff, ed);
            if (entry.Rank > 1 && entry.Score > 0 && entry.Percent > 0)
            {
                int alltimeScore = entry.AlltimeScore;
                int alltimePercent = entry.AlltimePercent;
                int alltimeRank = entry.AlltimeRank;
                int modifiers = entry.AlltimeModifiers;

                if (alltimeScore == -1) alltimeScore = entry.Score;
                if (alltimePercent == -1) alltimePercent = entry.Percent;
                if (alltimeRank == -1) alltimeRank = entry.Rank;
                if (modifiers == -1) modifiers = entry.Modifiers > 0 ? 1 << (entry.Modifiers - 1) : 0;

                if (alltimeScore > 99999999) alltimeScore = 99999999;

                var cachedScore = GetCachedScore(pvId, diff, ed);
                cachedScore.Score = alltimeScore;
                cachedScore.Percent = alltimePercent;
                cachedScore.Rank = alltimeRank;

                if ((modifiers & 1) != 0) cachedScore.OptionA = 1;
                if ((modifiers & 2) != 0) cachedScore.OptionB = 1;
                if ((modifiers & 4) != 0) cachedScore.OptionC = 1;
            }
        }

        void UpdateScoreCache()
        {
            for (int id = 0; id < 1000; id++)
            {
                for (int diff = 0; diff < 4; diff++)
                {
                    for (int ed = 0; ed < 2; ed++)
                    {
                        var cachedScore = GetCachedScore(id, diff, ed);
                        cachedScore.Initialize(id, ed);
                        UpdateSingleScoreCacheEntry(id, diff, ed);
                        // UpdateSingleScoreCacheRivalEntry(id, diff, ed);
                    }
                }
            }
        }

        void UpdateClearCounts()
        {
            var counts = new int[20];
            for (int i = 0; i < 20; i++)
            {
                counts[i] = 0;
            }

            for (int d = 0; d < 4; d++)
            {
                for (int i = 0; i < 1000; i++)
                {
                    var s = GetCachedScore(i, d, 0);
                    if (s.Rank > 1 && s.Rank <= 5 && s.Edition == 0) // at least clear and no greater than perfect and not ex
                    {
                        counts[d * 4 + s.Rank - 2] += 1;
                    }
                }
            }

            // exex special case
            for (int i = 0; i < 1000; i++)
            {
                var s = GetCachedScore(i, 3, 1);
                if (s.Rank > 1 && s.Rank <= 5 && s.Edition == 1) // at least clear and no greater than perfect and not ex
                {
                    counts[4 * 4 + s.Rank - 2] += 1;
                }
            }

            // perfects count as clears for <= excellent, etc
            for (int d = 0; d < 5; d++)
            {
                counts[d * 4 + 2] += counts[d * 4 + 3];
                counts[d * 4 + 1] += counts[d * 4 + 2];
                counts[d * 4 + 0] += counts[d * 4 + 1];
            }

            for (int i = 0; i < 20; i++)
            {
                Manipulator.WriteInt32(SONG_CLEAR_COUNTS_ADDRESS + i * 4, counts[i]);
            }
        }

        internal void GetScoreResults()
        {
            if (!isInitialized) return;
            var rank = Manipulator.ReadInt32(RESULTS_BASE_ADDRESS + 0xE8);
            var insurance = Manipulator.ReadByte(GAME_INFO_ADDRESS + 0x14);
            if (insurance != 0) return;

            // ========== Get Score Results ========== //

            var recordDate = DateTime.UtcNow;
            var resultBase = Manipulator.ReadInt64(RESULTS_BASE_ADDRESS + 0x100);

            var pvId = Manipulator.ReadInt32(resultBase + 0x2c);
            var difficulty = Manipulator.ReadInt32(resultBase + 0x34);
            var edition = Manipulator.ReadInt32(resultBase + 0x44);
            var modifier = Manipulator.ReadInt32(resultBase + 0x70);

            var cntHitTypes = Manipulator.ReadInt32Array(resultBase + 0x158, 5);
            var pctHitTypes = Manipulator.ReadInt32Array(resultBase + 0x16c, 5);
            var combo = Manipulator.ReadInt32(resultBase + 0x180);
            var challengeScore = Manipulator.ReadInt32(resultBase + 0x184);
            var holdScore = Manipulator.ReadInt32(resultBase + 0x188);
            var score = Manipulator.ReadInt32(resultBase + 0x18c);
            var percent = Manipulator.ReadInt32(resultBase + 0x190);
            var slideScore = Manipulator.ReadInt32(resultBase + 0x194);

            var songNameLen = Manipulator.ReadInt32(CURRENT_SONG_NAME_ADDRESS + 0x18);
            var songNameAdr = songNameLen < 0x10 ? CURRENT_SONG_NAME_ADDRESS : Manipulator.ReadInt64(CURRENT_SONG_NAME_ADDRESS);
            var songName = Manipulator.ReadUtf8String(songNameAdr);

            // ========== Get Player Data ========== //

            var playerNameAdr = Manipulator.ReadInt64(PLAYER_NAME_ADDRESS);
            var playerName = Manipulator.ReadUtf8String(playerNameAdr);
            var levelNameAdr = Manipulator.ReadInt64(PLAYER_LEVEL_NAME_ADDRESS);
            var levelName = Manipulator.ReadUtf8String(levelNameAdr);
            var level = Manipulator.ReadInt32(PLAYER_LEVEL_ADDRESS);
            var plateId = Manipulator.ReadInt32(PLAYER_PLATE_ID_ADDRESS);
            var plateEff = Manipulator.ReadInt32(PLAYER_PLATE_EFF_ADDRESS);
            var moduleEquip = Manipulator.ReadInt32Array(PLAYER_MODULE_EQUIP_ADDRESS, 3);

            // ========== Get High Score Record ========== //

            var entry = playerScore.GetScoreEntry(pvId, difficulty, edition);
            int alltimeScore = entry.AlltimeScore;
            int alltimePercent = entry.AlltimePercent;
            int alltimeRank = entry.AlltimeRank;
            int alltimeModifiers = entry.AlltimeModifiers;

            // ========== Process Score Data ========== //

            if (alltimeScore == -1) alltimeScore = entry.Score;
            if (alltimePercent == -1) alltimePercent = entry.Percent;
            if (alltimeRank == -1) alltimeRank = entry.Rank;
            if (alltimeModifiers == -1) alltimeModifiers = entry.Modifiers > 0 ? 1 << (entry.Modifiers - 1) : 0;

            var isNewScore = (score > alltimeScore);
            var isNewPercent = (percent > alltimePercent);
            var isNewRank = (rank > alltimeRank);

            var notes = new List<NoteScoreEntry>();
            for (int i = 0; i < 5; i++)
            {
                notes.Add(new NoteScoreEntry()
                {
                    Id = i,
                    Count = cntHitTypes[i],
                    Percent = pctHitTypes[i]
                });
            }

            // ========== Update Score History Record ========== //

            var record = new ScoreEntry()
            {
                PvId = pvId,
                Difficulty = difficulty,
                Edition = edition,
                SongName = songName.Trim(),
                PlayerName = playerName.Trim(),
                LevelName = levelName.Trim(),
                Level = level,
                PlateId = plateId,
                PlateEff = plateEff,
                AlltimeScore = (isNewScore) ? score : alltimeScore,
                AlltimePercent = (isNewPercent) ? percent : alltimePercent,
                AlltimeRank = (isNewRank) ? rank : alltimeRank,
                AlltimeModifiers = alltimeModifiers | ((rank > 1 && modifier > 0) ? (1 << (modifier - 1)) : 0),
                Rank = rank,
                Score = score,
                Percent = percent,
                Modifiers = modifier,
                Notes = notes,
                Combo = combo,
                ChallengeScore = challengeScore,
                HoldScore = holdScore,
                SlideScore = slideScore,
                IsNewScore = isNewScore ? 1 : 0,
                IsNewPercent = isNewPercent ? 1 : 0,
                ModuleEquip = moduleEquip,
                RecordDate = recordDate,
            };
            scoreHistory.Scores.Add(record);

            // ========== Update High Score Record ========== //

            if (rank > 1 && (isNewScore || isNewPercent || isNewRank)) playerScore.UpdateScoreEntry(pvId, difficulty, edition, record);

            UpdateSingleScoreCacheEntry(pvId, difficulty, edition);
            UpdateClearCounts();
            SaveHistoryData();
        }
    }

    [XmlRoot]
    public class ScoreHistory
    {
        [XmlArray] public List<ScoreEntry> Scores { get; set; } = new List<ScoreEntry>();
    }

    [XmlRoot]
    public class PlayerScore
    {
        [XmlArray] public List<ScoreEntry> Scores { get; set; } = new List<ScoreEntry>();

        public ScoreEntry GetScoreEntry(int id, int diff, int ed)
        {
            foreach (var s in Scores)
            {
                if (s.PvId == id && s.Difficulty == diff && s.Edition == ed) return s;
            }
            var n = new ScoreEntry() { PvId = id, Difficulty = diff, Edition = ed };
            Scores.Add(n); return n;
        }

        public void UpdateScoreEntry(ScoreEntry entry)
        {
            var length = Scores.Count;
            for (int i = 0; i < length; i++)
            {
                var s = Scores[i];
                if (s.PvId == entry.PvId && s.Difficulty == entry.Difficulty && s.Edition == entry.Edition)
                {
                    Scores.RemoveAt(i); break;
                }
            }
            Scores.Add(entry);
        }

        public void UpdateScoreEntry(int id, int diff, int ed, ScoreEntry entry)
        {
            var length = Scores.Count;
            for (int i = 0; i < length; i++)
            {
                var s = Scores[i];
                if (s.PvId == id && s.Difficulty == diff && s.Edition == ed)
                {
                    Scores.RemoveAt(i); break;
                }
            }
            entry.PvId = id;
            entry.Difficulty = diff;
            entry.Edition = ed;
            Scores.Add(entry);
        }
    }

    [Serializable]
    public class ScoreEntry
    {
        [XmlAttribute("Id")] public int PvId { get; set; } = -1;
        [XmlAttribute] public int Difficulty { get; set; } = -1;
        [XmlAttribute] public int Edition { get; set; } = -1;
        [XmlElement] public string SongName { get; set; } = String.Empty;
        [XmlElement] public string PlayerName { get; set; } = String.Empty;
        [XmlElement] public string LevelName { get; set; } = String.Empty;
        [XmlElement] public int Level { get; set; } = -1;
        [XmlElement] public int PlateId { get; set; } = -1;
        [XmlElement] public int PlateEff { get; set; } = -1;
        [XmlElement] public int AlltimeScore { get; set; } = -1;
        [XmlElement] public int AlltimePercent { get; set; } = -1;
        [XmlElement] public int AlltimeRank { get; set; } = -1;
        [XmlElement] public int AlltimeModifiers { get; set; } = -1;
        [XmlElement] public int Rank { get; set; } = -1;
        [XmlElement] public int Score { get; set; } = -1;
        [XmlElement] public int Percent { get; set; } = -1;
        [XmlElement] public int Modifiers { get; set; } = -1;
        [XmlArray, XmlArrayItem("Note")] public List<NoteScoreEntry> Notes { get; set; } = new List<NoteScoreEntry>();
        [XmlElement] public int Combo { get; set; } = -1;
        [XmlElement] public int ChallengeScore { get; set; } = -1;
        [XmlElement] public int HoldScore { get; set; } = -1;
        [XmlElement] public int SlideScore { get; set; } = -1;
        [XmlElement] public int IsNewScore { get; set; } = -1;
        [XmlElement] public int IsNewPercent { get; set; } = -1;
        [XmlElement] public int[] ModuleEquip { get; set; } = null;
        [XmlElement] public DateTime RecordDate { get; set; } = DateTime.UtcNow;
    }

    [Serializable]
    public class NoteScoreEntry
    {
        [XmlAttribute] public int Id { get; set; } = -1;
        [XmlElement] public int Count { get; set; } = -1;
        [XmlElement] public int Percent { get; set; } = -1;
    }

    internal class CachedScore
    {
        private long address; // total length is 0xe4
        private Manipulator Manipulator;

        public CachedScore(Manipulator manipulator, long address)
        {
            Manipulator = manipulator;
            this.address = address;
        }

        public int PvId
        {
            get { return Manipulator.ReadInt32(address); }
            set { Manipulator.WriteInt32(address, value); }
        }

        public int Edition
        {
            get { return Manipulator.ReadInt32(address + 4); }
            set { Manipulator.WriteInt32(address + 4, value); }
        }

        public int Rank // +0xac: clear rank
        {
            get { return Manipulator.ReadInt32(address + 0xAC); }
            set { Manipulator.WriteInt32(address + 0xAC, value); }
        }

        public int Score // +0xb0: score
        {
            get { return Manipulator.ReadInt32(address + 0xB0); }
            set { Manipulator.WriteInt32(address + 0xB0, value); }
        }

        public int Percent // +0xb4: best percent
        {
            get { return Manipulator.ReadInt32(address + 0xB4); }
            set { Manipulator.WriteInt32(address + 0xB4, value); }
        }

        public int RivalRank // +0xcc: rival clear rank
        {
            get { return Manipulator.ReadInt32(address + 0xCC); }
            set { Manipulator.WriteInt32(address + 0xCC, value); }
        }

        public int RivalScore // +0xd0: rival score
        {
            get { return Manipulator.ReadInt32(address + 0xD0); }
            set { Manipulator.WriteInt32(address + 0xD0, value); }
        }

        public int RivalPercent // +0xd4: rival percent
        {
            get { return Manipulator.ReadInt32(address + 0xD4); }
            set { Manipulator.WriteInt32(address + 0xD4, value); }
        }

        public byte OptionA // +0xe1: gamemode options
        {
            get { return Manipulator.ReadByte(address + 0xE1); }
            set { Manipulator.WriteByte(address + 0xE1, value); }
        }

        public byte OptionB
        {
            get { return Manipulator.ReadByte(address + 0xE2); }
            set { Manipulator.WriteByte(address + 0xE2, value); }
        }

        public byte OptionC
        {
            get { return Manipulator.ReadByte(address + 0xE3); }
            set { Manipulator.WriteByte(address + 0xE3, value); }
        }

        public void Initialize(int pv, int ed)
        {
            // this is just copied from 140113510
            // no clue what most of it is
            PvId = pv;
            Edition = ed;
            Manipulator.WriteInt32(address + 0x08, 0);
            Manipulator.WriteInt32(address + 0x0C, 0);
            Manipulator.WriteInt32(address + 0x10, 0);
            Manipulator.WriteInt32(address + 0x14, 0);
            Manipulator.WriteInt32(address + 0x18, 0);
            Manipulator.WriteInt32(address + 0x1C, 0);
            Manipulator.WriteInt64(address + 0x20, -1);
            Manipulator.WriteInt64(address + 0x28, -1);
            Manipulator.WriteInt64(address + 0x30, -1);
            Manipulator.WriteInt64(address + 0x38, -1);
            Manipulator.WriteInt64(address + 0x40, -1);
            Manipulator.WriteInt64(address + 0x48, -1);
            Manipulator.WriteInt64(address + 0x50, -1);
            Manipulator.WriteInt64(address + 0x58, -1);
            Manipulator.WriteInt64(address + 0x60, -1);
            Manipulator.WriteInt64(address + 0x68, -1);
            Manipulator.WriteInt64(address + 0x70, -1);
            Manipulator.WriteInt64(address + 0x78, -1);
            Manipulator.WriteInt32(address + 0x80, 0x01010101);
            Manipulator.WriteInt32(address + 0x84, 0x01010101);
            Manipulator.WriteInt32(address + 0x88, 0x01010101);
            Manipulator.WriteInt32(address + 0x8C, 0x01010101);
            Manipulator.WriteInt32(address + 0x90, 0x01010101);
            Manipulator.WriteInt32(address + 0x94, 0x01010101);
            Manipulator.WriteInt32(address + 0x98, 0);
            Manipulator.WriteInt64(address + 0x9C, -1);
            Manipulator.WriteInt64(address + 0xA4, -1);
            Manipulator.WriteInt32(address + 0xAC, -1);
            Manipulator.WriteInt64(address + 0xB0, 0);
            Manipulator.WriteInt32(address + 0xB8, -1);
            Manipulator.WriteByte(address + 0xBC, 0);
            Manipulator.WriteInt64(address + 0xC0, -1);
            Manipulator.WriteByte(address + 0xC8, 0);
            Manipulator.WriteInt32(address + 0xCC, -1);
            Manipulator.WriteInt64(address + 0xD0, 0);
            Manipulator.WriteInt32(address + 0xD8, -1);
            Manipulator.WriteByte(address + 0xDC, 0);
            Manipulator.WriteByte(address + 0xDD, 0);
            Manipulator.WriteByte(address + 0xDE, 0);
            Manipulator.WriteByte(address + 0xDF, 0);
            Manipulator.WriteByte(address + 0xE0, 0);
            Manipulator.WriteByte(address + 0xE1, 0);
            Manipulator.WriteByte(address + 0xE2, 0);
            Manipulator.WriteByte(address + 0xE3, 0);
        }
    }
}
