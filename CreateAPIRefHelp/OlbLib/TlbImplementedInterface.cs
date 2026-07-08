using MdxUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using IMPLTYPEFLAGS = System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS;

namespace OlbLib
{
  public class TlbImplementedInterface
  {
    public string TypeLibName { get; set; }

    public int IndexInTypeLib { get; set; }

    public string Name { get; set; }

    public string HelpString { get; set; }

    public bool IsEvent { get; protected set; }

    public TlbInterfaceInfo InterfaceInfo
    {
      get
      {
        return TlbLibCollection.GetInterface(TypeLibName, Name);
      }
    }

    public IMPLTYPEFLAGS Impltypeflags { get; set; }

    public TlbImplementedInterface(ITypeLib pTypeLib, int idx, IMPLTYPEFLAGS impltypeflags)
    {
      IndexInTypeLib = idx;
      Impltypeflags = impltypeflags;
      string sName;
      string sDocString;
      int dwHelpContext;
      string sHelpFile;
      pTypeLib.GetDocumentation(IndexInTypeLib, out sName, out sDocString, out dwHelpContext, out sHelpFile);
      Name = sName;
      HelpString = sDocString.ToAsciiOnly();

      pTypeLib.GetDocumentation(-1, out sName, out sDocString, out dwHelpContext, out sHelpFile);
      TypeLibName = sName;
      IsEvent = (impltypeflags & IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE) != 0;
    }
  }
}
