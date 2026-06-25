using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SyntaxGenerator
{
  public class SyntaxMaker
  {
    static bool boolWeb;
    private static readonly IDictionary<string, Assembly> Assemblies = new Dictionary<string, Assembly>();
    private string _defaultPathDllPath;

    public SyntaxMaker (string defaultPathDllPath)
    {
      _defaultPathDllPath = defaultPathDllPath;
        //Resolve ArcGIS Pro assemblies.
        AppDomain currentDomain = AppDomain.CurrentDomain;
      currentDomain.AssemblyResolve += new ResolveEventHandler(ResolveProAssemblyPath);
    }
    
    /// <summary>
    /// Resolves the ArcGIS Pro Assembly Path.  Called when loading of an assembly fails.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <returns>programmatically loaded assembly in the pro /bin path</returns>
    private Assembly ResolveProAssemblyPath(object sender, ResolveEventArgs args)
    {
      string assemblyPath = Path.Combine(_defaultPathDllPath, new AssemblyName(args.Name).Name + ".dll");
      if (!File.Exists(assemblyPath)) return null;
      Assembly assembly = Assembly.LoadFrom(assemblyPath);
      return assembly;
    }

    private static Assembly GetAssembly(string assemblyName)
    {
      var aName = assemblyName.ToLower();
      if (!Assemblies.Keys.Contains(aName))
      {
        Assemblies.Add(aName, Assembly.LoadFrom(aName));
      }
      return Assemblies[aName];
    }

    public (string VbSyntax, string CSharpSyntax) GetMdMethodSyntax(string asmName, string typeName, string methodName)
    {
      var result = GetHtmlMethodSyntax(asmName, typeName, methodName);
      result = ConvertHtmlToMarkdown(result);
      return result;
    }

    public (string VbSyntax, string CSharpSyntax) GetHtmlMethodSyntax(string asmName, string typeName, string methodName)
    {
      string sVbSyntax = string.Empty;
      string sCsSyntax = string.Empty;
      Assembly a = GetAssembly(asmName);
      string fullTypeName = GetTypeName(asmName, typeName);
      Type t = GetTypeFromName(a, fullTypeName);
      if (t == null) throw (new Exception ($@"Error in GetMethodSyntax: {fullTypeName} was not found in assembly {a.FullName}"));
      try
      {
        // Get Method doesn't work if there are overloads so we need to use GetMethods and report all overloads instead
        var methods = t.GetMethods(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
          .Where(x => x.Name == methodName);
        //MethodInfo m = t.GetMethod(methodName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        foreach (var m in methods)
        {
          sVbSyntax += GetMethodSigVB(m) + "\r\n";
        }
        foreach (var m in methods)
        {
          sCsSyntax += GetMethodSigCS(m) + "\r\n";
        }
      }
      catch (Exception ex)
      {
        throw new Exception($@"GetMethod: {typeName} {fullTypeName} {methodName}", ex);
      }
      //			if (isForWebHelp){
      //				sSyntax += GetMethodSigCPP(m);}
      return new (sVbSyntax, sCsSyntax);
    }

    public (string VbSyntax, string CSharpSyntax) GetMdPropertySyntax(string asmName, string typeName, string methodName)
    {
      var result = GetHtmlPropertySyntax(asmName, typeName, methodName);
      result = ConvertHtmlToMarkdown(result);
      return result;
    }

    public (string VbSyntax, string CSharpSyntax) GetHtmlPropertySyntax(string asmName, string typeName, string propertyName)
    {
      (string VbSyntax, string CSharpSyntax) result = (string.Empty, string.Empty);
      Assembly a = GetAssembly(asmName);
      string fullTypeName = GetTypeName(asmName, typeName);
      Type t = GetTypeFromName(a, fullTypeName); 
      try
      {
        PropertyInfo p = t.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        result.VbSyntax = GetPropertySigVB(p) + "\r\n";
        result.CSharpSyntax = GetPropertySigCS(p) + "\r\n";
      }
      catch (Exception ex)
      {
        throw new Exception($@"GetProperty: {typeName} {fullTypeName} {propertyName}", ex);
      }
      return result;
    }

    public (string VbSyntax, string CSharpSyntax) GetMdEventSyntax(string asmName, string typeName, string eventName)
    {       
      var result = GetHtmlEventSyntax(asmName, typeName, eventName);
      result = ConvertHtmlToMarkdown(result);
      return result;
    }

    public (string VbSyntax, string CSharpSyntax) GetHtmlEventSyntax(string asmName, string typeName, string eventName)
    {
      (string VbSyntax, string CSharpSyntax) result = (string.Empty, string.Empty);

      Assembly a = GetAssembly(asmName);
      EventInfo? e = null;
      string fullTypeName = GetTypeName(asmName, typeName + "_Event");
      Type t = GetTypeFromName(a, fullTypeName);
      try
      {
        if (t == null) t = GetTypeFromName(a, GetTypeName(asmName, typeName));
        if (t == null)
        {

          throw new Exception($@"Error: Event not found: {asmName} {typeName} {eventName}");
        }
        else
        {
          e = t.GetEvent(eventName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
          result.VbSyntax = GetEventSigVB(e) + "\r\n";
          result.CSharpSyntax = GetEventSigCS(e) + "\r\n";
        }
        //			if (isForWebHelp){sSyntax[0] += GetEventSigCPP(e);}
        if (e == null)
        {
          throw (new Exception($@"N/A error in GetEventSyntax_N/A Event not found: {asmName} {typeName} {eventName}"));
        }
        else
        {
          MethodInfo? m = e?.EventHandlerType?.GetMethod("Invoke");
          if (m == null)
          {
            throw (new Exception($@"N/A error in GetEventSyntax_N/A EventHandlerType Invoke method not found: {asmName} {typeName} {eventName}"));
          }
          result = GetMdMethodSyntax(asmName, GetLastString(e.EventHandlerType.ToString(), "."), "Invoke");
          result.VbSyntax = result.VbSyntax.Replace("Invoke", GetLastString(e.EventHandlerType.ToString(), "_"));
          result.CSharpSyntax = result.CSharpSyntax.Replace("public", "public delegate");
          result.VbSyntax = result.VbSyntax.Replace("Public", "Public Delegate");
          //sSyntax[2] = GetLastString(e.EventHandlerType.ToString(), ".");
          //sSyntax[3] = e.EventHandlerType.Namespace.ToString();
        }
      }
      catch (Exception ex)
      {
        throw new Exception($@"GetEvent: {typeName} {fullTypeName} {eventName}", ex);
      }
      return result;
    }

    public (string VbSyntax, string CSharpSyntax) GetMemberSyntax(string asmName, string typeName, string memberName, string id)
    {
      (string VbSyntax, string CSharpSyntax) result = (string.Empty, string.Empty);
      string newPropName = "";
      Assembly a = GetAssembly(asmName);
      string fullTypeName = GetTypeName(asmName, typeName);
      Type t = GetTypeFromName(a, fullTypeName);
      if (t == null)
      {
        throw (new Exception($@"Error in GetMemberSyntax: {fullTypeName} was not found in assembly {a.FullName}"));
      }
      MemberInfo[] m = t.GetMember(memberName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
      if (m.Length == 0)
      {
        throw (new Exception($@"Error in GetMemberSyntax no type for {memberName}: {fullTypeName} was not found in assembly {a.FullName}"));
      }
      PropertyInfo[] pi2 = t.GetProperties();
      switch (m[0].MemberType.ToString())
      {
        case "Method":
          result = GetMdMethodSyntax(asmName, typeName, memberName);
          break;
        case "Property":
          newPropName = FindMatch(pi2, id);
          if (newPropName == "")
          {
            newPropName = memberName;
          }
          result = GetMdPropertySyntax(asmName, typeName, newPropName);
          break;
        case "Event":
          result = GetMdEventSyntax(asmName, typeName, memberName);
          break;
      }
      return result;
    }

    public (string VbSyntax, string CSharpSyntax) GetStructSyntax(string asmName, string typeName, string htmlCsTemplate, string htmlVbTemplate)
    {
      try
      {
        FieldInfo[] fis = null;
        string s = "";
        string s2 = "";
        (string VbSyntax, string CSharpSyntax) result = (string.Empty, string.Empty);
        string ft = "";
        string ft2 = "";
        Assembly a = GetAssembly(asmName);

        string fullTypeName = GetTypeName(asmName, typeName);
        Type t = GetTypeFromName(a, fullTypeName);
        string theTypeName = fullTypeName;
        if (t != null)
        {
          fis = t.GetFields();
          theTypeName = t.Name;
        }
        var csSyntax = "public " + theTypeName + " {" + "\r\n";
        var vbSyntax = "Public Structure " + theTypeName + "\r\n";
        if (fis != null)
        {
          foreach (FieldInfo fi in fis)
          {
            ft = fi.FieldType.ToString();
            ft2 = fi.FieldType.ToString();
            ft = GetHRef(ft.Remove(0, ft.LastIndexOf(".") + 1));
            ft2 = GetHRefVB(ft2.Remove(0, ft2.LastIndexOf(".") + 1));
            csSyntax = csSyntax + "&nbsp;&nbsp;&nbsp;" + ft + " " + fi.Name.ToString() + ",\r\n";
            vbSyntax = vbSyntax + "&nbsp;&nbsp;&nbsp;" + fi.Name.ToString() + " As " + ft2 + "\r\n";
          }
        }
        else
        {
          throw (new Exception($@"Structure not found: {asmName} {typeName}"));
        }
        int idx1 = csSyntax.LastIndexOf(",");

        csSyntax = (idx1 >= 0 ? csSyntax.Remove(idx1, 1) : csSyntax) + ")" + "\r\n";
        vbSyntax = vbSyntax + "End Structure" + "\r\n";

        result = (htmlVbTemplate.Replace ("$CodeSnippet$", vbSyntax), htmlCsTemplate.Replace("$CodeSnippet$", csSyntax));
        return (result);
      }
      catch (Exception)
      {
                throw;
            }
    }

    public string GetChangedName(string asmName, string typeName, string id2)
    {
      try
      {
        string newName = "";
        Assembly a = GetAssembly(asmName);
        string fullTypeName = GetTypeName(asmName, typeName);
        Type t = GetTypeFromName(a, fullTypeName);
        if (t == null) return $@"Error in GetChangedName: {fullTypeName} was not found in assembly {a.FullName}";
        PropertyInfo[] pi2 = t.GetProperties();
        newName = FindMatch(pi2, id2);
        return (newName);
      }
      catch (Exception)
      {
        Console.Error.WriteLine($"Error: can find type: {GetTypeName(asmName, typeName)} in {asmName}");
        return "";
      }
    }

    public string FindMatch(PropertyInfo[] pi, string id)
    {
      string s = "";
      foreach (PropertyInfo p in pi)
      {
        object[] attrs = p.GetCustomAttributes(typeof(DispIdAttribute), true);
        if (attrs.Length > 0)
        {
          DispIdAttribute dispIdAtt = attrs[0] as DispIdAttribute;
          //Match with MemberInfo.MemberId
          if (id == dispIdAtt.Value.ToString())
          {
            return (p.Name);
          }
        }
      }
      return (s);
    }

    public string GetValueTypeSyntax(string asmName, string typeName, bool isForWebHelp)
    {
      string sSyntax = "";
      Assembly a = GetAssembly(asmName);
      string fullTypeName = GetTypeName(asmName, typeName);
      Type t = GetTypeFromName(a, fullTypeName);
      if (t.IsValueType)
      {
        sSyntax = GetValueTypeSigVB(t) + "\r\n";
        sSyntax += GetValueTypeSigCS(t);
      }
      return (sSyntax);
    }

    #region VB syntax

    #region get Signatures for Properties, Methods, and Events - VB

    private string GetPropertySigVB(PropertyInfo p)
    {
      string sSyntax = "";

      MethodInfo[] accs;
      ParameterInfo[] parameters = p.GetIndexParameters();

      if (parameters.Length == 0)
      {
        accs = p.GetAccessors();
        if (accs.Length > 0)
        {
          MethodInfo mi = null;
          mi = p.GetGetMethod();
          if (mi == null)
          {
            mi = p.GetSetMethod();
          }
          sSyntax += GetMethodKeywordsVB(mi);

          if (p.CanRead && !p.CanWrite)
          {
            sSyntax += "ReadOnly ";
          }
          if (p.CanWrite && !p.CanRead)
          {
            sSyntax += "WriteOnly ";
          }
          sSyntax += "Property " + p.Name;
          if (mi.ReturnType.ToString() != "System.Void")
          {
            if (mi.ReturnType.IsPrimitive || mi.ReturnType.Namespace.ToString() == "System")
            {
              sSyntax += " As </b>" + GetHRefVB(GetCasedTypeVB(GetLastString(mi.ReturnType.ToString(), ".")));
            }
            else if (mi.ReturnType.ToString().StartsWith("ESRI"))
            {
              sSyntax += " As </b>" + GetESRIHRef(mi.ReturnType);
            }
            else
            {
              sSyntax += " As </b>" + mi.ReturnType.ToString();
            }
          }
        }
      }
      else
      {
        accs = p.GetAccessors();
        if (p.CanRead)
        {
          MethodInfo m = p.GetGetMethod();
          sSyntax += GetMethodSigVB(m);
        }

        if (p.CanWrite)
        {
          MethodInfo m = p.GetSetMethod();
          sSyntax += GetMethodSigVB(m);
        }
      }
      return (sSyntax);
    }

    private string GetMethodSigVB(MethodInfo m)
    {
      string sSyntax = "";
      string sLeft = "";
      string sRight = "";
      string sBy = "";

      sSyntax = GetMethodKeywordsVB(m);
      if (m == null)
      {
        sSyntax += "N/A Error";
      }
      else
      {
        if (m.MemberType == MemberTypes.Event)
        {
          sSyntax += "Event " + m.Name;
        }
        else if (m.ReturnType.ToString() == "System.Void")
        {
          sSyntax += "Sub " + m.Name + " ( _\r\n";
        }
        else
        {
          sSyntax += "Function " + m.Name + " ( _\r\n";
        }

        ParameterInfo[] parameters2 = m.GetParameters();
        foreach (ParameterInfo parameter in parameters2)
        {
          if (parameter.IsOptional)
          {
            sLeft = "[";
            sRight = "]";
          }
          else
          {
            sLeft = "";
            sRight = "";
          }
          if (parameter.ParameterType.IsByRef)
          {
            sBy = "ByRef";
          }
          else
          {
            sBy = "ByVal";
          }
          if (parameter.ParameterType.IsPrimitive || parameter.ParameterType.Namespace.ToString() == "System")
          {
            sSyntax += "&nbsp;&nbsp;&nbsp;&nbsp;" + sLeft + "<b>" + sBy + "</b>&nbsp;<i>" + parameter.Name + "</i><b>&nbsp;As&nbsp;</b>" + GetHRefVB(GetCasedTypeVB(GetLastString(parameter.ParameterType.ToString().Trim(), "."))) + sRight + ", _\r\n";
          }
          else if (parameter.ParameterType.Namespace.ToString().StartsWith("ESRI"))
          {
            sSyntax += "&nbsp;&nbsp;&nbsp;&nbsp;" + sLeft + "<b>" + sBy + "</b>&nbsp;<i>" + parameter.Name + "</i><b>&nbsp;As&nbsp;</b>" + GetESRIHRef(parameter.ParameterType) + sRight + ", _\r\n";
          }
          else
          {
            sSyntax += "&nbsp;&nbsp;&nbsp;&nbsp;" + sLeft + "<b>" + sBy + "</b>&nbsp;<i>" + parameter.Name + "</i><b>&nbsp;As&nbsp;</b>" + parameter.ParameterType.ToString() + sRight + ", _\r\n";
          }
        }
      }
      sSyntax = sSyntax.Trim();
      int pos = sSyntax.LastIndexOf(",");
      if (pos != -1)
        sSyntax = sSyntax.Remove(pos, 1);
      if (m == null)
      {
        sSyntax += "No return type\r\n<b>)";
      }
      else
      {
        if (m.ReturnType.ToString() != "System.Void")
        {
          if (m.MemberType == MemberTypes.Event)
          {
            sSyntax += "<b>)&nbsp;As</b>&nbsp;" + GetHRefVB(GetCasedTypeVB(GetLastString(m.ReturnType.ToString(), ".").Trim()));
          }
          else if (m.ReturnType.IsPrimitive || m.ReturnType.Namespace.ToString() == "System")
          {
            sSyntax += "\r\n<b>)&nbsp;As</b>&nbsp;" + GetHRefVB(GetCasedTypeVB(GetLastString(m.ReturnType.ToString(), ".").Trim()));
          }
          else if (m.ReturnType.ToString().StartsWith("ESRI"))
          {
            sSyntax += "\r\n<b>)&nbsp;As</b>&nbsp;" + GetESRIHRef(m.ReturnType);
          }
          else
          {
            sSyntax += "\r\n<b>)&nbsp;As</b>&nbsp;" + m.ReturnType.ToString();
          }
        }
        else
        {
          sSyntax += "\r\n<b>)</b>";
        }
      }
      return (sSyntax);
    }

    private string GetEventSigVB(EventInfo e)
    {
      string sSyntax = "";
      if (e == null)
      {
        sSyntax += "N/A Error in GetEventSigVB";
      }
      else
      {
        sSyntax += GetEventKeywordsVB(e);
        sSyntax += e.Name.ToString() + " As" + "</b> " + "<a href=" + '\u0022' + GetLastString(e.EventHandlerType.ToString(), "_") + ".htm" + '\u0022' + ">" + GetLastString(e.EventHandlerType.ToString(), "_") + "</a>";
      }
      return (sSyntax);
    }

    private string GetValueTypeSigVB(Type t)
    {
      string sSyntax = "";
      FieldInfo[] fis = t.GetFields();
      foreach (FieldInfo fi in fis)
      {
        if (fi.FieldType.ToString().StartsWith("ESRI"))
        {
          sSyntax += "\r\n" + fi.Name.ToString() + "&nbsp;<b>As</b>&nbsp;" + GetESRIHRef(fi.FieldType);
        }
        else
        {
          sSyntax += "\r\n" + fi.Name.ToString() + "&nbsp;<b>As</b>&nbsp;" + GetHRefVB(GetCasedTypeVB(GetLastString(fi.FieldType.ToString(), ".")));
        }
      }
      return (sSyntax);
    }
    #endregion

    #region get Keywords for Methods and Events - VB

    private string GetMethodKeywordsVB(MethodInfo m)
    {
      //			if (m.GetType().IsSerializable)
      //				sSyntax += "&lt;Serializable&gt;\r\n";
      string sSyntax = "<b>";
      //			if (m.IsAbstract)
      //				sSyntax += "MustOverride ";
      //			else if (m.IsVirtual && !m.IsFinal) 
      //			if (m.IsVirtual && !m.IsFinal) 
      //				sSyntax += "Overridable ";
      if (m == null) sSyntax += "N/A Error";
      else
      {
        if (m.IsStatic)
          sSyntax += "Shared ";
        if (m.IsPublic)
          sSyntax += "Public ";
        if (m.IsPrivate)
          sSyntax += "Private ";
      }
      return (sSyntax);
    }

    private string GetEventKeywordsVB(EventInfo e)
    {
      string sSyntax = "<b>";
      //			if (e.EventHandlerType.IsAbstract)
      //				sSyntax += "MustOverride ";
      if (e == null)
      {
        sSyntax += "N/A Error in GetEventKeywordsVB";
      }
      else
      {
        if (e.EventHandlerType.IsPublic)
          sSyntax += "Public ";
        if (!e.EventHandlerType.IsPublic)
          sSyntax += "Private ";
        if (e.MemberType == MemberTypes.Event)
          sSyntax += "Event ";
      }
      return (sSyntax);
    }
    #endregion

    private string GetCasedTypeVB(string s)
    {
      switch (s)
      {
        case "Void":
          s = "";
          break;
        case "Int16":
          s = "Short";
          break;
        case "Int32":
          s = "Integer";
          break;
        case "Int64":
          s = "Long";
          break;
        case "Float":
          s = "Single";
          break;
      }
      return (s);
    }


    #endregion

    #region C# syntax

    #region Get Signature for Properties, Methods, Events - C#

    private string GetPropertySigCS(PropertyInfo p)
    {
      string sSyntax = "";

      MethodInfo[] accs;
      ParameterInfo[] parameters = p.GetIndexParameters();

      if (parameters.Length == 0)
      {
        accs = p.GetAccessors();
        if (accs.Length > 0)
        {
          MethodInfo mi = null;
          mi = p.GetGetMethod();
          if (mi == null)
            mi = p.GetSetMethod();
          sSyntax += GetMethodKeywordsCS(mi);
          if (mi.ReturnType.IsPrimitive || mi.ReturnType.Namespace.ToString() == "System")
          {
            sSyntax += GetHRef(GetCasedTypeCS(GetLastString(mi.ReturnType.ToString(), "."))) + " ";
          }
          else if (mi.ReturnType.ToString().StartsWith("ESRI"))
          {
            sSyntax += GetESRIHRef(mi.ReturnType) + " ";
          }
          else
          {
            sSyntax += mi.ReturnType.ToString() + " ";
          }
          sSyntax += p.Name;
          sSyntax += " {";

          if (p.CanRead)
          {
            sSyntax += "get; ";
          }
          if (p.CanWrite)
          {
            sSyntax += "set;";
          }
          sSyntax = sSyntax.Trim();
        }
        sSyntax += "}";

      }
      else
      {
        accs = p.GetAccessors();
        if (p.CanRead)
        {
          MethodInfo m = p.GetGetMethod();
          sSyntax += GetMethodSigCS(m);

        }

        if (p.CanWrite)
        {
          MethodInfo m = p.GetSetMethod();
          sSyntax += GetMethodSigCS(m);
        }
      }
      return (sSyntax);
    }


    private string GetMethodSigCS(MethodInfo m)
    {
      string sSyntax = "";
      string sRef = "";

      if (m == null)
      {
        sSyntax = "N/A Error in GetMethodSigCS";
      }
      else
      {
        sSyntax = GetMethodKeywordsCS(m);
        if (m.ReturnType.IsPrimitive || m.ReturnType.Namespace.ToString() == "System")
        {
          sSyntax += GetHRef(GetCasedTypeCS(GetLastString(m.ReturnType.ToString(), ".")));
        }
        else if (m.ReturnType.ToString().StartsWith("ESRI"))
        {
          sSyntax += GetESRIHRef(m.ReturnType);
        }
        else
        {
          sSyntax += m.ReturnType.ToString();
        }
        sSyntax += "<b> " + m.Name + " (</b>\r\n";
        ParameterInfo[] parameters2 = m.GetParameters();
        foreach (ParameterInfo parameter in parameters2)
        {
          if (parameter.ParameterType.IsByRef)
          {
            sRef = "ref ";
          }

          if (parameter.ParameterType.IsPrimitive || parameter.ParameterType.Namespace.ToString() == "System")
          {
            sSyntax += "&nbsp;&nbsp;&nbsp;&nbsp;" + sRef + GetHRef(GetCasedTypeCS(GetLastString(parameter.ParameterType.ToString(), ".").Trim())) + " <i>" + parameter.Name + ",</i>\r\n";
          }
          else if (parameter.ParameterType.Namespace.StartsWith("ESRI"))
          {
            sSyntax += "&nbsp;&nbsp;&nbsp;&nbsp;" + sRef + GetESRIHRef(parameter.ParameterType) + " <i>" + parameter.Name + ",</i>\r\n";
          }
          else
          {
            sSyntax += "&nbsp;&nbsp;&nbsp;&nbsp;" + parameter.ParameterType.ToString() + " <i>" + parameter.Name + ",</i>\r\n";
          }
        }
        sSyntax = sSyntax.Trim();
        int pos = sSyntax.LastIndexOf(",");
        if (pos != -1)
        {
          sSyntax = sSyntax.Remove(pos, 1);
          sSyntax += "\r\n);";
        }
        else
        {
          sSyntax += "\r\n);";
        }
      }
      return (sSyntax);
    }

    private string GetEventSigCS(EventInfo e)
    {
      string sSyntax = "";
      if (e == null)
      {
        sSyntax = "N/A Error in GetEventSigCS";
      }
      else
      {
        sSyntax += GetEventKeywordsCS(e);
        sSyntax += " " + "<a href=" + '\u0022' + GetLastString(e.EventHandlerType.ToString(), "_") + ".htm" + '\u0022' + ">" + GetLastString(e.EventHandlerType.ToString(), "_") + "</a> <b>" + e.Name.ToString() + "</b>";
      }
      return (sSyntax);
    }


    private string GetValueTypeSigCS(Type t)
    {
      string sSyntax = "";
      FieldInfo[] fis = t.GetFields();
      foreach (FieldInfo fi in fis)
      {
        if (fi.FieldType.ToString().StartsWith("ESRI"))
        {
          sSyntax += "&nbsp;&nbsp;&nbsp;&nbsp;" + GetESRIHRef(fi.FieldType) + " <i>" + fi.Name.ToString() + ",</i>\r\n";
        }
        else
        {
          sSyntax += "&nbsp;&nbsp;&nbsp;&nbsp;" + GetHRef(GetCasedTypeCS(GetLastString(fi.FieldType.ToString(), "."))) + " <i>" + fi.Name.ToString() + ",</i>\r\n";
        }
      }
      sSyntax = sSyntax.Trim();
      int pos = sSyntax.LastIndexOf(",");
      if (pos != -1)
      {
        sSyntax = sSyntax.Remove(pos, 1);
      }
      return (sSyntax);
    }
    #endregion

    #region Get Keywords for Methods and Events - C#

    private string GetMethodKeywordsCS(MethodInfo m)
    {
      string sSyntax = string.Empty;
      if (m == null)
      {
        sSyntax += "N/A Error ";
      }
      else
      {
        if (m.IsPublic)
          sSyntax += "public ";
        if (m.IsPrivate)
          sSyntax += "private ";
        //			if (m.IsAbstract)
        //				sSyntax += "abstract ";
        //			else if (m.IsVirtual && !m.IsFinal) 
        //			if (m.IsVirtual && !m.IsFinal) 
        //				sSyntax += "virtual ";

        if (m.IsStatic)
          sSyntax += "static ";
        if (m.MemberType == MemberTypes.Event)
          sSyntax += "event ";
      }
      return (sSyntax);
    }

    private string GetEventKeywordsCS(EventInfo e)
    {
      string sSyntax = string.Empty;
      if (e == null) {
        sSyntax += "N/A Error in GetEventKeywordsCS";
      }
      else
      {
        if (e.EventHandlerType.IsPublic)
          sSyntax += "public ";
        if (e.EventHandlerType.IsNotPublic)
          sSyntax += "private ";
        if (e.MemberType == MemberTypes.Event)
          sSyntax += "event";
      }
      return (sSyntax);
    }

    #endregion

    private string GetCasedTypeCS(string s)
    {
      switch (s)
      {
        case "Int16":
          s = "short";
          break;
        case "Int32":
          s = "int";
          break;
        case "Int64":
          s = "long";
          break;
        case "UInt16":
          s = "ushort";
          break;
        case "UInt32":
          s = "uint";
          break;
        case "UInt64":
          s = "ulong";
          break;
        case "Boolean":
          s = "bool";
          break;
        case "Single":
          s = "float";
          break;
        case "Void":
        case "Double":
        case "Long":
        case "Float":
        case "Byte":
        case "SByte":
        case "Decimal":
        case "String":
        case "Object":
          s = s.ToLower();
          break;
      }
      return (s);
    }


    #endregion

    #region Common

    private string GetNamespace(Assembly a)
    {
      string nsName = a.GetTypes()[0].Namespace.ToString();
      return (nsName);
    }

    private string GetTypeName(string asmName, string typeName)
    {
      var nspace = GetNamespace2(asmName);
      nspace = nspace.Replace("ESRI.Server", "ESRI.ArcGIS");
      return nspace + "." + typeName;
    }

    private Type GetTypeFromName(Assembly a, string fullTypeName)
    {
      var t = a.GetType(fullTypeName, false, true);
      if (t == null)
      {
        Console.Error.WriteLine($@"Error: {fullTypeName} was not found in assembly {a.FullName}");
        //foreach (var theType in a.GetTypes())
        //{
        //  System.Diagnostics.Debug.WriteLine($@"type: {theType.Name} {theType.FullName} {theType.Assembly}");
        //}
        //StringBuilder sb = new StringBuilder();
        //foreach (var theType in a.GetTypes().Where((typ) => { return typ.Name.StartsWith(fullTypeName.Substring(5)); }))
        //{
        //  sb.AppendLine($@"{theType.Name} {theType.FullName}");
        //}
        //System.IO.File.WriteAllText(@"c:\temp\list.txt", sb.ToString() );
      }
      else
      {
        //Console.Error.WriteLine($@"Found: {fullTypeName} in assembly {a.FullName}");
      }
      return t;
    }

    private string GetNamespace2(string asmName)
    {
      string nsName = GetLastString(asmName, "\\");

      nsName = nsName.Remove(nsName.LastIndexOf("."), 4);
      nsName = nsName.Replace("3DAnalyst", "Analyst3D");
      if (nsName.IndexOf("SystemUI") < 1 && nsName.IndexOf("SystemUt") < 0)
      {
        nsName = nsName.Replace("System", "esriSystem");
      }

      return (nsName);
    }

    private string GetLastString(string s, string delimiter)
    {
      if (string.IsNullOrEmpty(s)) return string.Empty;
      int pos = s.LastIndexOf(delimiter) + 1;
      s = s.Remove(0, pos);
      return (s);
    }

    private string GetHRefVB(string s)
    {
      string href = "";
      switch (s.ToLower())
      {
        case "boolean":
        case "boolean&":
        case "bool":
          href = "frlrfSystemBooleanClassTopic";
          s = "Boolean";
          break;
        case "wchar_t":
        case "char":
          href = "frlrfSystemCharClassTopic";
          s = "Char";
          break;
        case "string*":
        case "string&":
        case "string":
          href = "frlrfSystemStringClassTopic";
          s = "String";
          break;
        case "byte":
          href = "frlrfSystemByteClassTopic";
          break;
        case "int8":
        case "int8&":
        case "sbyte":
          href = "frlrfSystemSByteClassTopic";
          s = "Byte";
          break;
        case "int16":
        case "short":
          href = "frlrfSystemInt16ClassTopic";
          s = "Short";
          break;
        case "uint16":
          href = "frlrfSystemUInt16ClassTopic";
          s = "ushort";
          break;
        case "int":
        case "int32":
        case "int32&":
        case "integer":
          href = "frlrfSystemInt32ClassTopic";
          s = "Integer";
          break;
        case "uint32&":
        case "uint32":
        case "uint":
          href = "frlrfSystemUInt32ClassTopic";
          s = "Integer";
          break;
        case "int64":
        case "__int64":
        case "long":
          href = "frlrfSystemInt64ClassTopic";
          s = "Long";
          break;
        case "unsigned __int64":
        case "uint64":
          href = "frlrfSystemUInt64ClassTopic";
          s = "Long";
          break;
        case "float64":
        case "float64&":
        case "double&":
        case "double":
          href = "frlrfSystemDoubleClassTopic";
          s = "Double";
          break;
        case "float32":
        case "float32&":
        case "single":
          href = "frlrfSystemSingleClassTopic";
          s = "Single";
          break;
        case "void":
          href = "frlrfSystemVoidClassTopic";
          break;
        case "object*":
        case "object&":
        case "object":
          href = "frlrfSystemObjectClassTopic";
          s = "Object";
          break;
        default:
          return (s);

      }
      return s;
      // replaced return ("<a href=" + '\u0022' + "javascript:LinkKwd('" + href + "')" + '\u0022' + ">" + s + "</a>");
    }

    private string GetHRef(string s)
    {
      string href = "";
      switch (s.ToLower())
      {
        case "boolean":
        case "boolean&":
        case "bool":
          href = "frlrfSystemBooleanClassTopic";
          s = "bool";
          break;
        case "wchar_t":
        case "char":
          href = "frlrfSystemCharClassTopic";
          break;
        case "string*":
        case "string&":
        case "string":
          href = "frlrfSystemStringClassTopic";
          s = "string";
          break;
        case "byte":
          href = "frlrfSystemByteClassTopic";
          break;
        case "int8":
        case "int8&":
        case "sbyte":
          href = "frlrfSystemSByteClassTopic";
          s = "sbyte";
          break;
        case "int16":
        case "short":
          href = "frlrfSystemInt16ClassTopic";
          s = "short";
          break;
        case "uint16":
          href = "frlrfSystemUInt16ClassTopic";
          s = "ushort";
          break;
        case "int":
        case "int32":
        case "int32&":
        case "integer":
          href = "frlrfSystemInt32ClassTopic";
          s = "int";
          break;
        case "uint32&":
        case "uint32":
        case "uint":
          href = "frlrfSystemUInt32ClassTopic";
          s = "uint";
          break;
        case "int64":
        case "__int64":
        case "long":
          href = "frlrfSystemInt64ClassTopic";
          s = "long";
          break;
        case "unsigned __int64":
        case "uint64":
          href = "frlrfSystemUInt64ClassTopic";
          s = "ulong";
          break;
        case "float64":
        case "float64&":
        case "double&":
        case "double":
          href = "frlrfSystemDoubleClassTopic";
          s = "double";
          break;
        case "float32":
        case "float32&":
        case "single":
          href = "frlrfSystemSingleClassTopic";
          s = "single";
          break;
        case "void":
          href = "frlrfSystemVoidClassTopic";
          break;
        case "object*":
        case "object&":
        case "object":
          href = "frlrfSystemObjectClassTopic";
          s = "object";
          break;
        default:
          return (s);
      }
      return s;
      // return ("<a href=" + '\u0022' + "javascript:LinkKwd('" + href + "')" + '\u0022' + ">" + s + "</a>");
    }

    private string GetESRIHRef(Type t)
    {
      string tmp = t.ToString().Substring(9); //was 16
                                              //string s2 = t.ToString().Substring(t.ToString().LastIndexOf(".")+1);
      tmp = tmp.Replace("DDD", "3D");
      tmp = tmp.Replace("ControlsSupport", "Controls"); // Controls was Core
      tmp = tmp.Replace("MapControl", "Controls");
      tmp = tmp.Replace("PageLayoutControl", "Controls");
      tmp = tmp.Replace("ReaderControl", "Controls");
      tmp = tmp.Replace("TOCControl", "Controls");
      tmp = tmp.Replace("ToolbarControl", "Controls");
      string s3 = t.ToString().Substring(t.ToString().IndexOf("."), t.ToString().LastIndexOf(".") - t.ToString().IndexOf("."));
      s3 = s3.Substring(s3.LastIndexOf(".") + 1);
      //				return ("<a href=" + '\u0022' + "ms-help://" + "ESRI.ArcGIS/esri" + tmp.Substring(0,tmp.LastIndexOf(".")) +
      //					"/html/" + t.ToString().Substring(t.ToString().LastIndexOf(".")+1) + ".htm"  + '\u0022' + ">" + 
      //					t.ToString().Substring(t.ToString().LastIndexOf(".")+1) + "</a>");
      string s4 = @"<a href=""../esri" + s3 + @"/" + t.ToString().Substring(t.ToString().LastIndexOf(".") + 1) + @".htm"">" + t.Name + @"</a>";
      //string s4 = ("<a href=" + '\u0022' + "javascript:555LinkKwd('eaglrf" + s3 + t.Name + "')" + '\u0022' + ">" + t.Name + "</a>");
      s4 = s4.Replace("&", "");
      return (s4);
    }

    #endregion

    #region MD conversion

    private (string VbSyntax, string CSharpSyntax) ConvertHtmlToMarkdown((string VbSyntax, string CSharpSyntax) htmlInput)
    {
      (string VbSyntax, string CSharpSyntax) mdOutput;
      //// change html syntax to md
      //mdOutput.VbSyntax = htmlInput.VbSyntax.Replace("&nbsp;", " ").Replace("<b>", "**").Replace("</b>", "**").Replace("<i>", "*").Replace("</i>", "*");
      //mdOutput.CSharpSyntax = htmlInput.CSharpSyntax.Replace("&nbsp;", " ").Replace("<b>", "**").Replace("</b>", "**").Replace("<i>", "*").Replace("</i>", "*");
      //// change <pre><code class="language-cs"> to ```cs
      //mdOutput.VbSyntax = mdOutput.VbSyntax.Replace("<pre><code class=\"language-vb\">", "```vb").Replace("</code></pre>", "```\r\n");
      //mdOutput.CSharpSyntax = mdOutput.CSharpSyntax.Replace("<pre><code class=\"language-cs\">", "```cs").Replace("</code></pre>", "```\r\n");
      // use regex to match all sequences of <a href="...">...</a> using "<a href=\"(.*?)\">(.*?)</a>"
      var anchorPattern = @"<a href=""(.*?)"">(.*?)</a>";
      mdOutput.VbSyntax = System.Text.RegularExpressions.Regex.Replace(htmlInput.VbSyntax, anchorPattern, "[$2]($1)");
      mdOutput.CSharpSyntax = System.Text.RegularExpressions.Regex.Replace(htmlInput.CSharpSyntax, anchorPattern, "[$2]($1)");
      return mdOutput;
    }

    /// <summary>
    /// Converts an HTML <a> tag string into Markdown link format.
    /// </summary>
    private static string ConvertHtmlLinkToMarkdown(string html)
    {
      if (string.IsNullOrWhiteSpace(html))
        throw new ArgumentException("Input HTML cannot be null or empty.");

      var doc = new HtmlDocument();
      doc.LoadHtml(html);

      var linkNode = doc.DocumentNode.SelectSingleNode("//a");
      if (linkNode == null)
        throw new FormatException("No <a> tag found in the input HTML.");

      string href = linkNode.GetAttributeValue("href", "").Trim();
      string text = linkNode.InnerText.Trim();

      if (string.IsNullOrEmpty(href))
        throw new FormatException("The <a> tag does not contain an href attribute.");

      // Markdown format: [text](url)
      return $"[{text}]({href})";
    }

    #endregion MD conversion

    #region HTML elements for Delegate topics

    private string WriteDelegateElements(string delegateName, string namespaceName, string eventName)
    {
      string sElems = "";
      string intfoName = delegateName.Substring(0, delegateName.IndexOf("_"));
      delegateName = GetLastString(delegateName, "_");
      sElems += "<HTML>\r\n";
      sElems += "<HEAD>\r\n";
      sElems += "<META HTTP-EQUIV=" + '\u0022' + "Content-Type" + '\u0022' + " Content=" + '\u0022' + "text/html; charset=Windows-1252" + '\u0022' + ">\r\n";
      sElems += "<TITLE>" + delegateName + " Delegate</TITLE>\r\n";
      //sElems += "<SCRIPT language=" + '\u0022' + "javascript" + '\u0022' + " SRC=" + '\u0022' + "..\\scripts\\AOGlossary.js" + '\u0022' + ">\r\n";
      //sElems += "</SCRIPT>\r\n";
      //sElems += "<xml>\r\n";
      //sElems += "<MSHelp:TOCTitle Title=" + '\u0022' + delegateName + " Delegate" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:RLTitle Title=" + '\u0022' + delegateName + " Delegate" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "TargetOS" + '\u0022' + " Value=" + '\u0022' + "Windows" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "DocSet" + '\u0022' + " Value=" + '\u0022' + GetLastString(namespaceName, ".") + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "TopicType" + '\u0022' + " Value=" + '\u0022' + "kbSyntax" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "Locale" + '\u0022' + " Value=" + '\u0022' + "kbEnglish" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "DevLang" + '\u0022' + " Value=" + '\u0022' + "CSharp" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "DevLang" + '\u0022' + " Value=" + '\u0022' + "VB" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "DevLang" + '\u0022' + " Value=" + '\u0022' + "C++" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "DocSet" + '\u0022' + " Value=" + '\u0022' + "C#" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "DocSet" + '\u0022' + " Value=" + '\u0022' + "Visual Basic" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "DocSet" + '\u0022' + " Value=" + '\u0022' + "Visual C++" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Attr Name=" + '\u0022' + "LinkGroup" + '\u0022' + " Value=" + '\u0022' + "ArcObjectsHelp" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Keyword Index=" + '\u0022' + "A" + '\u0022' + " Term=" + '\u0022' + intfoName + " methods" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Keyword Index=" + '\u0022' + "K" + '\u0022' + " Term=" + '\u0022' + delegateName + " delegate" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Keyword Index=" + '\u0022' + "K" + '\u0022' + " Term=" + '\u0022' + intfoName + ", " + delegateName + " delegate" + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Keyword Index=" + '\u0022' + "F" + '\u0022' + " Term=" + '\u0022' + delegateName + '\u0022' + "/>\r\n";
      //sElems += "<MSHelp:Keyword Index=" + '\u0022' + "F" + '\u0022' + " Term=" + '\u0022' + namespaceName + "." + delegateName + '\u0022' + "/>\r\n";
      //sElems += "</xml>\r\n";
      //sElems += "<SCRIPT SRC=" + '\u0022' + "..\\scripts\\dtuelink.js" + '\u0022' + "></SCRIPT>\r\n";
      //sElems += "<SCRIPT language=" + '\u0022' + "JScript" + '\u0022' + " SRC=" + '\u0022' + "..\\scripts\\ns.js" + '\u0022' + "></SCRIPT>\r\n";
      sElems += "<link rel=" + '\u0022' + "stylesheet" + '\u0022' + " href=" + '\u0022' + "..\\scripts\\dtue_ie5.css" + '\u0022' + " />\r\n";
      sElems += "</HEAD>\r\n";
      sElems += "<body topmargin=" + '\u0022' + "0" + '\u0022' + " id=" + '\u0022' + "bodyID" + '\u0022' + " class=" + '\u0022' + "dtBODY" + '\u0022' + ">\r\n";
      //sElems += "<object id=" + '\u0022' + "obj_cook" + '\u0022' + " classid=" + '\u0022' + "clsid:59CC0C20-679B-11D2-88BD-0800361A1803" + '\u0022' + " style=" + '\u0022' + "display:none;" + '\u0022' + "></object>\r\n";
      sElems += "<div id=" + '\u0022' + "nsbanner" + '\u0022' + ">\r\n";
      sElems += "<div id=" + '\u0022' + "bannerrow1" + '\u0022' + ">\r\n";
      sElems += "<table class=" + '\u0022' + "bannerparthead" + '\u0022' + " cellspacing=" + '\u0022' + "0" + '\u0022' + ">\r\n";
      sElems += "<tr id=" + '\u0022' + "hdr" + '\u0022' + ">\r\n";
      sElems += "<td class=" + '\u0022' + "runninghead" + '\u0022' + " nowrap>ArcGIS&nbsp;Developer&nbsp;Help&nbsp;&nbsp;</td>\r\n";
      // (" + namespaceName + ")
      sElems += "<td class=" + '\u0022' + "product" + '\u0022' + " nowrap>&nbsp;</td>\r\n";
      sElems += "</tr>\r\n";
      sElems += "</table>\r\n";
      sElems += "</div>\r\n";
      sElems += "<div id=" + '\u0022' + "TitleRow" + '\u0022' + ">\r\n";
      sElems += "<H1 class=" + '\u0022' + "dtH1" + '\u0022' + "><a name=" + '\u0022' + delegateName + '\u0022' + "></A>" + delegateName + " Delegate</H1>\r\n";
      sElems += "</div></div>\r\n";
      sElems += "<div id=" + '\u0022' + "nstext" + '\u0022' + " valign=" + '\u0022' + "bottom" + '\u0022' + ">\r\n";
      sElems += "Represents the method that handles the <a href=" + '\u0022' + intfoName + "_" + eventName + ".htm" + '\u0022' + ">" + eventName + "</a> event.";
      return (sElems);
    }
    #endregion
  }
}