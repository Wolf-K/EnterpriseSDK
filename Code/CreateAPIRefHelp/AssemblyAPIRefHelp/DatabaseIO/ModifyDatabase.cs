using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.IO;

namespace DatabaseIO
{
  public static class ModifyDatabase
  {
    public static SqlUtil SqlUtil = null;

    public static bool OpenDB()
    {
      SqlUtil = new SqlUtil();
      return string.IsNullOrEmpty(SqlUtil.GetError());
    }

    public static string GetSqlError()
    {
      return SqlUtil.GetError();
    }

    public static void CloseDb()
    {
      if (SqlUtil != null)
      {
        SqlUtil.Close();
        SqlUtil = null;
      }
    }

    public static int GetHelpContext(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "HelpContext", $"Name = '{nt}'");
      if (row == null || row.IsNull("HelpContext")) return 0;
      int.TryParse(row["HelpContext"].ToString(), out int result);
      return result;
    }

    public static object GetAsmDescriptionFields(ref string nt)
    {
      string[] a = new string[2] { "", "" };
      DataRow row = SqlUtil.GetDataRow("Libraries", "[Short Description],[Long Description]", $"Name = '{nt}'");
      if (row == null) return new object[] { a[0], a[1] };
      if (!row.IsNull("[Short Description]")) a[0] = row["[Short Description]"].ToString();
      if (!row.IsNull("[Long Description]")) a[1] = row["[Long Description]"].ToString();
      return new object[] { a[0], a[1] };
    }

    public static string GetShortDesc(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Libraries", "[Short Description]", $"Name = '{nt}'");
      if (row == null || row.IsNull("[Short Description]")) return "";
      return row["[Short Description]"].ToString();
    }

    public static DataRow GetOverviewFields(ref string nt)
    {
      return SqlUtil.GetDataRow("Libraries", $"Name = '{nt}'");
    }

    public static string GetDocStatus(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "Status", $"Name = '{nt}'");
      if (row == null || row.IsNull("Status")) return "Not Started";
      return row["Status"].ToString();
    }

    public static string GetEditedBy(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "EditedBy", $"Name = '{nt}'");
      if (row == null || row.IsNull("EditedBy")) return "Esri Staff";
      return row["EditedBy"].ToString();
    }

    public static string GetPlatforms(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "SupportedPlatforms", $"Name = '{nt}'");
      if (row == null || row.IsNull("SupportedPlatforms")) return "";
      return row["SupportedPlatforms"].ToString();
    }

    public static string GetSeeAlso(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "SeeAlsos", $"Name = '{nt}'");
      if (row == null || row.IsNull("SeeAlsos")) return "";
      return row["SeeAlsos"].ToString();
    }

    public static string GetKeyword(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "Keywords", $"Name = '{nt}'");
      if (row == null || row.IsNull("Keywords")) return "";
      return row["Keywords"].ToString();
    }

    public static string GetConsistsOf(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "ConsistsOf", $"Name = '{nt}'");
      if (row == null || row.IsNull("ConsistsOf")) return "";
      return row["ConsistsOf"].ToString();
    }

    public static string GetHasOneOrMoreOf(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "HasOneOrMoreOf", $"Name = '{nt}'");
      if (row == null || row.IsNull("HasOneOrMoreOf")) return "";
      return row["HasOneOrMoreOf"].ToString();
    }

    public static string GetSingleton(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "Singleton", $"Name = '{nt}'");
      if (row == null || row.IsNull("Singleton")) return "false";
      return row["Singleton"].ToString();
    }

    public static string GetLicenseHRES(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "LicenseHRESULT", $"Name = '{nt}'");
      if (row == null || row.IsNull("LicenseHRESULT")) return "";
      return row["LicenseHRESULT"].ToString();
    }

    public static string GetIndexEntry(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "IndexEntry", $"Name = '{nt}'");
      if (row == null || row.IsNull("IndexEntry")) return "";
      return row["IndexEntry"].ToString();
    }

    public static string GetSupersededBy(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "SupersededBy", $"Name = '{nt}'");
      if (row == null || row.IsNull("SupersededBy")) return "";
      return row["SupersededBy"].ToString();
    }

    public static string GetProductCodeString(ref string sLibName)
    {
      DataRow row = SqlUtil.GetDataRow("Libraries", "ProductCode", $"Name = '{sLibName}'");
      short intCode = 0;
      if (row == null || row.IsNull("ProductCode"))
        return $"No record for {sLibName} in Libraries found";
      intCode = Convert.ToInt16(row["ProductCode"]);
      string sAvail = "Available with ";
      string sReq = " Requires ";
      string sArcGISProd = sAvail + "ArcGIS Server.";
      string sExtension = "";
      if (intCode >= 10)
      {
        switch ((short)(Convert.ToDouble(intCode.ToString().Substring(0, intCode.ToString().Length - 1)) * 10))
        {
          case 10: sExtension = sReq + "3D Analyst Extension."; break;
          case 20: sExtension = sReq + "Spatial Analyst Extension."; break;
          case 30: sExtension = sReq + "StreetMap Extension."; break;
          case 40: sExtension = sReq + "GeoStatistical Extension."; break;
          case 50: sExtension = sReq + "Publisher Extension."; break;
          case 60: sExtension = sReq + "Survey Analyst Extension."; break;
          case 70: sExtension = sReq + "ArcPress Extension."; break;
          case 80: sExtension = sReq + "ArcScan Extension."; break;
          case 90: sExtension = sReq + "Maplex Extension."; break;
          case 100: sExtension = sReq + "Tracking Analyst Extension."; break;
          case 110: sExtension = sReq + "Schematics Extension."; break;
          case 120: sExtension = sReq + "Network Analyst Extension."; break;
          case 130: sExtension = sReq + "Military Analyst Extension."; break;
          case 140: sExtension = sReq + "Data Interoperability Extension."; break;
        }
      }
      return sArcGISProd + sExtension;
    }

    public static bool IsProduct(string sLibName, ref short iProdCode)
    {
      short intCode;
      if (sLibName.IndexOf("esri", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        sLibName = sLibName.Substring(4);
      }
      DataRow row = SqlUtil.GetDataRow("Libraries", "ProductCode", $"Name = '{sLibName}'");
      if (row == null || row.IsNull("ProductCode")) return false;
      intCode = Convert.ToInt16(row["ProductCode"]);
      return (iProdCode == Convert.ToInt16(intCode.ToString().Substring(intCode.ToString().Length - 1)));
    }

    public static string GetInstIntfos(ref string nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "InstanceIntfos", $"Name = '{nt}'");
      if (row == null || row.IsNull("InstanceIntfos")) return "";
      return row["InstanceIntfos"].ToString();
    }

    public static int GetUnique()
    {
      DataRow row = SqlUtil.GetDataRow("UniqueNumber", "uniqueID", "1 = 1");
      if (row == null || row.IsNull("UniqueNumber")) return 0;
      int currentUnique = Convert.ToInt32(row["uniqueID"]);
      int nextUnique = currentUnique + 1;
      SqlUtil.UpdateIntParam("UniqueNumber", "uniqueID", nextUnique, $"uniqueID = {currentUnique}");
      return nextUnique;
    }

    public static string GetEnumErrorCode(ref object nt)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "ErrorCode", $"Name = '{nt}'");
      if (row == null || row.IsNull("ErrorCode")) return "";
      return row["ErrorCode"].ToString();
    }

    public static int GetParentHC(ref string strParent, ref int lngParentHC)
    {
      DataRow row = SqlUtil.GetDataRow("Documentation", "HelpContext", $"Name = '{strParent}'");
      if (row == null || row.IsNull("HelpContext")) return 0;
      int.TryParse(row["HelpContext"].ToString(), out int result);
      return result;
    }

    public static bool FindExample(ref string strTableName, ref string strLV)
    {
      DataRow row = SqlUtil.GetDataRow(strTableName, $"Name = '{strLV}'");
      return row != null;
    }

    public static void WriteDocToc(ref string libName, string theName, ref string theType, string theDescription, ref string theSyntax)
    {
      if (theType != "Namespace")
      {
        theName = libName + "::" + theName;
      }
      DataRow rowToc = SqlUtil.GetDataRow("toc", "[nametype],[description]", $"name = '{theName}'");
      if (rowToc == null)
      {
        if (!SqlUtil.InsertTocEdited(libName, theName, theType, theDescription, theSyntax, "Not Started", "OlbTree", DateTime.Now))
        {
          Console.WriteLine($"Error [{libName}]: unable to add to TOC: {theName}");
        }
        return;
      }
      if (rowToc[0].ToString().Equals(theType) && rowToc[1].ToString().Equals(theDescription))
      {
        if (!SqlUtil.UpdateTocConverted(theName))
        {
          Console.Error.WriteLine($"Error [{libName}]: unable to update converted flag in TOC: {theName}");
        }
        return;
      }
      if (!SqlUtil.UpdateTocEdited(theName, theType, theDescription, theSyntax, "Changed", "OlbTree", DateTime.Now))
      {
        Console.Error.WriteLine($"Error [{libName}]: unable to add to TOC: {theName}");
      }
    }

    public static bool ReadAllImagesIntoFiles(ref string rootPath, ref string name, ref string[] varReturn)
    {
      bool bOk = false;
      bool isDotnet = rootPath.ToLower().Contains(@"\dotnet\");
      string imageFilePath = isDotnet
          ? Path.Combine(rootPath.ToLower(), "bitmaps")
          : rootPath.ToLower().Replace(@"\web\", @"\web\bitmaps\");
      string webImgPath = string.Empty;
      if (!isDotnet)
        webImgPath = "." + imageFilePath.Substring(imageFilePath.IndexOf(@"\bitmaps\"));
      List<string> lstFileNames = SqlUtil.ReadAllImageFiles(name, imageFilePath, true);
      string archivePath = isDotnet
          ? rootPath.ToLower().Replace(@"\dotnet\", @"\vb\")
          : rootPath.Replace(@"\web\", @"\vb\");
      List<string> lstUpdates = new List<string>();

      for (int idx = 0; idx < varReturn.Length; idx++)
      {
        string html = varReturn[idx];
        int idxTag = html.IndexOf("<img", StringComparison.CurrentCultureIgnoreCase);
        bool ucase = false;
        if (idxTag >= 0)
        {
          string tag = html.Substring(idxTag, 4);
          ucase = tag.Equals("<IMG");
          if (!ucase && !tag.Equals("<img"))
          {
            Console.Error.WriteLine($"Bad img tag: {name} [{html.Substring(idxTag, Math.Min(40, html.Length - idxTag))}]");
          }
        }
        else
        {
          continue;
        }
        MatchCollection matches = Regex.Matches(html,
            ucase
                ? @"(?<=<IMG\s+[^>]*?(.*)src=\s*(?<q>['""]))(?<url>.+?)(?=\k<q>)"
                : @"(?<=<img\s+[^>]*?(.*)src=\s*(?<q>['""]))(?<url>.+?)(?=\k<q>)");
        if (idxTag >= 0 && matches.Count == 0)
        {
          Console.Error.WriteLine($"Regex not working: {name} [{html.Substring(idxTag, Math.Min(40, html.Length - idxTag))}] {html}");
        }
        foreach (Match m in matches)
        {
          if (m.Value.IndexOf("bitmap", StringComparison.CurrentCultureIgnoreCase) < 0)
          {
            Console.Error.WriteLine($"Error: Bitmap file: bad format: {m.Value} {name}");
            continue;
          }
          string origHtmlPath = m.Value.ToLower();
          string fileName = Path.GetFileName(origHtmlPath);
          if (fileName.Equals("geomcarc.gif", StringComparison.CurrentCultureIgnoreCase))
          {
            System.Diagnostics.Debug.WriteLine(fileName);
          }
          string sNewHtmlPath = string.Empty;
          string sNewFilePath = string.Empty;
          string sNewPath = string.Empty;
          if (isDotnet)
          {
            sNewHtmlPath = Path.Combine(@".\bitmaps", fileName).Replace(@"\", "/").ToLower();
            sNewFilePath = Path.Combine(Path.Combine(rootPath, "bitmaps"), fileName).Replace("/", @"\");
            if (!m.Value.Equals(sNewHtmlPath, StringComparison.CurrentCultureIgnoreCase))
            {
              lstUpdates.Add($"{m.Value}|{sNewHtmlPath}");
            }
          }
          else
          {
            sNewHtmlPath = Path.Combine(webImgPath, fileName).Replace(@"\", "/").ToLower();
            sNewFilePath = Path.Combine(
                Path.Combine(Directory.GetParent(rootPath).FullName, webImgPath.Substring(2)), fileName).Replace("/", @"\");
            varReturn[idx] = varReturn[idx].Replace(m.Value, sNewHtmlPath);
          }
          sNewPath = Path.GetDirectoryName(sNewFilePath);
          if (!Directory.Exists(sNewPath)) Directory.CreateDirectory(sNewPath);

          if (lstFileNames.Contains(fileName))
          {
            if (!File.Exists(sNewFilePath))
            {
              Console.Error.WriteLine($"File Not Found: {name} file: {sNewFilePath}");
            }
            continue;
          }
          string archiveFilePath = Path.Combine(archivePath, fileName).Replace("/", @"\").Replace(@"\.\", @"\").ToLower();
          string archiveFolder = Directory.GetParent(archiveFilePath).FullName;
          string[] archiveBitmaps = Directory.GetFiles(Directory.GetParent(archiveFolder).FullName, fileName, SearchOption.AllDirectories);
          bool nofileFound = archiveBitmaps.Length <= 0;
          if (nofileFound)
          {
            Console.Error.WriteLine($"Error: Archive Bitmap file: {fileName} [{name}] doesn't exist in {archiveFolder}");
          }
          else
          {
            archiveFilePath = archiveBitmaps[0];
            if (SqlUtil.InsertEdited(name, fileName, "OlbTree", DateTime.Now, Path.GetExtension(fileName).ToLower(), File.ReadAllBytes(archiveFilePath)))
            {
              File.Copy(archiveFilePath, sNewFilePath, true);
              if (!origHtmlPath.Equals(sNewHtmlPath))
              {
                varReturn[idx] = varReturn[idx].Replace(fileName, sNewHtmlPath);
              }
              string str = varReturn[idx];
              int startIdx = str.IndexOf("src=", StringComparison.CurrentCultureIgnoreCase);
              if (startIdx < 0)
              {
                int iLen = 60;
                if (iLen > (str.Length - startIdx)) iLen = str.Length - startIdx;
                Console.Error.WriteLine($"Index error {name} {str.Substring(idx, iLen)}");
              }
              else
              {
                int endIdx = str.IndexOf(str.Substring(startIdx + 4, 1), startIdx + 5);
                if (endIdx < 0)
                {
                  int iLen = 60;
                  if (iLen > (str.Length - startIdx)) iLen = str.Length - startIdx;
                  Console.Error.WriteLine($"Index error {name} {str.Substring(startIdx, iLen)}");
                }
                else
                {
                  endIdx += 1;
                }
              }
              lstUpdates.Add($"{m.Value}|{sNewHtmlPath}");
            }
            else
            {
              Console.Error.WriteLine($"Error: Unable to insert Bitmap file: {archiveFilePath} into image {name}");
            }
          }
        }
      }
      if (lstUpdates.Count > 0)
      {
        if (!SqlUtil.UpdateStringDocumentFields(lstUpdates, $"name = '{name}'"))
        {
          Console.Error.WriteLine($"Error: Unable to update image tag in documentation for {name} ");
        }
      }
      return bOk;
    }

    public static void WriteVersion(ref string version)
    {
      DataRow rowVersion = SqlUtil.GetDataRow("[Version]", "[Version],[OlbTreeDate]", $"[Version] = '{version}'");
      DateTime dtNow = DateTime.Now;
      if (rowVersion == null)
      {
        if (!SqlUtil.InsertVersion(version, dtNow))
        {
          Console.WriteLine($"Error: unable to add to version: {version}");
        }
        return;
      }
      DateTime dtLastUpdate = Convert.ToDateTime(rowVersion[1]);
      if (dtNow <= dtLastUpdate) return;
      if (!SqlUtil.UpdateVersion(version, dtNow))
      {
        Console.WriteLine($"Error: unable to add to version: {version}");
      }
    }
  }
}
