﻿using System.Xml.Serialization;

namespace K39C
{
    [XmlRoot]
    public class PlayerData
    {
        [XmlElement] public bool UseCard { get; set; } = true;
        [XmlElement] public string PlayerName { get; set; } = "ＮＯ－ＮＡＭＥ";
        [XmlElement] public string LevelName { get; set; } = "K39Cプレイヤー";
        [XmlElement] public int Level { get; set; } = 1;
        [XmlElement] public int PlateId { get; set; } = 0;
        [XmlElement] public int PlateEff { get; set; } = -1;
        [XmlElement] public int VocaloidPoint { get; set; } = 0;
        [XmlElement] public int SkinEquip { get; set; } = 0;
        [XmlElement] public int BtnSeEquip { get; set; } = 0;
        [XmlElement] public int SlideSeEquip { get; set; } = 0;
        [XmlElement] public int ChainSeEquip { get; set; } = 0;
        [XmlElement] public int TouchSeEquip { get; set; } = 0;
        [XmlElement] public byte ActToggle { get; set; } = 1;
        [XmlElement] public int ActVol { get; set; } = 100;
        [XmlElement] public int ActSlideVol { get; set; } = 100;
        [XmlElement] public int HpVol { get; set; } = 100;
        [XmlElement] public int PasswordStatus { get; set; } = -1;
        [XmlElement] public int PvSortKind { get; set; } = 2;
        // [XmlElement] public uint PlayDataId { get; set; } = 0;
        [XmlElement] public bool SetPlayData { get; set; } = true;
        [XmlElement] public ClearBorder ClearBorder { get; set; } = new ClearBorder();
        [XmlElement] public bool OptionDisp { get; set; } = true;
        [XmlElement] public bool KeepOption { get; set; } = false;
    }

    public class ClearBorder
    {
        [XmlAttribute("great")] public byte ShowGreat { get; set; } = 1;
        [XmlAttribute("excellent")] public byte ShowExcellent { get; set; } = 1;
        [XmlAttribute("rival")] public byte ShowRival { get; set; } = 1;

        public byte ToByte()
        {
            var show_great = (ShowGreat == 0) ? 0 : 1;
            var show_excellent = (ShowExcellent == 0) ? 0 : 1;
            var show_rival = (ShowRival == 0) ? 0 : 1;
            return (byte)(show_rival << 2 | show_excellent << 1 | show_great);
        }
    }
}
