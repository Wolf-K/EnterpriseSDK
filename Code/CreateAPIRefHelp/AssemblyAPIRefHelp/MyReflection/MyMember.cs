using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
  public class MyMember
  {
    public MyMember(MemberInfo memberInfo, MyTypeBase parentType)
    {
      ReflectionMember = memberInfo;
      IsKindOf = memberInfo.MemberType;
      MemberName = MyXmlDoc.MemberHelper.ToXmlDocMemberName(memberInfo);
      var memberDoc = MyXmlDoc.LookupComments.Current?.GetMemberDocumentation(memberInfo);
      Summary = memberDoc?.Summary ?? string.Empty;
      Remarks = memberDoc?.Remarks ?? string.Empty;
      Returns = memberDoc?.Returns ?? string.Empty;
      Name = memberInfo.Name ?? String.Empty;
      HasError = String.Empty;
      FullName = string.Empty;
      GenericsName = string.Empty;
      ParentType = parentType;
      switch (memberInfo.MemberType)
      {
        case MemberTypes.Event:
          NewReturnType = ((EventInfo)memberInfo)?.EventHandlerType?.ToString();
          CSharpSyntax = MdxUtil.CSharpSyntaxGenerator.GetEventSyntax((EventInfo)memberInfo);
          break;
        case MemberTypes.Field:
          NewReturnType = ((FieldInfo)memberInfo)?.FieldType?.ToString();
          CSharpSyntax = MdxUtil.CSharpSyntaxGenerator.GetFieldSyntax((FieldInfo)memberInfo);
          return;
        case MemberTypes.Method:
          // NewReturnType = ((MethodInfo)member)?.ReturnType?.ToString();
          try
          {
            var mr = ((MethodInfo)memberInfo)?.ReturnParameter;
            NewReturnType = mr?.Name;
            CSharpSyntax = MdxUtil.CSharpSyntaxGenerator.GetMethodSyntax((MethodInfo)memberInfo);
          }
          catch
          {
            NewReturnType = typeof(System.Object).FullName;
          }
          return;
        case MemberTypes.Property:
          if (memberInfo is PropertyInfo propInfo)
          {
            if (propInfo.GetMethod != null)
            {
              var propMethod = new MyMemberMethod(propInfo.GetMethod, parentType);
              NewReturnType = propMethod.ReturnType.FullName;
              //((PropertyInfo)member)?.PropertyType?.ToString();
            }
            CSharpSyntax = MdxUtil.CSharpSyntaxGenerator.GetPropertySyntax(propInfo);
          }
          break;
        default:
          break;
      }
      if (NewReturnType != null)
      {
        System.Diagnostics.Trace.WriteLine(NewReturnType.ToString());
      }
      IsInherited = memberInfo != null && memberInfo.DeclaringType != parentType.ReflectionType;
    }

    public MemberInfo? ReflectionMember { get; set; }

    public MemberTypes IsKindOf { get; private set; }

    public string MemberName { get; private set; }

    public bool IsInherited { get; private set; }

    public (bool IsObsolete, string? ObsoleteMessage) Obsolete { get; set; }

    public KindType KindType
    {
      get
      {
        switch (IsKindOf)
        {
          case MemberTypes.Field:
            return KindType.Field;
          case MemberTypes.Property:
            return KindType.Property;
          case MemberTypes.Constructor:
            return KindType.Constructor;
          case MemberTypes.Method:
            return KindType.Method;
          case MemberTypes.Event:
            return KindType.Event;
          case MemberTypes.TypeInfo:
            break;
          case MemberTypes.Custom:
            break;
          case MemberTypes.NestedType:
            break;
          case MemberTypes.All:
            break;
          default:
            break;
        }
        return KindType.Other;
      }
    }
    public virtual string Name { get; set; }
    public string PartialName
    {
      get
      {
        // esriCarto::Arguments
        return $"{ParentType.Namespace}::{Name}";
      }
    }
    public MemberInfo memberInfo { get; private set; }
    public string HasError { get; set; }
    public string? FullName { get; set; }
    public string GenericsName { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Returns { get; set; } = string.Empty;


    public string VbSyntax { get; set; } = "not defined";
    public string CSharpSyntax { get; set; } = "not defined";
    public string CSharpSyntaxShort { get { return CSharpSyntax.Replace("public ", "").Replace(" { }", ""); } }

    public string? NewReturnType { get; set; }
    public bool HasInternalParams { get; set; }

    public MyTypeBase ParentType { get; private set; }

    public override string ToString()
    {
      return $@"MyMember {Name}";
    }
  }
}
