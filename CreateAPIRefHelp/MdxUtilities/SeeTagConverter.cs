using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MdxUtil
{
  public static class SeeTagConverter
  {
    public static List<string> Namespaces = new List<string>();

    public static void InitializeNamespaces(IEnumerable<string>? namespaces)
    {
      Namespaces = namespaces?.ToList() ?? new List<string>();
      Namespaces.Sort((a, b) => b.Length.CompareTo(a.Length));
    }

    /// <summary>
    /// Converts a &lt;see&gt; tag (e.g., &lt;see cref="T:ArcGIS.Core.Data.FeatureDataset"/&gt;) to a markdown link.
    /// </summary>
    /// <param name="seeTag">The XML &lt;see&gt; tag as a string.</param>
    /// <returns>The markdown link string, or the original input if parsing fails.</returns>
    public static string ConvertSeeTagToMarkdown(string seeTag)
    {
      var match = Regex.Match(seeTag, @"<see\s+cref=""([^""]+)""\s*/?>");
      if (!match.Success)
        return seeTag;

      string cref = match.Groups[1].Value.Substring(2);

      string? matchingNamespace = Namespaces.FirstOrDefault(ns => cref.StartsWith(ns));
      string relativePath = string.Empty;
      string caption = cref;
      if (matchingNamespace != null && matchingNamespace.Length > 0)
      {
        relativePath = $@"../{matchingNamespace}/";
        caption = cref.Replace(matchingNamespace, string.Empty);
        if (caption.StartsWith(".")) caption = caption.Substring(1);
      }
      string markdown = $"[{cref}]({relativePath}{caption}{MdxUtil.MdxExtension})";
      return markdown;
    }

    /// <summary>
    /// Replaces all &lt;see /> tags in the input string with their markdown link equivalents.
    /// </summary>
    /// <param name="input">The input string possibly containing &lt;see /> tags.</param>
    /// <returns>The string with all &lt;see /> tags replaced by markdown links.</returns>
    public static string ReplaceSeeTagsWithMarkdown(string input)
    {
      if (string.IsNullOrEmpty(input)) return input;
      return Regex.Replace(input, "<see\\s+cref=\\\"[A-Z]:[^\\\"]+\\\"\\s*/?>", m => ConvertSeeTagToMarkdown(m.Value));
    }

    /// <summary>
    /// Unescapes XML-encoded text (e.g., &lt;, &gt;, &amp;, &quot;, &apos;).
    /// </summary>
    /// <param name="input">The XML-encoded string.</param>
    /// <returns>The unescaped string.</returns>
    public static string UnescapeXml(string input)
    {
      if (string.IsNullOrEmpty(input)) return input;
      return WebUtility.HtmlDecode(input);
    }
  }
}
