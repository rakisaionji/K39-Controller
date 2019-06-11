using System.Xml.Serialization;

namespace K39C
{
    [XmlRoot]
    public class PlayerData
    {
        [XmlElement] public string PlayerName { get; set; } = "ＮＯ－ＮＡＭＥ";
        [XmlElement] public int Level { get; set; } = 1;
        [XmlElement] public int PlateId { get; set; } = 0;
        [XmlElement] public int PlateEff { get; set; } = -1;
        [XmlElement] public int VocaloidPoint { get; set; } = 0;
        [XmlElement] public int SkinEquip { get; set; } = 0;
        [XmlElement] public byte ActToggle { get; set; } = 1;
        [XmlElement] public int ActVol { get; set; } = 100;
        [XmlElement] public int ActSlideVol { get; set; } = 100;
        [XmlElement] public int HpVol { get; set; } = 100;
        [XmlElement] public int PasswordStatus { get; set; } = -1;
        [XmlElement] public int PvSortKind { get; set; } = 2;
        [XmlElement] public int PlayIndex { get; set; } = 1;
    }
}
