using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OlbLib
{
  public class TlbConstantInfo
  {
    public string Name { get; set; }

    public string HelpString { get; set; }

    public string Namespace { get; protected set; }
    public int Index { get; set; }

    public List<TlbVarTypeInfo> Variables = [];

    public List<TlbMemberInfo> Members = [];


    public TlbConstantInfo(TlbTypeLibInfo pTypeLib, int idx)
    {
      Index = idx;

      string sName;
      string sDocString;
      int dwHelpContext;
      string sHelpFile;
      pTypeLib._iTypeLib.GetDocumentation(idx, out sName, out sDocString, out dwHelpContext, out sHelpFile);

      Name = sName;
      Namespace = pTypeLib.TypesInAssembly[sName].Namespace;
      HelpString = sDocString;

      TYPEATTR typeAttr;
      ITypeInfo typeInfo;
      pTypeLib._iTypeLib.GetTypeInfo(idx, out typeInfo);
      IntPtr typeAttrPtr = IntPtr.Zero;
      try
      {
        typeInfo.GetTypeAttr(out typeAttrPtr);
        typeAttr = (TYPEATTR)System.Runtime.InteropServices.Marshal.PtrToStructure(typeAttrPtr, typeof(TYPEATTR));
        for (int i = 0; i < typeAttr.cVars; i++)
        {
          TlbVarTypeInfo iVar = new TlbVarTypeInfo(pTypeLib, typeInfo, i, true);
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
