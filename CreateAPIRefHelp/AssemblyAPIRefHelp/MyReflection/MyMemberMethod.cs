using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
  public class MyMemberMethod : MyMember
  {
    public MyMemberMethod(System.Reflection.MethodInfo methodInfo, MyTypeBase parentType) : base(methodInfo, parentType)
    {
      IsStatic = methodInfo.IsStatic;
      IsPublic = methodInfo.IsPublic;
      //var ti = IntrospectionExtensions.GetTypeInfo();
      //      var custRetType = methodInfo.ReturnTypeCustomAttributes.ToString();
      try
      {
        var mr = methodInfo.ReturnParameter;
        ReturnType = new MyTypeBase(mr.ParameterType, false);
      }
      catch
      {
        var idx = methodInfo.ToString().IndexOf(' ');
        if (idx >= 0)
        {
          var retType = methodInfo.ToString().Substring(0, idx).Trim();
          if (!MyUtil.MissingTypes.ContainsKey(retType))
          {
            throw new Exception($@"Can't load this type: {retType}");
          }
          ReturnType = new MyTypeBase(MyUtil.MissingTypes[retType], false);
        }
      }

      FullName = methodInfo.ToString();
      //$@"{(!string.IsNullOrEmpty(custRetType) ? custRetType : ReturnType.Name)} {Name}";
      Parameters = new List<MyMemberParameter>();
      try
      {
        var paras = methodInfo.GetParameters();
        foreach (var param in paras)
        {
          var newParam = new MyMemberParameter(param);
          if (newParam.IsInternal)
          {
            HasInternalParams = true;
          }
          Parameters.Add(newParam);
        }
        if (methodInfo.IsGenericMethod || methodInfo.IsGenericMethodDefinition)
        {
          if (FullName != null)
          {
            foreach (var genArg in methodInfo.GetGenericArguments())
            {
              FullName = FullName.Replace($@"[{genArg.Name}]", $@"{{{genArg.Name}}}").Replace("`1", "");
            }
          }
          // methodInfo.GetGenericMethodDefinition())
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }
      //while (FullName.Contains("`"))
      //{
      //  FullName = MyUtil.FixTemplateTypeSyntax(FullName);
      //}
      //int firstParentheses = FullName.IndexOf('(');
      //if (firstParentheses >= 0)
      //{
      //  FullName = FullName.Substring(0, firstParentheses);
      //}

      NameSpace = parentType.Namespace;
      //if (this.ToString().Contains(")("))
      //  System.Diagnostics.Trace.WriteLine(this.ToString());
      //if (this.ToString().Contains(".Internal."))
      //  System.Diagnostics.Trace.WriteLine(this.ToString());
    }

    public bool IsUsable
    {
      get
      {
        bool bUsable = false;
        var typePrefix = ReturnType.ToString();
        bUsable = !typePrefix.Contains(".Internal.", StringComparison.OrdinalIgnoreCase)
          && !typePrefix.Contains(".ServiceContracts.", StringComparison.OrdinalIgnoreCase)
          && !typePrefix.StartsWith("XamlGeneratedNamespace");
        if (!bUsable)
          System.Diagnostics.Trace.WriteLine(this.ToString());
        return bUsable;
      }
    }

    public MyTypeBase ReturnType { get; set; }

    public string NameSpace { get; set; }

    public List<MyMemberParameter> Parameters { get; set; }
    public override string Name
    {
      get
      {
        var theName = base.Name;
        if (Arguments.Length > 0)
        {
          var args = $@"({Arguments})";
          theName += MyUtil.SimplifyParentheses(args);
        }
        return theName;
      }
      set => base.Name = value;
    }

    public string Arguments
    {
      get
      {
        if (Parameters.Count == 0) return string.Empty;
        return string.Join(", ", Parameters.Select(p => p.ToString()));
      }
    }
    public bool IsStatic { get; set; }
    public bool IsPublic { get; set; }
    public string StaticOrNot => (IsStatic ? "static " : String.Empty);

    public override string ToString()
    {
      var postFix = string.Join(", ", Parameters.Select(p => p.ToString()));
      return $@"{StaticOrNot}{FullName}".Replace(NameSpace + ".", string.Empty);// ({postFix})";
    }
  }
}
