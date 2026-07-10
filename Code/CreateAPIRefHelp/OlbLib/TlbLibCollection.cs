using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace OlbLib
{
  public class TlbLibCollection
  {
    public enum RegKind
    {
      RegKind_Default = 0,
      RegKind_Register = 1,
      RegKind_None = 2
    }

    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void LoadTypeLibEx(string strTypeLibName, RegKind regKind,
        [MarshalAs(UnmanagedType.Interface)] out object typeLib);

    public static TlbLibCollection Singleton = null;

    public static string ContainingFile
    {
      get
      {
        return Singleton.OlbPaths[CurrentLibrary];
      }
    }

    public static string CurrentLibrary { get; set; }

    public Dictionary<string, ITypeLib> TypeLibs = [];

    public IDictionary<string, string> OlbPaths = new Dictionary<string, string>();

    public IDictionary<string, TlbCoClassInfo> AllCoClasses = new Dictionary<string, TlbCoClassInfo>();
    public IDictionary<string, TlbInterfaceInfo> AllInterfaces = new Dictionary<string, TlbInterfaceInfo>();

    public IDictionary<string, TlbTypeLibInfo> Libraries = new Dictionary<string, TlbTypeLibInfo>();

    public TlbLibCollection()
    {
      Singleton = this;
    }

    private static bool _OneShot = true;
    public TlbLibCollection(string tlbPath, string selectedLibrary, string assemblyPrefix)
    {
      Singleton = this;
      CurrentLibrary = selectedLibrary;
      if (_OneShot)
      {
        // Setup event handler to resolve assemblies
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);
        _OneShot = false;
      }
      System.IO.Directory.SetCurrentDirectory(tlbPath);
      //// get the dll name to check dependencies
      //var dllName = $@"{selectedLibrary.Replace("esri", "Esri.Server.")}.dll";
      //var theFiles = System.IO.Directory.GetFiles(olbPath, dllName, System.IO.SearchOption.TopDirectoryOnly);
      //if (theFiles.Length == 1) GetDependendAssemblies(theFiles[0]);
      foreach (var tlbFile in System.IO.Directory.GetFiles(tlbPath, "esri*.tlb", System.IO.SearchOption.TopDirectoryOnly))
      {
        if (!System.IO.Path.GetFileName(tlbFile).StartsWith("esri")) continue;
        //var fname = System.IO.Path.GetFileNameWithoutExtension(tlbFile);
        //if (!tlbFile.Contains(selectedLibrary) && !DependendAssemblies.Contains (fname)) continue;
        LoadTypeLibEx(tlbFile, RegKind.RegKind_None, out object typeLib);
        if (typeLib == null)
        {
          Console.Error.WriteLine("LoadTypeLibEx failed.");
          return;
        }
        var pTypeLib = typeLib as ITypeLib;
        if (pTypeLib == null) continue;
        pTypeLib.GetDocumentation(-1, out string sName, out string sDocString, out int dwHelpContext, out string sHelpFile);
        //Console.Error.WriteLine($@"{sName} {sDocString} Type info count: {pTypeLib.GetTypeInfoCount()}");
        OlbPaths.Add(sName, tlbFile);
        TypeLibs.Add(sName, pTypeLib);
        Libraries.Add(sName, new TlbTypeLibInfo(pTypeLib, tlbFile, assemblyPrefix));
        //  try
        //  {
        //    //Assembly assembly = Assembly.LoadFile(dllFile);

        //    Assembly assembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(dllFile);
        //    assembly.GetTypes();
        //    // process types here


        //    foreach (var type in assembly.GetTypes())
        //    {
        //      Console.WriteLine(type.FullName);
        //    }
        //  }
        //  catch (Exception ex)
        //  {
        //    var reflectionEx = ex as ReflectionTypeLoadException;
        //    if (reflectionEx != null)
        //    {
        //      foreach (var exception in reflectionEx.LoaderExceptions)
        //      {
        //        Console.WriteLine(exception.Message);
        //      }
        //    }
        //  }
        //  Console.WriteLine("done");        
      }
    }

    private static IList<string> DependendAssemblies = [];

    //private static void GetDependendAssemblies(string file)
    //{
    //  try
    //  {
    //    DependendAssemblies.Clear();
    //    //Console.WriteLine($@"*** Loading assembly: {file}");
    //    Assembly assembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(file);
    //    //assembly.GetTypes();
    //    // process types here
    //    //Console.WriteLine($@" Types for {file}: ");
    //    //foreach (var type in assembly.GetTypes())
    //    //{
    //    //  Console.WriteLine($@"  {type.FullName}");
    //    //}
    //  }
    //  catch (Exception ex)
    //  {
    //    if (ex is System.Reflection.ReflectionTypeLoadException)
    //    {
    //      var typeLoadException = ex as ReflectionTypeLoadException;
    //      var loaderExceptions = typeLoadException.LoaderExceptions;
    //      foreach (var lEx in loaderExceptions)
    //      {
    //        Console.Error.WriteLine($@"Error {file}: {lEx.ToString()}");
    //      }
    //    }
    //    else
    //    {
    //      Console.Error.WriteLine($@"Exception while loading assembly {file}: {ex}");
    //    }
    //  }
    //}

    // method later in the class:
    private static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
    {
      var parts = args.Name.Split(',');
      var prefix = "ESRI.Server.";
      if (parts.Length > 0)
      {
        if (parts[0].StartsWith (prefix))
        {
          DependendAssemblies.Add("esri" + parts[0].Substring(prefix.Length));
        }
      }
      return Assembly.ReflectionOnlyLoad(args.Name);
    }

    public static TlbInterfaceInfo GetInterface(string typeLibName, string name)
    {
      if (Singleton.Libraries.TryGetValue(typeLibName, out TlbTypeLibInfo tLib))
      {
        foreach (var intf in tLib.InterfaceInfos)
        {
          if (intf.Name.Equals(name)) return intf;
        }
      }
      return null;
    }

    public static TlbCoClassInfo GetClassByName(string s)
    {
      foreach (var key in Singleton.Libraries.Keys)
      {
        foreach (var coclassInfo in Singleton.Libraries[key].CoClassInfos)
        {
          if (s.Equals(coclassInfo.Name)) return coclassInfo;
        }
      }
      return null;
    }

  }
}
