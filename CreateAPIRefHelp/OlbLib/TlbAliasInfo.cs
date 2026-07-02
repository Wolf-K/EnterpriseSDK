using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace OlbLib
{
  public class TlbAliasInfo
  {
    public string Name { get; set; }

    public string HelpString { get; set; }

    public string ParentName
    {
      get
      {
        return LibraryName;
      }
    }

    public int HelpContext { get; protected set; }

    public string LibraryName { get; protected set; }
    internal Type ManagedType { get; set; }

    public string Attributes { get; protected set; }

    public string AttributeMask { get; set; }

    public int Index { get; set; }

    public string ComType { get; set; }

    public string ClrType { get; set; }

    public TlbAliasInfo(TlbTypeLibInfo pTypeLib, int idx)
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
      HelpContext = dwHelpContext;

      ITypeInfo currentTypeInfo;
      pTypeLib._iTypeLib.GetTypeInfo(idx, out currentTypeInfo);
      Attributes = TlbUtil.GetAttrString(currentTypeInfo);

      //Get the TypeAttributes
      IntPtr typeDescPtr;
      currentTypeInfo.GetTypeAttr(out typeDescPtr);
      TYPEATTR typeAttr;
      try
      {
        typeAttr = (TYPEATTR)System.Runtime.InteropServices.Marshal.PtrToStructure(typeDescPtr, typeof(System.Runtime.InteropServices.ComTypes.TYPEATTR));
      }
      finally
      {
        if (typeDescPtr != IntPtr.Zero)
        {
          currentTypeInfo.ReleaseTypeAttr(typeDescPtr);
        }
      }

      ComType =
          TlbUtil.TypedescToString(pTypeLib,
                           currentTypeInfo,
                           typeAttr.tdescAlias,
                           TlbUtil.COMTYPE);
      ClrType =
          TlbUtil.TypedescToString(pTypeLib,
                                       currentTypeInfo,
                                       typeAttr.tdescAlias,
                                       !TlbUtil.COMTYPE);

    }
  }
}
