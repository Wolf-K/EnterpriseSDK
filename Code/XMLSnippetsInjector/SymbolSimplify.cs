//using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLSnippetsInjector
{
  internal static class SymbolSimplify
  {
    /// <summary>
    /// Take some types and 'standardize' them
    /// </summary>
    /// <param name="symbolName"></param>
    /// <returns>symbolName with standardized types</returns>
    internal static string StandardTypes(string symbolName)
    {
      var standardName = symbolName.Replace("STRING", "SYSTEM.STRING").Replace("SYSTEM.SYSTEM.STRING", "SYSTEM.STRING");
      standardName = standardName.Replace("INT32", "SYSTEM.INT32").Replace("SYSTEM.SYSTEM.INT32", "SYSTEM.INT32");
      standardName = standardName.Replace("INT64", "SYSTEM.INT64").Replace("SYSTEM.SYSTEM.INT64", "SYSTEM.INT64");
      standardName = standardName.Replace("BOOL", "BOOLEAN").Replace("BOOLEANEAN", "BOOLEAN");
      standardName = standardName.Replace("BOOLEAN", "SYSTEM.BOOLEAN").Replace("SYSTEM.SYSTEM.BOOLEAN", "SYSTEM.BOOLEAN");
      standardName = standardName.Replace("OBJECT", "SYSTEM.OBJECT").Replace("SYSTEM.SYSTEM.OBJECT", "SYSTEM.OBJECT");
      standardName = standardName.Replace("()", string.Empty);
      standardName = standardName.Replace(" ", string.Empty);
      return standardName;
    }

    internal static string SimplifyParentheses(string symbolName)
    {
      var symbol = SymbolSimplify.StandardTypes(symbolName);
      var idxStart = symbol.IndexOf('(');
      if (idxStart < 0) return symbol;
      var idxEnd = symbol.LastIndexOf(')');
      if (idxEnd < 0) return symbol;
      var parentheses = symbol.Substring(idxStart + 1, idxEnd - idxStart - 1);
      if (symbol.StartsWith(@"ArcGIS.Core.Geometry.PolylineBuilderEx.CreatePolyline"))
        System.Diagnostics.Trace.WriteLine(parentheses);

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
      // insert are all arguments now remove the argument names if any
      // i.e. (IEnumerable{MapPoint} points,SpatialReference spatialReference)
      // is now (IEnumerable{MapPoint},SpatialReference)
      var middle = string.Empty;
      insert = insert.Trim();
      var isType = true;
      var isComma = false;
      insert = insert.Replace(", ", ",");
      foreach (var c in insert)
      {
        if (c == ' ' && !isComma)
        {
          // potential switch to name
          isType = false;
          isComma = false;
          continue;
        }
        if (!isType && (Char.IsLetterOrDigit(c) || c == '_'))
        {
          // is NOT type skip this characters
          continue;
        }
        else
        {
          isComma = c == ',';
          isType = true;
          middle += c;
        }
      }
      return $@"{symbol.Substring(0, idxStart + 1)}{middle}{symbol.Substring(idxEnd)}";
    }

    internal static string MakeUpperAndShort(string symbolString)
    {
      var upperAndShort = symbolString.ToUpper();
      if (upperAndShort.Contains('<'))
        upperAndShort = upperAndShort.Substring(0, upperAndShort.IndexOf('<'));
      if (upperAndShort.Contains('('))
        upperAndShort = upperAndShort.Substring(0, upperAndShort.IndexOf('('));
      if (upperAndShort.Contains(".#", StringComparison.CurrentCulture))
        upperAndShort = upperAndShort.Substring(0, upperAndShort.IndexOf(".#"));
      return upperAndShort;
    }

    internal static string SimplifySymbolRef(string findSymbolRef)
    {
      var symbolToFind = findSymbolRef.Replace(@" = null", string.Empty);
      symbolToFind = symbolToFind.Replace("'", "`");
      symbolToFind = SimplifyParentheses(symbolToFind);
      symbolToFind = symbolToFind.Replace(@"._ctor", @".#ctor");
      symbolToFind = symbolToFind.Replace(@".#ctor()", @".#ctor").Replace('<', '{').Replace('>', '}').Replace("{T}", "``1");
      symbolToFind = symbolToFind.Replace(@"=null", string.Empty).Replace(" ", string.Empty);
      if (symbolToFind.EndsWith("()")) symbolToFind = symbolToFind.Substring(0, symbolToFind.Length - 2);
      // remove nullable type
      var idx = symbolToFind.IndexOf("nullable{", StringComparison.OrdinalIgnoreCase);
      var nullableFollow = '{';
      if (idx < 0)
      {
        idx = symbolToFind.IndexOf("nullable(", StringComparison.OrdinalIgnoreCase);
        nullableFollow = '(';
      }
      if (idx >= 0)
      {
        var prefix = symbolToFind.Substring(0, idx);
        idx += "nullable{".Length;
        var remainder = symbolToFind.Substring(idx);
        var postfix = string.Empty;
        var indent = 0;
        foreach (var chr in remainder)
        {
          idx++;
          if (chr == nullableFollow)
          {
            indent++;
            postfix += chr;
            continue;
          }
          if (chr == (nullableFollow == '{' ? '}' : ')'))
          {
            if (indent-- == 0)
            {
              if (idx < symbolToFind.Length)
              {
                postfix += symbolToFind.Substring(idx);
              }
              break;
            }
          }
          postfix += chr;
        }
        symbolToFind = prefix + postfix;
      }
      return symbolToFind;
    }
  }
}
