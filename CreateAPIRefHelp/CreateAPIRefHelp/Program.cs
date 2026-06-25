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

namespace CreateAPIRefHelp
{
  internal class Program
  {
    static int Main(string[] args)
    {
      int iReturnCode = 2;
      if (args is null || args.Length < 4 )
      {
        Console.WriteLine("Usage: CreateAPIRefHelp <path-to-tlb-or-dll> <output-html-root-folder> <namespace-prefix> <release-version-label> [/xml] [/clean]");
        return 1;
      }
      string pathToTlbOrDll = Path.GetFullPath(args[0]);
      string outputRoot = Path.GetFullPath(args[1]);
      string namespacePrefix = args[2] ?? string.Empty;
      string releaseVersion = args[3] ?? string.Empty;
      string bitmapPath = Path.Combine(Directory.GetParent(Directory.GetParent(pathToTlbOrDll).FullName).FullName, "bitmaps");
      string bitmapDestPath = Path.Combine(outputRoot, "bitmaps");
      bool cleanOutput = args.Any(arg => arg.Equals("/clean", StringComparison.OrdinalIgnoreCase));
      bool xmlDocOutput = args.Any(arg => arg.Equals("/xml", StringComparison.OrdinalIgnoreCase));
      if (!File.Exists(pathToTlbOrDll))
      {
        Console.WriteLine($"Error: input file not found: {pathToTlbOrDll}");
        return iReturnCode;
      }
      try
      {
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

      Console.WriteLine($"Input file:        {pathToTlbOrDll}");
      Console.WriteLine($"Output root:       {outputRoot}");
      Console.WriteLine($"Namespace prefix:  {namespacePrefix}");
      Console.WriteLine($"Release version:   {releaseVersion}");
      // get the file name of the input path without extension this will be called the library name
      string libraryName = Path.GetFileNameWithoutExtension(pathToTlbOrDll);
      Console.WriteLine($"Library name:      {libraryName}");
      var isTypeLib = Path.GetExtension(pathToTlbOrDll).Equals(".tlb", StringComparison.OrdinalIgnoreCase);
      Console.WriteLine($"Is Type Library:   {isTypeLib}");
      Console.WriteLine($"Clean output:      {cleanOutput}");
      Console.WriteLine($"XML doc output:    {xmlDocOutput}");

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
        if (isTypeLib)
        {
          // Load the Type Library and generate HTML documentation
          Console.WriteLine("Processing Type Library...");
          OlbLib.TlbLibCollection.CurrentLibrary = libraryName;
          var inputDir = Path.GetDirectoryName(pathToTlbOrDll);
          var tlbLibCollection = new OlbLib.TlbLibCollection(inputDir, libraryName, namespacePrefix);
          if (xmlDocOutput)
          {
            TlbWrite.WriteXmlDocument(tlbLibCollection, outputRoot, libraryName);
          }
          else
          {
            TlbWrite.WriteMDXFiles(tlbLibCollection, outputRoot, libraryName);
          }
        }
        else
        {
          // Load the Assembly and generate HTML documentation{
          var inputDir = Path.GetDirectoryName(pathToTlbOrDll);
          if (!Directory.Exists (inputDir)) throw new ArgumentException($@"Root folder doesn't exist: {inputDir}");
          string inputFile = pathToTlbOrDll;

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
              theType.WriteMdx(outputRoot, theType.Namespace);
            }
          }
          // write the TOC file into the outputRoot folder
          TocStore.WriteTocAsXml(Path.Combine(outputRoot, TocStore.TocFilename));
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
  }
}
