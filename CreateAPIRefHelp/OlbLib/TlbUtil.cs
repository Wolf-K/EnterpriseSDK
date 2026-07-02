using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using ELEMDESC = System.Runtime.InteropServices.ComTypes.ELEMDESC;
using FUNCDESC = System.Runtime.InteropServices.ComTypes.FUNCDESC;
using FUNCKIND = System.Runtime.InteropServices.ComTypes.FUNCKIND;
using IMPLTYPEFLAGS = System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS;
using INVOKEKIND = System.Runtime.InteropServices.ComTypes.INVOKEKIND;
using PARAMFLAG = System.Runtime.InteropServices.ComTypes.PARAMFLAG;
using TYPEATTR = System.Runtime.InteropServices.ComTypes.TYPEATTR;
using TYPEDESC = System.Runtime.InteropServices.ComTypes.TYPEDESC;
using TYPEKIND = System.Runtime.InteropServices.ComTypes.TYPEKIND;
using VARDESC = System.Runtime.InteropServices.ComTypes.VARDESC;

namespace OlbLib
{
  public static class TlbUtil
  {
    public const bool COMTYPE = true;

    internal class ComParamInfo
    {
      internal string Name;
      internal string Type;
      internal PARAMFLAG ParamFlags;

      internal ComParamInfo(string name, string typeName, PARAMFLAG paramFlags)
      {
        Name = name;
        Type = typeName;
        ParamFlags = paramFlags;
      }
    }


    internal static string GetAttrString(ITypeInfo typeInfo)
    {
      List<string> lstAttributes = new List<string>();
      //Get the TypeAttributes
      IntPtr typeDescPtr;
      typeInfo.GetTypeAttr(out typeDescPtr);
      try
      {
        var typeAttr = (System.Runtime.InteropServices.ComTypes.TYPEATTR)Marshal.PtrToStructure(typeDescPtr, typeof(System.Runtime.InteropServices.ComTypes.TYPEATTR));
        System.Runtime.InteropServices.ComTypes.TYPEFLAGS typeFlags = (System.Runtime.InteropServices.ComTypes.TYPEFLAGS)typeAttr.wTypeFlags;
        if ((typeFlags & System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FCANCREATE) == 0)
          lstAttributes.Add("request_edit");
        if ((typeFlags & System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FRESTRICTED) != 0)
          lstAttributes.Add("restricted");
      }
      finally
      {
        if (typeDescPtr != IntPtr.Zero)
        {
          typeInfo.ReleaseTypeAttr(typeDescPtr);
        }
      }
      return string.Join(",", lstAttributes);
    }

    public static Type GetManagedType(ITypeInfo typeInfo, string assemblyPath)
    {
      if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));
      if (string.IsNullOrWhiteSpace(assemblyPath)) throw new ArgumentException("Assembly path is required.", nameof(assemblyPath));

      var assembly = Assembly.LoadFrom(assemblyPath);
      return GetManagedType(typeInfo, assembly);
    }

    public static Type GetManagedType(ITypeInfo typeInfo, Assembly assembly)
    {
      if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));
      if (assembly == null) throw new ArgumentNullException(nameof(assembly));

      Guid typeGuid = Guid.Empty;
      IntPtr ptrTypeAttr = IntPtr.Zero;
      try
      {
        typeInfo.GetTypeAttr(out ptrTypeAttr);
        var typeAttr = (TYPEATTR)Marshal.PtrToStructure(ptrTypeAttr, typeof(TYPEATTR));
        typeGuid = typeAttr.guid;
      }
      finally
      {
        if (ptrTypeAttr != IntPtr.Zero)
        {
          typeInfo.ReleaseTypeAttr(ptrTypeAttr);
        }
      }

      if (typeGuid != Guid.Empty)
      {
        var typeByGuid = GetLoadableTypes(assembly).FirstOrDefault(t => t.GUID == typeGuid);
        if (typeByGuid != null)
        {
          return typeByGuid;
        }
      }

      typeInfo.GetDocumentation(-1, out string typeName, out string _, out int _, out string _);
      if (string.IsNullOrWhiteSpace(typeName))
      {
        return null;
      }

      var typeByName = GetLoadableTypes(assembly)
        .FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.Ordinal) ||
                             string.Equals(t.FullName, typeName, StringComparison.Ordinal));
      return typeByName;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
      try
      {
        return assembly.GetTypes();
      }
      catch (ReflectionTypeLoadException ex)
      {
        return ex.Types.Where(t => t != null);
      }
    }

    public static void GetMethods(TlbTypeLibInfo pTypeLib,
        Type ManagedType,
        int idx,
        string parentName,
        List<TlbMemberInfo> members)
    {
      ITypeInfo coclassOrInterfaceTypeInfo;
      pTypeLib._iTypeLib.GetTypeInfo(idx, out coclassOrInterfaceTypeInfo);

      //Get the TypeAttributes
      IntPtr ptrTypeAttr;
      try
      {
        coclassOrInterfaceTypeInfo.GetTypeAttr(out ptrTypeAttr);
      }
      catch (Exception ex)
      {
        coclassOrInterfaceTypeInfo.GetDocumentation(-1, out string sName, out string sDocString, out int dwHelpContext, out string sHelpFile);
        Console.Error.WriteLine($@"Error type lib {pTypeLib.Name} {parentName} {sName} has an error getting attribute type: {ex}");
        return;
      }
      try
      {
        var typeAttributes = (TYPEATTR)Marshal.PtrToStructure(ptrTypeAttr, typeof(TYPEATTR));
        var lastMemberName = string.Empty;
        TlbMemberInfo lastMember = null;

        //Lets get all the methods for this Type Info
        for (var idxMethod = 0; idxMethod < typeAttributes.cFuncs; idxMethod++)
        {
          IntPtr ptrFuncDesc;
          //Get the function description
          coclassOrInterfaceTypeInfo.GetFuncDesc(idxMethod, out ptrFuncDesc);
          var returnTypeName = string.Empty;
          try
          {
            var funcDesc = (FUNCDESC)Marshal.PtrToStructure(ptrFuncDesc, typeof(FUNCDESC));
            string memberName;
            string sNotUsed1;
            int dwNotUse2;
            string sNotUsed3;
            //Get the name of the method	
            coclassOrInterfaceTypeInfo.GetDocumentation(funcDesc.memid, out memberName, out sNotUsed1, out dwNotUse2, out sNotUsed3);
            int actLen;
            var memberNames = new string[100];
            coclassOrInterfaceTypeInfo.GetNames(funcDesc.memid, memberNames, memberNames.Length, out actLen);
            CodeMemberMethod meth = null;
            var lstParameters = new List<TlbParameterInfo>();
            var lstVcppParameters = new List<TlbParameterInfo>();
            var retType = funcDesc.elemdescFunc.tdesc;
            bool bFunc = funcDesc.invkind == INVOKEKIND.INVOKE_FUNC;
            if (bFunc)
            {
              if ((funcDesc.wFuncFlags & (short)System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FRESTRICTED) != 0
                  && funcDesc.funckind == FUNCKIND.FUNC_DISPATCH)
              {
                // skip restricted com methods
                continue;
              }
              meth = new CodeMemberMethod();
              //codeDom = meth;
              if (funcDesc.cParams > 0)
              {
                var parameters = AddParams(pTypeLib, coclassOrInterfaceTypeInfo, funcDesc,
                    memberNames, funcDesc.cParams);
                if (parameters.Count > 0)
                {
                  var limit = parameters.Count;
                  // If the last parameter is a retval and the
                  // function returns an HRESULT, then make
                  // the function return the last parameter
                  if ((VarEnum)funcDesc.elemdescFunc.tdesc.vt == VarEnum.VT_HRESULT)
                  {
                    ComParamInfo lastParam = (ComParamInfo)
                        parameters[parameters.Count - 1];
                    if ((lastParam.ParamFlags &
                         PARAMFLAG.PARAMFLAG_FRETVAL) != 0)
                    {
                      IntPtr elemPtr = funcDesc.lprgelemdescParam;
                      ELEMDESC elemDesc = new ELEMDESC();
                      // Point to the last one
                      elemPtr = new IntPtr(elemPtr.ToInt64() +
                                           ((parameters.Count - 1) *
                                            Marshal.SizeOf(elemDesc)));
                      elemDesc = (ELEMDESC)
                          Marshal.PtrToStructure(elemPtr,
                              typeof(ELEMDESC));
                      // Make the return type the last parameter's
                      retType = elemDesc.tdesc;
                      limit--;
                    }
                  }
                  // Only add up to the limit
                  // (may omit the last parameter)
                  AddDomParams(pTypeLib, coclassOrInterfaceTypeInfo, funcDesc, meth, parameters, limit, lstParameters);
                  // Vcpp needs all parameters
                  AddDomParams(pTypeLib, coclassOrInterfaceTypeInfo, funcDesc, meth, parameters, parameters.Count, lstVcppParameters);
                }
              }
              // HRESULT becomes void because its handled by the exception
              // mechanism, we just leave the return type null
              if ((VarEnum)retType.vt != VarEnum.VT_HRESULT)
              {
                var typeName = TlbUtil.TypedescToString(pTypeLib, coclassOrInterfaceTypeInfo, retType, !TlbUtil.COMTYPE);
                // Get rid of the ref since this is now a return type
                if (typeName.StartsWith("ref "))
                  typeName = typeName.Substring(4);
                //Console.Error.WriteLine($"CG - {methodName} return: {typeName}");
                meth.ReturnType = new CodeTypeReference(typeName);
                returnTypeName = typeName;
              }
            }
            else
            {
              if ((VarEnum)funcDesc.elemdescFunc.tdesc.vt == VarEnum.VT_HRESULT)
              {
                var parameters = AddParams(pTypeLib, coclassOrInterfaceTypeInfo, funcDesc, memberNames, funcDesc.cParams);
                if (parameters.Count > 0)
                {
                  var limit = parameters.Count;
                  // If the last parameter is a retval and the
                  // function returns an HRESULT, then make
                  // the function return the last parameter
                  if ((VarEnum)funcDesc.elemdescFunc.tdesc.vt == VarEnum.VT_HRESULT)
                  {
                    ComParamInfo lastParam = (ComParamInfo)
                        parameters[parameters.Count - 1];
                    if ((lastParam.ParamFlags &
                         PARAMFLAG.PARAMFLAG_FRETVAL) != 0)
                    {
                      IntPtr elemPtr = funcDesc.lprgelemdescParam;
                      ELEMDESC elemDesc = new ELEMDESC();
                      // Point to the last one
                      elemPtr = new IntPtr(elemPtr.ToInt64() +
                                           ((parameters.Count - 1) *
                                            Marshal.SizeOf(elemDesc)));
                      elemDesc = (ELEMDESC)
                          Marshal.PtrToStructure(elemPtr,
                              typeof(ELEMDESC));
                      // Make the return type the last parameter's
                      retType = elemDesc.tdesc;
                      //limit--;
                    }
                  }
                  AddDomParams(pTypeLib, coclassOrInterfaceTypeInfo, funcDesc, meth, parameters, limit, lstParameters);
                  AddDomParams(pTypeLib, coclassOrInterfaceTypeInfo, funcDesc, meth, parameters, limit, lstVcppParameters);
                }
              }
              // HRESULT becomes void because its handled by the exception
              // mechanism, we just leave the return type null
              if ((VarEnum)retType.vt != VarEnum.VT_HRESULT)
              {
                var typeName = TlbUtil.TypedescToString(pTypeLib, coclassOrInterfaceTypeInfo, retType, !TlbUtil.COMTYPE);
                // Get rid of the ref since this is now a return type
                if (typeName.StartsWith("ref "))
                  typeName = typeName.Substring(4);
                //Console.Error.WriteLine($"CG - {methodName} return: {typeName}");
                returnTypeName = typeName;
              }
              else returnTypeName = string.Empty;
            }
            if (memberName.Equals(lastMemberName))
            {
              lastMember.AlterInvokeKind = funcDesc.invkind;
              lastMember.AlterParameters = lstParameters;
              lastMember.AlterVcppParameters = lstVcppParameters;
            }
            else
            {
              lastMember = new TlbMemberInfo(pTypeLib, ManagedType, funcDesc, coclassOrInterfaceTypeInfo, idxMethod, returnTypeName, meth, lstParameters, lstVcppParameters, parentName);
              members.Add(lastMember);
            }
            lastMemberName = memberName;
          }
          finally
          {
            if (ptrFuncDesc != IntPtr.Zero)
            {
              //Release our function description stuff
              coclassOrInterfaceTypeInfo.ReleaseFuncDesc(ptrFuncDesc);
            }
          }
#if notused
                    IntPtr memberDescriptorPointer;
                    currentTypeInfo.GetFuncDesc(idxMethod, out memberDescriptorPointer);
                    var memberDescriptor = (FUNCDESC)Marshal.PtrToStructure(memberDescriptorPointer, typeof(FUNCDESC));

                    var memberNames = new string[255]; // member name at index 0; array contains parameter names too
                    int namesArrayLength;
                    currentTypeInfo.GetNames(memberDescriptor.memid, memberNames, 255, out namesArrayLength);

                    var memberName = memberNames[0];

                    var funcValueType = (VarEnum)memberDescriptor.elemdescFunc.tdesc.vt;
                    var memberDeclarationType = GetDeclarationType(memberDescriptor, funcValueType);

                    var asTypeName = string.Empty;
                    if (memberDeclarationType != DeclarationType.Procedure && !TypeNames.TryGetValue(funcValueType, out asTypeName))
                    {
                        asTypeName = funcValueType.ToString(); //TypeNames[VarEnum.VT_VARIANT];
                    }

                    var memberDeclaration = new Declaration(new QualifiedMemberName(typeQualifiedModuleName, memberName), moduleDeclaration, moduleDeclaration, asTypeName, false, false, Accessibility.Global, memberDeclarationType, null, Selection.Home);
                    yield return memberDeclaration;

                    var parameterCount = memberDescriptor.cParams - 1;
                    for (var paramIndex = 0; paramIndex < parameterCount; paramIndex++)
                    {
                        var paramName = memberNames[paramIndex + 1];

                        var paramPointer = new IntPtr(memberDescriptor.lprgelemdescParam.ToInt64() + Marshal.SizeOf(typeof(ELEMDESC)) * paramIndex);
                        var elementDesc = (ELEMDESC)Marshal.PtrToStructure(paramPointer, typeof(ELEMDESC));
                        var isOptional = elementDesc.desc.paramdesc.wParamFlags.HasFlag(PARAMFLAG.PARAMFLAG_FOPT);
                        var asParamTypeName = string.Empty;

                        var isByRef = false;
                        var isArray = false;
                        var paramDesc = elementDesc.tdesc;
                        var valueType = (VarEnum)paramDesc.vt;
                        if (valueType == VarEnum.VT_PTR || valueType == VarEnum.VT_BYREF)
                        {
                            //var paramTypeDesc = (TYPEDESC) Marshal.PtrToStructure(paramDesc.lpValue, typeof (TYPEDESC));
                            isByRef = true;
                            var paramValueType = (VarEnum)paramDesc.vt;
                            if (!TypeNames.TryGetValue(paramValueType, out asParamTypeName))
                            {
                                asParamTypeName = TypeNames[VarEnum.VT_VARIANT];
                            }
                            //var href = paramDesc.lpValue.ToInt32();
                            //ITypeInfo refTypeInfo;
                            //currentTypeInfo.GetRefTypeInfo(href, out refTypeInfo);

                            // todo: get type info?
                        }
                        if (valueType == VarEnum.VT_CARRAY || valueType == VarEnum.VT_ARRAY || valueType == VarEnum.VT_SAFEARRAY)
                        {
                            // todo: tell ParamArray arrays from normal arrays
                            isArray = true;
                        }

                        yield return new ParameterDeclaration(new QualifiedMemberName(typeQualifiedModuleName, paramName), memberDeclaration, asParamTypeName, isOptional, isByRef, isArray);
                    }
#endif
        }
      }
      finally
      {
        if (ptrTypeAttr != IntPtr.Zero)
        {
          coclassOrInterfaceTypeInfo.ReleaseTypeAttr(ptrTypeAttr);
        }
      }
      members.Sort((x, y) => string.Compare($@"{x.Name} {x.Index}", $@"{y.Name} {y.Index}"));
    }

    private static MemberInfo? GetManagedMember(
    ITypeInfo typeInfo,
    FUNCDESC funcDesc,
    Assembly assembly)
    {
      // Step 1: COM type -> managed Type
      Type? managedType = TlbUtil.GetManagedType(typeInfo, assembly);
      if (managedType == null) return null;

      int dispId = funcDesc.memid;

      // Step 2: member match by DispId first
      try
      {
        MethodInfo? method = managedType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .FirstOrDefault(m => m.GetCustomAttributes<DispIdAttribute>(true).Any(a => a.Value == dispId));

        if (method != null) return method;
      }
      catch { }
      try
      {
        PropertyInfo? prop = managedType
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .FirstOrDefault(p => p.GetCustomAttributes<DispIdAttribute>(true).Any(a => a.Value == dispId));

        if (prop != null) return prop;
      }
      catch { }

      // Fallback by COM name if no DispId attribute found
      typeInfo.GetDocumentation(dispId, out string name, out _, out _, out _);

      var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
      var parameterTypes = GetManagedParameterTypes(typeInfo, funcDesc, assembly);

      MethodInfo? methodBySignature = managedType.GetMethod(name,
                 flags,
                 binder: null,
                 types: parameterTypes,
                 modifiers: null);

      if (methodBySignature == null && TryGetManagedParameterTypesWithoutRetval(typeInfo, funcDesc, assembly, out Type[] nonRetvalTypes))
      {
        methodBySignature = managedType.GetMethod(name,
                   flags,
                   binder: null,
                   types: nonRetvalTypes,
                   modifiers: null);
      }

      return (MemberInfo?)methodBySignature
          ?? managedType.GetProperty(name, flags);
    }

    private static Type[] GetManagedParameterTypes(ITypeInfo typeInfo, FUNCDESC funcDesc, Assembly assembly)
    {
      if (funcDesc.cParams <= 0)
      {
        return Type.EmptyTypes;
      }

      var parameterTypes = new List<Type>(funcDesc.cParams);
      var elemSize = Marshal.SizeOf(typeof(ELEMDESC));
      for (int i = 0; i < funcDesc.cParams; i++)
      {
        var elemPtr = new IntPtr(funcDesc.lprgelemdescParam.ToInt64() + (i * elemSize));
        var elemDesc = (ELEMDESC)Marshal.PtrToStructure(elemPtr, typeof(ELEMDESC));
        parameterTypes.Add(GetManagedTypeFromTypeDesc(typeInfo, elemDesc.tdesc, assembly));
      }
      return parameterTypes.ToArray();
    }

    private static bool TryGetManagedParameterTypesWithoutRetval(ITypeInfo typeInfo, FUNCDESC funcDesc, Assembly assembly, out Type[] parameterTypes)
    {
      parameterTypes = Type.EmptyTypes;
      if (funcDesc.cParams <= 0 || (VarEnum)funcDesc.elemdescFunc.tdesc.vt != VarEnum.VT_HRESULT)
      {
        return false;
      }

      var elemSize = Marshal.SizeOf(typeof(ELEMDESC));
      var lastElemPtr = new IntPtr(funcDesc.lprgelemdescParam.ToInt64() + ((funcDesc.cParams - 1) * elemSize));
      var lastElemDesc = (ELEMDESC)Marshal.PtrToStructure(lastElemPtr, typeof(ELEMDESC));
      if ((lastElemDesc.desc.paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FRETVAL) == 0)
      {
        return false;
      }

      var types = new List<Type>(funcDesc.cParams - 1);
      for (int i = 0; i < funcDesc.cParams - 1; i++)
      {
        var elemPtr = new IntPtr(funcDesc.lprgelemdescParam.ToInt64() + (i * elemSize));
        var elemDesc = (ELEMDESC)Marshal.PtrToStructure(elemPtr, typeof(ELEMDESC));
        types.Add(GetManagedTypeFromTypeDesc(typeInfo, elemDesc.tdesc, assembly));
      }

      parameterTypes = types.ToArray();
      return true;
    }

    private static Type GetManagedTypeFromTypeDesc(ITypeInfo typeInfo, TYPEDESC typeDesc, Assembly assembly)
    {
      VarEnum vt = (VarEnum)typeDesc.vt;

      if (vt == VarEnum.VT_PTR || (typeDesc.vt & (short)VarEnum.VT_BYREF) != 0)
      {
        var innerDesc = (TYPEDESC)Marshal.PtrToStructure(typeDesc.lpValue, typeof(TYPEDESC));
        var innerType = GetManagedTypeFromTypeDesc(typeInfo, innerDesc, assembly);
        return innerType.IsByRef ? innerType : innerType.MakeByRefType();
      }

      if ((typeDesc.vt & (short)VarEnum.VT_ARRAY) != 0 ||
          (VarEnum)(typeDesc.vt & VT_TYPEMASK) == VarEnum.VT_SAFEARRAY ||
          (VarEnum)(typeDesc.vt & VT_TYPEMASK) == VarEnum.VT_CARRAY)
      {
        var innerDesc = (TYPEDESC)Marshal.PtrToStructure(typeDesc.lpValue, typeof(TYPEDESC));
        var innerType = GetManagedTypeFromTypeDesc(typeInfo, innerDesc, assembly);
        if (innerType.IsByRef)
        {
          innerType = innerType.GetElementType() ?? typeof(object);
        }
        return innerType.MakeArrayType();
      }

      if ((VarEnum)(typeDesc.vt & VT_TYPEMASK) == VarEnum.VT_USERDEFINED)
      {
        typeInfo.GetRefTypeInfo((int)typeDesc.lpValue, out ITypeInfo refTypeInfo);
        var userType = GetManagedType(refTypeInfo, assembly);
        if (userType != null)
        {
          return userType;
        }

        refTypeInfo.GetDocumentation(-1, out string refTypeName, out _, out _, out _);
        return assembly.GetType(refTypeName) ?? assembly.GetTypes().FirstOrDefault(t => t.Name == refTypeName) ?? typeof(object);
      }

      return (VarEnum)(typeDesc.vt & VT_TYPEMASK) switch
      {
        VarEnum.VT_EMPTY => typeof(IntPtr),
        VarEnum.VT_NULL => typeof(object),
        VarEnum.VT_I1 => typeof(sbyte),
        VarEnum.VT_UI1 => typeof(byte),
        VarEnum.VT_I2 => typeof(short),
        VarEnum.VT_UI2 => typeof(ushort),
        VarEnum.VT_I4 => typeof(int),
        VarEnum.VT_UI4 => typeof(uint),
        VarEnum.VT_I8 => typeof(long),
        VarEnum.VT_UI8 => typeof(ulong),
        VarEnum.VT_INT => typeof(int),
        VarEnum.VT_UINT => typeof(uint),
        VarEnum.VT_R4 => typeof(float),
        VarEnum.VT_R8 => typeof(double),
        VarEnum.VT_DECIMAL => typeof(decimal),
        VarEnum.VT_CY => typeof(decimal),
        VarEnum.VT_DATE => typeof(DateTime),
        VarEnum.VT_BOOL => typeof(bool),
        VarEnum.VT_BSTR => typeof(string),
        VarEnum.VT_LPSTR => typeof(string),
        VarEnum.VT_LPWSTR => typeof(string),
        VarEnum.VT_ERROR => typeof(int),
        VarEnum.VT_HRESULT => typeof(int),
        VarEnum.VT_DISPATCH => typeof(object),
        VarEnum.VT_UNKNOWN => typeof(object),
        VarEnum.VT_VARIANT => typeof(object),
        VarEnum.VT_VOID => typeof(void),
        VarEnum.VT_CLSID => typeof(Guid),
        _ => typeof(object)
      };
    }

    public static MemberInfo? FindManagedMemberInfo(ITypeInfo typeClassOrInterfaceInfo, Type managedClassOrInterfaceType, int memberId)
    {
      typeClassOrInterfaceInfo.GetTypeAttr(out var pAttr);
      try
      {
        var attr = (TYPEATTR)Marshal.PtrToStructure(pAttr, typeof(TYPEATTR));
        for (int i = 0; i < attr.cFuncs; i++)
        {
          typeClassOrInterfaceInfo.GetFuncDesc(i, out var pFunc);
          try
          {
            var funcDesc = (FUNCDESC)Marshal.PtrToStructure(pFunc, typeof(FUNCDESC));
            if (funcDesc.memid == memberId)
            {
              return GetManagedMember(typeClassOrInterfaceInfo, funcDesc, managedClassOrInterfaceType.Assembly);
            }
            //int dispId = funcDesc.memid;
            //if (dispId != memberId)
            //  continue;
            //List<Type> types = [];
            //// Loop through parameters
            //for (int p = 0; p < funcDesc.cParams; p++)
            //{
            //  ELEMDESC elemDesc = Marshal.PtrToStructure<ELEMDESC>(
            //      funcDesc.lprgelemdescParam + p * Marshal.SizeOf<ELEMDESC>()
            //  );
            //  // Get parameter type info
            //  types.Add(GetVarTypeName(elemDesc.tdesc));
            //}
            //typeClassOrInterfaceInfo.GetDocumentation(dispId, out var comName, out _, out _, out _);
            //var method = managedClassOrInterfaceType
            //    .GetMethods()
            //    .FirstOrDefault(m => m.GetCustomAttributes(typeof(DispIdAttribute), false)
            //        .Cast<DispIdAttribute>().Any(a => a.Value == dispId))
            //    ?? managedClassOrInterfaceType.GetMethod(comName, types.Count, BindingFlags.Instance | BindingFlags.Public, types.ToArray());
            //    if (method != null)
            //  return method.ReturnType;
            //var prop = managedClassOrInterfaceType
            //    .GetProperties()
            //    .FirstOrDefault(p => p.GetCustomAttributes(typeof(DispIdAttribute), false)
            //        .Cast<DispIdAttribute>().Any(a => a.Value == dispId))
            //    ?? managedClassOrInterfaceType.GetProperty(comName);
            //    if (prop != null)
            //  return prop.PropertyType;
            //// method/prop now points to the managed member match (if found)
          }
          finally { typeClassOrInterfaceInfo.ReleaseFuncDesc(pFunc); }
        }
      }
      finally { typeClassOrInterfaceInfo.ReleaseTypeAttr(pAttr); }
      return null;
    }

    // Helper: Convert TYPEDESC to readable type name
    //static Type GetVarTypeName(TYPEDESC tdesc)
    //{
    //  // Basic mapping for common VARTYPEs
    //  VarEnum vt = (VarEnum)tdesc.vt;
    //  switch (vt)
    //  {
    //    case VarEnum.VT_I4: return typeof(int);
    //    case VarEnum.VT_I2: return typeof(short);
    //    case VarEnum.VT_UI4: return typeof(uint);
    //    case VarEnum.VT_UI2: return typeof(ushort);
    //    case VarEnum.VT_R4: return typeof(float);
    //    case VarEnum.VT_R8: return typeof(double);
    //    case VarEnum.VT_BSTR: return typeof(string);
    //    case VarEnum.VT_BOOL: return typeof(bool);
    //    case VarEnum.VT_DATE: return typeof(DateTime);
    //    case VarEnum.VT_DECIMAL: return typeof(decimal);
    //    case VarEnum.VT_VARIANT: return typeof(object);

    //    case VarEnum.VT_PTR:
    //      // Pointer to another type
    //      var ptrDesc = Marshal.PtrToStructure<TYPEDESC>(typeDesc.lpValue);
    //      return ConvertTypeDesc(ptrDesc, typeInfo);

    //    case VarEnum.VT_SAFEARRAY:
    //      // SAFEARRAY of another type
    //      var saDesc = Marshal.PtrToStructure<TYPEDESC>(typeDesc.lpValue);
    //      return ConvertTypeDesc(saDesc, typeInfo).MakeArrayType();

    //    case VarEnum.VT_USERDEFINED:
    //      // User-defined type: get referenced type info
    //      int href = typeDesc.lpValue.ToInt32();
    //      typeInfo.GetRefTypeInfo(href, out ITypeInfo refTypeInfo);
    //      refTypeInfo.GetTypeAttr(out IntPtr pTypeAttr);
    //      try
    //      {
    //        var typeAttr = Marshal.PtrToStructure<TYPEATTR>(pTypeAttr);
    //        return Type.GetTypeFromCLSID(typeAttr.guid);
    //      }
    //      finally
    //      {
    //        refTypeInfo.ReleaseTypeAttr(pTypeAttr);
    //      }

    //    default:
    //      throw new NotSupportedException($"VarEnum {vt} not supported.");
    //  }
    //}

    public static void GetImplementedInterfaces(TlbTypeLibInfo pTypeLib,
        Type managedType,
        int idx,
        List<TlbImplementedInterface> implementedInterfaces)
    {
      pTypeLib._iTypeLib.GetTypeInfo(idx, out ITypeInfo currentTypeInfo);

      //Get the TypeAttributes
      IntPtr ptrTypeAttr;
      try
      {
        currentTypeInfo.GetTypeAttr(out ptrTypeAttr);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($@"Error type lib {pTypeLib.Name} {idx} has an error getting GetImplementedInterfaces: {ex}");
        return;
      }
      try
      {
        var typeAttributes =
            (System.Runtime.InteropServices.ComTypes.TYPEATTR)
                Marshal.PtrToStructure(ptrTypeAttr, typeof(System.Runtime.InteropServices.ComTypes.TYPEATTR));
        //Lets get all the interfaces for this Type Info
        for (var idxInterface = 0; idxInterface < typeAttributes.cImplTypes; idxInterface++)
        {
          try
          {
            //Get the function description
            currentTypeInfo.GetImplTypeFlags(idxInterface, out IMPLTYPEFLAGS impltypeflags);
            currentTypeInfo.GetRefTypeOfImplType(idxInterface, out int hrefImplType);
            currentTypeInfo.GetRefTypeInfo(hrefImplType, out ITypeInfo implTypeInfo);
            //Get the name of the method	
            implTypeInfo.GetDocumentation(-1, out string interfaceName,
                out string sNotUsed1, out int dwNotUse2, out string sNotUsed3);
            if (interfaceName.Equals("IUnknown") || interfaceName.Equals("IDispatch")) continue;
            if (interfaceName.StartsWith("_")) continue;
            implTypeInfo.GetContainingTypeLib(out ITypeLib pContainTypeLib, out int containTypeLibIndex);
            implementedInterfaces.Add(new TlbImplementedInterface(pContainTypeLib, containTypeLibIndex, impltypeflags));
          }
          catch (Exception ex)
          {
            Console.Error.WriteLine($@"*** Ignore Exception: {ex}");
          }
        }
      }
      finally
      {
        if (ptrTypeAttr != IntPtr.Zero)
        {
          currentTypeInfo.ReleaseTypeAttr(ptrTypeAttr);
        }
      }
      implementedInterfaces.Sort((x, y) => string.Compare($@"{x.Name} {x.IndexInTypeLib}", $@"{y.Name} {y.IndexInTypeLib}"));
    }

    public static ArrayList AddParams(TlbTypeLibInfo typeLib,
                                      ITypeInfo typeInfo,
                                      FUNCDESC funcDesc,
                                      string[] names,
                                      int paramCount)
    {
      IntPtr elemPtr = funcDesc.lprgelemdescParam;
      var parameters = new ArrayList();
      for (var i = 0; i < paramCount; i++)
      {
        ELEMDESC elemDesc = (ELEMDESC)Marshal.PtrToStructure(elemPtr, typeof(ELEMDESC));
        ComParamInfo pi = new ComParamInfo
            (names[i + 1],
             TlbUtil.TypedescToString
             (typeLib,
              typeInfo,
              elemDesc.tdesc,
              TlbUtil.COMTYPE),
             elemDesc.desc.paramdesc.wParamFlags);
        parameters.Add(pi);
        // Point to the next one
        elemPtr = new IntPtr(elemPtr.ToInt64() + Marshal.SizeOf(elemDesc));
      }
      return parameters;
    }

    public static string GetVisibleName(string strName)
    {
      /*
       *             NETtlbinf32.TypeLibInfo tlb4cc;
      int lTNum;
      var tli = TliApp;
      var guids = FindLibraries(strName);
      if (guids.Count <= 0) return strName;
      var sresult = strName;
      foreach (var guid in guids)
      {
          tlb4cc = tli.TypeLibInfoFromRegistry(GetAssemblyGuidFromName(guid), 1, 0, 0);
          lTNum = tlb4cc.GetTypeInfoNumber[strName];
          if (tlb4cc.GetTypeInfo[lTNum].TypeKind != NETtlbinf32.TypeKinds.TKIND_COCLASS)
              return strName;
          if (tlb4cc.GetTypeInfo[lTNum].TypeKind == NETtlbinf32.TypeKinds.TKIND_COCLASS)
              sresult = strName + "Class";
      }
      return sresult;
      */
      return strName;
    }

    /*
    public static readonly IDictionary<VarEnum, string> TypeNames = new Dictionary<VarEnum, string>
    {
        {VarEnum.VT_DISPATCH, "DISPATCH"},
        {VarEnum.VT_VOID, string.Empty},
        {VarEnum.VT_VARIANT, "Variant"},
        {VarEnum.VT_BLOB_OBJECT, "Object"},
        {VarEnum.VT_STORED_OBJECT, "Object"},
        {VarEnum.VT_STREAMED_OBJECT, "Object"},
        {VarEnum.VT_BOOL, "Boolean"},
        {VarEnum.VT_BSTR, "String"},
        {VarEnum.VT_LPSTR, "String"},
        {VarEnum.VT_LPWSTR, "String"},
        {VarEnum.VT_I1, "Variant"}, // no signed byte type in VBA
        {VarEnum.VT_UI1, "Byte"},
        {VarEnum.VT_I2, "Integer"},
        {VarEnum.VT_UI2, "Variant"}, // no unsigned integer type in VBA
        {VarEnum.VT_I4, "Long"},
        {VarEnum.VT_UI4, "Variant"}, // no unsigned long integer type in VBA
        {VarEnum.VT_I8, "Variant"}, // LongLong on 64-bit VBA
        {VarEnum.VT_UI8, "Variant"}, // no unsigned LongLong integer type in VBA
        {VarEnum.VT_INT, "Long"}, // same as I4
        {VarEnum.VT_UINT, "Variant"}, // same as UI4
        {VarEnum.VT_DATE, "Date"},
        {VarEnum.VT_DECIMAL, "Currency"}, // best match?
        {VarEnum.VT_EMPTY, "Empty"},
        {VarEnum.VT_R4, "Single"},
        {VarEnum.VT_R8, "Double"},
    };

    public static string GetTypeName(ITypeInfo info)
    {
        string typeName;
        string docString; // todo: put the docString to good use?
        int helpContext;
        string helpFile;
        info.GetDocumentation(-1, out typeName, out docString, out helpContext, out helpFile);

        return typeName;
    }

    public IEnumerable<Declaration> GetDeclarationsForReference(Reference reference)
    {
        var projectName = reference.Name;
        var path = reference.FullPath;

        var projectQualifiedModuleName = new QualifiedModuleName(projectName, projectName);
        var projectQualifiedMemberName = new QualifiedMemberName(projectQualifiedModuleName, projectName);

        var projectDeclaration = new Declaration(projectQualifiedMemberName, null, null, projectName, false, false, Accessibility.Global, DeclarationType.Project);
        yield return projectDeclaration;

        ITypeLib typeLibrary;
        LoadTypeLibEx(path, REGKIND.REGKIND_NONE, out typeLibrary);

        var typeCount = typeLibrary.GetTypeInfoCount();
        for (var i = 0; i < typeCount; i++)
        {
            ITypeInfo info;
            typeLibrary.GetTypeInfo(i, out info);

            if (info == null)
            {
                continue;
            }

            var typeName = GetTypeName(info);
            var typeDeclarationType = GetDeclarationType(typeLibrary, i);

            QualifiedModuleName typeQualifiedModuleName;
            QualifiedMemberName typeQualifiedMemberName;
            if (typeDeclarationType == DeclarationType.Enumeration ||
                typeDeclarationType == DeclarationType.UserDefinedType)
            {
                typeQualifiedModuleName = projectQualifiedModuleName;
                typeQualifiedMemberName = new QualifiedMemberName(projectQualifiedModuleName, typeName);
            }
            else
            {
                typeQualifiedModuleName = new QualifiedModuleName(projectName, typeName);
                typeQualifiedMemberName = new QualifiedMemberName(typeQualifiedModuleName, typeName);
            }

            var moduleDeclaration = new Declaration(typeQualifiedMemberName, projectDeclaration, projectDeclaration, typeName, false, false, Accessibility.Global, typeDeclarationType, null, Selection.Home);
            yield return moduleDeclaration;

            IntPtr typeAttributesPointer;
            info.GetTypeAttr(out typeAttributesPointer);

            var typeAttributes = (TYPEATTR)Marshal.PtrToStructure(typeAttributesPointer, typeof(TYPEATTR));
            //var implements = GetImplementedInterfaceNames(typeAttributes, info);

            for (var memberIndex = 0; memberIndex < typeAttributes.cFuncs; memberIndex++)
            {
                IntPtr memberDescriptorPointer;
                info.GetFuncDesc(memberIndex, out memberDescriptorPointer);
                var memberDescriptor = (FUNCDESC)Marshal.PtrToStructure(memberDescriptorPointer, typeof(FUNCDESC));

                var memberNames = new string[255]; // member name at index 0; array contains parameter names too
                int namesArrayLength;
                info.GetNames(memberDescriptor.memid, memberNames, 255, out namesArrayLength);

                var memberName = memberNames[0];

                var funcValueType = (VarEnum)memberDescriptor.elemdescFunc.tdesc.vt;
                var memberDeclarationType = GetDeclarationType(memberDescriptor, funcValueType);

                var asTypeName = string.Empty;
                if (memberDeclarationType != DeclarationType.Procedure && !TypeNames.TryGetValue(funcValueType, out asTypeName))
                {
                    asTypeName = funcValueType.ToString(); //TypeNames[VarEnum.VT_VARIANT];
                }

                var memberDeclaration = new Declaration(new QualifiedMemberName(typeQualifiedModuleName, memberName), moduleDeclaration, moduleDeclaration, asTypeName, false, false, Accessibility.Global, memberDeclarationType, null, Selection.Home);
                yield return memberDeclaration;

                var parameterCount = memberDescriptor.cParams - 1;
                for (var paramIndex = 0; paramIndex < parameterCount; paramIndex++)
                {
                    var paramName = memberNames[paramIndex + 1];

                    var paramPointer = new IntPtr(memberDescriptor.lprgelemdescParam.ToInt64() + Marshal.SizeOf(typeof(ELEMDESC)) * paramIndex);
                    var elementDesc = (ELEMDESC)Marshal.PtrToStructure(paramPointer, typeof(ELEMDESC));
                    var isOptional = elementDesc.desc.paramdesc.wParamFlags.HasFlag(PARAMFLAG.PARAMFLAG_FOPT);
                    var asParamTypeName = string.Empty;

                    var isByRef = false;
                    var isArray = false;
                    var paramDesc = elementDesc.tdesc;
                    var valueType = (VarEnum)paramDesc.vt;
                    if (valueType == VarEnum.VT_PTR || valueType == VarEnum.VT_BYREF)
                    {
                        //var paramTypeDesc = (TYPEDESC) Marshal.PtrToStructure(paramDesc.lpValue, typeof (TYPEDESC));
                        isByRef = true;
                        var paramValueType = (VarEnum)paramDesc.vt;
                        if (!TypeNames.TryGetValue(paramValueType, out asParamTypeName))
                        {
                            asParamTypeName = TypeNames[VarEnum.VT_VARIANT];
                        }
                        //var href = paramDesc.lpValue.ToInt32();
                        //ITypeInfo refTypeInfo;
                        //info.GetRefTypeInfo(href, out refTypeInfo);

                        // todo: get type info?
                    }
                    if (valueType == VarEnum.VT_CARRAY || valueType == VarEnum.VT_ARRAY || valueType == VarEnum.VT_SAFEARRAY)
                    {
                        // todo: tell ParamArray arrays from normal arrays
                        isArray = true;
                    }

                    yield return new ParameterDeclaration(new QualifiedMemberName(typeQualifiedModuleName, paramName), memberDeclaration, asParamTypeName, isOptional, isByRef, isArray);
                }
            }

            for (var fieldIndex = 0; fieldIndex < typeAttributes.cVars; fieldIndex++)
            {
                IntPtr ppVarDesc;
                info.GetVarDesc(fieldIndex, out ppVarDesc);

                var varDesc = (VARDESC)Marshal.PtrToStructure(ppVarDesc, typeof(VARDESC));

                var names = new string[255];
                int namesArrayLength;
                info.GetNames(varDesc.memid, names, 255, out namesArrayLength);

                var fieldName = names[0];
                var fieldValueType = (VarEnum)varDesc.elemdescVar.tdesc.vt;
                var memberType = GetDeclarationType(varDesc, typeDeclarationType);

                string asTypeName;
                if (!TypeNames.TryGetValue(fieldValueType, out asTypeName))
                {
                    asTypeName = TypeNames[VarEnum.VT_VARIANT];
                }

                yield return new Declaration(new QualifiedMemberName(typeQualifiedModuleName, fieldName), moduleDeclaration, moduleDeclaration, asTypeName, false, false, Accessibility.Global, memberType, null, Selection.Home);
            }
        }
    }

    //private IEnumerable<string> GetImplementedInterfaceNames(TYPEATTR typeAttr, ITypeInfo info)
    //{
    //    for (var implIndex = 0; implIndex < typeAttr.cImplTypes; implIndex++)
    //    {
    //        int href;
    //        info.GetRefTypeOfImplType(implIndex, out href);

    //        ITypeInfo implTypeInfo;
    //        info.GetRefTypeInfo(href, out implTypeInfo);

    //        var implTypeName = GetTypeName(implTypeInfo);

    //        yield return implTypeName;
    //        //Debug.WriteLine(string.Format("\tImplements {0}", implTypeName));
    //    }
    //}

    private DeclarationType GetDeclarationType(ITypeLib typeLibrary, int i)
    {
        TYPEKIND typeKind;
        typeLibrary.GetTypeInfoType(i, out typeKind);

        DeclarationType typeDeclarationType = DeclarationType.Control; // todo: a better default
        if (typeKind == TYPEKIND.TKIND_ENUM)
        {
            typeDeclarationType = DeclarationType.Enumeration;
        }
        else if (typeKind == TYPEKIND.TKIND_COCLASS || typeKind == TYPEKIND.TKIND_INTERFACE ||
                 typeKind == TYPEKIND.TKIND_ALIAS || typeKind == TYPEKIND.TKIND_DISPATCH)
        {
            typeDeclarationType = DeclarationType.Class;
        }
        else if (typeKind == TYPEKIND.TKIND_RECORD)
        {
            typeDeclarationType = DeclarationType.UserDefinedType;
        }
        else if (typeKind == TYPEKIND.TKIND_MODULE)
        {
            typeDeclarationType = DeclarationType.Module;
        }
        return typeDeclarationType;
    }

    private DeclarationType GetDeclarationType(FUNCDESC funcDesc, VarEnum funcValueType)
    {
        DeclarationType memberType;
        if (funcDesc.invkind.HasFlag(INVOKEKIND.INVOKE_PROPERTYGET))
        {
            memberType = DeclarationType.PropertyGet;
        }
        else if (funcDesc.invkind.HasFlag(INVOKEKIND.INVOKE_PROPERTYPUT))
        {
            memberType = DeclarationType.PropertyLet;
        }
        else if (funcDesc.invkind.HasFlag(INVOKEKIND.INVOKE_PROPERTYPUTREF))
        {
            memberType = DeclarationType.PropertySet;
        }
        else if (funcValueType == VarEnum.VT_VOID)
        {
            memberType = DeclarationType.Procedure;
        }
        else if (funcDesc.funckind == FUNCKIND.FUNC_PUREVIRTUAL)
        {
            memberType = DeclarationType.Event;
        }
        else
        {
            memberType = DeclarationType.Function;
        }
        return memberType;
    }

    private DeclarationType GetDeclarationType(VARDESC varDesc, DeclarationType typeDeclarationType)
    {
        var memberType = DeclarationType.Variable;
        if (varDesc.varkind == VARKIND.VAR_CONST)
        {
            memberType = typeDeclarationType == DeclarationType.Enumeration
                ? DeclarationType.EnumerationMember
                : DeclarationType.Constant;
        }
        else if (typeDeclarationType == DeclarationType.UserDefinedType)
        {
            memberType = DeclarationType.UserDefinedTypeMember;
        }
        return memberType;
    }
    */



    // The TypeLibrary is used to resolve any typedefs
    public static string TypedescToString(TlbTypeLibInfo typeLib,
                                          ITypeInfo typeInfo,
                                          TYPEDESC typeDesc,
                                          bool comType)
    {
      string ret = TypedescToStringInt(typeLib, typeInfo, typeDesc, comType, 0);
      return ret;
    }

    public const int VT_TYPEMASK = 0xfff;

    public static string TypedescToStringInt(TlbTypeLibInfo typeLib,
                                             ITypeInfo typeInfo,
                                             TYPEDESC typeDesc,
                                             bool comType,
                                             int level)
    {
      string ret;
      try
      {
        if ((VarEnum)typeDesc.vt == VarEnum.VT_PTR ||
            (VarEnum)(typeDesc.vt & VT_TYPEMASK) ==
            VarEnum.VT_SAFEARRAY)
        {
          var pTypeDesc = (TYPEDESC)Marshal.PtrToStructure(typeDesc.lpValue, typeof(TYPEDESC));
          ret = TypedescToStringInt(typeLib,
                                    typeInfo, pTypeDesc,
                                    comType, level + 1);
          if ((VarEnum)(typeDesc.vt & VT_TYPEMASK) ==
              VarEnum.VT_SAFEARRAY)
          {
            // FIXME - what about the non-comType
            return "SAFEARRAY(" + ret + ")";
          }
          if (comType)
          {
            ret += "*";
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
        if ((VarEnum)(typeDesc.vt & VT_TYPEMASK) ==
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
          for (int i = 0; i < pArrayDesc.cDims; i++) ret += "[]";
          return ret;
        }
        if ((VarEnum)typeDesc.vt == VarEnum.VT_USERDEFINED)
        {
          ITypeInfo uTypeInfo = null;
          // FIXME - sometimes this chokes and hangs due to a bad
          // handle value here, need to do something to prevent this
          int href = (int)typeDesc.lpValue;
          typeInfo.GetRefTypeInfo(href, out uTypeInfo);
          if (uTypeInfo != null)
          {
            string docName;
            string docString;
            int helpContext;
            string helpFile;
            uTypeInfo.GetDocumentation(-1, out docName,
                                      out docString,
                                      out helpContext,
                                      out helpFile);
            // Fix up misc references
            if (docName.Equals("GUID")) docName = "System.Guid";
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
        return GetTypeString(typeLib, typeInfo, (int)typeDesc.vt, comType);
      }
      catch (Exception ex)
      {
        typeInfo.GetDocumentation(-1, out string sName, out string sDocString, out int dwHelpContext, out string sHelpFile);
        Console.Error.WriteLine($@"Error type conversion error {typeLib.Name} {sName}: {ex}");
        return "(error)";
      }
    }

    private static void AddDomParams(TlbTypeLibInfo typeLib,
                                     ITypeInfo typeInfo,
                                     FUNCDESC funcDesc,
                                     CodeMemberMethod meth,
                                     ArrayList parameters,
                                     int limit,
                                     List<TlbParameterInfo> lstParameters)
    {
      IntPtr elemPtr = funcDesc.lprgelemdescParam;
      for (var index = 0; index < limit; index++)
      {
        var elemDesc = (ELEMDESC)Marshal.PtrToStructure(elemPtr, typeof(ELEMDESC));
        var parameter = (ComParamInfo)parameters[index];
        var paramType = TlbUtil.TypedescToString(typeLib,
             typeInfo,
             elemDesc.tdesc,
             !TlbUtil.COMTYPE);
        string paramInOut = null;
        if ((parameter.ParamFlags & PARAMFLAG.PARAMFLAG_FIN) != 0)
        {
          paramInOut = "In";
        }
        else if
            ((parameter.ParamFlags & PARAMFLAG.PARAMFLAG_FOUT) != 0)
        {
          paramInOut = "Out";
          // Ref becomes out for an output parameter
          if (paramType.StartsWith("ref "))
            paramType = "out " + paramType.Substring(4);
          else
            paramType = "out " + paramType;
        }
        CodeParameterDeclarationExpression paramExpr =
            new CodeParameterDeclarationExpression
                (paramType, parameter.Name);
        if (paramInOut != null)
        {
          paramExpr.CustomAttributes.Add
              (new CodeAttributeDeclaration
                  ("System.Runtime.InteropServices." + paramInOut));
        }
        lstParameters.Add(new TlbParameterInfo(typeLib,
                                  typeInfo, index, parameter.Name, "", paramType + " " + paramInOut,
                                  parameter.ParamFlags,
                                  elemDesc.tdesc));

        if (meth != null) meth.Parameters.Add(paramExpr);
        // Point to the next one
        elemPtr = new IntPtr(elemPtr.ToInt64() + Marshal.SizeOf(elemDesc));
      }
    }

    public static string ModifyLinkForVC(string sLink)
    {
      string sPfx = "";
      var sInterfaceName = "";
      string sMemberName = "";
      string sHRef = "";
      try
      {
        if (sLink.Contains("ctrl_") || sLink.Contains("publ_"))
        {
          if (sLink.IndexOf('_', 5) >= 0) return sLink;
          sInterfaceName = ExtractMid(sLink, "_", "_");
        }
        else if (sLink.Contains("_"))
          sInterfaceName = ExtractMid(sLink, @"""", "_");
        // Wolf: 12/03/2015: fixed exception thrown: target has no quotes, i.e. target=_blank>
        if (!(sLink.IndexOf("target=_blank", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
            sLink.IndexOf(@"target=""_blank""", StringComparison.CurrentCultureIgnoreCase) >= 0))
        {
          if (sLink.Contains("."))
            sMemberName = ExtractMid(sLink, "_", ".");
          else
            sMemberName = ExtractMid(sLink, "_", @""">");
        }
        if (string.IsNullOrEmpty(sInterfaceName) || string.IsNullOrEmpty(sMemberName)) return sLink;

        TlbMemberInfo member = null;
        foreach (var key in TlbLibCollection.Singleton.Libraries.Keys)
        {
          foreach (var intInfo in TlbLibCollection.Singleton.Libraries[key].InterfaceInfos)
          {
            if (intInfo.Name.Equals(sInterfaceName))
            {
              foreach (var mem in intInfo.Members)
              {
                if (mem.Name.Equals(sMemberName))
                {
                  member = mem;
                  break;
                }
              }
            }
            if (member != null) break;
          }
          if (member != null) break;
        }
        if (member == null) return sLink;
        switch ((short)member.InvokeKinds)
        {
          case 2:
          case 6:
          case 10:
          case 14:
            sPfx = "get_";
            break;
          case 4:
          case 12:
            sPfx = "put_";
            break;
          case 8:
            sPfx = "putref_";
            break;
          default:
            sPfx = "";
            break;
        }
        sHRef = ExtractMid(sLink, "<", ">");
        sPfx = sHRef.Replace("_" + sMemberName, "_" + sPfx + sMemberName);
      }
      catch (Exception ex)
      {

        Console.Error.WriteLine($"ModifyLinkForVC Error: {ex}");
      }
      return sLink.Replace(sHRef, sPfx);
    }

    private static string ExtractMid(string target, string token1, string token2)
    {
      var pos1 = target.IndexOf(token1);
      var pos2 = target.IndexOf(token2, pos1 + 1);
      return pos2 < 0 ? target.Substring(pos1) : target.Substring(pos1 + 1, pos2 - pos1 - 1);
    }


    public static string GetVCPPVarType(int iVarType, TlbVarTypeInfo vt, string typeName)
    {
      var v = "";
      var shortType = typeName.Replace("ref ", string.Empty);
      try
      {
        switch (iVarType)
        {
          case 0:
            if (vt.TypeInfo.TypeKind == TYPEKIND.TKIND_ENUM)
              v = GetEnumAlias2(vt.TypeInfo.Name);
            //v = GetEnumAlias2(vt.TypeInfo.Parent, vt.TypeInfo.Parent.Constants.IndexedItem[vt.TypeInfoNumber]);
            else
              v = vt.TypeInfo.Name;  //& strPointer
            break;
          case 1:
            v = "NULL";
            break;
          case 2:
            v = "short"; //VT_I2
            break;
          case 3:
            v = "long"; //VT_I4
            break;
          case 4:
            v = "float"; //VT_R4
            break;
          case 5:
            v = "double"; //VT_R8
            break;
          case 6:
            v = "CY"; //
            break;
          case 7:
            v = "DATE"; //
            break;
          case 8:
            v = "BSTR"; //
            break;
          case 9:
            v = "IDispatch*"; //
            break;
          case 10:
            v = "SCODE"; //
            break;
          case 11:
            v = "VARIANT_BOOL"; //
            break;
          case 12:
            v = "VARIANT"; //
            break;
          case 13:
            v = "LPUNKNOWN"; //
            break;
          case 14:
            v = "Decimal"; //
            break;
          case 16:
            v = "Signed char "; //VT_I1
            break;
          case 17:
            v = "Byte"; //VT_UI1
            break;
          case 18:
            v = "Unsigned short"; //VT_UI2
            break;
          case 19:
            v = "Unsigned long"; //VT_UI4
            break;
          case 20:
            v = "Signed 64-bit int"; //VT_I8
            break;
          case 21:
            v = "Unsigned 64-bit int"; //VT_UI8
            break;
          case 22:
            v = "Long"; //VT_INT
            break;
          case 23:
            v = "Unsigned machine int"; //VT_UINT
            break;
          case 24:
            v = ""; //VT_VOID
            break;
          case 25:
            v = ""; //VT_HRESULT
            break;
          case 26:
            v = GetVcppType(shortType); //VT_PTR
            break;
          case 27:
            v = "SafeArray"; //
            break;
          case 28:
            v = "CArray"; //
            break;
          case 29:
            v = GetVcppType(shortType); //
            break;
          case 30:
            v = "LPSTR"; //
            break;
          case 31:
            v = "LPWSTR"; //
            break;
          case 53:
            v = "Unsigned long"; //VT_UI4
            break;
          case 64:
            v = "Filetime"; //
            break;
          case 65:
            v = "Blob"; //
            break;
          case 66:
            v = "Stream"; //
            break;
          case 67:
            v = "Storage"; //
            break;
          case 68:
            v = "Streamed object"; //
            break;
          case 69:
            v = "Stored object"; //
            break;
          case 70:
            v = "Blob object"; //
            break;
          case 71:
            v = "CF"; //
            break;
          case 72:
            v = "CLSID"; //
            break;
          case 92:
            v = "LPWSTR"; //
            break;
          case 4096:
            v = ""; //VT_VECTOR
            break;
          case 8192:
            v = "Array"; //
            break;
          case 16384:
            v = "BYREF"; //
            break;
          case 32768:
            v = "Reserved"; //
            break;
        }
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Error: {ex.ToString()}");
      }
      return v;
    }

    private static Dictionary<string, string> _mapVppTypeStrings = new Dictionary<string, string>()
    {
        {"Variant"   ,"VARIANT_VARIANT"          },
        {"Object"    ,"VARIANT_BLOB_OBJECT"      },
        {"Boolean"   ,"VARIANT_BOOL"             },
        {"String"    ,"VARIANT_BSTR"             },
        {"Byte"      ,"VARIANT_UI1"              },
        {"Integer"   ,"VARIANT_I2"               },
        {"Long"      ,"VARIANT_INT"              },
        {"Date"      ,"VARIANT_DATE"             },
        {"Currency"  ,"VARIANT_DECIMAL"          },
        {"Single"    ,"VARIANT_R4"               },
        {"Double"    ,"VARIANT_R8"               }
    };

    public static StringBuilder SbTypes = new StringBuilder();

    private static string GetVcppType(string sVbType)
    {
      if (_mapVppTypeStrings.ContainsKey(sVbType)) return _mapVppTypeStrings[sVbType];
      
      foreach (var key in MapVarEnumToString.Keys)
      {
        if (MapVarEnumToString[key].Item1.Equals(sVbType))
        {
          SbTypes.AppendLine(sVbType);
          return MapVarEnumToString[key].Item2;
        }
      }
      return sVbType;
    }


    public static string GetEnumAlias2(string name)
    {
      return name;
    }

#if Old
      public static string GetEnumAlias2(NETtlbinf32.TypeLibInfo t, NETtlbinf32.ConstantInfo cx)
        {
            foreach (NETtlbinf32.ConstantInfo c2 in t.Constants)
            {
                if (c2.TypeKind == NETtlbinf32.TypeKinds.TKIND_ALIAS)
                {
                    if (cx.Name == c2.ResolvedType.TypeInfo.Name)
                    {
                        return c2.Name;
                    }
                }
            }
            return cx.Name;
        }
#endif

    public static string GetVBVarType(int iVarType, TlbVarTypeInfo vt, string typeName, bool bIncludeAs)
    {
      var v = "";
      var shortType = typeName.Replace("ref ", string.Empty);
      switch (iVarType)
      {
        case 0:
          if (vt.TypeInfo.TypeKind == TYPEKIND.TKIND_ENUM)
            v = GetEnumAlias2(vt.TypeInfo.Name);
          //v = GetEnumAlias2(vt.TypeInfo.Parent, vt.TypeInfo.Parent.Constants.IndexedItem[vt.TypeInfoNumber]);
          else
            v = vt.TypeInfo.Name; // '& strPointer
          break;
        case 1:
          v = "Null";
          break;
        case 2:
          v = "Integer";
          break;
        case 3:
          v = "Long";
          break;
        case 4:
          v = "Single";
          break;
        case 5:
          v = "Double";
          break;
        case 6:
          v = "Currency";
          break;
        case 7:
          v = "Date";
          break;
        case 8:
          v = "String";
          break;
        case 9:
          v = "Object";
          break;
        case 10:
          v = "Error";
          break;
        case 11:
          v = "Boolean";
          break;
        case 12:
          v = "Variant";
          break;
        case 13:
          v = "IUnknown Pointer";
          break;
        case 16:
          v = "Signed char";
          break;
        case 17:
          v = "Byte";
          break;
        case 18:
          v = "Unsigned short";
          break;
        case 19:
          v = "Unsigned long";
          break;
        case 20:
          v = "Signed 64-bit int";
          break;
        case 21:
          v = "Unsigned 64-bit int";
          break;
        case 22:
          v = "Long";
          break;
        case 23:
          v = "Unsigned machine int";
          break;
        case 24:
          v = "";
          bIncludeAs = false;
          //VT_VOID;
          break;
        case 53:
          v = "Unsigned long";
          break;
        case 25:
          v = "";
          bIncludeAs = false; // VT_HRESULT;
          break;
        case 26:
          v = shortType;
          break;
        case 27:
          v = "SafeArray";
          break;
        case 28:
          v = "CArray";
          break;
        case 29:
          v = shortType;
          break;
        case 30:
          v = "LPSTR";
          break;
        case 31:
          v = "LPWSTR";
          break;
        case 65:
          v = "Blob";
          break;
        case 70:
          v = "Blob object";
          break;
        case 92:
          v = "LPWSTR";
          break;
        case 8192:
          v = "Array";
          break;
      }
      if (bIncludeAs) v = " As " + v;
      return v;
    }

    public static string TransFlag(PARAMFLAG paramFlag, TlbParameterInfo p)
    {
      string transFlag = "";
      switch ((short)paramFlag)
      {
        case 0:
          transFlag = "";
          break;
        case 1:
          transFlag = "[in]";
          break;
        case 2:
          transFlag = "[out]";
          break;
        case 3:
          transFlag = "[in, out]";
          break;
        case 4:
          transFlag = "[lcid]";
          break;
        case 8:
          transFlag = "[retval]";
          break;
        case 10:
          transFlag = "[out, retval]";
          break;
        case 16:
          transFlag = "[optional]";
          break;
        case 17:
          transFlag = "[in, optional]";
          break;
        case 18:
          transFlag = "[out, optional]";
          break;
        case 32:
          {
            string v = p.Default ? p.DefaultValue.ToString() : string.Empty;
            transFlag = "[defaultvalue(" + v + ")]";
          }
          break;
        case 33:
          {
            string v = p.Default ? p.DefaultValue.ToString() : string.Empty;
            transFlag = "[in, defaultvalue(" + v + ")]";
          }
          break;
        case 49:
          {
            string v = p.Default ? p.DefaultValue.ToString() : string.Empty;
            transFlag = "[in, optional, defaultvalue(" + v + ")]";
          }
          break;
      }
      return transFlag;
    }

    public static string TransFlagVC(PARAMFLAG paramFlag, TlbParameterInfo p)
    {
      string transFlagVC = "";
      switch ((short)paramFlag)
      {
        case 0:
          transFlagVC = "";
          break;
        case 1:
          transFlagVC = "[in]";
          break;
        case 2:
          transFlagVC = "[out]";
          break;
        case 3:
          transFlagVC = "[in, out]";
          break;
        case 4:
          transFlagVC = "[lcid]";
          break;
        case 8:
          transFlagVC = "[retval]";
          break;
        case 10:
          transFlagVC = "[out, retval]";
          break;
        case 16:
          transFlagVC = "[optional]";
          break;
        case 17:
          transFlagVC = "[in, optional]";
          break;
        case 18:
          transFlagVC = "[out, optional]";
          break;
        case 32:
          {
            string v = p.Default ? p.DefaultValue.ToString() : string.Empty;
            if (v == @"True" || v == @"False")
              v = "VARIANT_" + v.ToUpper();
            transFlagVC = "[defaultvalue(" + v + ")]";
          }
          break;
        case 33:
          {
            string v = p.Default ? p.DefaultValue.ToString() : string.Empty;
            if (v == @"True" || v == @"False")
              v = "VARIANT_" + v.ToUpper();
            transFlagVC = "[in, defaultvalue(" + v + ")]";
          }
          break;
        case 49:
          {
            string v = p.Default ? p.DefaultValue.ToString() : string.Empty;
            if (v == @"True" || v == @"False")
              v = "VARIANT_" + v.ToUpper();
            transFlagVC = "[in, optional, defaultvalue(" + v + ")]";
          }
          break;
      }
      return transFlagVC;
    }

    public static string GetTypeString(TlbTypeLibInfo typeLib, ITypeInfo typeInfo, int iVarEnum, bool isCom)
    {
      if (!Enum.IsDefined(typeof(VarEnum), iVarEnum))
      {
        if (MapIVarEnumToString.ContainsKey(iVarEnum))
        {
          return isCom ? MapIVarEnumToString[iVarEnum].Item1 : MapIVarEnumToString[iVarEnum].Item2;
        }
        string docName;
        string docString;
        int helpContext;
        string helpFile;
        typeInfo.GetDocumentation(-1, out docName,
                                  out docString,
                                  out helpContext,
                                  out helpFile);
        Console.Error.WriteLine($@"Error {typeLib.Name} {docName} has an invalid value for typeDesc.vt of {iVarEnum}.");
        return $@"vt={iVarEnum}";
      }
      VarEnum varEnum = (VarEnum)iVarEnum;
      return isCom ? MapVarEnumToString[varEnum].Item1 : MapVarEnumToString[varEnum].Item2;
    }

    public static Dictionary<int, Tuple<string, string>> MapIVarEnumToString = new Dictionary<int, Tuple<string, string>>()
        {
            { 53, new Tuple<string, string>("unsigned long", "System.UInt32" )},
            { 92, new Tuple<string, string>("LPWSTR", "System.String" )}
        };

    public static Dictionary<VarEnum, Tuple<string, string>> MapVarEnumToString = new Dictionary<VarEnum, Tuple<string, string>>()
        {
            { VarEnum.VT_EMPTY, new Tuple<string, string>("void", "System.IntPtr")},
            { VarEnum.VT_NULL, new Tuple<string, string>("null", "null" )},
            { VarEnum.VT_I2, new Tuple<string, string>("short", "System.Int16" )},
            { VarEnum.VT_I4, new Tuple<string, string>("long", "System.Int32" )},
            { VarEnum.VT_R4, new Tuple<string, string>("single", "System.Single" )},
            { VarEnum.VT_R8, new Tuple<string, string>("double", "System.Double" )},
            { VarEnum.VT_CY, new Tuple<string, string>("CURRENCY", "System.Decimal" )},
            { VarEnum.VT_DATE, new Tuple<string, string>("DATE", "System.DateTime" )},
            { VarEnum.VT_BSTR, new Tuple<string, string>("BSTR", "System.String" )},
            { VarEnum.VT_DISPATCH, new Tuple<string, string>("IDispatch", "System.Object" )},
            { VarEnum.VT_ERROR, new Tuple<string, string>("SCODE", "System.Int32" )},
            { VarEnum.VT_BOOL, new Tuple<string, string>("bool", "Boolean" )},
            { VarEnum.VT_VARIANT, new Tuple<string, string>("VARIANT", "Variant" )},
            { VarEnum.VT_UNKNOWN, new Tuple<string, string>("IUnknown", "IUnknown Pointer" )},
            { VarEnum.VT_DECIMAL, new Tuple<string, string>("wchar_t", "System.UInt16" )},
            { VarEnum.VT_I1, new Tuple<string, string>("char", "System.SByte" )},
            { VarEnum.VT_UI1, new Tuple<string, string>("unsigned char", "System.Byte" )},
            { VarEnum.VT_UI2, new Tuple<string, string>("unsigned short", "System.UInt16" )},
            { VarEnum.VT_UI4, new Tuple<string, string>("unsigned long", "System.UInt32" )},
            { VarEnum.VT_I8, new Tuple<string, string>("int64", "System.Int64" )},
            { VarEnum.VT_UI8, new Tuple<string, string>("uint64", "System.UInt64" )},
            { VarEnum.VT_INT, new Tuple<string, string>("int", "System.Int32" )},
            { VarEnum.VT_UINT, new Tuple<string, string>("unsigned int", "System.UInt32" )},
            { VarEnum.VT_VOID, new Tuple<string, string>("void", "System.IntPtr" )},
            { VarEnum.VT_HRESULT, new Tuple<string, string>("HRESULT", "System.Int32" )},
            { VarEnum.VT_PTR, new Tuple<string, string>("PTR", "System.Int32" )},
            { VarEnum.VT_SAFEARRAY, new Tuple<string, string>("SAFEARRAY", "" )},
            { VarEnum.VT_CARRAY, new Tuple<string, string>("CARRAY", "" )},
            { VarEnum.VT_USERDEFINED, new Tuple<string, string>("USERDEFINED", "" )},
            { VarEnum.VT_LPSTR, new Tuple<string, string>("LPSTR", "System.String" )},
            { VarEnum.VT_LPWSTR, new Tuple<string, string>("LPWSTR", "System.String" )},
            { VarEnum.VT_RECORD, new Tuple<string, string>("RECORD", "")},
            { VarEnum.VT_FILETIME, new Tuple<string, string>("FILETIME", "" )},
            { VarEnum.VT_BLOB, new Tuple<string, string>("BLOB", "" )},
            { VarEnum.VT_STREAM, new Tuple<string, string>("STREAM", "" )},
            { VarEnum.VT_STORAGE, new Tuple<string, string>("STORAGE", "" )},
            { VarEnum.VT_STREAMED_OBJECT, new Tuple<string, string>("STREAMED_OBJECT", "" )},
            { VarEnum.VT_STORED_OBJECT, new Tuple<string, string>("STORED_OBJECT", "" )},
            { VarEnum.VT_BLOB_OBJECT,  new Tuple<string, string>("BLOB_OBJECT", "" )},
            { VarEnum.VT_CF, new Tuple<string, string>("CF", "" )},
            { VarEnum.VT_CLSID, new Tuple<string, string>("CLSID", "Guid" )},
            { VarEnum.VT_VECTOR, new Tuple<string, string>("VERSIONED_STREAM", "" )},
            { VarEnum.VT_ARRAY, new Tuple<string, string>("INT_PTR", "System.IntPtr" )},
            { VarEnum.VT_BYREF, new Tuple<string, string>("UINT_PTR", "System.IntPtr" )}
        };



  }

  //TODO: obsolete

  public enum EnumInvokeKinds
  {
    INVOKE_UNKNOWN = 0,
    INVOKE_FUNC = 1,
    INVOKE_PROPERTYGET = 2,
    INVOKE_PROPERTYPUT = 4,
    INVOKE_PROPERTYPUTREF = 8,
    INVOKE_EVENTFUNC = 16,
    INVOKE_CONST = 32
  }
  //TODO: obsolete

  public enum EnumTliVarType
  {
    VT_EMPTY = 0,
    VT_NULL = 1,
    VT_I2 = 2,
    VT_I4 = 3,
    VT_R4 = 4,
    VT_R8 = 5,
    VT_CY = 6,
    VT_DATE = 7,
    VT_BSTR = 8,
    VT_DISPATCH = 9,
    VT_ERROR = 10,
    VT_BOOL = 11,
    VT_VARIANT = 12,
    VT_UNKNOWN = 13,
    VT_DECIMAL = 14,
    VT_I1 = 16,
    VT_UI1 = 17,
    VT_UI2 = 18,
    VT_UI4 = 19,
    VT_I8 = 20,
    VT_UI8 = 21,
    VT_INT = 22,
    VT_UINT = 23,
    VT_VOID = 24,
    VT_HRESULT = 25,
    VT_PTR = 26,
    VT_SAFEARRAY = 27,
    VT_CARRAY = 28,
    VT_USERDEFINED = 29,
    VT_LPSTR = 30,
    VT_LPWSTR = 31,
    VT_RECORD = 36,
    VT_FILETIME = 64,
    VT_BLOB = 65,
    VT_STREAM = 66,
    VT_STORAGE = 67,
    VT_STREAMED_OBJECT = 68,
    VT_STORED_OBJECT = 69,
    VT_BLOB_OBJECT = 70,
    VT_CF = 71,
    VT_CLSID = 72,
    VT_VECTOR = 4096,
    VT_ARRAY = 8192,
    VT_BYREF = 16384,
    VT_RESERVED = 32768
  }

  public struct ARRAYDESC
  {
    public TYPEDESC tdescElem;
    public short cDims;

    // We don't care about the rest of this structure, only the first
    // part.  And its hard since its variable length.
    //public int[] rgbounds;
  }

  public struct CORRECT_VARDESC
  {
    public int memid;
    public string lpstrSchema;
    public VARDESC.DESCUNION u;
    public ELEMDESC elemdescVar;
    public short wVarFlags;
    public VarEnum varkind;
  }
}


