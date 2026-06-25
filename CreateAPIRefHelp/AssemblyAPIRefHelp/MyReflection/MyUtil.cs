using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyReflection
{
  public class MyUtil
  {
    public static Dictionary<string, Type> MissingTypes = [];

    public static SyntaxGenerator.SyntaxMaker? SyntaxMaker { get; set; }

    public static void InitilizeMissingAssemblies()
    {
      try
      {
        //var parity = (Parity)Enum.Parse(typeof(Parity), Parity.Even.ToString(), true);
        //var stopBits = (StopBits)Enum.Parse(typeof(StopBits), StopBits.One.ToString(), true);
        //var missingType = parity.GetType();
        //if (missingType != null) MissingTypes.Add(@"System.IO.Ports.Parity", missingType);
        //missingType = stopBits.GetType();
        //if (missingType != null) MissingTypes.Add(@"System.IO.Ports.StopBits", missingType);
      }
      catch (Exception e)
      {
        Console.WriteLine($@"System.IO.Ports load failed: {e.ToString()}");
      }
    }

    /// <summary>
    /// replace `1[...] into &lt;...&gt;
    /// </summary>
    /// <param name="input"></param>
    /// <param name="genericTypes">List of Generics</param>
    /// <returns></returns>
    internal static string FixTemplateTypeSyntax(string input,
      List<MyTypeBase> genericTypes)
    {
      var output = new StringBuilder(input);
      var idxTag = input.IndexOf('`');
      var tag = "`";
      if (idxTag >= 0)
      {
        tag = input.Substring(idxTag, 2);
      }
      var idxStart = input.IndexOf(tag);
      if (idxStart >= 0)
      {
        output = new StringBuilder(input.Substring(0, idxStart));
        var convert = true;
        var numOpenParentheses = 0;
        for (int i = idxStart + tag.Length; i < input.Length; i++)
        {
          if (convert)
          {
            if (input[i] == '[')
            {
              if (numOpenParentheses++ == 0)
                output.Append('<');
              else
                output.Append('[');
            }
            else
            {
              if (input[i] == ']')
              {
                if (--numOpenParentheses == 0)
                {
                  output.Append('>');
                  convert = false;
                }
                else
                  output.Append(']');
              }
              else output.Append(input[i]);
            }
          }
          else
          {
            output.Append(input[i]);
          }
        }
      }
      return output.ToString();
    }


    private static string LongSymbolPattern = @"([a-zA-Z0-9\.]+,\s[a-zA-Z0-9\.]+,\sVersion=[0-9\.]+,\sCulture=\w+,\sPublicKeyToken=[0-9a-fA-F]{16})|(\[[a-zA-Z0-9\.]+\],\s[a-zA-Z0-9\.]+,\sVersion=[0-9\.]+,\sCulture=\w+,\sPublicKeyToken=[0-9a-fA-F]{16})";
    private static Regex? LongSymbolRegex = null;

    /// <summary>
    /// change long symbol reference:
    /// [ArcGIS.Core.Geometry.GeometryBag, ArcGIS.Core, Version=12.9.0.0, Culture=neutral, PublicKeyToken=8fc3cc631e44ad86]
    /// System.Tuple<[System.Boolean, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Uri, System.Private.Uri, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]>
    /// System.Collections.Generic.IList`1[System.Collections.Generic.List`1[[ArcGIS.Core.Geometry.Segment], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
    /// to short symbol reference:
    /// ArcGIS.Core.Geometry.GeometryBag
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    internal static string RemoveVersionSpecifics(string input)
    {
      if (LongSymbolRegex == null) LongSymbolRegex = new Regex(LongSymbolPattern);
      var splitList = LongSymbolRegex.Split(input);
      var output = string.Empty;
      foreach (var split in splitList)
      {
        if (string.IsNullOrEmpty(split)) continue;
        {
          if (split.Contains("PublicKeyToken"))
          {
            var parts = split.Split(',');
            output += parts[0];
          }
          else
          {
            output += split;
          }
        }
      }
      return output;
    }

    public static string GetAssemblyFileVersion(string assemblyPath)
    {
      System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyPath);
      string version = fvi.FileVersion ?? "no file version";
      var parts = version.Split('.');
      if (parts.Length != 4) return version;
      version = $@"{parts[0].Substring(1)}.{parts[1]}.{parts[3]}";
      return version;
    }

    public static string ReplaceWordsFromDictionary(string paragraph, Dictionary<string, string> replacements)
    {
      var result = new StringBuilder(paragraph);
      foreach (var lookup in replacements.Keys)
      {
        if (paragraph.IndexOf(lookup) == -1) continue;
        result.Replace(lookup, replacements[lookup]);
      }
      return result.ToString();
    }

    public static string SimplifyParentheses(string symbol)
    {
      var idxStart = symbol.IndexOf('(');
      if (idxStart < 0) return symbol;
      var idxEnd = symbol.LastIndexOf(')');
      if (idxEnd < 0) return symbol;
      var parentheses = symbol.Substring(idxStart + 1, idxEnd - idxStart - 1);
      string insert = string.Empty;
      string lastWord = string.Empty;
      foreach (var chr in parentheses)
      {
        if (char.IsLetterOrDigit(chr) || chr == '_')
        {
          lastWord += chr;
          continue;
        }
        if (chr == '.')
        {
          lastWord = String.Empty;
          continue;
        }
        // we have some other punctuation
        if (lastWord.Length > 0)
        {
          insert += lastWord;
          lastWord = String.Empty;
        }
        insert += chr == '+' ? '.' : chr;
      }
      insert += lastWord;
      return $@"{symbol.Substring(0, idxStart + 1)}{insert}{symbol.Substring(idxEnd)}";
    }
  }
}
