using System.Collections.Generic;
using System.Xml.Linq;

namespace XMLSnippetsInjector
{
    internal class XmlDocInfo
    {
        public required string FilePath { get; set; }
        public required XDocument Document { get; set; }
        public List<XmlMemberInfo> Members { get; } = new List<XmlMemberInfo>();
        public int InjectionsCount { get; set; } = 0;
        public bool Changed { get; set; } = false;
    }
}
