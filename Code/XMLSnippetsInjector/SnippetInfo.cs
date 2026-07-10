using System.Collections.Generic;
using System.IO;

namespace XMLSnippetsInjector
{
    internal class SnippetInfo
    {
        public required string Cref { get; set; }
        public required string Code { get; set; }
        public required string SourceFile { get; set; }
        public required int IndexInSource { get; set; }
        public bool Used { get; set; } = false;
        public List<string> TriedMatches { get; } = new List<string>();
        public List<(string XmlFile, string MemberName)> MatchedTo { get; } = new List<(string, string)>();
        public string Id => $"{Path.GetFileName(SourceFile)}::{IndexInSource}";
        public required string RegionName { get; set; }
    }
}
