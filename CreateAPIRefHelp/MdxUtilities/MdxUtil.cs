using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MdxUtil
{
  public static class MdxUtil
  {
    public static readonly string MdxExtension = ".md";
    /// <summary>
    /// All templates that are used to create .mdx files for namespaces, types and members are stored as static properties in this class. The templates are read from the TlbTemplates folder and loaded into these properties at the start of the program. 
    /// The templates contain placeholders that are replaced with actual values when creating the .mdx files.
    /// </summary>
    public static string? PageNamespaceTemplate { get; set; }
    public static string? PageComNamespaceTemplate { get; set; }
    public static string? PageInterfaceTemplate { get; set; }
    public static string? PageClassTemplate { get; set; }
    public static string? PageCoClassTemplate { get; set; }
    public static string? PageEnumTemplate { get; set; }
    public static string? RemarksTemplate { get; set; }
    public static string? SampleTemplate { get; set; }
    public static string? InheritanceTemplate { get; set; }
    public static string? MethodParameterTemplate { get; set; }
    public static string? MemberDetailTemplate { get; set; }
    public static string? MemberRemarksTemplate { get; set; }
    public static string? MethodHeaderTemplate { get; set; }
    public static string? MemberHeader2Template { get; set; }
    public static string? MemberHeader3Template { get; set; }
    public static string? MemberMdTemplate { get; set; }
    public static string? MemberDetailMdTemplate { get; set; }
    public static string? EnumHeaderTemplate { get; set; }
    public static string? ConstructorTemplate { get; set; }
    public static string? ConstructorDetailMdTemplate { get; set; }
    public static string? SyntaxTemplate { get; set; }

    /// <summary>
    /// Create a unique member name
    /// </summary>
    /// <param name="namespaceName"></param>
    /// <param name="memberType"></param>
    /// <param name="memberName"></param>
    /// <param name="methodOrProperty"></param>
    /// <returns></returns>
    public static string CreateMemberName( 
      string memberType, 
      string? memberName = "", 
      string methodOrProperty = "")
    {
      var sb = new StringBuilder();
      // capitalize first letter of member type the remainder is lower case
      if (!string.IsNullOrEmpty(memberType))
      {
        if (memberType.Length > 0 && memberType.StartsWith('_'))
          sb.Append(memberType); // if the member type starts with "_", we will keep it as is, this is for namespace which we want to append "_Namespace" to make it unique and avoid conflict with type names.
        else
        {
          var firstChar = memberType[..1].ToUpper();
          var restChars = memberType.Length > 1 ? memberType[1..].ToLower() : string.Empty;
          sb.Append(firstChar);
          sb.Append(restChars);
        }
      }
      if (!string.IsNullOrEmpty(memberName)) sb.Append(memberName);
      if (!string.IsNullOrEmpty(methodOrProperty)) sb.Append(methodOrProperty);
      return sb.ToString();
    }

    /// <summary>
    /// Creates the full file path for a member's .mdx file within the specified output directory and namespace
    /// subfolder.
    /// </summary>
    /// <remarks>If the namespace subfolder does not exist within the output directory, it is created
    /// automatically. The resulting file name is generated using the specified namespace, optional prefix, and member
    /// name.</remarks>
    /// <param name="outputRoot">The root directory where the member file should be created. Cannot be null or empty.</param>
    /// <param name="namespaceName">The namespace used to create a subfolder within the output directory. Cannot be null or empty.</param>
    /// <param name="name">The name of the member for which the file path is generated. Cannot be null or empty.</param>
    /// <param name="memberNamePrefix">An optional prefix to include in the generated member file name. If null, no prefix is used.</param>
    /// <returns>A string containing the full file path to the member's .mdx file, including the namespace subfolder and
    /// generated file name.</returns>
    public static string CreateMemberFilePath(string outputRoot,
      string namespaceName,
      string name,
      string? memberNamePrefix = null)
    {
      // create a sub folder if it doesn't exist
      var mdxFolder = Path.Combine(outputRoot, namespaceName);
      if (!Directory.Exists(mdxFolder)) Directory.CreateDirectory(mdxFolder);

      // create the complete file path for the class .mdx file
      var classFilename = $@"{CreateMemberName(memberNamePrefix ?? string.Empty, name)}{MdxUtil.MdxExtension}";
      return Path.Combine(mdxFolder, classFilename);
    }

    public static string CreateMemberRelativePath(string namespaceName,
      string name,
      string? memberNamePrefix = null)
    {
      // create the relative file path for the class .mdx file
      return $@"{Path.Combine(namespaceName, CreateMemberName(memberNamePrefix ?? string.Empty, name))}{MdxUtil.MdxExtension}";
    }

    /// <summary>
    /// Create a short version of the Namespace
    /// </summary>
    /// <param name="fullNamespaceName"></param>
    /// <returns></returns>
    public static string GetShortLibName(string fullNamespaceName)
    {
      var lcaseLibName = fullNamespaceName.ToLower();
      if (lcaseLibName.StartsWith("esri"))
      {
        return fullNamespaceName[4..];
      }
      return fullNamespaceName;
    }

    /// <summary>
    /// creates an md table with two columns from the dictionary passed in
    /// <tr>
    ///   <td><a href = "IAISRequest.html">IAISRequest</a></td>
    ///   <td> Provides access to members that controls an AIS request.</td>
    /// </tr>
    /// </summary>
    /// <param name="namespaceName"></param>
    /// <param name="memberType"></param>
    /// <param name="columns">dictionary of column names and descriptions</param>
    /// <returns></returns>
    public static string CreateTwoColumnTable(string namespaceName, string memberType, List<(string? Name, string? Description)> colList)
    {
      var sb = new StringBuilder();
      foreach (var column in colList)
      {
        sb.AppendLine($@"|[{column.Name}]({CreateMemberName(memberType, column.Name)}{MdxUtil.MdxExtension})|{column.Description}|");
      }
      return sb.ToString();
    }

    /// <summary>
    /// creates an md table with two columns from the dictionary passed in
    /// <tr>
    ///   <td><a href = "IAISRequest.html">IAISRequest</a></td>
    ///   <td> Provides access to members that controls an AIS request.</td>
    /// </tr>
    /// </summary>
    /// <param name="namespaceName"></param>
    /// <param name="memberType"></param>
    /// <param name="columns">dictionary of column names and descriptions</param>
    /// <returns></returns>
    public static string CreateLocalLinkTwoColumnTable(string namespaceName, string memberType, List<(string? Name, string? Description)> colList)
    {
      var sb = new StringBuilder();
      foreach (var column in colList)
      {
        sb.AppendLine($@"| [{column.Name}]({ToMarkdownAnchor(column.Name)})|{column.Description}|");
      }
      return sb.ToString();
    }

    /// <summary>
    /// Generates an MD table row string for a three-column table representing members and their descriptions.
    /// </summary>
    /// <remarks>The returned string includes only the table row elements (<tr>...</tr>) for each member. The
    /// caller is responsible for providing the enclosing <table> element and any table headers as needed.</remarks>
    /// <param name="namespaceName">The name of the library to which the members belong. Used to construct member links.</param>
    /// <param name="memberType">The type of the member (such as class, method, or property). Used to build the link for each member.</param>
    /// <param name="columns">A list containing the member's invoke type, name, description. Each entry is used to
    /// populate a row in the table.</param>
    /// <returns>A string containing MD markup for table rows, where each row includes a link to the member, its type, and its
    /// description.</returns>
    public static string CreateThreeColumnTable(string namespaceName, string memberType, List<(string InvokeImg, string Name, string Description)> columns)
    {
      var sb = new StringBuilder();
      foreach (var column in columns)
      {
        sb.AppendLine($@"|![{column.InvokeImg} member type](../bitmaps/{column.InvokeImg}.gif)|[{column.Name}]({ToMarkdownAnchor(column.Name)})|{column.Description}|");
      }
      return sb.ToString();
    }

    /// <summary>
    /// Generates an MD table row string for a three-column table representing members and their descriptions.
    /// </summary>
    /// <remarks>The returned string includes only the table row elements (<tr>...</tr>) for each member. The
    /// caller is responsible for providing the enclosing <table> element and any table headers as needed.</remarks>
    /// <param name="namespaceName">The name of the library to which the members belong. Used to construct member links.</param>
    /// <param name="memberType">The type of the member (such as class, method, or property). Used to build the link for each member.</param>
    /// <param name="columns">A list containing the member's invoke type, name, description. Each entry is used to
    /// populate a row in the table.</param>
    /// <returns>A string containing MD markup for table rows, where each row includes a link to the member, its type, and its
    /// description.</returns>
    public static string CreateThreeSimpleColumnTable(string namespaceName, string memberType, List<(string Name, string Value, string Description)> columns)
    {
      var sb = new StringBuilder();
      foreach (var column in columns)
      {
        sb.AppendLine($@"|  **{column.Name}**|{column.Value}|{column.Description}|");
      }
      return sb.ToString();
    }

    /// <summary>
    /// Converts a heading or text into a valid Markdown anchor link (e.g., "#my-heading").
    /// Compatible with GitHub/DocFX style anchors.
    /// </summary>
    public static string ToMarkdownAnchor(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        throw new ArgumentException("Input cannot be null or empty.", nameof(text));

      // Convert to lowercase
      string anchor = text.ToLowerInvariant();

      // Remove all characters except letters, numbers, spaces, and hyphens
      anchor = Regex.Replace(anchor, @"[^\p{L}\p{Nd}\s-]", "");

      // Replace spaces with hyphens
      anchor = Regex.Replace(anchor, @"\s+", "-");

      // Collapse multiple hyphens
      anchor = Regex.Replace(anchor, @"-+", "-");

      // Trim leading/trailing hyphens
      anchor = anchor.Trim('-');

      return "#" + anchor;
    }


  }

  public static class SignatureHelper
  {/// <summary>
   /// Returns a C#-style readable property signature.
   /// </summary>
    public static string GetPropertySignature(PropertyInfo property)
    {
      if (property == null)
        throw new ArgumentNullException(nameof(property));

      var sb = new StringBuilder();

      // Determine accessor methods
      var getter = property.GetGetMethod(true);
      var setter = property.GetSetMethod(true);

      // Determine visibility (use the most visible accessor)
      MethodInfo primaryAccessor = getter ?? setter;
      if (primaryAccessor == null)
        throw new InvalidOperationException("Property has neither getter nor setter.");

      sb.Append(GetAccessModifier(primaryAccessor));

      // Add modifiers: static / virtual / override
      if (primaryAccessor.IsStatic)
        sb.Append(" static");

      if (primaryAccessor.IsVirtual && !primaryAccessor.IsFinal)
      {
        if (IsOverride(primaryAccessor))
          sb.Append(" override");
        else
          sb.Append(" virtual");
      }

      sb.Append(" ");

      // Property type
      sb.Append(GetReadableTypeName(property.PropertyType));
      sb.Append(" ");

      // Property name
      sb.Append(property.Name);

      // Indexer parameters
      var indexParams = property.GetIndexParameters();
      if (indexParams.Length > 0)
      {
        sb.Append("[");
        sb.Append(string.Join(", ", indexParams.Select(p =>
            $"{GetReadableTypeName(p.ParameterType)} {p.Name}")));
        sb.Append("]");
      }

      sb.Append(" {");

      // GET visibility
      if (getter != null)
      {
        sb.Append(" get");
        string getAccess = GetAccessorModifier(getter, primaryAccessor);
        if (!string.IsNullOrEmpty(getAccess))
          sb.Append($" {getAccess}");
        sb.Append(";");
      }

      // SET visibility
      if (setter != null)
      {
        sb.Append(" set");
        string setAccess = GetAccessorModifier(setter, primaryAccessor);
        if (!string.IsNullOrEmpty(setAccess))
          sb.Append($" {setAccess}");
        sb.Append(";");
      }

      sb.Append(" }");

      return sb.ToString();
    }

    private static string GetAccessModifier(MethodBase method)
    {
      if (method.IsPublic) return "public";
      if (method.IsFamily) return "protected";
      if (method.IsAssembly) return "internal";
      if (method.IsFamilyOrAssembly) return "protected internal";
      if (method.IsFamilyAndAssembly) return "private protected";
      return "private";
    }

    private static string GetAccessorModifier(MethodBase accessor, MethodBase primaryAccessor)
    {
      // Different from primary accessor?
      string accVis = GetAccessModifier(accessor);
      string primVis = GetAccessModifier(primaryAccessor);
      return accVis != primVis ? accVis : "";
    }

    private static bool IsOverride(MethodInfo method)
    {
      return method.GetBaseDefinition().DeclaringType != method.DeclaringType;
    }

    /// <summary>
    /// Converts CLR type names into readable C# syntax.
    /// Handles generics, nullable types, arrays, and built-in keywords.
    /// </summary>
    private static string GetReadableTypeName(Type t)
    {
      if (t == null)
        return "void";

      // Nullable<T> -> T?
      if (Nullable.GetUnderlyingType(t) is Type underlying)
        return $"{GetReadableTypeName(underlying)}?";

      if (t.IsArray)
        return $"{GetReadableTypeName(t.GetElementType())}[{new string(',', t.GetArrayRank() - 1)}]";

      // Built-in types
      switch (Type.GetTypeCode(t))
      {
        case TypeCode.Boolean: return "bool";
        case TypeCode.Byte: return "byte";
        case TypeCode.Char: return "char";
        case TypeCode.Decimal: return "decimal";
        case TypeCode.Double: return "double";
        case TypeCode.Int16: return "short";
        case TypeCode.Int32: return "int";
        case TypeCode.Int64: return "long";
        case TypeCode.SByte: return "sbyte";
        case TypeCode.Single: return "float";
        case TypeCode.String: return "string";
        case TypeCode.UInt16: return "ushort";
        case TypeCode.UInt32: return "uint";
        case TypeCode.UInt64: return "ulong";
      }

      // Generic types
      if (t.IsGenericType)
      {
        var genericTypeName = t.Name.Substring(0, t.Name.IndexOf('`'));
        var args = t.GetGenericArguments().Select(GetReadableTypeName);
        return $"{genericTypeName}<{string.Join(", ", args)}>";
      }

      return t.Name;
    }

    // Returns a readable C#-style method signature.
    public static string GetSignature(this MethodInfo method)
    {
      if (method == null)
        throw new ArgumentNullException(nameof(method));

      var sb = new StringBuilder();

      // Return type
      sb.Append(GetTypeName(method.ReturnType));
      sb.Append(" ");

      // Method name
      sb.Append(method.Name);

      // Generic type arguments
      if (method.IsGenericMethod)
      {
        var genericArgs = method.GetGenericArguments()
                                .Select(t => GetTypeName(t));
        sb.Append("<");
        sb.Append(string.Join(", ", genericArgs));
        sb.Append(">");
      }

      // Parameters
      sb.Append("(");
      var parameters = method.GetParameters();

      sb.Append(string.Join(", ", parameters.Select(p =>
      {
        string modifier = p.IsOut ? "out "
                      : p.ParameterType.IsByRef ? "ref "
                      : "";

        // For ref/out, remove the trailing '&' in the parameter type
        Type paramType = p.ParameterType.IsByRef
            ? p.ParameterType.GetElementType()
            : p.ParameterType;

        return modifier + GetTypeName(paramType) + " " + p.Name;
      })));
      sb.Append(")");

      return sb.ToString();
    }

    // Produces readable type names (e.g., List<int>, string, int)
    private static string GetTypeName(Type type)
    {
      if (type == null)
        return "";

      // Built-in aliases
      if (type == typeof(void)) return "void";
      if (type == typeof(int)) return "int";
      if (type == typeof(string)) return "string";
      if (type == typeof(bool)) return "bool";
      if (type == typeof(object)) return "object";
      if (type == typeof(double)) return "double";
      if (type == typeof(float)) return "float";
      if (type == typeof(long)) return "long";
      if (type == typeof(short)) return "short";
      if (type == typeof(byte)) return "byte";
      if (type == typeof(char)) return "char";
      if (type == typeof(decimal)) return "decimal";

      // Generic types
      if (type.IsGenericType)
      {
        var typeName = type.Name.Substring(0, type.Name.IndexOf('`'));
        var args = type.GetGenericArguments()
                       .Select(t => GetTypeName(t));
        return $"{typeName}<{string.Join(", ", args)}>";
      }

      return type.Name;
    }
  }

  public class InheritanceHelper
  {
    /// <summary>
    /// Determines whether the specified member is an override of a base class member.
    /// </summary>
    /// <param name="member">The member to check.</param>
    /// <returns>True if the member is an override; otherwise, false.</returns>
    public static bool IsOverride(MemberInfo member)
    {
      return member switch
      {
        MethodInfo method => IsMethodOverride(method),
        PropertyInfo property => IsPropertyOverride(property),
        _ => false
      };
    }

    private static bool IsMethodOverride(MethodInfo method)
    {
      if (!method.IsVirtual) return false;
      return method.GetBaseDefinition() != method;
    }

    private static bool IsPropertyOverride(PropertyInfo property)
    {
      var get = property.GetMethod;
      var set = property.SetMethod;

      return (get != null && IsMethodOverride(get))
          || (set != null && IsMethodOverride(set));
    }


    /// <summary>
    /// Recursively collects all base types and interfaces of a given type.
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    public static Type? GetInheritedFrom(MemberInfo member)
    {
      return member switch
      {
        MethodInfo m => GetMethodBaseDeclaringType(m),
        PropertyInfo p => GetPropertyBaseDeclaringType(p),
        _ => member.DeclaringType
      };
    }

    private static Type? GetMethodBaseDeclaringType(MethodInfo method)
    {
      // For overrides, this returns the original base declaration type
      return method.GetBaseDefinition().DeclaringType;
    }

    private static Type? GetPropertyBaseDeclaringType(PropertyInfo property)
    {
      // Property itself has no GetBaseDefinition; use accessor(s)
      var accessor = property.GetMethod ?? property.SetMethod;
      if (accessor == null) return property.DeclaringType;

      return accessor.GetBaseDefinition().DeclaringType;
    }

    /// <summary>
    /// Builds the inheritance tree for a given TypeInfo.
    /// </summary>
    /// <param name="typeInfo">The TypeInfo instance to inspect.</param>
    /// <returns>A list of types from the given type up to System.Object.</returns>
    public static List<Type> GetInheritanceTree(TypeInfo typeInfo)
    {
      if (typeInfo == null)
        throw new ArgumentNullException(nameof(typeInfo), "TypeInfo cannot be null.");

      var tree = new List<Type>();
      Type? currentType = typeInfo.AsType();

      // Traverse the inheritance chain
      while (currentType != null)
      {
        tree.Add(currentType);
        currentType = currentType.BaseType;
      }

      return tree;
    }


    /// <summary>
    /// Finds all classes that derive from the given base type (TypeInfo).
    /// Searches all currently loaded assemblies in the AppDomain.
    /// </summary>
    /// <param name="baseTypeInfo">The TypeInfo of the base class.</param>
    /// <returns>List of derived class types.</returns>
    public static List<Type> GetDerivedClasses(TypeInfo baseTypeInfo)
    {
      if (baseTypeInfo == null)
        throw new ArgumentNullException(nameof(baseTypeInfo));

      if (!baseTypeInfo.IsClass && !baseTypeInfo.IsInterface)
        throw new ArgumentException("Provided type must be a class or interface.", nameof(baseTypeInfo));

      var derivedTypes = new List<Type>();

      // Iterate through all loaded assemblies
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        Type[] types;
        try
        {
          types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
          // Handle partial load failures
          types = ex.Types.Where(t => t != null).ToArray();
        }

        foreach (var type in types)
        {
          if (type == null || !type.IsClass || type.IsAbstract)
            continue;

          // Check if the type is derived from the base type
          if (baseTypeInfo.IsAssignableFrom(type.GetTypeInfo()) && type != baseTypeInfo.AsType())
          {
            derivedTypes.Add(type);
          }
        }
      }
      return derivedTypes;
    }

    /// <summary>
    /// Prints the inheritance tree in a readable format.
    /// </summary>
    public static string PrintInheritanceTree(TypeInfo typeInfo)
    {
      var sbResult = new StringBuilder();
      var tree = GetInheritanceTree(typeInfo);
      int indent = 0;
      for (int i = tree.Count - 1; i >= 0; i--)
      {
        sbResult.AppendLine(new string(' ', indent++ * 2) + "- " + tree[i].FullName);
      }
      var derivedClasses = GetDerivedClasses(typeInfo);
      if (derivedClasses.Count > 0)
      {
        foreach (var derived in derivedClasses)
        {
          sbResult.AppendLine(new string(' ', indent * 2) + "- " + derived.FullName);
        }
      }
      return sbResult.ToString();
    }
  }

    public static class CSharpSyntaxGenerator
  {
    /// <summary>
    /// Generates a C# class, struct, interface, or enum declaration string based on the provided type information.
    /// </summary>
    /// <param name="typeInfo">The type information from which to generate the C# declaration. Cannot be null.</param>
    /// <returns>A string representing the C# declaration for the specified type, including all modifiers, base types, and interfaces.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="typeInfo"/> is null.</exception>
    public static string GetClassSyntax(TypeInfo typeInfo)
    {
      if (typeInfo == null)
        throw new ArgumentNullException(nameof(typeInfo));

      var sb = new StringBuilder();

      // Access modifier
      if (typeInfo.IsPublic || typeInfo.IsNestedPublic) sb.Append("public ");
      else if (typeInfo.IsNotPublic || typeInfo.IsNestedAssembly) sb.Append("internal ");
      else if (typeInfo.IsNestedFamily) sb.Append("protected ");
      else if (typeInfo.IsNestedFamORAssem) sb.Append("protected internal ");
      else if (typeInfo.IsNestedPrivate) sb.Append("private ");

      // Class modifiers
      if (typeInfo.IsAbstract && typeInfo.IsSealed) sb.Append("static ");
      else if (typeInfo.IsAbstract) sb.Append("abstract ");
      else if (typeInfo.IsSealed) sb.Append("sealed ");

      // Type kind
      if (typeInfo.IsInterface) sb.Append("interface ");
      else if (typeInfo.IsEnum) sb.Append("enum ");
      else if (typeInfo.IsValueType && !typeInfo.IsEnum) sb.Append("struct ");
      else sb.Append("class ");

      // Name (handle generics)
      sb.Append(GetTypeName(typeInfo));

      // Base type and interfaces
      var baseType = typeInfo.BaseType;
      var interfaces = typeInfo.ImplementedInterfaces
          .Where(i => i != typeof(object))
          .Select(GetTypeName)
          .ToList();

      var baseList = new StringBuilder();
      if (baseType != null && baseType != typeof(object) && !typeInfo.IsInterface && !typeInfo.IsValueType)
        baseList.Append(GetTypeName(baseType));

      if (interfaces.Any())
      {
        if (baseList.Length > 0) baseList.Append(", ");
        baseList.Append(string.Join(", ", interfaces));
      }

      if (baseList.Length > 0)
        sb.Append(" : ").Append(baseList);

      sb.AppendLine();
      sb.Append("{");
      sb.AppendLine();
      sb.Append("}");

      return sb.ToString();
    }

    /// <summary>
    /// Returns a readable type name, handling generics and nested types.
    /// </summary>
    private static string GetTypeName(Type type)
    {
      if (type.IsGenericType)
      {
        var genericTypeName = type.Name.Substring(0, type.Name.IndexOf('`'));
        var genericArgs = type.GetGenericArguments()
                              .Select(GetTypeName)
                              .ToArray();
        return $"{genericTypeName}<{string.Join(", ", genericArgs)}>";
      }
      return type.Name;
    }

    /// <summary>
    /// Generates a C# event declaration syntax string based on the specified event metadata.
    /// </summary>
    /// <remarks>The generated syntax includes access modifiers, event handler type, and event name.
    /// The event handler type is displayed with proper generic formatting.</remarks>
    /// <param name="eventInfo">The event metadata from which to generate the C# event declaration. Cannot be null.</param>
    /// <returns>A string representing the C# syntax for the specified event, including all modifiers, handler type, and name.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="eventInfo"/> is null.</exception>
    public static string GetEventSyntax(EventInfo eventInfo)
    {
      if (eventInfo == null)
        throw new ArgumentNullException(nameof(eventInfo));

      var sb = new StringBuilder();

      // Get the add method to determine access modifiers (events use add/remove accessors)
      var addMethod = eventInfo.GetAddMethod(true);
      var removeMethod = eventInfo.GetRemoveMethod(true);

      var primaryAccessor = addMethod ?? removeMethod;
      if (primaryAccessor == null)
        return $"// Event {eventInfo.Name} has no accessible accessors";

      // Access modifiers
      if (primaryAccessor.IsPublic) sb.Append("public ");
      else if (primaryAccessor.IsFamily) sb.Append("protected ");
      else if (primaryAccessor.IsAssembly) sb.Append("internal ");
      else if (primaryAccessor.IsPrivate) sb.Append("private ");
      else if (primaryAccessor.IsFamilyOrAssembly) sb.Append("protected internal ");
      else if (primaryAccessor.IsFamilyAndAssembly) sb.Append("private protected ");

      // Additional modifiers
      if (primaryAccessor.IsStatic) sb.Append("static ");
      try
      {
        if (primaryAccessor.IsVirtual && !primaryAccessor.IsFinal && primaryAccessor.GetBaseDefinition() == primaryAccessor)
          sb.Append("virtual ");
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($@"{ex}");
      }
      if (primaryAccessor.IsAbstract) sb.Append("abstract ");
      try
      {
        if (primaryAccessor.IsVirtual && primaryAccessor.GetBaseDefinition() != primaryAccessor)
          sb.Append("override ");
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($@"{ex}");
      }
      // Event keyword
      sb.Append("event ");

      // Event handler type
      if (eventInfo.EventHandlerType != null)
      {
        sb.Append(GetFriendlyTypeName(eventInfo.EventHandlerType));
      }
      else
      {
        sb.Append("EventHandler");
      }

      sb.Append(" ");

      // Event name
      sb.Append(eventInfo.Name);

      sb.Append(";");

      return sb.ToString();
    }

    /// <summary>
    /// Generates a C# field declaration syntax string based on the specified field metadata.
    /// </summary>
    /// <remarks>The generated syntax includes access modifiers, field type, field name, and any applicable
    /// modifiers such as static, readonly, or const. For constant fields, the constant value is included.</remarks>
    /// <param name="field">The field metadata from which to generate the C# field declaration. Cannot be null.</param>
    /// <returns>A string representing the C# syntax for the specified field, including all modifiers, type, and name.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="field"/> is null.</exception>
    public static string GetFieldSyntax(FieldInfo field)
    {
      if (field == null)
        throw new ArgumentNullException(nameof(field));

      var sb = new StringBuilder();

      // Access modifiers
      if (field.IsPublic) sb.Append("public ");
      else if (field.IsFamily) sb.Append("protected ");
      else if (field.IsAssembly) sb.Append("internal ");
      else if (field.IsPrivate) sb.Append("private ");
      else if (field.IsFamilyOrAssembly) sb.Append("protected internal ");
      else if (field.IsFamilyAndAssembly) sb.Append("private protected ");

      // Additional modifiers
      if (field.IsStatic && !field.IsLiteral) sb.Append("static ");

      if (field.IsLiteral)
      {
        // const field
        sb.Append("const ");
      }
      else if (field.IsInitOnly)
      {
        // readonly field
        sb.Append("readonly ");
      }

      // Check for volatile
      var requiredModifiers = field.Attributes & FieldAttributes.FieldAccessMask;
      if ((field.Attributes & FieldAttributes.HasFieldRVA) != 0)
      {
        // This is a rough check - volatile fields don't have a direct attribute
        // but we can check custom attributes
        var volatileAttr = field.GetCustomAttributes(false)
          .Any(attr => attr.GetType().Name.Contains("Volatile"));
        if (volatileAttr) sb.Append("volatile ");
      }

      // Field type
      sb.Append(GetFriendlyTypeName(field.FieldType));
      sb.Append(" ");

      // Field name
      sb.Append(field.Name);

      // Constant value (for const fields)
      if (field.IsLiteral && field.GetRawConstantValue() != null)
      {
        sb.Append(" = ");
        var constantValue = field.GetRawConstantValue();

        if (constantValue == null)
          sb.Append("null");
        else if (constantValue is string strValue)
          sb.Append($"\"{strValue}\"");
        else if (constantValue is char charValue)
          sb.Append($"'{charValue}'");
        else if (constantValue is bool boolValue)
          sb.Append(boolValue.ToString().ToLower());
        else if (constantValue is float floatValue)
          sb.Append($"{floatValue}f");
        else if (constantValue is double doubleValue)
          sb.Append($"{doubleValue}d");
        else if (constantValue is decimal decimalValue)
          sb.Append($"{decimalValue}m");
        else if (constantValue is long longValue)
          sb.Append($"{longValue}L");
        else if (constantValue is uint uintValue)
          sb.Append($"{uintValue}u");
        else if (constantValue is ulong ulongValue)
          sb.Append($"{ulongValue}ul");
        else
          sb.Append(constantValue);
      }

      sb.Append(";");

      return sb.ToString();
    }

    /// <summary>
    /// Generates a C# method declaration syntax string based on the specified method metadata.
    /// </summary>
    /// <remarks>The generated syntax includes access modifiers, return type, method name, generic type parameters,
    /// and parameter list with types and names.</remarks>
    /// <param name="method">The method metadata from which to generate the C# method declaration. Cannot be null.</param>
    /// <returns>A string representing the C# syntax for the specified method, including all modifiers, return type,
    /// name, and parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> is null.</exception>
    public static string GetMethodSyntax(MethodInfo method)
    {
      if (method == null)
        throw new ArgumentNullException(nameof(method));

      var sb = new StringBuilder();

      // Access modifiers
      if (method.IsPublic) sb.Append("public ");
      else if (method.IsFamily) sb.Append("protected ");
      else if (method.IsAssembly) sb.Append("internal ");
      else if (method.IsPrivate) sb.Append("private ");
      else if (method.IsFamilyOrAssembly) sb.Append("protected internal ");
      else if (method.IsFamilyAndAssembly) sb.Append("private protected ");

      // Additional modifiers
      if (method.IsStatic) sb.Append("static ");
      if (method.IsAbstract) sb.Append("abstract ");
      else {
        var modifier = GetMethodInheritanceModifier(method);
        sb.Append(modifier+" ");
        if (method.Name.Contains ("GetDefinition"))
          System.Diagnostics.Trace.WriteLine($"Method: {method.Name} {method.DeclaringType.Name}, Modifier: {modifier}");
      }

      // Check for async methods (has AsyncStateMachineAttribute)
      //var isAsync = method.GetCustomAttributes(false)
      //  .Any(attr => attr.GetType().Name == "AsyncStateMachineAttribute");
      //if (isAsync) sb.Append("async ");

      // Return type
      sb.Append(GetFriendlyTypeName(method.ReturnType));
      sb.Append(" ");

      // Method name
      sb.Append(method.Name);

      // Generic type parameters
      if (method.IsGenericMethod)
      {
        var genericArgs = method.GetGenericArguments();
        sb.Append("<");
        sb.Append(string.Join(", ", genericArgs.Select(t => t.Name)));
        sb.Append(">");
      }

      // Parameters
      sb.Append("(");
      var parameters = method.GetParameters();

      sb.Append(string.Join(", ", parameters.Select((p, index) =>
      {
        var paramSb = new StringBuilder();

        // Parameter modifiers (ref, out, in, params)
        if (p.ParameterType.IsByRef)
        {
          if (p.IsOut)
            paramSb.Append("out ");
          else if (p.IsIn)
            paramSb.Append("in ");
          else
            paramSb.Append("ref ");
        }
          //else if (p.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
          //{
          //  paramSb.Append("params ");
          //}

        // this keyword for extension methods (first parameter of static method)
        //if (method.IsStatic && index == 0 && 
        //    method.GetCustomAttributes(false)
        //      .Any(attr => attr.GetType().Name == "ExtensionAttribute"))
        //{
        //  paramSb.Append("this ");
        //}

        // Parameter type
        var paramType = p.ParameterType;
        if (paramType.IsByRef)
          paramType = paramType.GetElementType()!;

        paramSb.Append(GetFriendlyTypeName(paramType));
        paramSb.Append(" ");
        paramSb.Append(p.Name ?? $"param{index + 1}");

        // Default value for optional parameters
        if (p.IsOptional && p.HasDefaultValue)
        {
          paramSb.Append(" = ");
          if (p.RawDefaultValue == null)  // Changed from p.DefaultValue
            paramSb.Append("null");
          else if (p.RawDefaultValue is string)  // Changed from p.DefaultValue
            paramSb.Append($"\"{p.RawDefaultValue}\"");  // Changed from p.DefaultValue
          else if (p.RawDefaultValue is bool)  // Changed from p.DefaultValue
            paramSb.Append(p.RawDefaultValue.ToString()!.ToLower());  // Changed from p.DefaultValue
          else
            paramSb.Append(p.RawDefaultValue);  // Changed from p.DefaultValue
        }

        return paramSb.ToString();
      })));

      sb.Append(");");

      return sb.ToString();
    }

    /// <summary>
    /// Generates a C# method inheritance modifier string based on the specified method metadata.
    /// </summary>
    /// <param name="method">The method metadata from which to generate the inheritance modifier. Cannot be null.</param>
    /// <returns>A string representing the C# inheritance modifier for the specified method, such as "override", "new", or "virtual".</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> is null.</exception>
    public static string GetMethodInheritanceModifier(MethodInfo method)
    {
      if (method == null) throw new ArgumentNullException(nameof(method));

      // 1) True override (including covariant return overrides)
      if (method.IsVirtual && method.GetBaseDefinition() != method)
        return "override";

      // 2) Hides base member => treat as "new" in generated syntax
      if (HidesBaseMethod(method))
        return "new";

      // 3) Virtual but not override
      if (method.IsVirtual && !method.IsFinal && !method.IsAbstract)
        return "virtual";

      return string.Empty;
    }

    private static bool HidesBaseMethod(MethodInfo method)
    {
      var declaringType = method.DeclaringType;
      var baseType = declaringType?.BaseType;
      if (baseType == null) return false;

      var flags = BindingFlags.Instance | BindingFlags.Static |
                  BindingFlags.Public | BindingFlags.NonPublic;

      var parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();

      // Same name + same parameter list in base => hidden member
      var baseMatch = baseType.GetMethod(method.Name, flags, binder: null, types: parameters, modifiers: null);
      return baseMatch != null;
    }

    /// <summary>
    /// Generates a C# property declaration syntax string based on the specified property metadata.
    /// </summary>
    /// <remarks>The generated syntax includes the appropriate access modifier, property type, property name,
    /// and accessor methods (get/set) with their respective access modifiers.</remarks>
    /// <param name="prop">The property metadata from which to generate the C# property declaration. Cannot be null.</param>
    /// <returns>A string representing the C# syntax for the specified property, including access modifier, type, name,
    /// and accessors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="prop"/> is null.</exception>
    public static string GetPropertySyntax(PropertyInfo prop)
    {
      if (prop == null)
        throw new ArgumentNullException(nameof(prop));

      var sb = new StringBuilder();

      // Get the getter/setter methods to determine access modifiers
      var getMethod = prop.GetGetMethod(true);
      var setMethod = prop.GetSetMethod(true);

      // Determine the primary access level (most accessible accessor)
      var primaryAccessor = getMethod ?? setMethod;
      if (primaryAccessor == null)
        return $"// Property {prop.Name} has no accessible accessors";

      // Primary access modifier
      if (primaryAccessor.IsPublic) sb.Append("public ");
      else if (primaryAccessor.IsFamily) sb.Append("protected ");
      else if (primaryAccessor.IsAssembly) sb.Append("internal ");
      else if (primaryAccessor.IsPrivate) sb.Append("private ");
      else if (primaryAccessor.IsFamilyOrAssembly) sb.Append("protected internal ");
      else if (primaryAccessor.IsFamilyAndAssembly) sb.Append("private protected ");

      // Additional modifiers
      if (primaryAccessor.IsStatic) sb.Append("static ");
      //if (primaryAccessor.IsVirtual && !primaryAccessor.IsFinal && primaryAccessor.GetBaseDefinition() == primaryAccessor)
      //  sb.Append("virtual ");
      if (primaryAccessor.IsAbstract) sb.Append("abstract ");
      //if (primaryAccessor.IsVirtual && primaryAccessor.GetBaseDefinition() != primaryAccessor)
      //  sb.Append("override ");

      // Property type and name
      sb.Append(GetFriendlyTypeName(prop.PropertyType));
      sb.Append(" ");
      sb.Append(prop.Name);
      sb.Append(" { ");

      // Accessors
      if (getMethod != null && (getMethod.IsPublic || getMethod.IsFamily || getMethod.IsAssembly || getMethod.IsFamilyOrAssembly))
      {
        // Add access modifier if different from primary
        if (getMethod != primaryAccessor && !AreAccessorsEqual(getMethod, primaryAccessor))
        {
          sb.Append(GetAccessorModifier(getMethod, primaryAccessor));
        }
        sb.Append("get; ");
      }

      if (setMethod != null && (setMethod.IsPublic || setMethod.IsFamily || setMethod.IsAssembly || setMethod.IsFamilyOrAssembly))
      {
        // Add access modifier if different from primary
        if (setMethod != primaryAccessor && !AreAccessorsEqual(setMethod, primaryAccessor))
        {
          sb.Append(GetAccessorModifier(setMethod, primaryAccessor));
        }
        sb.Append("set; ");
      }

      sb.Append("}");

      return sb.ToString().Trim();
    }

    /// <summary>
    /// Determines the access modifier string for an accessor if it differs from the primary accessor.
    /// </summary>
    private static string GetAccessorModifier(MethodInfo accessor, MethodInfo primaryAccessor)
    {
      if (accessor.IsPrivate) return "private ";
      if (accessor.IsFamily) return "protected ";
      if (accessor.IsAssembly) return "internal ";
      if (accessor.IsFamilyAndAssembly) return "private protected ";
      return "";
    }

    /// <summary>
    /// Checks if two accessors have the same access level.
    /// </summary>
    private static bool AreAccessorsEqual(MethodInfo accessor1, MethodInfo accessor2)
    {
      return accessor1.IsPublic == accessor2.IsPublic &&
             accessor1.IsPrivate == accessor2.IsPrivate &&
             accessor1.IsFamily == accessor2.IsFamily &&
             accessor1.IsAssembly == accessor2.IsAssembly &&
             accessor1.IsFamilyOrAssembly == accessor2.IsFamilyOrAssembly &&
             accessor1.IsFamilyAndAssembly == accessor2.IsFamilyAndAssembly;
    }

    /// <summary>
    /// Generates a C# constructor declaration syntax string based on the specified constructor metadata.Generates a C#-style constructor declaration from ConstructorInfo
    /// </summary>
    /// <remarks>The generated syntax includes the appropriate access modifier, constructor name, parameter
    /// list with types and default values, and an empty body. For static constructors, the output is marked
    /// accordingly.</remarks>
    /// <param name="ctor">The constructor metadata from which to generate the C# constructor declaration. Cannot be null.</param>
    /// <returns>A string representing the C# syntax for the specified constructor, including access modifier, name, parameters,
    /// and an empty body.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctor"/> is null.</exception>
    public static string GetConstructorSyntax(ConstructorInfo ctor)
    {
      if (ctor == null)
        throw new ArgumentNullException(nameof(ctor));

      var sb = new StringBuilder();

      // Access modifier
      if (ctor.IsPublic) sb.Append("public ");
      else if (ctor.IsFamily) sb.Append("protected ");
      else if (ctor.IsAssembly) sb.Append("internal ");
      else if (ctor.IsPrivate) sb.Append("private ");
      else if (ctor.IsFamilyOrAssembly) sb.Append("protected internal ");

      // Static constructor check
      if (ctor.IsStatic)
      {
        sb.Append(ctor.DeclaringType!.Name);
        sb.Append("() { } // static constructor");
        return sb.ToString();
      }

      // Constructor name
      sb.Append(ctor.DeclaringType!.Name);
      sb.Append("(");

      // Parameters
      var parameters = ctor.GetParameters();
      sb.Append(string.Join(", ", parameters.Select(p =>
      {
        var paramType = GetFriendlyTypeName(p.ParameterType);
        var paramStr = $"{paramType} {p.Name}";
        // can't reliably get default value for parameters, so we will just put "null" for optional parameters

        if (p.IsOptional)
          paramStr += " = " + (p.RawDefaultValue ?? "null");
        return paramStr;
      })));

      sb.Append(")");

      // Empty body for syntax representation
      sb.Append(" { }");

      return sb.ToString();
    }

    /// <summary>
    /// Converts a <see cref="Type"/> instance to its C#-friendly type name, including support for generics and arrays.Converts a Type to a C#-friendly name (handles generics, arrays, etc.)
    /// </summary>
    /// <remarks>Common .NET types such as Int32, String, Boolean, Object, and Void are mapped to their C#
    /// keyword equivalents. Generic types and arrays are formatted using standard C# syntax.</remarks>
    /// <param name="type">The type to convert to a C#-friendly name. Cannot be null.</param>
    /// <returns>A string representing the C#-friendly name of the specified type. For example, returns "List<int>" for a generic
    /// list of integers.</returns>
    private static string GetFriendlyTypeName(Type type)
    {
      if (type.IsGenericType && type.Name.IndexOf('`') >= 0)
      {
        var genericTypeName = type.Name.Substring(0, type.Name.IndexOf('`'));
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
        return $"{genericTypeName}<{genericArgs}>";
      }
      else if (type.IsArray)
      {
        return GetFriendlyTypeName(type.GetElementType()!) + "[]";
      }
      else
      {
        return type.Name switch
        {
          "Int32" => "int",
          "String" => "string",
          "Boolean" => "bool",
          "Object" => "object",
          "Void" => "void",
          _ => type.Name
        };
      }
    }

  }

  public static class VbSyntaxGenerator
  {
    /// <summary>
    /// Generates a VB.NET-style constructor call string for the given ConstructorInfo.
    /// Example output: New Namespace.ClassName(ByVal param1 As Integer, ByVal param2 As String)
    /// </summary>
    /// <remarks>The generated syntax includes the appropriate access modifier, constructor name, parameter
    /// list with types and default values, and an empty body. For static constructors, the output is marked
    /// accordingly.</remarks>
    /// <param name="ctor">The constructor metadata from which to generate the VB.NET constructor declaration. Cannot be null.</param>
    /// <returns>A string representing the VB.NET syntax for the specified constructor, including access modifier, name, parameters,
    /// and an empty body.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctor"/> is null.</exception>
    public static string GetConstructorSyntax(ConstructorInfo ctor)
    {
      if (ctor == null)
        throw new ArgumentNullException(nameof(ctor));

      Type declaringType = ctor.DeclaringType ?? throw new InvalidOperationException("Constructor has no declaring type.");

      // Build the VB.NET "New" syntax
      StringBuilder sb = new StringBuilder();
      sb.Append("New ");
      sb.Append(GetVBTypeName(declaringType));

      // Append parameters
      ParameterInfo[] parameters = ctor.GetParameters();
      sb.Append("(");
      sb.Append(string.Join(", ", parameters.Select((p, i) =>
          $"ByVal {p.Name ?? $"param{i + 1}"} As {GetVBTypeName(p.ParameterType)}"
      )));
      sb.Append(")");

      return sb.ToString();
    }

    /// <summary>
    /// Converts a System.Type to a VB.NET-friendly type name.
    /// Handles generics, arrays, and built-in type aliases.
    /// </summary>
    static string GetVBTypeName(Type type)
    {
      if (type == null) return "Object";

      // VB.NET aliases for common types
      if (type == typeof(int)) return "Integer";
      if (type == typeof(string)) return "String";
      if (type == typeof(bool)) return "Boolean";
      if (type == typeof(double)) return "Double";
      if (type == typeof(float)) return "Single";
      if (type == typeof(object)) return "Object";
      if (type == typeof(void)) return "Void";
      if (type == typeof(decimal)) return "Decimal";
      if (type == typeof(byte)) return "Byte";
      if (type == typeof(char)) return "Char";
      if (type == typeof(long)) return "Long";
      if (type == typeof(short)) return "Short";

      // Handle arrays
      if (type.IsArray)
        return $"{GetVBTypeName(type.GetElementType())}()";

      // Handle generics
      if (type.IsGenericType)
      {
        string baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
        string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetVBTypeName));
        return $"{baseName}(Of {genericArgs})";
      }

      // Default: use full name without assembly info
      return type.FullName ?? type.Name;
    }
  }

}
