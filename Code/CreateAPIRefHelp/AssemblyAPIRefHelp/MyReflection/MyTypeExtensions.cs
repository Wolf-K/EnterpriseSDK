using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyReflection
{
  public static class MyTypeExtensions
  {
    /// <summary>
    /// Converts the '1 syntax into more readable <> generic type 
    /// strings
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static string GenericTypeString(this Type t)
    {
      if (!t.IsGenericType)
      {
        return t.GetFullName()
                .ReplacePlusWithDotInNestedTypeName();
      }

      return t.GetGenericTypeDefinition()
              .GetFullName()
              .ReplacePlusWithDotInNestedTypeName()
              .ReplaceGenericParametersInGenericTypeName(t);
    }

    public static string? GenericTypeStringNoNameSpace(this Type t)
    {
      if (!t.IsGenericType)
      {
        return t.GetFullNameWithoutNamespace()?
                .ReplacePlusWithDotInNestedTypeName();
      }

      return t.GetGenericTypeDefinition()?
              .GetFullNameWithoutNamespace()?
              .ReplacePlusWithDotInNestedTypeName()
              .ReplaceGenericParametersInGenericTypeName(t);
    }

    public static string? GetFullNameWithoutNamespace(this Type type)
    {
      if (type.IsGenericParameter)
      {
        return type.Name;
      }
      const int dotLength = 1;
      if (type.Namespace == null) return type.FullName;
      return type.FullName?.Substring(type.Namespace.Length + dotLength);
    }
    public static string GetFullName(this Type type)
    {
      return type.FullName ?? type.Name;
    }

    public static string ReplacePlusWithDotInNestedTypeName(this string typeName)
    {
      return typeName.Replace('+', '.');
    }

    public static string ReplaceGenericParametersInGenericTypeName(this string typeName, Type t)
    {
      var genericArguments = t.GetGenericArguments();

      const string regexForGenericArguments = @"`[1-9]\d*";

      var rgx = new Regex(regexForGenericArguments);

      typeName = rgx.Replace(typeName, match =>
      {
        var currentGenericArgumentNumbers = int.Parse(match.Value.Substring(1));
        var currentArguments = string.Join(",", genericArguments.Take(currentGenericArgumentNumbers).Select(GenericTypeString));
        genericArguments = genericArguments.Skip(currentGenericArgumentNumbers).ToArray();
        return string.Concat("<", currentArguments, ">");
      });

      return typeName;
    }
    public static bool IsDelegate(this Type type)
    {
      var baseTypeName = type.BaseType != null ? type.BaseType.Name : String.Empty;
      return baseTypeName == "MulticastDelegate";
    }

  }

  public static class TypeInfoAllMemberExtensions
  {
    public static IEnumerable<ConstructorInfo> GetAllConstructors(this TypeInfo typeInfo)
        => GetAll(typeInfo, ti => ti.DeclaredConstructors);

    public static IEnumerable<EventInfo> GetAllEvents(this TypeInfo typeInfo)
        => GetAll(typeInfo, ti => ti.DeclaredEvents);

    public static IEnumerable<FieldInfo> GetAllFields(this TypeInfo typeInfo)
        => GetAll(typeInfo, ti => ti.DeclaredFields);

    public static IEnumerable<MemberInfo> GetAllMembers(this TypeInfo typeInfo)
        => GetAll(typeInfo, ti => ti.DeclaredMembers);

    public static IEnumerable<MethodInfo> GetAllMethods(this TypeInfo typeInfo)
    {
      var enumeration = GetAll(typeInfo, ti => ti.DeclaredMethods);
      var sorted = enumeration.OrderBy(p => p.Name).ThenBy(p => p.DeclaringType?.FullName);
      return sorted;
    }

    public static IEnumerable<MethodInfo> GetUniqueMethods(this TypeInfo typeInfo)
    {
      var enumeration = typeInfo
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !m.IsAbstract) // Exclude abstract methods
            .GroupBy(m => new
            {
              m.DeclaringType,
              m.Name,
              GenericArity = m.IsGenericMethod ? m.GetGenericArguments().Length : 0,
              Params = string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName))
            })
            .Select(g => g.First());
      var sorted = enumeration.OrderBy(p => p.Name).ThenBy(p => p.DeclaringType?.FullName);
      return sorted;
    }

    public static IEnumerable<TypeInfo> GetAllNestedTypes(this TypeInfo typeInfo)
        => GetAll(typeInfo, ti => ti.DeclaredNestedTypes);

    public static IEnumerable<PropertyInfo> GetAllProperties(this TypeInfo typeInfo)
        => GetAll(typeInfo, ti => ti.DeclaredProperties);

    public static IEnumerable<PropertyInfo> GetUniqueProperties(this TypeInfo typeInfo)
    {
      var enumeration = typeInfo
          .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
          .GroupBy(p => (p.DeclaringType, p.Name)) // group by declaring type and property name
          .Select(g => g.First()); // take the first occurrence
      var sorted = enumeration.OrderBy(p => p.Name).ThenBy(p => p.DeclaringType?.FullName);
      return sorted;
    }

    private static IEnumerable<T> GetAll<T>(TypeInfo? typeInfo, Func<TypeInfo, IEnumerable<T>> accessor)
    {
      while (typeInfo != null)
      {
        foreach (var t in accessor(typeInfo))
        {
          yield return t;
        }
        if (typeInfo == null) continue;
        var tinfoName = typeInfo.FullName;
        if (!string.IsNullOrEmpty(tinfoName) && tinfoName.Contains(".Internal."))
        {
          typeInfo = null;
          continue;
        }
        typeInfo = typeInfo.BaseType?.GetTypeInfo();
        // certain baseclasses are ignored by Document! X
        if (typeInfo?.FullName == "System.Object")
          typeInfo = null;
      }
    }
  }

  public static class MyMemberInfo
  {
    /// <summary>
    /// Get the BrowsableNever and the Obsolete information for a member.
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <param name="obsolete"></param>
    /// <returns></returns>
    public static bool IsEditorBrowsableNeverOrObsolete(this MemberInfo memberInfo, out (bool IsObsolete, string? ObsoleteMessage, bool IsBrowsableNever) obsolete)
    {
      var custAttrs = memberInfo.CustomAttributes;
      //var isEditorBrowsableNever = false;
      obsolete.IsObsolete = false;
      obsolete.ObsoleteMessage = string.Empty;
      obsolete.IsBrowsableNever = false;
      var memberInfoSum = memberInfo.ToString();
      if (memberInfoSum != null
        && memberInfoSum.Contains("DomainDescription DomainDescription", StringComparison.CurrentCultureIgnoreCase))
        System.Diagnostics.Trace.WriteLine(memberInfo.Name);
      foreach (var attr in custAttrs)
      {
        if (attr.AttributeType.Name == "EditorBrowsableAttribute" &&
            Convert.ToInt32(attr.ConstructorArguments[0].Value) == 1)
        {
          //isEditorBrowsableNever = true;
          continue;
        }
        if (attr.AttributeType.Name == "ObsoleteAttribute")
        {
          obsolete.IsObsolete = true;
          if (memberInfo?.ReflectedType != null
            && memberInfo.ReflectedType.FullName != null
            && !memberInfo.ReflectedType.FullName.StartsWith("System"))
          {
            if (attr.ConstructorArguments.Count > 0)
            {
              System.Diagnostics.Trace.WriteLine(attr.ConstructorArguments[0].Value);
              obsolete.ObsoleteMessage = attr.ConstructorArguments[0].Value?.ToString();
            }
          }
        }
      }
      return false;
    }
  }

}
