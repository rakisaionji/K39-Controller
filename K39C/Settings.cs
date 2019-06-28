using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace K39C
{
    [XmlRoot]
    public class Settings
    {
        [XmlElement] public string DivaPath { get; set; } = String.Empty;
        [XmlElement] public string Arguments { get; set; } = String.Empty;
        [XmlElement] public GlutCursor GlutCursor { get; set; } = GlutCursor.NONE;
        [XmlElement] public bool ApplyPatch { get; set; } = false;
        [XmlElement] public bool HideCredits { get; set; } = false;
        [XmlElement] public bool FreePlay { get; set; } = false;
        [XmlElement] public bool TemporalAA { get; set; } = true;
        [XmlElement] public bool MorphologicalAA { get; set; } = true;
        [XmlElement] public int WaitTime { get; set; } = 20;
        [XmlElement] public bool TouchEmulator { get; set; } = false;
        [XmlElement] public bool ScaleComponent { get; set; } = false;
        [XmlElement] public bool PlayerDataManager { get; set; } = true;
        [XmlElement] public bool PvModuleManager { get; set; } = false;
        [XmlElement] public bool SysTimer { get; set; } = false;
        [XmlElement] public string KeychipId { get; set; } = String.Empty;
        [XmlElement] public string MainId { get; set; } = String.Empty;
        [XmlArray("DivaPlugins"), XmlArrayItem("DivaPlugin")] public List<string> DivaPlugins { get; set; } = new List<string>();

        public void Reset()
        {
            TemporalAA = true;
            MorphologicalAA = true;
            TouchEmulator = false;
            ScaleComponent = false;
            PlayerDataManager = true;
            PvModuleManager = true;
            SysTimer = false;
            KeychipId = String.Empty;
            MainId = String.Empty;
            DivaPlugins = new List<string>();
        }
    }
}
