using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace MyXmlDoc
{
	public class LookupComments
  {
    public static LookupComments? Current { get; set; }

    public static Assembly DefaultAssembly { get; private set; }

    public LookupComments(Assembly defaultAssembly)
    {
      DefaultAssembly = defaultAssembly;
      Current = this;
    }

    public XMLDocMemberInfo GetMemberDocumentation(object memberInfoOrNamespace)
    {
      XMLDocMemberInfo xmlDocMemberInfo = new();
      if (memberInfoOrNamespace == null)
      {
        System.Diagnostics.Trace.WriteLine("Stop: memberInfoOrNamespace is null");
      }
      var xmlDocMemberName = MemberHelper.ToXmlDocMemberName(memberInfoOrNamespace);
      if (string.IsNullOrEmpty(xmlDocMemberName))
      {
        System.Diagnostics.Trace.WriteLine("Stop: xmlDocMemberName is null or empty");
      }
      var xmlNode = FindInLookupComments(DefaultAssembly, xmlDocMemberName);
      if (xmlNode == null) return xmlDocMemberInfo;
      var sumaryNode = xmlNode.SelectSingleNode("summary");
      var summary = string.Empty;
      if (sumaryNode != null) 
      {
        summary = sumaryNode.InnerXml.Trim();
        if (summary != null && !string.IsNullOrEmpty(summary) && summary.Length > 0)
        {
          if (summary.Contains("<see "))
            System.Diagnostics.Trace.WriteLine(summary);
          summary = MdxUtil.SeeTagConverter.ReplaceSeeTagsWithMarkdown(summary);
          // remove all new lines and tabs
          summary = Regex.Replace(summary, @"\r\n?|\n|\t", " ");
        }
      }
      var remarks = UnescapeXml(xmlNode.SelectSingleNode("remarks")?.InnerXml.Trim() ?? string.Empty).Trim();
      if (!string.IsNullOrEmpty(remarks) && remarks.Length > 0)
      {
        remarks = MdxUtil.SeeTagConverter.ReplaceSeeTagsWithMarkdown(remarks);
      }
      var returns = UnescapeXml(xmlNode.SelectSingleNode("returns")?.InnerXml.Trim() ?? string.Empty).Trim();
      if (!string.IsNullOrEmpty(returns) && returns.Length > 0)
      {
        returns = MdxUtil.SeeTagConverter.ReplaceSeeTagsWithMarkdown(returns);
      }
      var parameters = new List<(string Name, string Description)>();
      var selectedParams = xmlNode.SelectNodes("param");
      if (selectedParams != null)
      {
        foreach (XmlNode paramNode in selectedParams)
        {
          var name = paramNode.Attributes?["name"]?.Value ?? string.Empty;
          var description = paramNode.InnerText.Trim();
          parameters.Add((name, description));
        }
        xmlDocMemberInfo.Parameters = parameters;
      }
      if (!string.IsNullOrEmpty(summary))
      {
        xmlDocMemberInfo.Summary = summary;
      }
      xmlDocMemberInfo.Remarks = remarks;
      xmlDocMemberInfo.Returns = returns;
      return xmlDocMemberInfo;
    }

    public static string UnescapeXml(string input)
    {
      if (string.IsNullOrEmpty(input)) return input;
      return System.Net.WebUtility.HtmlDecode(input);
    }

    private readonly Dictionary<string, Dictionary<string, XmlNode?>> DicAssemblyMemberLookup = new();

    public XmlNode? FindInLookupComments(Assembly assembly, string memberName)
    {
      if (assembly == null) return null;
      if (string.IsNullOrEmpty(assembly.FullName)) return null;
      if (!DicAssemblyMemberLookup.ContainsKey(assembly.FullName)) return null;
      if (!DicAssemblyMemberLookup[assembly.FullName].ContainsKey(memberName)) return null;
      return DicAssemblyMemberLookup[assembly.FullName][memberName];
    }

    internal void LoadMemberDictionary(Assembly assembly)
    {
      if (assembly.FullName == null) return;
      if (DicAssemblyMemberLookup.ContainsKey(assembly.FullName)) return;
      // the assembly wasn't loaded
      // we need to initialize the assembly
      var xmlDocument = DocumentationExtensions.XmlFromAssembly(assembly);
      if (!xmlDocument.HasChildNodes) return;
      var newDictionary = new Dictionary<string, XmlNode?>();
      // get all Members
      var docMembers = xmlDocument["doc"]?["members"];
      if (docMembers == null) return;
      var itemNo = 0;
      foreach (XmlNode memberNode in docMembers.ChildNodes)
      {
        var attribs = memberNode.Attributes;
        if (attribs == null) continue;
        var fullName = attribs["name"]?.Value;
        if (fullName == null) continue;
        //if (fullName.Contains("#ctor")
        //     && fullName.Contains('{')
        //     && fullName.Contains('}'))
        //{
        //  // replace the string between { and } with ''1
        //  fullName = Regex.Replace(fullName, @"\{.*?\}", @"''1");
        //}
        itemNo++;
        newDictionary.Add(fullName, memberNode);
      }
      DicAssemblyMemberLookup.Add(assembly.FullName, newDictionary);
    }
  }

  public class XMLDocMemberInfo
  {
    public string Summary { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Returns { get; set; } = string.Empty;
    public List<(string Name, string Description)> Parameters { get; set; } = [];
    public List<(string Description, string Cref, string Code)> ExampleCode { get; set; } = [];
  }

  /// <summary>
  /// Utility class to provide documentation for various types where available with the assembly
  /// example usage:
  /// var typeSummary = typeof([Type Name]).GetSummary();
  /// var methodSummary = typeof([Type Name]).GetMethod("[Method Name]").GetSummary();
  /// </summary>
  public static class DocumentationExtensions
  {
    /// <summary>
    /// Provides the documentation comments for a specific method
    /// </summary>
    /// <param name="methodInfo">The MethodInfo (reflection data) of the member to find documentation for</param>
    /// <returns>The XML fragment describing the method</returns>
    public static XmlNode? GetDocumentation(this MethodInfo methodInfo)
    {
      // Calculate the parameter string as this is in the member name in the XML
      var parametersString = "";
      foreach (var parameterInfo in methodInfo.GetParameters())
      {
        if (parametersString.Length > 0)
        {
          parametersString += ",";
        }
        parametersString += parameterInfo.ParameterType.FullName;
      }
      //AL: 15.04.2008 ==> BUG-FIX remove “()” if parametersString is empty
#pragma warning disable CS8604 // Possible null reference argument.
      if (parametersString.Length > 0)
        return XmlFromName(methodInfo.DeclaringType, 'M', methodInfo.Name + "(" + parametersString + ")");
      else
        return XmlFromName(methodInfo.DeclaringType, 'M', methodInfo.Name);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    /// <summary>
    /// Provides the documentation comments for a specific member
    /// </summary>
    /// <param name="memberInfo">The MemberInfo (reflection data) or the member to find documentation for</param>
    /// <param name="throwExceptions">if true throw exception if search fails</param>
    /// <returns>The XML fragment describing the member</returns>
    public static XmlNode? GetDocumentation(this MemberInfo memberInfo, bool throwExceptions)
    {
      try
      {
        // First character [0] of member type is prefix character in the name in the XML
        var memberName = XmlDocMemberNameBuilder.GetXmlDocMemberName(memberInfo);
        var memberType = memberName[0];
        var declarationType = memberInfo.DeclaringType;
        if (declarationType == null)
        {
          declarationType = memberInfo as Type;
        }
        var xmlMemberNode = XmlFromName(declarationType, memberType, memberName);
        return xmlMemberNode;
      }
      catch (Exception)
      {
        if (throwExceptions) throw;
        return null;
      }
    }

    /// <summary>
    /// Returns the Xml documentation summary comment for this member
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <returns></returns>
    public static string GetSummary(this MemberInfo memberInfo)
    {
      var element = memberInfo.GetDocumentation(false);
      var summaryElm = element?.SelectSingleNode("summary");
      if (summaryElm == null) return "";
      return summaryElm.InnerText.Trim();
    }

    /// <summary>
    /// Returns true if Xml documentation has the exclude tag comment for this member
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <returns>True if tag exists</returns>
    public static bool HasExcludeTag(this MemberInfo memberInfo)
    {
      if (memberInfo == null) return true;
			var element = memberInfo.GetDocumentation(false);
      var excludeElm = element?.SelectSingleNode("exclude");
      return excludeElm != null;
    }

    /// <summary>
    /// Returns true if Xml documentation has the exclude tag comment for this member
    /// </summary>
    /// <param name="typeInfo"></param>
    /// <returns>True if tag exists</returns>
    public static bool HasExcludeTag(this TypeInfo typeInfo)
    {
      var element = typeInfo.GetDocumentation(false);
      var excludeElm = element?.SelectSingleNode("exclude");
      return excludeElm != null;
    }

    /// <summary>
    /// Provides the documentation comments for a specific type
    /// </summary>
    /// <param name="type">Type to find the documentation for</param>
    /// <returns>The XML fragment that describes the type</returns>
    public static XmlNode? GetDocumentation(this Type type)
    {
      // Prefix in type names is T
      return XmlFromName(type, 'T', "");
    }

    /// <summary>
    /// Gets the summary portion of a type's documentation or returns an empty string if not available
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetSummary(this Type type)
    {
      var element = type.GetDocumentation(true);
      var summaryElm = element?.SelectSingleNode("summary");
      if (summaryElm == null) return "";
      return summaryElm.InnerText.Trim();
    }

    /// <summary>
    /// Returns true if Xml documentation has the exclude tag comment for this member
    /// </summary>
    /// <param name="type"></param>
    /// <returns>True if exclude tag exists</returns>
    public static bool HasExcludeTag(this Type type)
    {
      var element = type.GetDocumentation(false);
      var excludeElm = element?.SelectSingleNode("exclude");
      return excludeElm != null;
    }

    /// <summary>
    /// Obtains the XML Element that describes a reflection element by searching the 
    /// members for a member that has a name that describes the element.
    /// </summary>
    /// <param name="type">The type or parent type, used to fetch the assembly</param>
    /// <param name="prefix">The prefix as seen in the name attribute in the documentation XML</param>
    /// <param name="name">Where relevant, the full name qualifier for the element</param>
    /// <returns>The member that has a name that describes the specified reflection element</returns>
    private static XmlNode? XmlFromName(this Type type, char prefix, string searchName)
    {
      
      // make sure that the assembly is cached
      if (LookupComments.Current == null) throw new Exception($@"LookupComment Current is null");
      Assembly containingAssembly = type.Assembly;
      LookupComments.Current.LoadMemberDictionary(containingAssembly);

      // check if we have the full name in our Dictionary
      var findMember = LookupComments.Current.FindInLookupComments(containingAssembly, searchName);
      if (findMember == null)
      {
        if (!containingAssembly.FullName.StartsWith ("ArcGIS.") ) return null;
      }
      return findMember;
    }

    /// <summary>
    /// A cache used to remember Xml documentation for assemblies
    /// </summary>
    private static readonly Dictionary<Assembly, XmlDocument> Cache = new Dictionary<Assembly, XmlDocument>();

    /// <summary>
    /// A cache used to store failure exceptions for assembly lookups
    /// </summary>
    private static readonly Dictionary<Assembly, Exception> FailCache = new Dictionary<Assembly, Exception>();

    /// <summary>
    /// Obtains the documentation file for the specified assembly
    /// </summary>
    /// <param name="assembly">The assembly to find the XML document for</param>
    /// <returns>The XML document</returns>
    /// <remarks>This version uses a cache to preserve the assemblies, so that 
    /// the XML file is not loaded and parsed on every single lookup</remarks>
    public static XmlDocument XmlFromAssembly(this Assembly assembly)
    {
      if (FailCache.ContainsKey(assembly))
      {
        throw FailCache[assembly];
      }
      try
      {
        if (!Cache.ContainsKey(assembly))
        {
          // load the docuemnt into the cache
          Cache[assembly] = XmlFromAssemblyNonCached(assembly);
        }
        return Cache[assembly];
      }
      catch (Exception exception)
      {
        FailCache[assembly] = exception;
        throw;
      }
    }

    /// <summary>
    /// Loads and parses the documentation file for the specified assembly
    /// </summary>
    /// <param name="assembly">The assembly to find the XML document for</param>
    /// <returns>The XML document</returns>
    private static XmlDocument XmlFromAssemblyNonCached(Assembly assembly)
    {
      var assemblyFilename = assembly.Location;
      if (!string.IsNullOrEmpty(assemblyFilename))
      {
        StreamReader streamReader;
        try
        {
          var xmlPath = Path.ChangeExtension(assemblyFilename, ".xml");
          if (!File.Exists(xmlPath)) return new XmlDocument();
          streamReader = new StreamReader(xmlPath);
        }
        catch (FileNotFoundException exception)
        {
          throw new Exception("XML documentation not present (make sure it is turned on in project properties when building)", exception);
        }

        var xmlDocument = new XmlDocument();
        xmlDocument.Load(streamReader);
        return xmlDocument;
      }
      else
      {
        throw new Exception("Could not ascertain assembly filename", null);
      }
    }
  }
}