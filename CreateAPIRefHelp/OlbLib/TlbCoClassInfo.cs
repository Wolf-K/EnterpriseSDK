using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace OlbLib
{
  public class TlbCoClassInfo
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


    public string FullName
    {
      get
      {  // i.e. esriCarto::ClassX
        return $"{LibraryName}::{Name}";
      }
    }

    public string LibraryName { get; protected set; }
    public string Namespace { get; protected set; }

    public string Attributes { get; protected set; }

    public List<TlbMemberInfo> Members = [];

    public List<TlbImplementedInterface> ImplementedInterfaceInfos = [];

    public int Index { get; set; }

    public TlbCoClassInfo(TlbTypeLibInfo pTypeLib, int idx)
    {
      Index = idx;
      LibraryName = pTypeLib.Name;
      string sName;
      string sDocString;
      int dwHelpContext;
      string sHelpFile;
      pTypeLib._iTypeLib.GetDocumentation(idx, out sName, out sDocString, out dwHelpContext, out sHelpFile);
      Name = sName;
      Namespace = pTypeLib.TypesInAssembly[sName].Namespace;
      HelpString = sDocString;
      TlbUtil.GetMethods(pTypeLib, idx, sName, Members);
      TlbUtil.GetImplementedInterfaces(pTypeLib, idx, ImplementedInterfaceInfos);

      ITypeInfo currentTypeInfo;
      pTypeLib._iTypeLib.GetTypeInfo(idx, out currentTypeInfo);

      Attributes = TlbUtil.GetAttrString(currentTypeInfo);
    }

    public bool HasSource()
    {
      return false;
    }
  }
}
