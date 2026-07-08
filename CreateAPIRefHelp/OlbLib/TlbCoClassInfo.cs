using MdxUtil;
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
    internal Type ManagedType { get; set; }

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
      HelpString = sDocString.ToAsciiOnly();

      ITypeInfo currentTypeInfo;
      pTypeLib._iTypeLib.GetTypeInfo(idx, out currentTypeInfo);
      try
      {
        if (pTypeLib.ManagedAssembly != null)
          ManagedType = TlbUtil.GetManagedType(currentTypeInfo, pTypeLib.ManagedAssembly);
        else
          ManagedType = null;
      }
      catch
      {
        Console.Error.WriteLine($@"*** Can't get managed type for {Name}");
        ManagedType = null;
      }

      TlbUtil.GetMethods(pTypeLib, ManagedType, idx, sName, Members);
      TlbUtil.GetImplementedInterfaces(pTypeLib, ManagedType, idx, ImplementedInterfaceInfos);

      Attributes = TlbUtil.GetAttrString(currentTypeInfo);
    }

    public bool HasSource()
    {
      return false;
    }
  }
}
