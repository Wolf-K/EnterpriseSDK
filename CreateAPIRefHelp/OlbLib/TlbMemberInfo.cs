using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace OlbLib
{
  public class TlbMemberInfo : IComparable<TlbMemberInfo>
  {
    public TlbTypeLibInfo ParentTypeLibInfo { get; set; }

    public string Name { get; set; }

    public string PartialName
    {
      get
      {
        // i.e. esriCarto::Arguments
        return $"{LibraryName}::{Name}";
      }
    }


    public string FullName
    {
      get
      {  // i.e. esriCarto::IAISRequest::Arguments
        return $"{LibraryName}::{ParentName}::{Name}";
      }
    }

    public string HelpString { get; set; }

    public string ParentName { get; protected set; }

    public string LibraryName { get; protected set; }
    public string Namespace { get; protected set; }

    public string InterfaceParentName { get; protected set; }

    private bool? _isEvent;

    public void SetIsEvent(bool bNewIsEvent) { _isEvent = bNewIsEvent; }

    public bool IsEvent
    {
      get
      {
        if (_isEvent != null) return _isEvent.Value;
        _isEvent = false;
        if (TlbLibCollection.Singleton.Libraries.ContainsKey(LibraryName))
        {
          foreach (var interfInfo in TlbLibCollection.Singleton.Libraries[LibraryName].InterfaceInfos)
          {
            if (interfInfo.Name.Equals(this.ParentName) && interfInfo.IsEvent)
            {
              _isEvent = true;
              break;
            }
          }
        }
        return _isEvent.Value;
      }
    }

    public int MemberId { get; set; }

    public int HelpContext { get; protected set; }

    public INVOKEKIND InvokeKind { get; set; }

    public string InvokeKindString
    {
      get
      {
        return InvokeKind.ToString();
      }
    }

    public INVOKEKIND AlterInvokeKind { get; internal set; }

    public int InvokeKinds
    {
      get
      {
        return ((int)InvokeKind | (int)AlterInvokeKind);
      }
    }

    public FUNCKIND FuncKind { get; set; }

    public string ReturnTypeString { get; set; }

    public TlbVarTypeInfo ReturnType { get; set; }

    public CodeMemberMethod Meth { get; set; }

    public List<TlbParameterInfo> Parameters { get; protected set; }

    public List<TlbParameterInfo> AlterParameters { get; set; }

    public List<TlbParameterInfo> AlterVcppParameters { get; set; }

    public List<TlbParameterInfo> VcppParameters { get; protected set; }

    public string VcppVarType { get; protected set; }

    public string VbVarType { get; protected set; }

    public string Value { set; get; }

    public int Index { get; set; }

    private void GetSyntax()
    {
      switch (InvokeKinds)
      {
        case (int)INVOKEKIND.INVOKE_FUNC:
          var theSyntax = ParentTypeLibInfo.SyntaxMaker.GetMdMethodSyntax(ParentTypeLibInfo.AssemblyName, ParentName, Name);
          CSharpSyntax = theSyntax.CSharpSyntax;
          VbSyntax = theSyntax.VbSyntax;
          break;
        default:
          var defaultSyntax = ParentTypeLibInfo.SyntaxMaker.GetMemberSyntax(ParentTypeLibInfo.AssemblyName, ParentName, Name, Index.ToString());
          CSharpSyntax = defaultSyntax.CSharpSyntax;
          VbSyntax = defaultSyntax.VbSyntax;
          break;
      }
    }

    public string VbSyntax { get; protected set; }

    public string CSharpSyntax { get; protected set; }

    /// <summary>
    /// Clone CTor swaps the 'Alter' properties in for the standard props
    /// </summary>
    /// <param name="clone"></param>
    public TlbMemberInfo(TlbMemberInfo clone)
    {
      Parameters = clone.AlterParameters;
      VcppParameters = clone.AlterVcppParameters;
      InvokeKind = clone.AlterInvokeKind;
      Name = clone.Name;
      HelpString = clone.HelpString;
      ParentName = clone.ParentName;
      LibraryName = clone.LibraryName;
      InterfaceParentName = clone.InterfaceParentName;
      MemberId = clone.MemberId;
      HelpContext = clone.HelpContext;
      FuncKind = clone.FuncKind;
      ReturnTypeString = clone.ReturnTypeString;
      ReturnType = clone.ReturnType;
      Meth = clone.Meth;
      VcppVarType = clone.VcppVarType;
      VbVarType = clone.VbVarType;
      Value = clone.Value;
      Index = clone.Index;
      ParentTypeLibInfo = clone.ParentTypeLibInfo;
      GetSyntax();
    }

    public TlbMemberInfo(TlbTypeLibInfo pTypeLib, FUNCDESC funcdesc, ITypeInfo pTypeInfo, int idx,
                            string returnTypeStr, CodeMemberMethod meth,
                            List<TlbParameterInfo> parameters, List<TlbParameterInfo> vcppParameters, string parentName)
    {
      Index = idx;
      ParentTypeLibInfo = pTypeLib;
      LibraryName = pTypeLib.Name;
      MemberId = funcdesc.memid;
      InvokeKind = funcdesc.invkind;
      FuncKind = funcdesc.funckind;
      ReturnTypeString = returnTypeStr;
      Parameters = parameters;
      VcppParameters = vcppParameters;
      ParentName = parentName;

      pTypeInfo.GetDocumentation(MemberId, out string sName, out string sDocString, out int dwHelpContext, out string sHelpFile);

      Name = sName;

      if (pTypeLib.TypesInAssembly.ContainsKey(sName))
        Namespace = pTypeLib.TypesInAssembly[sName].Namespace;
      HelpString = sDocString;
      HelpContext = dwHelpContext;
      Meth = meth;
      GetSyntax();
    }

    int IComparable<TlbMemberInfo>.CompareTo(TlbMemberInfo other)
    {
      if (other == null) return 1;
      int iComp = Name.CompareTo(other.Name);
      if (iComp != 0) return iComp;
      return Index - other.Index;
    }
  }
}
