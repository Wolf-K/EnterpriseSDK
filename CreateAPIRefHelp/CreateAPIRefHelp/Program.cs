using CreateAPIRefHelp.Properties;
using DatabaseIO;
using MdxUtil;
using MyReflection;
using MyXmlDoc;
using OlbLib;
using SyntaxGenerator;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace CreateAPIRefHelp
{
  internal class Program
  {
    static int Main(string[] args)
    {
      int iReturnCode = 2;
      if (args is null || args.Length < 5)
      {
        Console.WriteLine("Usage: CreateAPIRefHelp <path-to-tlb-or-dll> <output-html-root-folder> <namespace-prefix> <release-version-label> <include-list.json> [/xml] [/clean]");
        return 1;
      }
      string pathToTlbOrDll = Path.GetFullPath(args[0]);
      string outputRoot = Path.GetFullPath(args[1]);
      string namespacePrefix = args[2] ?? string.Empty;
      string releaseVersion = args[3] ?? string.Empty;
      string? includeListPath = args[4] ?? string.Empty;
      string bitmapPath = Path.Combine(Directory.GetParent(Directory.GetParent(pathToTlbOrDll).FullName).FullName, "bitmaps");
      string bitmapDestPath = Path.Combine(outputRoot, "bitmaps");
      bool cleanOutput = args.Any(arg => arg.Equals("/clean", StringComparison.OrdinalIgnoreCase));
      bool xmlDocOutput = args.Any(arg => arg.Equals("/xml", StringComparison.OrdinalIgnoreCase));
      if (!Directory.Exists(pathToTlbOrDll))
      {
        Console.WriteLine($"Error: directory not found: {pathToTlbOrDll}");
        return iReturnCode;
      }
      try
      {
        if(!Directory.Exists(outputRoot))
          Directory.CreateDirectory(outputRoot);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error: cannot create or access output folder '{outputRoot}': {ex.Message}");
        iReturnCode = 3;
        return iReturnCode;
      }
      if (string.IsNullOrWhiteSpace(namespacePrefix))
      {
        Console.WriteLine("Error: namespace prefix must be specified and not whitespace.");
        iReturnCode = 4;
        return iReturnCode;
      }
      // make sure that namespace prefix ends with a dot
      if (!namespacePrefix.EndsWith("."))
      {
        namespacePrefix += ".";
      }
      if (string.IsNullOrWhiteSpace(releaseVersion))
      {
        Console.WriteLine("Error: release version label must be specified and not whitespace.");
        iReturnCode = 5;
        return iReturnCode;
      }

      Console.WriteLine($"Tlb/Dll directory: {pathToTlbOrDll}");
      Console.WriteLine($"Output root:       {outputRoot}");
      Console.WriteLine($"Namespace prefix:  {namespacePrefix}");
      Console.WriteLine($"Release version:   {releaseVersion}");
      Console.WriteLine($"Clean output:      {cleanOutput}");
      Console.WriteLine($"XML doc output:    {xmlDocOutput}");
      Console.WriteLine($"Include list:      {includeListPath ?? "none"}");

      HashSet<string>? includedFiles = null;
      if (!string.IsNullOrWhiteSpace(includeListPath))
      {
        includeListPath = Path.GetFullPath(includeListPath);
        if (!File.Exists(includeListPath))
        {
          Console.WriteLine($"Error: include list file not found: {includeListPath}");
          return 9;
        }

        try
        {
          includedFiles = LoadIncludeFileSet(includeListPath);
          Console.WriteLine($"Include list:      {includeListPath}");
          Console.WriteLine($"Included files:    {includedFiles.Count}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error: invalid include list JSON '{includeListPath}': {ex.Message}");
          return 10;
        }
      }

      if (cleanOutput)
      {
        try
        {
          Console.WriteLine("Cleaning output folder...");
          DirectoryInfo di = new(outputRoot);
          foreach (FileInfo file in di.GetFiles())
          {
            file.Delete();
          }
          foreach (DirectoryInfo dir in di.GetDirectories())
          {
            dir.Delete(true);
          }
          Console.WriteLine("Output folder cleaned.");
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine($"Error: cannot clean output folder '{outputRoot}': {ex.Message}");
          iReturnCode = 7;
          return iReturnCode;
        }
      }
      // copy bitmaps if they exist
      if (Directory.Exists(bitmapPath))
      {
        try
        {
          Console.WriteLine("Copying bitmaps...");
          Directory.CreateDirectory(bitmapDestPath);
          foreach (string file in Directory.GetFiles(bitmapPath))
          {
            string destFile = Path.Combine(bitmapDestPath, Path.GetFileName(file));
            File.Copy(file, destFile, true);
          }
          Console.WriteLine("Bitmaps copied.");
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine($"Error: cannot copy bitmaps from '{bitmapPath}' to '{bitmapDestPath}': {ex.Message}");
          iReturnCode = 8;
          return iReturnCode;
        }
      }
      else
      {
        Console.WriteLine("No bitmaps found to copy.");
      }
      // The input is a valid file, the output folder exists (or was created),
      // and both namespace prefix and release version are provided.
      // Further processing of the Type Library (.tlb) or Assembly (.dll)
      // and generation of MDX files should be implemented here.
      SqlUtil? sqlUtil = null;
      try
      {
        // setup DB Connection String
        if (!ModifyDatabase.OpenDB(Resources._DbConnectionStr))
        {
          throw new Exception($@"Cannot open database connection. Error: {sqlUtil?.GetError()}");
        }
        // load all templates from embedded resources
        MdxUtil.MdxUtil.PageNamespaceTemplate = Resources.PageNamespaceMdTemplate;
        MdxUtil.MdxUtil.PageComNamespaceTemplate = Resources.PageComNamespaceMdTemplate;
        MdxUtil.MdxUtil.PageInterfaceTemplate = Resources.PageInterfaceMdTemplate;
        MdxUtil.MdxUtil.PageCoClassTemplate = Resources.PageCoClassMdTemplate;
        MdxUtil.MdxUtil.PageClassTemplate = Resources.PageClassMdTemplate;
        MdxUtil.MdxUtil.PageEnumTemplate = Resources.PageEnumMdTemplate;
        MdxUtil.MdxUtil.InheritanceTemplate = Resources.InheritanceMdTemplate;
        MdxUtil.MdxUtil.MemberMdTemplate = Resources.MemberMdTemplate;
        MdxUtil.MdxUtil.MemberHeader2Template = Resources.MethodHeader2MdTemplate;
        MdxUtil.MdxUtil.MemberHeader3Template = Resources.MethodHeader3MdTemplate;
        MdxUtil.MdxUtil.MemberDetailTemplate = Resources.MemberDetailMdTemplate;
        MdxUtil.MdxUtil.MemberRemarksTemplate = Resources.MemberRemarksMdTemplate;
        MdxUtil.MdxUtil.MethodParameterTemplate = Resources.MethodParameterMdTemplate;
        MdxUtil.MdxUtil.RemarksTemplate = Resources.RemarksMdTemplate;
        MdxUtil.MdxUtil.EnumHeaderTemplate = Resources.EnumHeaderMdTemplate;
        MdxUtil.MdxUtil.ConstructorTemplate = Resources.ConstructorMdTemplate;
        MdxUtil.MdxUtil.ConstructorDetailMdTemplate = Resources.ConstructorDetailMdTemplate;
        MdxUtil.MdxUtil.SampleTemplate = Resources.SampleMdTemplate;
        MdxUtil.MdxUtil.SyntaxTemplate = Resources.SyntaxMdTemplate;
        // get all .tlb and .dll files in the pathToTlbOrDll directory
        foreach (string tlbOrDllFile in GetInputLibraryFiles(pathToTlbOrDll))
        {
          string fileName = Path.GetFileName(tlbOrDllFile);
          if (includedFiles != null && !IsIncludedForOutput(fileName, includedFiles))
          {
            Console.WriteLine($"Skipping output for '{fileName}' because it is not listed in include list.");
            continue;
          }
          Console.WriteLine($"Processing: {fileName}");
          var isTypeLib = Path.GetExtension(tlbOrDllFile).Equals(".tlb", StringComparison.OrdinalIgnoreCase);
          if (isTypeLib)
          {
            // Load the Type Library and generate HTML documentation
            // get the file name of the input path without extension this will be called the library name
            string libraryName = Path.GetFileNameWithoutExtension(tlbOrDllFile);
            Console.WriteLine($"Processing Type Library: {libraryName}");
            TlbLibCollection.CurrentLibrary = libraryName;
            var inputDir = Path.GetDirectoryName(tlbOrDllFile);
            var tlbLibCollection = new TlbLibCollection(inputDir, libraryName, namespacePrefix);
            if (xmlDocOutput)
            {
              var dllName = TlbWrite.WriteXmlDocument(tlbLibCollection, outputRoot, libraryName);
              // write the DLL file to the output folder for DocFX to use for generating API documentation
              var sourceFile = Path.Combine(inputDir, $"{dllName}.dll");
              File.Copy(sourceFile, Path.Combine(outputRoot, $"{dllName}.dll"), true);
            }
            else
            {
              TlbWrite.WriteMDXFiles(tlbLibCollection, outputRoot, libraryName);
            }
          }
          else
          {
            // Load the Assembly and generate HTML documentation{
            var inputDir = Path.GetDirectoryName(tlbOrDllFile);
            if (!Directory.Exists(inputDir)) throw new ArgumentException($@"Root folder doesn't exist: {inputDir}");
            string inputFile = tlbOrDllFile;

            // Get the array of runtime assemblies.
            // This will allow us to at least inspect types depending only on BCL.
            string[] runtimeAssemblies = Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");

            // Create the list of assembly paths consisting of runtime assemblies and the input file.
            var paths = new List<string>(runtimeAssemblies);
            // Add the list of Pro paths consisting of runtime assemblies and the inspected assembly.
            if (inputDir != null)
            {
              string[] proDlls = Directory.GetFiles(inputDir, "*.dll", SearchOption.AllDirectories);
              paths.AddRange(proDlls);
            }
            paths.Add(inputFile);
            //// Create MetadataLoadContext that can resolve assemblies using the created list.
            //var resolver = new PathAssemblyResolver(paths);
            //using var mlc = new MetadataLoadContext(resolver);
            //// Load assembly into MetadataLoadContext.
            //Assembly assembly = mlc.LoadFromAssemblyPath(inputFile);
            Assembly assembly = Assembly.LoadFrom(inputFile);
            AssemblyName name = assembly.GetName();
            // ready the syntax generator
            MyUtil.SyntaxMaker = new SyntaxMaker(inputFile);
            // prep for triple slash
            var tripleSlash = new LookupComments(assembly);
            // Get all types in the assembly and save the information in the database.
            var types = new List<MyTypeBase>();
            Type[] assemblyTypes = assembly.GetTypes();
            var namespaces = assemblyTypes.Select(t => t.Namespace).Where(ns => ns != null
                              && !ns.Contains(".Internal.")
                              && !ns.EndsWith(".Internal")).Distinct();
            MdxUtil.SeeTagConverter.InitializeNamespaces(namespaces);
            for (int i = 0; i < assemblyTypes.Length; i++)
            {
              TypeInfo t = (TypeInfo)assemblyTypes[i];
              try
              {
                if (t.FullName == null) continue;
                if (t.FullName.Contains(".Internal.", StringComparison.OrdinalIgnoreCase)) continue;
                if (t.IsNotPublic) continue;
                if (t.IsNestedPrivate) continue;
                if (!t.IsPublic) continue;
                var members = t.DeclaredMembers;
                if (members.Count() == 0) continue;
                if (t.HasExcludeTag())
                {
                  continue;
                }
                var bType = MyTypeBase.GetTypeBaseFromTypeInfo(t, true);
                if (bType != null) types.Add(bType);
              }
              catch (Exception ex)
              {
                // We are missing the required dependency assembly.
                Console.Error.WriteLine("Error: " + ex.Message);
              }
            }
            var namespaceTypes = new List<MyTypeBase>();
            foreach (var theNamespace in namespaces)
            {
              // there is no type for namespace so we add our own
              // namespace is special it needs the list of all types in that namespace 
              var typesInNamespace = types.Where(t => t.Namespace == theNamespace).ToList();
              namespaceTypes.Add(new MyNamespace(theNamespace, typesInNamespace));
            }
            types.InsertRange(0, namespaceTypes);
            List<string> processedNamespace = [];
            foreach (var theType in types)
            {
              // use the namespace as the subfolder name under the output root
              // then output the types in that namespace in that subfolder
              // create a list of types in that namespace and pass it to the MDX writer
              if (theType.KindOf == KindType.Namespace)
              {
                if (processedNamespace.Contains(theType.Namespace)) continue;
                processedNamespace.Add(theType.Namespace);
                if (xmlDocOutput)
                {
                  theType.WriteXML(outputRoot, Path.GetFileNameWithoutExtension(inputFile));
                }
                else
                {
                  theType.WriteMdx(outputRoot, theType.Namespace);
                }
              }
            }
            if (!xmlDocOutput)
            {
              // write the TOC file into the outputRoot folder
              TocStore.WriteTocAsXml(Path.Combine(outputRoot, TocStore.TocFilename));
            }
          }
        }
        iReturnCode = 0;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Error: processing failed: {ex.Message}");
        Console.Error.WriteLine(ex.ToString());
        iReturnCode = 6;
      }
      finally
      {
        sqlUtil?.Close();
        Console.WriteLine("Processing completed.");
      }
      return iReturnCode;
    }

    private static string? GetIncludeJsonPath(string[] args)
    {
      for (int i = 4; i < args.Length; i++)
      {
        var arg = args[i];
        if (arg.StartsWith("/includejson:", StringComparison.OrdinalIgnoreCase))
        {
          return arg.Substring("/includejson:".Length).Trim('"');
        }
        if (arg.Equals("/includejson", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
          return args[i + 1].Trim('"');
        }

        if (arg.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
          return arg.Trim('"');
        }
      }
      return null;
    }

    private static HashSet<string> LoadIncludeFileSet(string includeListPath)
    {
      using var stream = File.OpenRead(includeListPath);
      using var document = JsonDocument.Parse(stream);
      var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      void AddEntries(JsonElement arrayElement)
      {
        foreach (var item in arrayElement.EnumerateArray())
        {
          if (item.ValueKind == JsonValueKind.String)
          {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
              files.Add(NormalizePathOrName(value));
            }
          }
        }
      }

      var root = document.RootElement;
      if (root.ValueKind == JsonValueKind.Array)
      {
        AddEntries(root);
      }
      else if (root.ValueKind == JsonValueKind.Object)
      {
        if (root.TryGetProperty("files", out var filesNode) && filesNode.ValueKind == JsonValueKind.Array)
        {
          AddEntries(filesNode);
        }
        if (root.TryGetProperty("dll", out var dllNode) && dllNode.ValueKind == JsonValueKind.Array)
        {
          AddEntries(dllNode);
        }
        if (root.TryGetProperty("tlb", out var tlbNode) && tlbNode.ValueKind == JsonValueKind.Array)
        {
          AddEntries(tlbNode);
        }
      }
      else
      {
        throw new InvalidDataException("Root JSON must be an array or an object with array properties.");
      }
      return files;
    }

    private static bool IsIncludedForOutput(string inputPath, HashSet<string> includedFiles)
    {
      if (includedFiles.Count == 0)
      {
        return false;
      }

      var normalizedFullPath = NormalizePathOrName(inputPath);
      var normalizedFileName = NormalizePathOrName(Path.GetFileName(inputPath));
      var normalizedFileNameWithoutExt = Path.GetFileNameWithoutExtension(normalizedFileName);

      return includedFiles.Contains(normalizedFullPath)
             || includedFiles.Contains(normalizedFileName)
             || includedFiles.Contains(normalizedFileNameWithoutExt);
    }

    private static string NormalizePathOrName(string pathOrName)
    {
      if (string.IsNullOrWhiteSpace(pathOrName))
      {
        return string.Empty;
      }

      var trimmed = pathOrName.Trim().Trim('"');
      if (Path.IsPathRooted(trimmed))
      {
        return Path.GetFullPath(trimmed);
      }

      return trimmed;
    }

    private static List<string> GetInputLibraryFiles(string pathToTlbOrDll)
    {
      return Directory
        .EnumerateFiles(pathToTlbOrDll, "*", SearchOption.TopDirectoryOnly)
        .Where(file => file.EndsWith(".tlb", StringComparison.OrdinalIgnoreCase)
                       || file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        .OrderBy(Path.GetFileName)
        .ToList();
    }
  }
}
