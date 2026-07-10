using System.Xml.Linq;

namespace XMLSnippetsInjector
{
    internal class XmlMemberInfo
    {
        public required XElement Element { get; set; }
        public required string RawName { get; set; }
        public required string NormalizedName { get; set; }
        public bool HasInjection { get; set; } = false;
        public required string SimplifiedName { get; set; }
    }
}
