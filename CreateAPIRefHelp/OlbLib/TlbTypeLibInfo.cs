using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TYPEKIND = System.Runtime.InteropServices.ComTypes.TYPEKIND;

namespace OlbLib
{
  public class TlbTypeLibInfo
  {
    protected Hashtable _typeDefHash;

    public string Name { get; set; }

    public string AssemblyName { get; set; }

    public string HelpString { get; set; }

    public string Version { get; set; }

    public SyntaxGenerator.SyntaxMaker SyntaxMaker { get; set; }

    public Guid Id { get; set; }

    public List<TlbTypeDefInfo> Libraries = [];

    internal Hashtable TypeDefHash
    {
      get
      {
        // Allocate on demand
        if (_typeDefHash == null)
        {
          _typeDefHash = new Hashtable();
        }
        return _typeDefHash;
      }
    }

    public List<TlbCoClassInfo> CoClassInfos = [];

    public List<TlbInterfaceInfo> InterfaceInfos = [];

    public List<TlbConstantInfo> ConstantInfos = [];

    public List<TlbRecordInfo> RecordInfos = [];

    public List<TlbAliasInfo> AliasInfos = [];

    internal ITypeLib _iTypeLib { get; set; }

    internal Dictionary<string, Type> TypesInAssembly { get; set; }

    public TlbTypeLibInfo(ITypeLib pTypeLib, string libraryPath, string assemblyPrefix)
    {
      _iTypeLib = pTypeLib;

      pTypeLib.GetDocumentation(-1, out string sName, out string sDocString, out int dwHelpContext, out string sHelpFile);
      Name = sName;
      HelpString = sDocString;

      pTypeLib.GetLibAttr(out IntPtr ptr);
      try
      {
        var typeAttr =
            (System.Runtime.InteropServices.ComTypes.TYPELIBATTR)
                Marshal.PtrToStructure(ptr, typeof(System.Runtime.InteropServices.ComTypes.TYPELIBATTR));
        Id = typeAttr.guid;
        Version = $@"{typeAttr.wMajorVerNum}.{typeAttr.wMinorVerNum}";
        //Console.WriteLine($@"{typeAttr.wMajorVerNum}.{typeAttr.wMinorVerNum} {typeAttr.guid}");
      }
      finally
      {
        if (ptr != IntPtr.Zero)
        {
          pTypeLib.ReleaseTLibAttr(ptr);
        }
      }

      // get the assembly because only typelibs in the assembly should be listed.
      var fileInfo = new System.IO.FileInfo(libraryPath);
      string parent_directory_path = fileInfo.DirectoryName;
      AssemblyName = GetAssemblyName(fileInfo.DirectoryName, sName, assemblyPrefix);
      TypesInAssembly = GetTypesFromAssembly(AssemblyName);

      SyntaxMaker = new SyntaxGenerator.SyntaxMaker(libraryPath);

      var typeInfoCount = pTypeLib.GetTypeInfoCount();
      for (var idx = 0; idx < typeInfoCount; idx++)
      {
        pTypeLib.GetDocumentation(idx, out sName, out sDocString, out dwHelpContext, out sHelpFile);
        if (sName.StartsWith("_")) continue;
        if (TypesInAssembly != null && !TypesInAssembly.Keys.Contains (sName))
        {
          Debug.WriteLine($@"Type not found: {sName} in {Name}");
          continue;
        }
        pTypeLib.GetTypeInfoType(idx, out TYPEKIND pTypeKind);
        bool bProcessError = true;
        switch (pTypeKind)
        {
          case TYPEKIND.TKIND_COCLASS:
            CoClassInfos.Add(new TlbCoClassInfo(this, idx));
            bProcessError = false;
            break;
          case TYPEKIND.TKIND_ALIAS:
            AliasInfos.Add(new TlbAliasInfo(this, idx));
            bProcessError = false;
            break;
          case TYPEKIND.TKIND_DISPATCH:
            InterfaceInfos.Add(new TlbInterfaceInfo(this, idx));
            bProcessError = false;
            break;
          case TYPEKIND.TKIND_ENUM:
            ConstantInfos.Add(new TlbConstantInfo(this, idx));
            bProcessError = false;
            break;
          case TYPEKIND.TKIND_INTERFACE:
            InterfaceInfos.Add(new TlbInterfaceInfo(this, idx));
            bProcessError = false;
            break;
          case TYPEKIND.TKIND_RECORD:
            RecordInfos.Add(new TlbRecordInfo(this, idx));
            bProcessError = false;
            break;
          case TYPEKIND.TKIND_MODULE:
            break;
          case TYPEKIND.TKIND_UNION:
            break;
          case TYPEKIND.TKIND_MAX:
            break;
          default:
            break;
        }
        if (bProcessError)
        {
          Console.WriteLine("*** Error in TlbTypeLibInfo {0} {1} {2}",
              Name, sName, pTypeKind);
        }
      }
      CoClassInfos.Sort((x, y) => string.Compare($@"{x.Name} {x.Index}", $@"{y.Name} {y.Index}"));
      ConstantInfos.Sort((x, y) => string.Compare($@"{x.Name} {x.Index}", $@"{y.Name} {y.Index}"));
      InterfaceInfos.Sort((x, y) => string.Compare($@"{x.Name} {x.Index}", $@"{y.Name} {y.Index}"));
      RecordInfos.Sort((x, y) => string.Compare($@"{x.Name} {x.Index}", $@"{y.Name} {y.Index}"));
      AliasInfos.Sort((x, y) => string.Compare($@"{x.Name} {x.Index}", $@"{y.Name} {y.Index}"));
      ReadTypeLibInfoInternal(pTypeLib);
    }

    private string GetAssemblyName(string libraryPath, string sTypeLibName, string assemblyPrefix)
    {
      var typeLibName = sTypeLibName.Replace("esri", assemblyPrefix).Replace("_", ".");
      return System.IO.Path.Combine (libraryPath, $@"{typeLibName}.dll");
    }

    private Dictionary<string, Type> GetTypesFromAssembly(string assemblyFile)
    {
      Dictionary<string, Type> dicTypes = [];
      try
      {
        var assembly = Assembly.LoadFrom(assemblyFile);
        foreach (var typ in assembly.GetTypes())
        {
          dicTypes.Add(typ.Name, typ);
        }
      }
      catch (Exception) {
        Console.Error.WriteLine($@"Assembly not found: {assemblyFile}");
      }
      return dicTypes;
    }

    // Returns the underlying type if this is a typedef
    internal string ResolveTypeDef(string typeName, bool comType)
    {
      if (_typeDefHash == null) return typeName;
      var typeDef = (TlbTypeDefInfo)_typeDefHash[typeName];
      if (typeDef == null) return typeName;
      if (comType) return typeDef._varComType;
      return typeDef._varClrType;
    }

    // This is not supposed to throw unless something is really wrong
    internal void ReadTypeLibInfoInternal(ITypeLib pTypeLib)
    {
      // Get the list of classes and interfaces implemented by this library
      // First do only the interfaces because we need to hook the default
      // interface to the class
      var typeInfoCount = _iTypeLib.GetTypeInfoCount();
      TYPEKIND typeKind;
      for (var i = 0; i < typeInfoCount; i++)
      {
        _iTypeLib.GetTypeInfoType(i, out typeKind);
        Libraries.Add(new TlbTypeDefInfo(this, typeKind, i));
      }
      //// Now do the classes
      //for (int i = 0; i < typeInfoCount; i++)
      //{
      //    _iTypeLib.GetTypeInfoType(i, out typeKind);
      //    if (typeKind == TYPEKIND.TKIND_COCLASS)
      //    {
      //        ComClassInfo classInfo = ComClassInfo.GetClassInfo(this, typeKind, i);
      //        classInfo._container = this;
      //        classInfo._interfaces = new ArrayList();
      //        for (int j = 0; j < classInfo._cImplTypes; j++)
      //        {
      //            int intImpl;
      //            int implTypeFlags;
      //            UCOMITypeInfo intType;
      //            classInfo._typeInfo.GetRefTypeOfImplType(j, out intImpl);
      //            classInfo._typeInfo.GetRefTypeInfo(intImpl, out intType);
      //            classInfo._typeInfo.GetImplTypeFlags(j, out implTypeFlags);
      //            ComInterfaceInfo intInfo = (ComInterfaceInfo)_interfaces[intType];
      //            if (intInfo != null)
      //            {
      //                // j == 0 indicates the default interface
      //                classInfo.AddInterface(intInfo, j == 0);
      //                if ((((IMPLTYPEFLAGS)implTypeFlags) &
      //                    IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE) != 0)
      //                    intInfo.IsSource = true;
      //            }
      //        }
      //        _classes.Add(classInfo);
      //        _members.Add(classInfo);
      //        _memberNames.Add(classInfo.Name, classInfo);
      //        if (TraceUtil.If(null, TraceLevel.Verbose))
      //        {
      //            Trace.WriteLine("TypeLib - has type: " + classInfo + " " + typeKind);
      //        }
      //    }
      //}
      //_typeLibInfoRead = true;
    }

  }
}
