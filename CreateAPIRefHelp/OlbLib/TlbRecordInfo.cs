using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace OlbLib
{
  public class TlbRecordInfo
  {
    public string Name { get; set; }

    public string HelpString { get; set; }

    public List<TlbMemberInfo> Members = new List<TlbMemberInfo>();

    public List<TlbVarTypeInfo> Variables = new List<TlbVarTypeInfo>();

    public int Index { get; set; }

    public string LibraryName { get; protected set; }

    internal Type ManagedType { get; set; }

    public string ParentContainingFile
    {
      get
      {
        return TlbLibCollection.Singleton.OlbPaths[LibraryName];
      }
    }

    public TlbRecordInfo(TlbTypeLibInfo pTypeLib, int idx)
    {
      LibraryName = pTypeLib.Name;
      Index = idx;

      string sName;
      string sDocString;
      int dwHelpContext;
      string sHelpFile;
      pTypeLib._iTypeLib.GetDocumentation(idx, out sName, out sDocString, out dwHelpContext, out sHelpFile);

      Name = sName;

      HelpString = sDocString;
      TYPEATTR typeAttr;
      ITypeInfo typeInfo;
      pTypeLib._iTypeLib.GetTypeInfo(idx, out typeInfo);
      try
      {
        if (pTypeLib.ManagedAssembly != null)
          ManagedType = TlbUtil.GetManagedType(typeInfo, pTypeLib.ManagedAssembly);
        else
          ManagedType = null;
      }
      catch
      {
        Console.Error.WriteLine($@"*** Can't get managed type for {Name}");
        ManagedType = null;
      } 
      IntPtr typeAttrPtr = IntPtr.Zero;
      try
      {
        typeInfo.GetTypeAttr(out typeAttrPtr);
        typeAttr = (TYPEATTR)System.Runtime.InteropServices.Marshal.PtrToStructure(typeAttrPtr, typeof(TYPEATTR));
        for (int i = 0; i < typeAttr.cVars; i++)
        {
          TlbVarTypeInfo iVar = new TlbVarTypeInfo(pTypeLib, typeInfo, i, false);
          Variables.Add(iVar);
        }
      }
      finally
      {
        if (typeAttrPtr != IntPtr.Zero)
          typeInfo.ReleaseTypeAttr(typeAttrPtr);
      }
    }
  }
}
