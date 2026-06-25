using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MyXmlDoc
{

  public static class XmlDocMemberNameBuilder
  {
    /// <summary>
    /// Gets the XML documentation member name for a given reflection member.
    /// </summary>
    public static string GetXmlDocMemberName(MemberInfo member)
    {
      if (member == null) throw new ArgumentNullException(nameof(member));
      return member switch
      {
        Type t => "T:" + GetTypeName(t),
        MethodInfo m => "M:" + GetMethodName(m),
        ConstructorInfo c => "M:" + GetConstructorName(c),
        PropertyInfo p => "P:" + GetPropertyName(p),
        FieldInfo f => "F:" + GetTypeName(f.DeclaringType) + "." + f.Name,
        EventInfo e => "E:" + GetTypeName(e.DeclaringType) + "." + e.Name,
        _ => throw new NotSupportedException($"Unsupported member type: {member.GetType().Name}")
      };
    }

    private static string GetTypeName(Type type)
    {
      if (type.IsGenericParameter)
        return (type.DeclaringMethod != null ? "``" : "`") + type.GenericParameterPosition;

      string name = type.FullName ?? type.Name;

      // Handle nested types
      name = name.Replace('+', '.');

      // Handle generic type definitions
      if (type.IsGenericType)
      {
        var tickIndex = name.IndexOf('`');
        if (tickIndex >= 0)
          name = name.Substring(0, tickIndex) + "`" + type.GetGenericArguments().Length;
      }

      return name;
    }

    private static string GetTypeNameForCtor(Type type)
    {
      if (type.IsGenericParameter)
        return (type.DeclaringMethod != null ? "``" : "`") + type.GenericParameterPosition;

      string name = type.FullName ?? type.Name;

      // Handle nested types
      name = name.Replace('+', '.');

      // Handle generic type definitions
      if (type.IsGenericType)
      {
        var tickIndex = name.IndexOf('`');
        if (tickIndex >= 0)
        {
          var match = Regex.Match(name, @"[^A-Za-z]*([A-Za-z][A-Za-z0-9_.+]*)\s*,");
          if (match.Success)
          {
            name = name.Substring(0, tickIndex) + "{" + match.Groups[1].Value + "}";
          }
          else
          {
            name = name.Substring(0, tickIndex) + "`" + type.GetGenericArguments().Length;
          }
        }
      }

      return name;
    }

    private static string GetMethodName(MethodInfo method)
    {
      var sb = new StringBuilder();
      sb.Append(GetTypeName(method.DeclaringType));
      sb.Append(".");
      sb.Append(method.Name);

      // Handle generic methods
      if (method.IsGenericMethod)
        sb.Append("``").Append(method.GetGenericArguments().Length);

      var parameters = method.GetParameters();
      if (parameters.Length > 0)
      {
        sb.Append("(");
        sb.Append(string.Join(",", parameters.Select(p => GetParameterTypeName(p.ParameterType))));
        sb.Append(")");
      }

      return sb.ToString();
    }

    private static string GetConstructorName(ConstructorInfo ctor)
    {
      var sb = new StringBuilder();
      sb.Append(GetTypeName(ctor.DeclaringType));
      sb.Append(".#ctor");

      var parameters = ctor.GetParameters();
      if (parameters.Length > 0)
      {
        sb.Append("(");
        sb.Append(string.Join(",", parameters.Select(p => GetParameterTypeNameForCtor(p.ParameterType))));
        sb.Append(")");
      }

      return sb.ToString();
    }

    private static string GetPropertyName(PropertyInfo prop)
    {
      var sb = new StringBuilder();
      sb.Append(GetTypeName(prop.DeclaringType));
      sb.Append(".");
      sb.Append(prop.Name);

      var indexParams = prop.GetIndexParameters();
      if (indexParams.Length > 0)
      {
        sb.Append("(");
        sb.Append(string.Join(",", indexParams.Select(p => GetParameterTypeName(p.ParameterType))));
        sb.Append(")");
      }

      return sb.ToString();
    }

    private static string GetParameterTypeName(Type type)
    {
      if (type.IsByRef)
        return GetParameterTypeName(type.GetElementType()) + "@";

      if (type.IsPointer)
        return GetParameterTypeName(type.GetElementType()) + "*";

      if (type.IsArray)
        return GetParameterTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";

      var ret = GetTypeName(type);
      bool endsWithBacktickNumber = Regex.IsMatch(ret, @"`\d+$");
      if (endsWithBacktickNumber) 
      {
        // in .NET, when you see a type name ending with a backtick () followed by a number
        // (e.g., List`1, Dictionary`2), that number indicates how many generic type parameters
        // the type has .
        if (type.FullName == null)
        {
          ret = ret.Replace(@"`1", @"{``0}");
          // if the string 'Func' is not preceded by 'System.' then replace 'Func' with 'System.Func'
          if (!Regex.IsMatch(ret, @"System\.Func"))
          {
            ret = ret.Replace("Func", "System.Func");
          }
        }
        else
        {
          System.Diagnostics.Trace.WriteLine($@"{type.FullName}");
          var index = ret.IndexOf("`");
          var arg = type.GetGenericArguments()[0];
          ret = ret.Substring(0, index) + "{" + GetGenericTypeName(arg) + "}";
        }
      }
      return ret;
    }

    private static string GetGenericTypeName(Type arg)
    {
      var ret = GetTypeName(arg);
      bool endsWithBacktickNumber = Regex.IsMatch(ret, @"`\d+$");
      if (endsWithBacktickNumber)
      {
        var index = ret.IndexOf("`");
        if (arg.IsGenericParameter)
          ret = ret.Substring(0, index) + "{" + arg.Name + "}";
        else
          ret = ret.Substring(0, index) + "{" + arg.FullName + "}";
        index = ret.IndexOf("`");
      }
      return ret;
    }


    private static string GetParameterTypeNameForCtor(Type type)
    {
      if (type.IsByRef)
        return GetParameterTypeName(type.GetElementType()) + "@";

      if (type.IsPointer)
        return GetParameterTypeName(type.GetElementType()) + "*";

      if (type.IsArray)
        return GetParameterTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";

      return GetTypeNameForCtor(type);
    }

    /// <summary>
    /// Builds XML-doc type member name from a runtime Type.
    /// Example: typeof(RegisteredPresentationEvent<,>) -> "T:ArcGIS.Core.Events.RegisteredPresentationEvent`2"
    /// Usage: var s = XmlDocMemberNameBuilder.ToTypeMemberName(
    /// "RegisteredPresentationEvent<TSubscriptionParam,TPayload>",
    /// "ArcGIS.Core.Events");
    /// </summary>
    public static string ToTypeMemberName(Type type)
    {
      if (type == null) throw new ArgumentNullException(nameof(type));

      // XML doc type names use generic type definition + arity (`N)
      var t = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

      var fullName = t.FullName ?? t.Name;
      // Nested type separator in XML-doc names is '.'
      fullName = fullName.Replace('+', '.');

      return $"T:{fullName}";
    }

    /// <summary>
    /// Builds XML-doc type member name from C# type display text.
    /// Example:
    ///   csharpType = "RegisteredPresentationEvent<TSubscriptionParam,TPayload>"
    ///   namespaceName = "ArcGIS.Core.Events"
    /// Result:
    ///   "T:ArcGIS.Core.Events.RegisteredPresentationEvent`2"
    /// </summary>
    public static string ToTypeMemberName(string csharpType, string namespaceName)
    {
      if (string.IsNullOrWhiteSpace(csharpType))
        throw new ArgumentException("Value cannot be null or whitespace.", nameof(csharpType));
      if (string.IsNullOrWhiteSpace(namespaceName))
        throw new ArgumentException("Value cannot be null or whitespace.", nameof(namespaceName));

      csharpType = csharpType.Trim();
      namespaceName = namespaceName.Trim().TrimEnd('.');

      int lt = csharpType.IndexOf('<');
      if (lt < 0)
      {
        // non-generic
        return $"T:{namespaceName}.{csharpType}";
      }

      int gt = csharpType.LastIndexOf('>');
      if (gt < 0 || gt < lt)
        throw new FormatException("Invalid generic type format.");

      string typeName = csharpType[..lt].Trim();
      string genericArgsText = csharpType.Substring(lt + 1, gt - lt - 1);

      int arity = SplitTopLevelGenericArguments(genericArgsText).Count;
      return $"T:{namespaceName}.{typeName}`{arity}";
    }

    private static List<string> SplitTopLevelGenericArguments(string input)
    {
      var result = new List<string>();
      if (string.IsNullOrWhiteSpace(input)) return result;

      var sb = new StringBuilder();
      int depth = 0;

      foreach (char ch in input)
      {
        if (ch == '<') depth++;
        if (ch == '>') depth--;

        if (ch == ',' && depth == 0)
        {
          result.Add(sb.ToString().Trim());
          sb.Clear();
          continue;
        }

        sb.Append(ch);
      }

      if (sb.Length > 0)
        result.Add(sb.ToString().Trim());

      return result;
    }
  }
  public static class MethodInfoExtensions
  {
    /// <summary>
    /// Returns a full method signature string including generic arguments.
    /// </summary>
    /// <param name="method">The method info to get the signature for.</param>
    /// <param name="withReturnType">Whether to include the return type in the signature.</param>
    /// <param name="withDeclarationType">Whether to include the declaring type in the signature.</param>
    /// <param name="withParameterName">Whether to include parameter names in the signature.</param>
    /// <returns>The full method signature string.</returns>
    public static string ToFullSignatureString(this MethodInfo method, 
      bool withReturnType, 
      bool withDeclarationType,
      bool withParameterName)
    {
      if (method == null)
        throw new ArgumentNullException(nameof(method));

      var sb = new StringBuilder();

      if (withReturnType)
      {
        // Return type
        sb.Append(GetFriendlyTypeName(method.ReturnType));
        sb.Append(' ');
      }

      if (withDeclarationType)
      {
        // Declaring type
        if (method.DeclaringType != null)
        {
          sb.Append(GetFriendlyTypeName(method.DeclaringType));
          sb.Append('.');
        }
      }

      // Method name
      sb.Append(method.Name);

      // Generic arguments (if any)
      if (method.IsGenericMethod)
      {
        var genericArgs = method.GetGenericArguments()
                                .Select(GetFriendlyTypeName);
        sb.Append('<');
        sb.Append(string.Join(", ", genericArgs));
        sb.Append('>');
      }

      // Parameters
      var parameters = method.GetParameters()
                             .Select(p => $"{GetFriendlyTypeName(p.ParameterType)}{(withParameterName ? " " + p.Name : string.Empty)}");
      sb.Append('(');
      sb.Append(string.Join(", ", parameters));
      sb.Append(')');

      return sb.ToString();
    }

    /// <summary>
    /// Returns a readable type name, handling generics and arrays.
    /// </summary>
    private static string GetFriendlyTypeName(Type type)
    {
      if (type.IsGenericType)
      {
        var typeName = type.Name;
        var backtickIndex = typeName.IndexOf('`');
        if (backtickIndex > 0)
          typeName = typeName.Substring(0, backtickIndex);

        var genericArgs = type.GetGenericArguments()
                              .Select(GetFriendlyTypeName);
        return $"{type.Namespace}.{typeName}<{string.Join(", ", genericArgs)}>";
      }
      else if (type.IsArray)
      {
        return $"{GetFriendlyTypeName(type.GetElementType())}[]";
      }
      else
      {
        return type.FullName ?? type.Name;
      }
    }
  }

  public static class MemberHelper
  { 
    // memberName example: "T:ArcGIS.Core.Events.RegisteredPresentationEvent`2"
    // memberXml example: <member name="T:..."><typeparam name="TSubscriptionParam"/>...</member>
    public static string ToCSharpTypeDisplay(string memberName, XElement? memberXml = null)
    {
      if (string.IsNullOrWhiteSpace(memberName))
        return string.Empty;
      // Strip xml-doc prefix (T:, M:, etc.)
      var qualified = memberName.Length > 2 && memberName[1] == ':'
          ? memberName[2..]
          : memberName;
      // Remove method/property suffix if present (not needed for T:, but safe)
      var paren = qualified.IndexOf('(');
      if (paren >= 0)
        qualified = qualified[..paren];
      // Type leaf: Namespace.Outer+Inner`2 -> Inner`2
      var leaf = qualified.Split('.', '+').Last();
      // Parse generic arity from `N
      var m = Regex.Match(leaf, @"^(?<name>[^`]+)(?:`(?<arity>\d+))?$");
      if (!m.Success)
        return leaf;
      var typeName = m.Groups["name"].Value;
      var arity = m.Groups["arity"].Success ? int.Parse(m.Groups["arity"].Value) : 0;
      if (arity == 0)
        return typeName;
      // Try to read real type parameter names from XML: <typeparam name="...">
      var typeParams = memberXml?
          .Elements("typeparam")
          .Select(x => (string?)x.Attribute("name"))
          .Where(n => !string.IsNullOrWhiteSpace(n))
          .Cast<string>()
          .ToList() ?? [];

      // Fallback if XML missing/incomplete
      if (typeParams.Count != arity)
        typeParams = Enumerable.Range(1, arity).Select(i => $"T{i}").ToList();

      return $"{typeName}<{string.Join(",", typeParams)}>";
    }
    /// <summary>
    /// Converts a reflection member (type, method, property, field, event, or namespace) to the member name string
    /// used for XML documentation lookup (e.g., "T:Namespace.Type", "M:Namespace.Type.Method", etc.).
    /// </summary>
    /// <param name="member">The reflection member (Type, MethodInfo, PropertyInfo, FieldInfo, EventInfo, or string for namespace).</param>
    /// <returns>The XML documentation member name string, or null if not supported.</returns>
    public static string? ToXmlDocMemberName(object member)
    {
      switch (member)
      {
        case Type type:
          return $"T:{type.FullName?.Replace('+', '.')}";
        case MethodInfo method:
          //return $"M:{method.DeclaringType?.FullName?.Replace('+', '.')}.{GetMethodSignature(method)}";
          return XmlDocMemberNameBuilder.GetXmlDocMemberName(method);
        case ConstructorInfo ctor:
          //return $"M:{ctor.DeclaringType?.FullName?.Replace('+', '.')}.#ctor{GetParametersSignature(ctor.GetParameters())}";
          return XmlDocMemberNameBuilder.GetXmlDocMemberName(ctor);
        case PropertyInfo prop:
          // return $"P:{prop.DeclaringType?.FullName?.Replace('+', '.')}.{prop.Name}";
          return XmlDocMemberNameBuilder.GetXmlDocMemberName(prop);
        case FieldInfo field:
          // return $"F:{field.DeclaringType?.FullName?.Replace('+', '.')}.{field.Name}";
          return XmlDocMemberNameBuilder.GetXmlDocMemberName(field);
        case EventInfo evt:
          // return $"E:{evt.DeclaringType?.FullName?.Replace('+', '.')}.{evt.Name}";
          return XmlDocMemberNameBuilder.GetXmlDocMemberName(evt);
        case string ns when ns.EndsWith("."):
          return $"N:{ns.TrimEnd('.')}";
        default:
          return null;
      }
    }

    private static string GetMethodSignature(MethodInfo method)
    {
      var sb = new StringBuilder();
      sb.Append(method.Name);
      var parameters = method.GetParameters();
      if (parameters.Length > 0)
      {
        sb.Append(GetParametersSignature(parameters));
      }
      return sb.ToString();
    }

    private static string GetParametersSignature(ParameterInfo[] parameters)
    {
      if (parameters.Length == 0) return string.Empty;
      var sb = new StringBuilder("(");
      for (int i = 0; i < parameters.Length; i++)
      {
        if (i > 0) sb.Append(",");
        sb.Append(GetParameterTypeName(parameters[i].ParameterType));
      }
      sb.Append(")");
      return sb.ToString();
    }

    private static string GetParameterTypeName(Type type)
    {
      if (type.IsByRef)
        type = type.GetElementType()!;
      if (type.IsGenericType)
      {
        var genericTypeDef = type.GetGenericTypeDefinition();
        var genericArgs = type.GetGenericArguments();
        var typeName = genericTypeDef.FullName;
        int tickIndex = typeName!.IndexOf('`');
        if (tickIndex > 0)
          typeName = typeName.Substring(0, tickIndex);
        return $"{typeName}[{string.Join(",", genericArgs.Select(GetParameterTypeName))}]";
      }
      if (type.IsArray)
        return GetParameterTypeName(type.GetElementType()!) + "[]";
      return type.FullName ?? type.Name;
    }
  }
}
