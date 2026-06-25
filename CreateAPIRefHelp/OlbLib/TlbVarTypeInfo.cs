using System;
using System.Runtime.InteropServices.ComTypes;

namespace OlbLib
{
  public class TlbVarTypeInfo
  {
    public string Name { get; set; }

    public string HelpString { get; set; }

    public int HelpContext { get; protected set; }

    public string Value { get; set; }

    public int VarType { get; set; }

    public bool IsExternalType { get; set; }

    public TlbTypeLibInfo TypeLibInfoExternal { get; protected set; }

    public TlbTypeInfo TypeInfo { get; set; }

    public string VbVarTypeInfo { get; protected set; }

    public int VarTypeInfoPointerLevel { get; set; }

    private string _stars = null;

    public string Stars
    {
      get
      {
        if (_stars == null)
        {
          _stars = "";
          if (TypeInfo != null)
          {
            for (var i = 0; i < TypeInfo.Stars; i++)
            {
              _stars += "*";
            }
          }
        }
        return _stars;
      }
    }

    public string VcppVarTypeInfo { get; set; }

    public TlbVarTypeInfo(TlbTypeLibInfo pTypeLib, ITypeInfo pTypeInfo, int idx, bool bNeedsValue)
    {
      IntPtr varDescPtr = IntPtr.Zero;
      CORRECT_VARDESC varDesc;
      try
      {
        pTypeInfo.GetVarDesc(idx, out varDescPtr);
        varDesc = (CORRECT_VARDESC)System.Runtime.InteropServices.Marshal.PtrToStructure(varDescPtr, typeof(CORRECT_VARDESC));

        string sName;
        string sDocString;
        int dwHelpContext;
        string sHelpFile;
        pTypeInfo.GetDocumentation(varDesc.memid, out sName, out sDocString, out dwHelpContext, out sHelpFile);
        Name = sName;
        HelpString = sDocString;
        HelpContext = dwHelpContext;

        int actLen;
        String[] memberNames = new String[100];
        pTypeInfo.GetNames(varDesc.memid,
                           memberNames,
                           memberNames.Length,
                           out actLen);
        Name = memberNames[0];
        if (bNeedsValue)
        {
          try
          {
            Object value = System.Runtime.InteropServices.Marshal.GetObjectForNativeVariant
                (varDesc.u.lpvarValue);
            Value = value.ToString();
          }
          catch (Exception ex)
          {
            Value = $@"Unknown variant: 0x {varDesc.u.lpvarValue.ToInt32().ToString("X")} {ex.ToString()}";
          }
        }
        else
        {
          Value = string.Empty;
        }
        TYPEDESC typeDesc = varDesc.elemdescVar.tdesc;
        VarType = typeDesc.vt;

        //TlbUtil.TypedescToString
        //(pTypeLib,
        // pTypeInfo,
        // typeDesc,
        // TlbUtil.COMTYPE);
      }
      finally
      {
        if (varDescPtr != IntPtr.Zero)
          pTypeInfo.ReleaseVarDesc(varDescPtr);
      }
    }

    public TlbVarTypeInfo(TlbTypeLibInfo pTypeLib, ITypeInfo pTypeInfo, int idx, string name, TYPEDESC typeDesc)
    {
      Name = name;
      VarType = typeDesc.vt;
      //System.Diagnostics.Debug.WriteLine($@"TlbVarTypeInfo: {VarType.ToString()}");
      ////try
      ////{
      ////    if (VarType == EnumTliVarType.VT_VARIANT)
      ////        System.Diagnostics.Debug.WriteLine("variant");

      ////    else if (vtInfo.TypedVariant != null)
      ////        TypeNameOfTypedVariant = Microsoft.VisualBasic.Information.TypeName(vtInfo.TypedVariant);
      ////}
      ////catch (Exception ex)
      ////{
      ////    System.Diagnostics.Debug.WriteLine($@"Information.TypeName: for VarType {VarType.ToString()} {ex}");
      ////}
      var typeName = TlbUtil.TypedescToString(pTypeLib, pTypeInfo, typeDesc, !TlbUtil.COMTYPE);
      TypeInfo = new TlbTypeInfo(pTypeLib, pTypeInfo, typeDesc);
      IsExternalType = TypeInfo != null && pTypeLib.Name != TypeInfo.LibraryName;
      VbVarTypeInfo = TlbUtil.GetVBVarType(typeDesc.vt, this, typeName, false);
      VcppVarTypeInfo = TlbUtil.GetVCPPVarType(typeDesc.vt, this, typeName);
      VarTypeInfoPointerLevel = 0; // vtInfo.PointerLevel;
                                   //if (vtInfo.TypeInfo != null) TypeInfo = new TlbTypeInfo(vtInfo.TypeInfo, true);
    }
  }
}
