using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace XMLSnippetsInjector
{
  internal class Program
  {
    // Simple file logger nested inside Program
    private static class Logger
    {
      private static StreamWriter? _writer;
      private static readonly object _lock = new();
      private static string? _currentPath;

      // Initialize default log file in current directory
      public static void InitDefault()
      {
        var path = Path.Combine(Directory.GetCurrentDirectory(), $"xmlsnippetsinjector-{DateTime.Now:yyyyMMddHHmmss}.log");
        Init(path);
      }

      // Initialize or reinitialize log file at specified path
      public static void Init(string path)
      {
        lock (_lock)
        {
          try
          {
            if (!string.IsNullOrEmpty(_currentPath) && string.Equals(_currentPath, path, StringComparison.OrdinalIgnoreCase))
            {
              // already initialized to this path
            }
            else
            {
              // close any existing writer
              if (_writer != null)
              {
                try { _writer.Flush(); } catch { }
                try { _writer.Dispose(); } catch { }
                _writer = null;
              }

              var dir = Path.GetDirectoryName(path);
              if (!string.IsNullOrEmpty(dir))
              {
                Directory.CreateDirectory(dir);
              }

              var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
              _writer = new StreamWriter(fs) { AutoFlush = true };
              _currentPath = path;
              Log($"Log started: {path}");
            }
          }
          catch (Exception ex)
          {
            // Fallback: if file logging fails, write to console as last resort
            try { Console.WriteLine($"Failed to initialize log file '{path}': {ex.Message}"); } catch { }
          }
        }
      }

      // Set log file to new path (alias for Init)
      public static void SetLogFile(string path) => Init(path);

      // Write a timestamped message to the log
      public static void Log(string message)
      {
        var line = $"{DateTime.Now:O} {message}";
        lock (_lock)
        {
          if (_writer != null)
          {
            try
            {
              _writer.WriteLine(line);
            }
            catch
            {
              // ignore write errors
            }
          }
          else
          {
            // No writer available, fallback to console
            try { Console.WriteLine(line); } catch { }
          }
        }
      }

      // Convenience to log exceptions with optional context
      public static void LogException(string context, Exception ex)
      {
        Log($"{context}: {ex.Message}");
        if (ex.StackTrace != null)
        {
          Log(ex.StackTrace);
        }
      }

      // Close and dispose writer
      public static void Close()
      {
        lock (_lock)
        {
          if (_writer != null)
          {
            try { Log("Log closed"); } catch { }
            try { _writer.Dispose(); } catch { }
            _writer = null;
            _currentPath = null;
          }
        }
      }
    }

    // The supporting classes have been moved to separate files:
    // Config.cs, SnippetInfo.cs, XmlMemberInfo.cs, XmlDocInfo.cs

    // LoadConfig remains in this file and uses the Config type from Config.cs
    private static Config LoadConfig()
    {
      // try current directory then base directory
      var paths = new[] { Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), Path.Combine(AppContext.BaseDirectory, "appsettings.json") };
      foreach (var p in paths)
      {
        try
        {
          if (File.Exists(p))
          {
            var json = File.ReadAllText(p);
            var cfg = JsonSerializer.Deserialize<Config>(json);
            if (cfg != null) return cfg;
          }
        }
        catch
        {
          // ignore and continue to defaults
        }
      }

      return new Config();
    }

    // Plan (pseudocode):
    // 1. Load configuration via LoadConfig().
    // 2. Parse command-line args; ensure at least 3 non-flag args (xmlInputDir, csInputDir, outputDir).
    // 3. Validate directories and optionally clear outputDir.
    // 4. Build snippets dictionary from C# files.
    // 5. Load all XML files into memory and build XmlDocInfo list with member entries.
    // 6. Conditionally remove any <example> nodes where the child <code> has a 'source' attribute
    //    and the attribute value contains "ArcGIS\SharedArcGIS" (case-insensitive).
    //    - Iterate every loaded XmlDocInfo.Document
    //    - For each, collect descendant <example> elements into a list to avoid modifying during iteration
    //    - For each example, inspect example.Element("code") and its "source" attribute
    //    - If source contains the configured SharedPath substring (case-insensitive), remove the <example> element
    //    - Mark the XmlDocInfo as Changed if any were removed
    // 7. Proceed to match snippets and inject examples as before.
    // 8. Save modified documents and generate reports.
    static int Main(string[] args)
    {
      // Initialize logging immediately so early messages are captured
      Logger.InitDefault();

      try
      {
        var config = LoadConfig();

        if (args.Length < 3)
        {
          Logger.Log(config.ToolUsage);
          return 1;
        }

        // control removal of existing files in output folder
        bool clear = args.Any(a => string.Equals(a, "-clear", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "--clear", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "-c", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "c", StringComparison.OrdinalIgnoreCase));

        // control removal of existing example nodes (conditional by source)
        bool removeExamples = args.Any(a => string.Equals(a, "-r", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "--remove-examples", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "r", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "remove-examples", StringComparison.OrdinalIgnoreCase));

        // control removal of ALL existing <example> nodes regardless of source
        bool removeAllExamples = args.Any(a => string.Equals(a, "-a", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "--remove-all-examples", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "a", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "remove-all-examples", StringComparison.OrdinalIgnoreCase));
        if (removeAllExamples)
        {
          Logger.Log("Flag set: removing all <example> nodes from loaded XML documents.");
        }

        // control processing/rendering of ALL existing <example> nodes regardless of source
        bool processAllExamples = args.Any(a => string.Equals(a, "-p", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "--process-all-examples", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "p", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "process-all-examples", StringComparison.OrdinalIgnoreCase));

        if (processAllExamples)
        {
          Logger.Log("Flag set: processing all <example> nodes in loaded XML documents.");
        }

        // first three non-flag args are directories
        var nonFlagArgs = args.Where(a => !a.StartsWith('-') || a == "-c" || a == "c").ToArray();
        if (nonFlagArgs.Length < 3)
        {
          Logger.Log(config.ToolUsage);
          //Console.WriteLine("Expected at least 3 path arguments: xmlInputDir csInputDir outputDir");
          return 1;
        }

        string xmlInputDir = nonFlagArgs[0];
        string csInputDir = nonFlagArgs[1];
        string outputDir = nonFlagArgs[2];

        if (!Directory.Exists(xmlInputDir))
        {
          Logger.Log($"XML input directory does not exist: {xmlInputDir}");
          return 1;
        }

        if (!Directory.Exists(csInputDir))
        {
          Logger.Log($"C# input directory does not exist: {csInputDir}");
          return 1;
        }

        if (Directory.Exists(outputDir) && clear)
        {
          try
          {
            Directory.Delete(outputDir, true);
            Logger.Log($"Cleared output directory: {outputDir}");
          }
          catch (Exception ex)
          {
            Logger.Log($"Failed to clear output directory: {ex.Message}");
            return 1;
          }
        }

        Directory.CreateDirectory(outputDir);

        // switch log into outputDir/reports for more relevant placement
        try
        {
          var logDir = Path.Combine(outputDir, "reports");
          var logPath = Path.Combine(logDir, $"xmlsnippetsinjector-{DateTime.Now:yyyyMMddHHmmss}.log");
          Logger.SetLogFile(logPath);
        }
        catch (Exception ex)
        {
          Logger.Log($"Failed to switch log file into output directory: {ex.Message}");
        }

        var snippets = BuildSnippetsDictionary(csInputDir);

        // Load all XML docs into memory
        var xmlFiles = Directory.GetFiles(xmlInputDir, "*.xml", SearchOption.AllDirectories);
        var xmlDocs = new List<XmlDocInfo>();
        foreach (var xmlFile in xmlFiles)
        {
          try
          {
            var doc = XDocument.Load(xmlFile);
            var xmlDocInfo = new XmlDocInfo { FilePath = xmlFile, Document = doc };
            var memberElements = doc.Descendants("member");
            foreach (var member in memberElements)
            {
              var nameAttr = member.Attribute("name");
              if (nameAttr == null) continue;
              var raw = nameAttr.Value.Trim();

              // skip internal members
              if (config.InternalNamespaceMarker.Length > 0 && raw.Contains(config.InternalNamespaceMarker, StringComparison.OrdinalIgnoreCase))
              {
                continue;
              }
              xmlDocInfo.Members.Add(new XmlMemberInfo { Element = member, RawName = raw, NormalizedName = NormalizeMember(raw), SimplifiedName = SymbolSimplify.SimplifySymbolRef(raw) });
            }

            xmlDocs.Add(xmlDocInfo);
          }
          catch (Exception ex)
          {
            Logger.Log($"Failed to load XML '{xmlFile}': {ex.Message}");
          }
        }

        // Conditionally remove <example> nodes whose <code> child has a source attribute
        // that contains the path "ArcGIS\SharedArcGIS" (case-insensitive).
        if (!processAllExamples && (removeExamples || removeAllExamples))
        {
          foreach (var doc in xmlDocs)
          {
            bool removedAny = false;
            // Use Descendants and ToList to avoid modifying the collection while iterating
            var examples = doc.Document.Descendants("example").ToList();
            foreach (var example in examples)
            {
              if (removeAllExamples)
              {
                example.Remove();
                removedAny = true;
                continue;
              }

              var codeElem = example.Element("code");
              var srcAttr = codeElem?.Attribute("source");
              if (srcAttr != null && !string.IsNullOrEmpty(srcAttr.Value) &&
                  srcAttr.Value.Contains(config.SharedPath, StringComparison.OrdinalIgnoreCase))
              {
                example.Remove();
                removedAny = true;
              }
            }

            if (removedAny)
            {
              doc.Changed = true;
            }
          }
        }

        // Conditionally process all <example> nodes
        if (processAllExamples)
        {
          foreach (var doc in xmlDocs)
          {
            bool modified = false;
            var examples = doc.Document.Descendants("example").ToList();
            foreach (var example in examples)
            {
              // Extract content of the <example> node
              var content = example.Value.Trim();

              // Find the <summary> tag in the parent node
              var parent = example.Parent;
              var summaryText = parent?.Element("summary")?.Value.Trim();

              if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(summaryText))
              {
                // Inject content into a <code> tag with a description from the <summaryText> tag
                var codeTag = new XElement("code",
                  new XAttribute("description", summaryText),
                  new XElement("example", content)
                );

                // replace the current <example> tag with the <code> tag
                example.ReplaceWith(codeTag);
                modified = true;
              }
            }

            if (modified)
            {
              doc.Changed = true;
            }
          }
        }

        // Now loop over snippets and try to match them to XML members
        var allSnippetInfos = snippets.SelectMany(kvp => kvp.Value).ToList();

        foreach (var snippet in allSnippetInfos)
        {
          // attempt matches across all XML members
          var matches = new List<(XmlDocInfo Doc, XmlMemberInfo Member)>();

          // First try exact name match (or trimming the first 2 characters like 'M:' from member names)
          foreach (var doc in xmlDocs)
          {
            var r = doc.Members.Where(s => !string.IsNullOrEmpty(s.RawName) && (s.RawName[2..].Equals(snippet.Cref, StringComparison.Ordinal) ||
                                            s.RawName.Equals(snippet.Cref, StringComparison.Ordinal))
                                     );
            if (!r.Any())
              continue;
            foreach (var member in r)
            {
              //if (string.Equals(member.RawName, snippet.Cref, StringComparison.Ordinal))
              // string.Equals(member.RawName.Substring(2, member.RawName.Length-2) , snippet.Cref, StringComparison.Ordinal)
              //{
              matches.Add((doc, member));
              //}
            }
          }

          // Try normalized name match
          if (matches.Count == 0)
          {
            var normalizedSnippetName = NormalizeMember(snippet.Cref);

            foreach (var doc in xmlDocs)
            {
              var r = doc.Members.Where(s => (s.NormalizedName[2..].Equals(normalizedSnippetName, StringComparison.Ordinal) ||
                                              s.NormalizedName.Equals(normalizedSnippetName))
                                       );
              if (!r.Any())
                continue;

              foreach (var member in r)
              {
                matches.Add((doc, member));
              }
            }
          }

          /* Let's keep it tight for now - exact matches only
          // Still nothing? Try exact name match with "contains" (case-insensitive)
          if (matches.Count == 0)
          {
            foreach (var doc in xmlDocs)
            {
              var r = doc.Members.Where(s => s.RawName.Contains(snippet.Cref, StringComparison.InvariantCultureIgnoreCase));
              if (!r.Any())
                continue;
              foreach (var member in r)
              {
                //if (string.Equals(member.RawName, snippet.Cref, StringComparison.Ordinal))
                // string.Equals(member.RawName.Substring(2, member.RawName.Length-2) , snippet.Cref, StringComparison.Ordinal)
                //{
                matches.Add((doc, member));
                //}
              }
            }
          }

          // If no exact matches, try endswith and contains
          if (matches.Count == 0)
          {
            foreach (var doc in xmlDocs)
            {
              foreach (var member in doc.Members)
              {
                if (member.NormalizedName.EndsWith(normalizedSnippetName, StringComparison.Ordinal) || normalizedSnippetName.EndsWith(member.NormalizedName, StringComparison.Ordinal) || member.NormalizedName.Contains(normalizedSnippetName, StringComparison.Ordinal) || normalizedSnippetName.Contains(member.NormalizedName, StringComparison.Ordinal))
                {
                  matches.Add((doc, member));
                }
              }
            }
          }

          // Propagate to anything that matches the snippet name without parameters
          if (matches.Count == 0)
          {
            // remove parentheses from snippet name (already done) and also try simple name (after last '.')
            var simpleName = GetSimpleMemberName(normalizedSnippetName);
            foreach (var doc in xmlDocs)
            {
              foreach (var member in doc.Members)
              {
                var memberSimple = GetSimpleMemberName(member.NormalizedName);
                if (string.Equals(memberSimple, simpleName, StringComparison.Ordinal))
                {
                  matches.Add((doc, member));
                }
              }
            }
          }
          */

          // If still no matches, record tried possibilities (e.g., all member names we compared)
          if (matches.Count == 0)
          {
            // record some attempts for reporting (top 50 member names)
            var tried = xmlDocs.SelectMany(d => d.Members.Select(m => m.RawName)).Take(50);
            snippet.TriedMatches.AddRange(tried);
          }
          else
          {
            // perform injection for each matched member
            foreach (var (doc, member) in matches)
            {
              // Ensure we don't inject the same snippet more than once for this member.
              bool alreadyHasCref = member.Element.Elements("example").Any(e => string.Equals((string)e.Attribute("cref"), snippet.Cref, StringComparison.Ordinal));
              bool alreadyHasCode = member.Element.Elements("example").Any(e =>
              {
                var codeEl = e.Element("code");
                if (codeEl == null) return false;
                // Compare inner text; if existing uses CDATA it will be in Value
                return string.Equals(codeEl.Value.Trim(), snippet.Code.Trim(), StringComparison.Ordinal);
              });

              string regionName = snippet.RegionName ?? string.Empty;

              int examplesWithDesc = CountExamplesWithCodeDescription(member.Element, regionName);
              Logger.Log($"Examples in {member.RawName} with code description '{regionName}': {examplesWithDesc}");

              bool alreadyHasRegion = examplesWithDesc > 0;

              if (alreadyHasCref || alreadyHasCode || alreadyHasRegion)
              {
                // Mark snippet as already used (in case we missed it)
                snippet.Used = true;
                // skip injecting duplicate
                continue;
              }

              // create example node with description attribute from the snippet's region name
              var example = new XElement("example",
                 new XElement("code", snippet.Code
                              , new XAttribute("description", snippet.RegionName)
                              , new XAttribute("cref", snippet.Cref)
                            )
                          );
              member.Element.Add(example);
              member.HasInjection = true;
              doc.InjectionsCount++;
              doc.Changed = true;
              snippet.Used = true;
              snippet.MatchedTo.Add((doc.FilePath, member.RawName));
            }
          }
        }

        // After processing snippets, write XML files to output dir; also build reports
        var reportsDir = Path.Combine(outputDir, "reports");
        Directory.CreateDirectory(reportsDir);

        int totalInjections = 0;
        int filesWithInjections = 0;
        var xmlFilesWithNoInjections = new List<string>();

        foreach (var doc in xmlDocs)
        {
          var relative = Path.GetRelativePath(xmlInputDir, doc.FilePath);
          var outPath = Path.Combine(outputDir, relative);
          var outDir = Path.GetDirectoryName(outPath);
          if (outDir != null && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

          if (doc.Changed)
          {
            doc.Document.Save(outPath);
            totalInjections += doc.InjectionsCount;
            filesWithInjections++;
          }
          else
          {
            // copy original
            if (!doc.FilePath.Equals(outPath, StringComparison.OrdinalIgnoreCase))
              File.Copy(doc.FilePath, outPath, true);
            //File.Copy(doc.FilePath, outDir, true);
            xmlFilesWithNoInjections.Add(doc.FilePath);
          }
        }

        // Generate reports
        // 1) Detailed injection report
        var injectionReport = new StringBuilder();
        injectionReport.AppendLine("Injection Report");
        injectionReport.AppendLine("================");
        foreach (var snippet in allSnippetInfos.Where(s => s.Used))
        {
          foreach (var (XmlFile, MemberName) in snippet.MatchedTo)
          {
            injectionReport.AppendLine($"Snippet: {snippet.Id} | Cref: {snippet.Cref} | Source: {snippet.SourceFile}");
            injectionReport.AppendLine($"  Injected into: {XmlFile} -> {MemberName}");
            injectionReport.AppendLine();
          }
        }
        File.WriteAllText(Path.Combine(reportsDir, "injection-report.txt"), injectionReport.ToString());

        // 2) XML members without any snippet injected
        var membersWithout = new StringBuilder();
        membersWithout.AppendLine("XML Members Without Snippets");
        membersWithout.AppendLine("============================");
        foreach (var doc in xmlDocs)
        {
          foreach (var m in doc.Members.Where(mm => !mm.HasInjection))
          {
            membersWithout.AppendLine($"{doc.FilePath} -> {m.RawName}");
          }
        }
        File.WriteAllText(Path.Combine(reportsDir, "xml-members-without-snippets.txt"), membersWithout.ToString());

        // 3) Detailed report of unused snippets
        var unusedReport = new StringBuilder();
        unusedReport.AppendLine("Unused Snippets");
        unusedReport.AppendLine("===============");
        foreach (var snippet in allSnippetInfos.Where(s => !s.Used))
        {
          unusedReport.AppendLine($"Snippet: {snippet.Id} | Cref: {snippet.Cref} | Source: {snippet.SourceFile}");
          unusedReport.AppendLine("Code:");
          unusedReport.AppendLine(snippet.Code);
          if (snippet.TriedMatches.Count > 0)
          {
            unusedReport.AppendLine("Tried matches (sample):");
            foreach (var t in snippet.TriedMatches)
              unusedReport.AppendLine($"  {t}");
          }
          unusedReport.AppendLine();
        }
        File.WriteAllText(Path.Combine(reportsDir, "unused-snippets.txt"), unusedReport.ToString());

        // 4) XML files with no snippet injected
        var filesNoInjectSb = new StringBuilder();
        filesNoInjectSb.AppendLine("XML files with no injections");
        filesNoInjectSb.AppendLine("===========================");
        foreach (var f in xmlFilesWithNoInjections)
          filesNoInjectSb.AppendLine(f);
        File.WriteAllText(Path.Combine(reportsDir, "xml-files-with-no-injections.txt"), filesNoInjectSb.ToString());

        // 5) Summary
        var summary = new StringBuilder();
        summary.AppendLine("Summary");
        summary.AppendLine("=======");
        summary.AppendLine($"Total xml files processed: {xmlDocs.Count}");
        summary.AppendLine($"Xml files with injections: {filesWithInjections}");
        summary.AppendLine($"Xml files with no injections: {xmlFilesWithNoInjections.Count}");
        summary.AppendLine($"Total snippets found: {allSnippetInfos.Count}");
        summary.AppendLine($"Total snippets used (matched and injected): {allSnippetInfos.Count(s => s.Used)}");
        summary.AppendLine($"Total snippets unused: {allSnippetInfos.Count(s => !s.Used)}");
        summary.AppendLine($"Total injections made: {totalInjections}");
        summary.AppendLine();

        // Add injections per xml file
        summary.AppendLine("Injections per XML file:");
        foreach (var doc in xmlDocs.OrderByDescending(d => d.InjectionsCount))
        {
          summary.AppendLine($"{doc.FilePath}: {doc.InjectionsCount}");
        }

        File.WriteAllText(Path.Combine(reportsDir, "summary.txt"), summary.ToString());

        // New: generate cref usage report (which crefs had any snippet used vs unused)
        var crefGroups = allSnippetInfos
          .GroupBy(s => s.Cref, StringComparer.Ordinal)
          .Select(g => new
          {
            Cref = g.Key,
            TotalSnippets = g.Count(),
            UsedSnippets = g.Count(si => si.Used),
            AnyUsed = g.Any(si => si.Used),
            SourceFiles = g.Select(si => si.SourceFile).Distinct().ToList()
          })
          .OrderByDescending(x => x.UsedSnippets)
          .ThenBy(x => x.Cref)
          .ToList();

        var crefReportSb = new StringBuilder();
        crefReportSb.AppendLine("Cref Usage Report");
        crefReportSb.AppendLine("=================");
        crefReportSb.AppendLine();

        crefReportSb.AppendLine("Used Crefs:");
        foreach (var g in crefGroups.Where(x => x.AnyUsed))
        {
          crefReportSb.AppendLine($"{g.Cref}  (used snippets: {g.UsedSnippets}/{g.TotalSnippets})");
          if (g.SourceFiles.Count > 0)
          {
            crefReportSb.AppendLine("  Sources:");
            foreach (var s in g.SourceFiles)
              crefReportSb.AppendLine("    " + s);
          }
        }

        crefReportSb.AppendLine();
        crefReportSb.AppendLine("Unused Crefs:");
        foreach (var g in crefGroups.Where(x => !x.AnyUsed))
        {
          crefReportSb.AppendLine($"{g.Cref}  (snippets: {g.TotalSnippets})");
        }

        crefReportSb.AppendLine();
        crefReportSb.AppendLine("Summary:");
        int totalDistinctCrefs = crefGroups.Count;
        int totalCrefsUsed = crefGroups.Count(x => x.AnyUsed);
        int totalCrefsUnused = totalDistinctCrefs - totalCrefsUsed;
        int totalSnippets = allSnippetInfos.Count;
        int totalSnippetsUsed = allSnippetInfos.Count(s => s.Used);

        crefReportSb.AppendLine($"Total distinct crefs: {totalDistinctCrefs}");
        crefReportSb.AppendLine($"Crefs with at least one used snippet: {totalCrefsUsed}");
        crefReportSb.AppendLine($"Crefs with no used snippets: {totalCrefsUnused}");
        crefReportSb.AppendLine($"Total snippets: {totalSnippets}");
        crefReportSb.AppendLine($"Total snippets used: {totalSnippetsUsed}");

        var crefReportPath = Path.Combine(reportsDir, "cref-usage.txt");
        File.WriteAllText(crefReportPath, crefReportSb.ToString());

        // Console summaryText for quick visibility is now logged to file
        Logger.Log($"Cref usage: {totalDistinctCrefs} distinct crefs, {totalCrefsUsed} used, {totalCrefsUnused} unused. Report: {crefReportPath}");

        Logger.Log("Completed. Reports are in: " + reportsDir);
        return 0;
      }
      finally
      {
        // Ensure logger is closed and flushed on exit
        Logger.Close();
      }
    }

    // Build dictionary: key = cref name (as found after //cref:), value = list of SnippetInfo
    private static Dictionary<string, List<SnippetInfo>> BuildSnippetsDictionary(string csDir)
    {
      var dict = new Dictionary<string, List<SnippetInfo>>(StringComparer.Ordinal);
      var csFiles = Directory.GetFiles(csDir, "*.cs", SearchOption.AllDirectories);
      var crefRegex = new Regex("^\\s*//\\s*cref:\\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

      foreach (var file in csFiles)
      {
        var lines = File.ReadAllLines(file);
        var pendingCrefs = new List<string>();
        int snippetIndex = 0;
        for (int i = 0; i < lines.Length; i++)
        {
          var line = lines[i];
          var m = crefRegex.Match(line);
          if (m.Success)
          {
            var name = m.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(name))
              pendingCrefs.Add(name);
            continue;
          }

          // If we have pending crefs, look for a region to capture snippet
          if (pendingCrefs.Count > 0)
          {
            // find next #region
            int regionStart = -1;
            for (int j = i; j < lines.Length; j++)
            {
              if (lines[j].TrimStart().StartsWith("#region", StringComparison.OrdinalIgnoreCase))
              {
                regionStart = j;
                break;
              }
              // allow some comments/blank lines between cref and region
              if (!string.IsNullOrWhiteSpace(lines[j]) && !lines[j].TrimStart().StartsWith("//"))
              {
                // not a region and not just comment/blank - give up on this pending block
                break;
              }
            }

            if (regionStart >= 0)
            {
              int regionEnd = -1;
              for (int j = regionStart + 1; j < lines.Length; j++)
              {
                if (lines[j].TrimStart().StartsWith("#endregion", StringComparison.OrdinalIgnoreCase))
                {
                  regionEnd = j;
                  break;
                }
              }

              if (regionEnd >= 0 && regionEnd > regionStart)
              {
                var snippetLines = lines.Skip(regionStart + 1).Take(regionEnd - regionStart - 1).ToArray();
                var snippet = string.Join(Environment.NewLine, snippetLines).TrimEnd();

                // capture region name text after "#region"
                var regionLine = lines[regionStart].Trim();
                string regionName = string.Empty;
                if (regionLine.Length > 7 && regionLine.StartsWith("#region", StringComparison.OrdinalIgnoreCase))
                {
                  regionName = regionLine[7..].Trim();
                }

                foreach (var cref in pendingCrefs)
                {
                  var info = new SnippetInfo
                  {
                    Cref = cref.Replace(" ", ""), // Remove any space inside the Cref "call"
                    Code = snippet,
                    SourceFile = file,
                    IndexInSource = snippetIndex++,
                    RegionName = regionName
                  };

                  if (!dict.TryGetValue(cref, out var list))
                  {
                    list = [];
                    dict[cref] = list;
                  }
                  list.Add(info);
                }

                // move i to regionEnd to continue parsing after the region
                i = regionEnd;
                pendingCrefs.Clear();
                continue;
              }
            }

            // if we get here, we didn't find a region - clear pending crefs to avoid infinite retention
            pendingCrefs.Clear();
          }
        }
      }

      return dict;
    }

    // Add inside the Program class
    private static int CountExamplesWithCodeDescription(XElement element, string description)
    {
      if (element == null) return 0;
      if (string.IsNullOrWhiteSpace(description)) return 0;

      var count = element
        .Elements("example")
        .Count(ex => string.Equals((string?)ex.Element("code")?.Attribute("description"), description, StringComparison.InvariantCultureIgnoreCase));

      return count;
    }

    private static string NormalizeMember(string s)
    {
      if (string.IsNullOrWhiteSpace(s)) return string.Empty;
      var t = s.Trim();
      // remove leading prefix like 'M:' 'T:' etc
      var colonIndex = t.IndexOf(':');
      if (colonIndex >= 0 && colonIndex < 3)
      {
        t = t[(colonIndex + 1)..];
      }

      // remove parameter lists in parentheses
      t = Regex.Replace(t, "\\(.*\\)$", "", RegexOptions.Compiled);

      // normalize whitespace
      return t.Trim();
    }

    private static string GetSimpleMemberName(string normalized)
    {
      if (string.IsNullOrEmpty(normalized)) return string.Empty;
      var parts = normalized.Split('.');
      return parts.Length > 0 ? parts.Last() : normalized;
    }

    // Add this method inside the Program class to handle the extraction and injection of example tags from XML files.
    private static void ExtractAndInjectExamples(List<XmlDocInfo> xmlDocs)
    {
      foreach (var doc in xmlDocs)
      {
        bool modified = false;

        // Iterate through all <example> tags in the XML document
        var examples = doc.Document.Descendants("example").ToList();
        foreach (var example in examples)
        {
          // Extract the content of the <example> tag
          var exampleContent = example.Value.Trim();

          // Check if the parent <member> tag has a <summaryText> tag
          var parentMember = example.Ancestors("member").FirstOrDefault();
          var summaryTag = parentMember?.Element("summary");
          if (summaryTag != null)
          {
            var description = summaryTag.Value.Trim();

            // Create a new <code> tag with the extracted content and description
            var codeTag = new XElement("code",
                new XAttribute("description", description),
                exampleContent
            );

            // Replace the <example> tag content with the new <code> tag
            example.RemoveNodes();
            example.Add(codeTag);

            modified = true;
          }
        }

        // Mark the document as changed if any modifications were made
        if (modified)
        {
          doc.Changed = true;
        }
      }
    }
  }
}
