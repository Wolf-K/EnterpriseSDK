using System;

namespace XMLSnippetsInjector
{
    internal class Config
    {
        public string ToolUsage { get; set; } = @"Usage: XMLSnippetsInjector [options] <xmlInputDir> <csInputDir> <outputDir>

Options:
  -c, --clear                 Clear the output directory before processing.
  -r, --remove-examples       Remove <example> nodes with specific source attributes.
  -a, --remove-all-examples   Remove all <example> nodes regardless of source.
  -p, --process-all-examples  Process all <example> nodes regardless of source.

Arguments:
  <xmlInputDir>   Directory containing XML input files.
  <csInputDir>    Directory containing C# source files.
  <outputDir>     Directory to write processed XML files and reports.
";
        public string InternalNamespaceMarker { get; set; } = ".Internal.";
        public string SharedPath { get; set; } = "ArcGIS\\SharedArcGIS";
    }
}
