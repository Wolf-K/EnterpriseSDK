using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MyReflection
{
  public enum KindType
  {
    Class,
    Enum,
    Struct,
    Interface,
    Delegate,
    Other,
    Namespace,
    Event,
    Field,
    Method,
    Property,
    Constructor
  }

  public class MyTypeBase
  {
    public MyTypeBase(Type? myType, bool processRecursive)
    {
      ReflectionType = myType;
      if (myType != null)
      {
        ReflectionTypeInfo = IntrospectionExtensions.GetTypeInfo(myType);
        MemberName = MyXmlDoc.MemberHelper.ToXmlDocMemberName(myType);
        var memberDoc = MyXmlDoc.LookupComments.Current?.GetMemberDocumentation(myType);
        Summary = memberDoc?.Summary ?? string.Empty;
        Remarks = memberDoc?.Remarks ?? string.Empty;
        Returns = memberDoc?.Returns ?? string.Empty;
      }
      Namespace = myType?.Namespace ?? string.Empty;
      KindOf = KindType.Other;
      IsAbstract = myType?.IsAbstract ?? false;
      IsSealed = myType?.IsSealed ?? false;
      IsPublic = myType?.IsPublic ?? true;
      GenericTypes = [];
      IsGenericType = myType?.IsGenericType ?? false;
      if (IsGenericType)
      {
        var genericTypes = myType?.GetGenericArguments();
        if (genericTypes != null && genericTypes.Length > 0)
        {
          foreach (var genericType in genericTypes)
          {
            GenericTypes.Add(new MyTypeBase(genericType, false));
          }
        }
      }
      Name = myType?.Name ?? string.Empty;
      FullName = myType?.FullName ?? myType?.Name ?? string.Empty;
      if (FullName.Contains("PublicKeyToken="))
        FullName = MyUtil.RemoveVersionSpecifics(FullName);
      GenericsName = myType?.GenericTypeString() ?? string.Empty;
      HasError = string.Empty;
      Members = [];
      EnumParameters = [];
      Type? baseType = myType?.BaseType;
      BaseTypeName = (baseType != null && !String.Equals(baseType.FullName, "System.Object", StringComparison.Ordinal))
                      ? baseType.FullName ?? string.Empty : string.Empty;
      //if (FullName.Contains("`"))
      //  System.Diagnostics.Trace.WriteLine(FullName);
      //while (FullName.Contains("`"))
      //{
      //  FullName = MyUtil.FixTemplateTypeSyntax(FullName);
      //}
      if (FullName.Contains("GetDefinition"))
        System.Diagnostics.Trace.WriteLine(FullName);
      if (FullName.Contains("PublicKeyToken="))
        throw new Exception(FullName);
      if (Name.Contains("PublicKeyToken="))
        throw new Exception(Name);
      //while (Name.Contains("`"))
      //{
      //  Name = MyUtil.FixTemplateTypeSyntax(Name);
      //}
    }

    public MyTypeBase(string? theNamespace)
    {
      ReflectionType = null;
      MemberName = MyXmlDoc.MemberHelper.ToXmlDocMemberName(theNamespace+'.');
      var memberDoc = MyXmlDoc.LookupComments.Current?.GetMemberDocumentation(theNamespace + '.');
      Summary = memberDoc?.Summary ?? string.Empty;
      Remarks = memberDoc?.Remarks ?? string.Empty;
      Returns = memberDoc?.Returns ?? string.Empty;
      Namespace = theNamespace;
      KindOf = KindType.Other;
      IsAbstract = false;
      IsSealed = false;
      IsPublic = true;
      GenericTypes = [];
      Name = theNamespace;
      FullName = theNamespace;
      if (FullName.Contains("PublicKeyToken="))
        FullName = MyUtil.RemoveVersionSpecifics(FullName);
      GenericsName = theNamespace;
      HasError = string.Empty;
      Members = [];
      EnumParameters = [];
      Type? baseType = null;
      BaseTypeName = (baseType != null && !String.Equals(baseType.FullName, "System.Object", StringComparison.Ordinal))
                      ? baseType.FullName ?? string.Empty : string.Empty;
      //if (FullName.Contains("`"))
      //  System.Diagnostics.Trace.WriteLine(FullName);
      //while (FullName.Contains("`"))
      //{
      //  FullName = MyUtil.FixTemplateTypeSyntax(FullName);
      //}
      if (FullName.Contains("GetDefinition"))
        System.Diagnostics.Trace.WriteLine(FullName);
      if (FullName.Contains("PublicKeyToken="))
        throw new Exception(FullName);
      if (Name.Contains("PublicKeyToken="))
        throw new Exception(Name);
      //while (Name.Contains("`"))
      //{
      //  Name = MyUtil.FixTemplateTypeSyntax(Name);
      //}
    }

    // if the type is a namespace, then ReflectionType will be null and the information will be populated from the list of types in that namespace
    public Type? ReflectionType { get; private set; }
    public TypeInfo? ReflectionTypeInfo { get; private set; }
    public bool IsPublic { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public bool IsGenericType { get; set; }
    public string StaticOrNot => (IsAbstract && IsSealed ? "static " : String.Empty);
    public string Namespace { get; set; }
    public KindType KindOf { get; set; }

    public string? MemberName { get; private set; }

    public string CSharpSyntax { get; set; } = string.Empty;
    public string Name { get; set; }
    public string FullName { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Returns { get; set; } = string.Empty;
    public string GenericsName { get; set; }
    public string BaseTypeName { get; set; }
    public List<MyMember> Members { get; set; }
    public string? ReturnType { get; set; }
    public string HasError { get; set; }
    public List<MyMemberEnumField> EnumParameters { get; private set; }
    public List<MyTypeBase> GenericTypes { get; private set; }

    public override string ToString()
    {
      return $@"{(IsPublic ? "public " : String.Empty)}{StaticOrNot}{KindOf.ToString().ToLower()} {FullName}";

      //return $@"{(IsPublic ? "public " : String.Empty)}{FullName}";
    }

    public static MyTypeBase? GetTypeBaseFromTypeInfo(TypeInfo t, bool processRecursive)
    {
      MyTypeBase TypeBase = new(t, false);
      try
      {
        if (TypeBase.ToString().Contains("UNITTYPECODE", StringComparison.OrdinalIgnoreCase))
          System.Diagnostics.Trace.WriteLine(TypeBase.ToString());
        if (t.IsDelegate())
        {
          TypeBase = new MyDelegate(t, false);
        }
        else if (t.IsClass)
        {
          //System.Diagnostics.Trace.Write("class ");
          TypeBase = new MyClass(t, processRecursive);
        }
        else if (t.IsValueType)
        {
          Type? baseType = t.BaseType;
          if (String.Equals(baseType?.FullName, "System.Enum", StringComparison.InvariantCulture))
          {
            //System.Diagnostics.Trace.Write("enum ");
            TypeBase = new MyEnum(t, processRecursive);
          }
          else
          {
            //System.Diagnostics.Trace.Write("struct ");
            TypeBase = new MyStruct(t, processRecursive);
          }
        }
        else if (t.IsInterface)
        {
          //System.Diagnostics.Trace.Write("interface ");
          TypeBase = new MyInterface(t, processRecursive);
        }
        else
        {
          //System.Diagnostics.Trace.Write("other ");
          TypeBase = new MyOther(t, processRecursive);
        }
      }
      catch (Exception ex)
      {
        // We are missing the required dependency assembly.
        if (TypeBase != null)
          TypeBase.HasError = $@"GetMembers Error: {ex.Message}";
        throw;
      }
      return TypeBase;
    }

    public virtual void WriteMdx(string outputRoot, string namespaceName)
    {
      Console.WriteLine($@"WriteMdx: {outputRoot} {namespaceName} {FullName}");
    }

    public virtual void WriteXML(string outputRoot, string namespaceName)
    {
      Console.WriteLine($@"WriteXML: {outputRoot} {namespaceName} {FullName}");
    }

    public virtual List<XElement> CreateXML()
    {
      Console.WriteLine($@"CreateXML: {Name} base");
      return [];
    }


    /// <summary>
    /// The expected output should look like this under the member node:
    /// &lt;example&gt;
    /// codeTitle
    /// ```csharp
    /// code
    /// ```
    /// &lt;/example&gt;   
    /// </summary>
    /// <param name="sourceMember">the XML element representing the member</param>
    /// <param name="codeTitle">the title of the code snippet</param>
    /// <param name="code">the code snippet</param>
    /// <param name="language">csharp</param>
    internal static void MakeCodeNode(XElement sourceMember, string codeTitle, string code, string language = "csharp")
    {
      var exampleNode = new XElement("example");
      var titleNode = new XElement("codeTitle", codeTitle);
      exampleNode.Add(titleNode);
      var codeNode = new XElement("code", code);
      codeNode.SetAttributeValue("language", language);
      exampleNode.Add(codeNode);
      sourceMember.Add(exampleNode);
    }

    /// <summary>
    /// The expected output should look like this under the member node:
    /// &lt;remarks&gt;
    /// remarks
    /// &lt;/remarks&gt;   
    /// </summary>
    /// <param name="sourceMember">the XML element representing the member</param>
    /// <param name="remark">the remark text</param>
    internal static void MakeRemarkNode(XElement sourceMember, string remarks)
    {
      if (!string.IsNullOrEmpty(remarks))
      {
        var remarksNode = new XElement("remarks", remarks);
        sourceMember.Add(remarksNode);
      }
    }
  }
}
