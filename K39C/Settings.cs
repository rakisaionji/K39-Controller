using System;
using System.Xml.Serialization;

namespace K39C
{
    [XmlRoot]
    public class Settings
    {
        [XmlElement] public bool TouchEmulator { get; set; } = false;
        [XmlElement] public bool ScaleComponent { get; set; } = false;
        [XmlElement] public bool PlayerDataManager { get; set; } = true;
        [XmlElement] public bool SysTimer { get; set; } = false;
        [XmlElement] public string KeychipId { get; set; } = String.Empty;
        [XmlElement] public string MainId { get; set; } = String.Empty;

        public void Reset()
        {
            TouchEmulator = false;
            ScaleComponent = false;
            PlayerDataManager = true;
            SysTimer = false;
            KeychipId = String.Empty;
            MainId = String.Empty;
        }
    }
}
