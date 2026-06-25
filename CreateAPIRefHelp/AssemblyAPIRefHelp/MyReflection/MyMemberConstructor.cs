using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
  public class MyMemberConstructor : MyMember
  {
    public MyMemberConstructor(System.Reflection.ConstructorInfo ctorInfo, MyTypeBase parentType) : base(ctorInfo, parentType)
    {
      FullName = $@"{Name}";
      FullName = ctorInfo.ToString();
      //$@"{(!string.IsNullOrEmpty(custRetType) ? custRetType : ReturnType.Name)} {Name}";
      var paras = ctorInfo.GetParameters();
      Parameters = [];
      foreach (var param in paras)
      {
        Parameters.Add(new MyMemberParameter(param));
      }
      // Get the syntax
      CSharpSyntax = MdxUtil.CSharpSyntaxGenerator.GetConstructorSyntax(ctorInfo);
      VbSyntax = MdxUtil.VbSyntaxGenerator.GetConstructorSyntax(ctorInfo);
      //if (ctorInfo.IsGenericMethod || ctorInfo.IsGenericMethodDefinition)
      //{
      //  foreach (var genArg in ctorInfo.GetGenericArguments())
      //  {
      //    FullName = FullName.Replace($@"[{genArg.Name}]", $@"<{genArg.Name}>").Replace("`1", "");
      //  }
      //  // methodInfo.GetGenericMethodDefinition())
      //}
      //while (FullName.Contains("`"))
      //{
      //  FullName = MyUtil.FixTemplateTypeSyntax(FullName);
      //}
      //if (FullName.Contains("`"))
      //  System.Diagnostics.Trace.WriteLine(FullName);
      //int firstParentheses = FullName.IndexOf('(');
      //if (firstParentheses >= 0)
      //{
      //  FullName = FullName.Substring(0, firstParentheses);
      //}
    }

    public List<MyMemberParameter> Parameters { get; set; }

    public override string ToString()
    {
      var postFix = string.Join(", ", Parameters.Select(p => p.Name));
      //if (Parameters.Count > 0)
      //  System.Diagnostics.Trace.WriteLine(postFix);
      return $@"{Name} ({postFix})";
    }
  }
}
