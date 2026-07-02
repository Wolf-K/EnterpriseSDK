using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace OlbLib
{
  public class TlbTypeInfo
  {
    public string Name { get; protected set; }

    public int Stars { get; protected set; }

    public System.Runtime.InteropServices.ComTypes.TYPEKIND TypeKind { get; protected set; }

    public string LibraryName { get; set; }

    internal Type ManagedType { get; set; }

    public List<TlbMemberInfo> Members { get; protected set; }

    public TlbTypeInfo(TlbTypeLibInfo pTypeLib, ITypeInfo pTypeInfo, System.Runtime.InteropServices.ComTypes.TYPEDESC typeDesc)
    {
      Name = TypedescToStringInt(pTypeLib, pTypeInfo, typeDesc, true, 0);
    }

    private string TypedescToStringInt(TlbTypeLibInfo typeLib,
                                       ITypeInfo typeInfo,
                                       System.Runtime.InteropServices.ComTypes.TYPEDESC typeDesc,
                                       bool comType,
                                       int level)
    {
      string ret;
      try
      {
        if ((VarEnum)typeDesc.vt == VarEnum.VT_PTR ||
            (VarEnum)(typeDesc.vt & TlbUtil.VT_TYPEMASK) ==
            VarEnum.VT_SAFEARRAY)
        {
          var pTypeDesc = (System.Runtime.InteropServices.ComTypes.TYPEDESC)Marshal.PtrToStructure(typeDesc.lpValue, typeof(System.Runtime.InteropServices.ComTypes.TYPEDESC));
          ret = TypedescToStringInt(typeLib,
                                    typeInfo, pTypeDesc,
                                    comType, level + 1);
          if ((VarEnum)(typeDesc.vt & TlbUtil.VT_TYPEMASK) == VarEnum.VT_SAFEARRAY)
          {
            // FIXME - what about the non-comType
            return "SAFEARRAY(" + ret + ")";
          }
          if (comType)
          {
            ret += "*";
            Stars++;
          }
          else
          {
            // void* become IntPtr
            if (ret.Equals("void"))
              ret = "System.IntPtr";
            else
            {
              // The first pointer is not a ref, its only
              // a ref if there are two
              // FIXME - what if there are more?
              if (level == 1)
                ret = "ref " + ret;
            }
          }
          return ret;
        }
        if ((VarEnum)(typeDesc.vt & TlbUtil.VT_TYPEMASK) ==
            VarEnum.VT_CARRAY)
        {
          // typeDesc.lpValue in this case is really the laValue 
          // (since TYPEDESC is a contains a union of pointers)
          ARRAYDESC pArrayDesc =
              (ARRAYDESC)Marshal.PtrToStructure(typeDesc.lpValue,
                                              typeof(ARRAYDESC));
          ret = TypedescToStringInt(typeLib, typeInfo,
                                   pArrayDesc.tdescElem,
                                   comType,
                                   level + 1);
          // Just show the number of dimensions, don't worry about
          // showing the size of each since we don't want to 
          // get into marshaling the variable length ARRAYDESC
          // structure
          for (int i = 0; i < pArrayDesc.cDims; i++)
            ret += "[]";
          return ret;
        }
        if ((VarEnum)typeDesc.vt == VarEnum.VT_USERDEFINED)
        {
          // FIXME - sometimes this chokes and hangs due to a bad
          // handle value here, need to do something to prevent this
          int href = (int)typeDesc.lpValue;
          typeInfo.GetRefTypeInfo(href, out ITypeInfo uTypeInfo);
          if (uTypeInfo != null)
          {
            uTypeInfo.GetDocumentation(-1, out string docName,
                                                  out string docString,
                                                  out int helpContext,
                                                  out string helpFile);
            uTypeInfo.GetContainingTypeLib(out ITypeLib uTypeLib, out int uIndex);
            uTypeLib.GetDocumentation(-1, out string uDocName,
                                                  out string uDocString,
                                                  out int uHelpContext,
                                                  out string uHelpFile);
            LibraryName = uDocName;
            // Fix up misc references
            if (docName.Equals("GUID"))
              docName = "System.Guid";
            // Present the user names for the types in COM
            // mode, but for the CLR types, get the real
            // underlying names
            if (!comType)
              return typeLib.ResolveTypeDef(docName, comType);
            return docName;
          }
          else
          {
            Console.Error.WriteLine($@"USER: {typeLib.Name} 0x{href.ToString("X")} ***UNKNOWN***");
            return "(userDef unknown)";
          }
        }
        return TlbUtil.GetTypeString(typeLib, typeInfo, (int)typeDesc.vt, comType);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($@"ActiveX type conversion error: {ex}");
        return "(error)";
      }
    }
  }
}
