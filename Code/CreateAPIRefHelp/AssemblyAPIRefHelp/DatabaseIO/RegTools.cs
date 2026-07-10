using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseIO
{
  using Microsoft.Win32;
  using System;

  internal class RegTools
  {
    public void CreateNewKey(string newKeyName, RegistryHive predefinedKey)
    {
      using (var baseKey = RegistryKey.OpenBaseKey(predefinedKey, RegistryView.Default))
      {
        using (var newKey = baseKey.CreateSubKey(newKeyName, true))
        {
          // Key created or opened. No further action needed.
        }
      }
    }

    public void SetKeyValue(string keyName, string valueName, object valueSetting, RegistryValueKind valueType)
    {
      using (var key = Registry.CurrentUser.OpenSubKey(keyName, true))
      {
        if (key != null)
        {
          key.SetValue(valueName, valueSetting, valueType);
        }
      }
    }

    public string QueryValue(string keyName, string valueName)
    {
      using (var key = Registry.CurrentUser.OpenSubKey(keyName))
      {
        if (key != null)
        {
          var value = key.GetValue(valueName);
          return value?.ToString() ?? string.Empty;
        }
      }
      return string.Empty;
    }
  }
  public static class RegToolUtils
  {
    public static string OlbRegistrySettings => @"Software\ESRI\OLBTreeDotNet_DailyBuild\Settings";

    /// <summary>
    /// Returns the value of a registry key as a string, or empty string if not found or error.
    /// </summary>
    public static string GetRegistryStringValue(string sSubKey)
    {
      try
      {
        using (var key = Registry.CurrentUser.OpenSubKey(OlbRegistrySettings))
        {
          var result = key?.GetValue(sSubKey);
          if (result != null) return result.ToString();
        }
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(
            $@"Error trying to get registry key @: {OlbRegistrySettings} value {sSubKey}: {ex}");
      }
      return string.Empty;
    }
  }
}
