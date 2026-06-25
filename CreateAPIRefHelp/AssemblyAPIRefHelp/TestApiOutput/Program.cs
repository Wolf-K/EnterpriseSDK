using DatabaseIO;
using MyReflection;
using MyXmlDoc;
using System;
using System.IO;
using System.Reflection;
using static System.Net.WebRequestMethods;

namespace TestAPIOutput
{
  internal class Program
  {
    static int Main(string[] args)
    {
      if (args.Length < 2)
      {
        Console.WriteLine("Usage: TestAPIOutput.exe <assembly root> <input dll> <output path>");
        return 0;
      }
      try
      {
        string rootFolder = args[0];
        var dirRoot = new DirectoryInfo(rootFolder);
        if (!dirRoot.Exists) throw new ArgumentException($@"Root folder doesn't exist: {rootFolder}");
        var dirRootParent = (dirRoot.Parent?.Name) ?? throw new ArgumentException($@"Root folder doesn't exist: {rootFolder}");
        string inputFile = args[1];

        // Get the array of runtime assemblies.
        // This will allow us to at least inspect types depending only on BCL.
        string[] runtimeAssemblies = Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");

        // Create the list of assembly paths consisting of runtime assemblies and the input file.
        var paths = new List<string>(runtimeAssemblies);
        // Add the list of Pro paths consisting of runtime assemblies and the inspected assembly.
        if (rootFolder != null)
        {
          string[] proDlls = Directory.GetFiles(rootFolder, "*.dll", SearchOption.AllDirectories);
          paths.AddRange(proDlls);
        }

        paths.Add(inputFile);
        // open the database connection
        if (!ModifyDatabase.OpenDB())
        {
          throw new Exception($@"Database open error: {ModifyDatabase.SqlUtil.GetError()}");
        }
        // Create MetadataLoadContext that can resolve assemblies using the created list.
        var resolver = new PathAssemblyResolver(paths);
        using (var mlc = new MetadataLoadContext(resolver))
        {
          using (mlc)
          {
            // Load assembly into MetadataLoadContext.
            Assembly assembly = mlc.LoadFromAssemblyPath(inputFile);
            AssemblyName name = assembly.GetName();
            if (!ModifyDatabase.OpenDB())
            {
              throw new Exception($@"Database open error: {ModifyDatabase.SqlUtil.GetError()}");
            }
            // Print assembly attribute information.
            //System.Diagnostics.Trace.WriteLine(name.Name + " has following attributes: ");
            foreach (CustomAttributeData attr in assembly.GetCustomAttributesData())
            {
              try
              {
                //System.Diagnostics.Trace.WriteLine(attr.AttributeType);
              }
              catch (FileNotFoundException ex)
              {
                // We are missing the required dependency assembly.
                Console.WriteLine("Error getting attribute type: " + ex.Message);
              }
            }
            Console.WriteLine();

            // Get all types in the assembly and save the information in the database.
            var types = new List<MyTypeBase>();
            foreach (TypeInfo t in assembly.GetTypes())
            {
              try
              {
                if (t.FullName == null) continue;
                if (t.FullName.Contains(".Internal.", StringComparison.OrdinalIgnoreCase)) continue;
                if (t.IsNotPublic) continue;
                if (t.IsNestedPrivate) continue;

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
                Console.WriteLine("Error: " + ex.Message);
              }
            }
            // Print assembly type information by namespace
            Console.WriteLine(name.Name + " contains following types: ");

            var sortedTypes = MyTypeComparer.CompleteSort(types);

            //Excel.WriteToSpreadsheet(excelFile, sortedTypes);
            var namespaces = types.Select(t => t.Namespace).Distinct();
            foreach (var ns in namespaces)
            {
              Console.WriteLine(ns);
              foreach (var type in types)
              {
                if (type.Namespace != ns) continue;
                Console.Write(" ");
                Console.WriteLine(type);
                foreach (var member in type.Members)
                {
                  Console.Write("  ");
                  Console.WriteLine(member);
                }
                foreach (var enumParam in type.EnumParameters)
                {
                  Console.Write("  ");
                  Console.WriteLine(enumParam);
                }
              }
            }
          }
        }

        ModifyDatabase.CloseDb();
        return 0;
      }
      catch (IOException ex)
      {
        Console.WriteLine("I/O error occured when trying to load assembly: ");
        Console.WriteLine(ex.ToString());
        return 1;
      }
      catch (UnauthorizedAccessException ex)
      {
        Console.WriteLine("Access denied when trying to load assembly: ");
        Console.WriteLine(ex.ToString());
        return 1;
      }
    }
  }
}