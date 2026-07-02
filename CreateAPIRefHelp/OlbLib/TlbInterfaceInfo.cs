using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace OlbLib
{
  public class TlbInterfaceInfo
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
      {
        // esriCarto::Arguments
        return $"{LibraryName}::{Name}";
      }
    }

    public int HelpContext { get; protected set; }

    public string LibraryName { get; protected set; }

    internal Type ManagedType { get; set; }

    public string Attributes { get; protected set; }

    public string AttributeMask { get; set; }

    public int Index { get; set; }

    public bool IsEvent
    {
      get
      {
        bool isEvent = false;
        foreach (var coClassInfo in CoClasses)
        {
          foreach (var implIntInfo in coClassInfo.ImplementedInterfaceInfos)
          {
            if (Name.Equals(implIntInfo.Name))
            {
              if (implIntInfo.IsEvent)
              {
                isEvent = true;
                break;
              }
            }
          }
          if (isEvent) break;
        }
        return isEvent;
      }
    }

    public List<TlbMemberInfo> Members { get; set; }

    private List<TlbImplementedInterface> _directInheritedInterfaces = null;

    private List<TlbImplementedInterface> _allInheritedInterfaces = null;

    public List<TlbImplementedInterface> InheritedInterfaces
    {
      get
      {
        if (_allInheritedInterfaces == null)
        {
          _allInheritedInterfaces = new List<TlbImplementedInterface>();
          foreach (var inheritedInterface in _directInheritedInterfaces)
          {
            _allInheritedInterfaces.Add(inheritedInterface);
            if (inheritedInterface.InterfaceInfo == null) continue;
            foreach (var childInherited in inheritedInterface.InterfaceInfo.InheritedInterfaces)
            {
              _allInheritedInterfaces.Add(childInherited);
            }
          }
          return _allInheritedInterfaces;
        }
        return _allInheritedInterfaces;
      }
    }

    private List<TlbMemberInfo> _inheritedInferfaceMembers = null;

    public List<TlbMemberInfo> InheritedInterfaceMembers
    {
      get
      {
        if (_inheritedInferfaceMembers == null)
        {
          _inheritedInferfaceMembers = new List<TlbMemberInfo>();
          foreach (var inheritedInterface in InheritedInterfaces)
          {
            if (inheritedInterface.InterfaceInfo == null) continue;
            foreach (var member in inheritedInterface.InterfaceInfo.AllMembers)
            {
              if (!Members.Contains(member)
                  && !_inheritedInferfaceMembers.Contains(member))
                _inheritedInferfaceMembers.Add(member);
            }
          }
          if (_inheritedInferfaceMembers.Count > 0)
            _inheritedInferfaceMembers.Sort((x, y) => string.Compare($@"{x.Name} {x.Index}", $@"{y.Name} {y.Index}"));
          return _inheritedInferfaceMembers;
        }
        return _inheritedInferfaceMembers;
      }
    }

    private List<TlbMemberInfo> _allmembers = null;

    public List<TlbMemberInfo> AllMembers
    {
      get
      {
        if (_allmembers == null)
        {
          _allmembers = new List<TlbMemberInfo>(Members);
          _allmembers.AddRange(InheritedInterfaceMembers);
          if (_allmembers.Count > 0)
            _allmembers.Sort((x, y) => string.Compare($@"{x.Name} {x.Index}", $@"{y.Name} {y.Index}"));
          return _allmembers;
        }
        return _allmembers;
      }
    }

    public string ParentContainingFile
    {
      get
      {
        return TlbLibCollection.Singleton.OlbPaths[LibraryName];
      }
    }

    public TlbInterfaceInfo(TlbTypeLibInfo pTypeLib, int idx)
    {
      LibraryName = pTypeLib.Name;
      Index = idx;

      Members = [];
      _directInheritedInterfaces = new List<TlbImplementedInterface>();

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
      if (ManagedType != null)
      {
        TlbUtil.GetMethods(pTypeLib, ManagedType, idx, sName, Members);
        TlbUtil.GetImplementedInterfaces(pTypeLib, ManagedType, idx, _directInheritedInterfaces);
      }
      Attributes = TlbUtil.GetAttrString(currentTypeInfo);
    }

    private List<TlbCoClassInfo> lstCoClasses = null;

    /// <summary>
    /// CoClasses the implement this Interface
    /// </summary>
    /// <returns></returns>
    public List<TlbCoClassInfo> CoClasses
    {
      get
      {
        if (lstCoClasses == null)
        {
          lstCoClasses = new List<TlbCoClassInfo>();
          foreach (var key in TlbLibCollection.Singleton.Libraries.Keys)
          {
            foreach (var coClassInfo in TlbLibCollection.Singleton.Libraries[key].CoClassInfos)
            {
              foreach (var intf in coClassInfo.ImplementedInterfaceInfos)
              {
                if (intf.Name.Equals(this.Name) && intf.TypeLibName.Equals(this.LibraryName))
                {
                  lstCoClasses.Add(coClassInfo);
                  break;
                }
              }

            }
          }
          lstCoClasses.Sort((x, y) => string.Compare(x.Name, y.Name));
        }
        return lstCoClasses;
      }
    }
  }
}
