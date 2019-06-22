using System.Collections.Generic;
using System.Xml.Serialization;

namespace K39C
{
    [XmlRoot]
    public class PvModules
    {
        [XmlElement("PvModule")]
        public List<PvModule> Modules { get; set; }

        public PvModules()
        {
            Modules = new List<PvModule>();
        }

        public PvModule Get(uint pvId)
        {
            foreach (var module in Modules) if (module.PvId == pvId) return module;
            return null;
        }
    }

    public class PvModule
    {
        [XmlElement]
        public uint PvId { get; set; }

        [XmlElement("CostumeId")]
        public List<int> Costumes { get; set; }
    }
}
