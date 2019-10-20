using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace K39C
{
    [XmlRoot]
    public class Settings
    {
        [XmlElement] public DivaExe Executable { get; set; } = new DivaExe();
        [XmlElement] public DivaSys System { get; set; } = new DivaSys();
        [XmlElement] public DivaComp Components { get; set; } = new DivaComp();
        [XmlElement] public DivaPatch DivaPatches { get; set; } = new DivaPatch();
        [XmlArray("DivaPlugins"), XmlArrayItem("DivaPlugin")] public List<string> DivaPlugins { get; set; } = new List<string>();

        [Serializable]
        public class DivaExe
        {
            [XmlElement] public string DivaPath { get; set; } = String.Empty;
            [XmlElement] public string Arguments { get; set; } = String.Empty;
            [XmlElement] public bool ApplyPatch { get; set; } = true;
            [XmlElement] public int WaitTime { get; set; } = 20;

            public static string[] CustomResArgs = { "-qvga", "-vga", "-wvga", "-svga", "-xga", "-hdtv720", "-hdtv720_dbd", "-wxga", "-wxga_dbd", "-wxga2", "-uxga", "-hdtv1080", "-wuxga", "-wqhd", "-wqxga" };

            public bool IsCustomRes()
            {
                foreach (var item in CustomResArgs) if (Arguments.Contains(item)) return true;
                return false;
            }
        }

        [Serializable]
        public class DivaComp
        {
            [XmlElement] public bool TouchEmulator { get; set; } = false;
            [XmlElement] public bool ScaleComponent { get; set; } = false;
            [XmlElement] public bool PlayerDataManager { get; set; } = false;
        }

        [Serializable]
        public class DivaSys
        {
            [XmlElement] public string KeychipId { get; set; } = String.Empty;
            [XmlElement] public string MainId { get; set; } = String.Empty;
            [XmlElement] public TimerDisplay SysTimer { get; set; } = TimerDisplay.DEFAULT;
            [XmlElement] public bool TemporalAA { get; set; } = true;
            [XmlElement] public bool MorphologicalAA { get; set; } = true;
            [XmlElement] public ErrorDisplay ErrorDisplay { get; set; } = ErrorDisplay.SKIP_CARD;
        }

        [Serializable]
        public class DivaPatch
        {
            [XmlElement] public GlutCursor GlutCursor { get; set; } = GlutCursor.NONE;
            [XmlElement] public bool RamPathFix { get; set; } = false;
            [XmlElement] public bool MdataPathFix { get; set; } = false;
            [XmlElement] public bool FreePlay { get; set; } = false;
            [XmlElement] public bool HideCredits { get; set; } = false;

            // Other Features by somewhatlurker
            [XmlElement] public bool HidePvUi { get; set; } = false;
            [XmlElement] public bool HidePvMark { get; set; } = false;
            [XmlElement] public bool HideLyrics { get; set; } = false;
            [XmlElement] public bool HideSeBtn { get; set; } = false;
            [XmlElement] public bool HideVolume { get; set; } = false;
            [XmlElement] public StatusIcon CardIcon { get; set; } = StatusIcon.DEFAULT;
            [XmlElement] public StatusIcon NetIcon { get; set; } = StatusIcon.DEFAULT;
        }

        public void Reset()
        {
            System.KeychipId = String.Empty;
            System.MainId = String.Empty;
            System.TemporalAA = true;
            System.MorphologicalAA = true;
            Components.TouchEmulator = false;
            Components.ScaleComponent = false;
            Components.PlayerDataManager = true;
            DivaPlugins = new List<string>();
        }
    }

    public enum StatusIcon
    {
        DEFAULT,
        ERROR,
        WARNING,
        OK,
        HIDDEN
    }

    public enum ErrorDisplay
    {
        DEFAULT,
        SKIP_CARD,
        HIDDEN
    }

    public enum TimerDisplay
    {
        DEFAULT = 0,
        FREEZE = 1,
        HIDDEN = 2
    }
}
