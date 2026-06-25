using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Net.ServerSentEvents;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DatabaseIO
{

  public class SqlUtil
  {
    /// <summary>
    /// web sql connection string
    /// </summary>
    private static string _sqlConnectionStr = string.Empty;

    private SqlConnection sqlconnection = new();
    private string sqlerror = "";

    public SqlUtil(string sqlConnectionStr)
    {
      _sqlConnectionStr = sqlConnectionStr;
      sqlconnection = new SqlConnection(_sqlConnectionStr);
      try
      {
        sqlconnection.Open();
      }
      catch (SqlException ex)
      {
        sqlerror = $"Unable to open database {_sqlConnectionStr}: [{ex}]";
      }
    }

    public void Close()
    {
      sqlconnection?.Close();
    }

    public string GetError()
    {
      return sqlerror;
    }

    /// <summary>
    /// Query database using sql query and return a datatable
    /// </summary>
    /// <param name="query">select query with where clause [and order by]</param>
    /// <returns>datatable with query result</returns>
    public DataTable GetData(string query)
    {
      var dt = new DataTable();
      using (var cmd = new SqlCommand(query))
      {
        using (var sda = new SqlDataAdapter())
        {
          cmd.CommandType = CommandType.Text;
          cmd.Connection = sqlconnection;
          sda.SelectCommand = cmd;
          sda.Fill(dt);
        }
      }
      return dt;
    }

    /// <summary>
    /// Query database using table name and return a datatable
    /// </summary>
    /// <param name="tableName">table name to return</param>
    /// <returns>datatable with query result</returns>
    public DataTable GetDataTable(string tableName)
    {
      var dt = new DataTable();
      using (var cmd = new SqlCommand($"select * from {tableName}"))
      {
        using (var sda = new SqlDataAdapter())
        {
          cmd.CommandType = CommandType.Text;
          cmd.Connection = sqlconnection;
          sda.SelectCommand = cmd;
          sda.Fill(dt);
          return dt;
        }
      }
    }

    public DataRow GetDataRow(string selectClause)
    {
      var dt = new DataTable();
      using (var cmd = new SqlCommand(selectClause))
      {
        using (var sda = new SqlDataAdapter())
        {
          cmd.CommandType = CommandType.Text;
          cmd.Connection = sqlconnection;
          sda.SelectCommand = cmd;
          sda.Fill(dt);
          foreach (DataRow row in dt.Rows)
          {
            return row;
          }
        }
      }
      return null;
    }

    public DataRow? GetDataRowFieldData(string tableName, string[] fields, string whereClause)
    {
      var dt = new DataTable();
      string where = "";
      if (!string.IsNullOrEmpty(whereClause))
      {
        where = $"where {whereClause}";
      }
      var fieldList = string.Join(",", fields);
      using var cmd = new SqlCommand($"select {fieldList} from {tableName} {where}");
      using var sda = new SqlDataAdapter();
      cmd.CommandType = CommandType.Text;
      cmd.Connection = sqlconnection;
      sda.SelectCommand = cmd;
      sda.Fill(dt);
      foreach (DataRow row in dt.Rows)
      {
        return row;
      }
      return null;
    }

    public DataRow GetDataRow(string tableName, string whereClause)
    {
      var dt = new DataTable();
      string where = "";
      if (!string.IsNullOrEmpty(whereClause))
      {
        where = $"where {whereClause}";
      }
      using (var cmd = new SqlCommand($"select * from {tableName} {where}"))
      {
        using (var sda = new SqlDataAdapter())
        {
          cmd.CommandType = CommandType.Text;
          cmd.Connection = sqlconnection;
          sda.SelectCommand = cmd;
          sda.Fill(dt);
          foreach (DataRow row in dt.Rows)
          {
            return row;
          }
        }
      }
      return null;
    }

    public DataRow GetDataRow(string tableName, string fieldName, string whereClause)
    {
      var dt = new DataTable();
      using (var cmd = new SqlCommand($"select {fieldName} from {tableName} where {whereClause}"))
      {
        using (var sda = new SqlDataAdapter())
        {
          cmd.CommandType = CommandType.Text;
          cmd.Connection = sqlconnection;
          sda.SelectCommand = cmd;
          sda.Fill(dt);
          foreach (DataRow row in dt.Rows)
          {
            return row;
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Execute a scalar query and returns the int result
    /// </summary>
    /// <param name="countQuery">scalar query</param>
    /// <returns>int result of the scalar query</returns>
    public int GetCount(string countQuery)
    {
      int iCount = 0;
      using (var cmd = new SqlCommand(countQuery, sqlconnection))
      {
        iCount = Convert.ToInt32(cmd.ExecuteScalar());
      }
      return iCount;
    }

    public bool UpdateIntParam(string tableName, string fieldName, int intParam, string whereClause)
    {
      bool bUpdated = false;
      using (var command = sqlconnection.CreateCommand())
      {
        command.CommandText = $"UPDATE {tableName} set {fieldName}=@ParamInt where {whereClause}";
        command.Parameters.AddWithValue("@ParamInt", intParam);
        bUpdated = command.ExecuteNonQuery() == 1;
      }
      return bUpdated;
    }

    public bool UpdateStringField(string tableName, string fieldName, string newString, string whereClause)
    {
      bool bUpdated = false;
      using (var command = sqlconnection.CreateCommand())
      {
        command.CommandText = $"UPDATE {tableName} set {fieldName}=@P0 where {whereClause}";
        command.Parameters.AddWithValue("@P0", newString);
        bUpdated = command.ExecuteNonQuery() == 1;
      }
      return bUpdated;
    }

    public bool UpdateStringDocumentFields(List<string> lstReplace, string whereClause)
    {
      bool bUpdated = false;
      List<string> lstFields = new List<string>
        {
            "Long Description",
            "Remarks",
            "SeeAlsos",
            "When To Use",
            "C#-specific",
            "C++-specific",
            "VBNET-specific",
            "Java-specific",
            "VB6-specific"
        };
      var cmd = $"select * from [Documentation] where {whereClause}";
      var docAdapter = new SqlDataAdapter(cmd, sqlconnection);
      var docDataSet = new DataSet();
      docAdapter.Fill(docDataSet);
      var docDt = docDataSet.Tables[0];

      foreach (DataRow docRow in docDt.Rows)
      {
        foreach (string field in lstFields)
        {
          if (docRow.IsNull(field)) continue;
          foreach (string searchReplace in lstReplace)
          {
            string[] parts = searchReplace.Split('|');
            int idx = docRow[field].ToString().IndexOf(parts[0], StringComparison.CurrentCultureIgnoreCase);
            if (idx < 0) continue;
            string str = docRow[field].ToString();
            docRow[field] = str.Replace(parts[0], parts[1]);
            if (idx >= 0)
            {
              int length = parts[0].Length;
              Console.WriteLine($"{whereClause} {field} {str.Substring(idx, length)}");
            }
          }
        }
      }
      var t = new SqlCommandBuilder(docAdapter);
      docAdapter.UpdateCommand = t.GetUpdateCommand(true);
      int updateCount = docAdapter.Update(docDataSet);
      bUpdated = updateCount > 0;
      if (bUpdated)
      {
        Console.WriteLine($"update img tag in doc fields count: {updateCount}");
      }
      else
      {
        Console.Error.WriteLine($"unable to update img tags: {whereClause}");
      }
      return bUpdated;
    }

    /// <summary>
    /// Update the edited by,date columns
    /// </summary>
    /// <param name="name">uniquely identifies the record to update</param>
    /// <param name="editedBy">New text for editedby field</param>
    /// <param name="editedTime">New time for editedDate field</param>
    /// <returns>true if updated</returns>
    public bool UpdateTocEdited(string name, string nametype, string description, string syntax, string status, string editedBy, DateTime editedTime)
    {
      bool bUpdated = false;
      using (var command = sqlconnection.CreateCommand())
      {
        command.CommandText = $"UPDATE toc Set [nametype]=@p0,[description]=@p1,[syntax]=@p2,[Status]=@p3,[EditedBy]=@p4,[EditedDate]=@p5,converted=@p6 where name = '{name}'";
        command.Parameters.AddWithValue("@p0", nametype);
        command.Parameters.AddWithValue("@p1", string.IsNullOrEmpty(description) ? "" : description);
        command.Parameters.AddWithValue("@p2", string.IsNullOrEmpty(syntax) ? "" : syntax);
        command.Parameters.AddWithValue("@p3", status);
        command.Parameters.AddWithValue("@p4", editedBy);
        command.Parameters.AddWithValue("@p5", editedTime.ToUniversalTime());
        command.Parameters.AddWithValue("@p6", true);
        bUpdated = command.ExecuteNonQuery() == 1;
      }
      return bUpdated;
    }

    /// <summary>
    /// Update the Converted column to true
    /// </summary>
    /// <param name="name">uniquely identifies the record to update</param>
    /// <returns>true if updated</returns>
    public bool UpdateTocConverted(string name)
    {
      bool bUpdated = false;
      using (var command = sqlconnection.CreateCommand())
      {
        command.CommandText = $"UPDATE toc Set converted=@p0 where name = '{name}'";
        command.Parameters.AddWithValue("@p0", true);
        bUpdated = command.ExecuteNonQuery() == 1;
      }
      return bUpdated;
    }

    public bool InsertTocEdited(string library, string name, string nametype, string description, string syntax, string status, string editedBy, DateTime editedTime)
    {
      bool bInserted = false;
      using (var command = sqlconnection.CreateCommand())
      {
        command.CommandText = "INSERT INTO toc ([library],[name],[nametype],[description],[syntax],[Status],[EditedBy],[EditedDate],[converted]) VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8)";
        command.Parameters.AddWithValue("@p0", library);
        command.Parameters.AddWithValue("@p1", name);
        command.Parameters.AddWithValue("@p2", nametype);
        command.Parameters.AddWithValue("@p3", string.IsNullOrEmpty(description) ? "" : description);
        command.Parameters.AddWithValue("@p4", string.IsNullOrEmpty(syntax) ? "" : syntax);
        command.Parameters.AddWithValue("@p5", status);
        command.Parameters.AddWithValue("@p6", editedBy);
        command.Parameters.AddWithValue("@p7", editedTime.ToUniversalTime());
        command.Parameters.AddWithValue("@p8", true);
        bInserted = command.ExecuteNonQuery() == 1;
      }
      return bInserted;
    }

    /// <summary>
    /// Read all images from the image table using a given name key into a given root path
    /// </summary>
    /// <param name="name"></param>
    /// <param name="path"></param>
    /// <param name="overWrite">true to overwrite old data</param>
    /// <returns>list of file names</returns>
    public List<string> ReadAllImageFiles(string name, string path, bool overWrite)
    {
      string query = $"SELECT Image, FileName FROM Image where name = '{name}' and archived = 0";
      List<string> lstFileNames = new List<string>();
      using (var cmd = sqlconnection.CreateCommand())
      {
        cmd.CommandText = query;
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
          string fileName = dr["FileName"].ToString();
          string filePath = Path.Combine(path, fileName);
          lstFileNames.Add(fileName);
          if (!Directory.Exists(path)) Directory.CreateDirectory(path);
          if (File.Exists(filePath))
          {
            if (!overWrite) continue;
            File.Delete(filePath);
          }
          File.WriteAllBytes(filePath, (byte[])dr["Image"]);
        }
      }
      return lstFileNames;
    }

    /// <summary>
    /// Insert New image record
    /// </summary>
    /// <param name="name"></param>
    /// <param name="fileName"></param>
    /// <param name="editedBy"></param>
    /// <param name="editedDate"></param>
    /// <param name="imageType"></param>
    /// <param name="image"></param>
    /// <returns>return true if inserted; false otherwise</returns>
    public bool InsertEdited(string name, string fileName, string editedBy, DateTime editedDate, string imageType, byte[] image)
    {
      bool bUpdated = false;
      using (var command = sqlconnection.CreateCommand())
      {
        command.CommandText = "Insert INTO Image (Id,name,fileName,imageType,image,editedBy,EditedDate,archived) VALUES (@Id,@name,@fileName,@imageType,@image,@editedBy,@EditedDate,@archived)";
        command.Parameters.AddWithValue("@Id", Guid.NewGuid());
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@fileName", fileName);
        command.Parameters.AddWithValue("@imageType", imageType);
        command.Parameters.AddWithValue("@image", image);
        command.Parameters.AddWithValue("@EditedBy", editedBy);
        command.Parameters.AddWithValue("@EditedDate", editedDate.ToUniversalTime());
        command.Parameters.AddWithValue("@archived", false);
        bUpdated = command.ExecuteNonQuery() == 1;
      }
      return bUpdated;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="version"></param>
    /// <param name="updatedTime"></param>
    /// <returns>true if updated</returns>
    public bool UpdateVersion(string version, DateTime updatedTime)
    {
      bool bUpdated = false;
      using (var command = sqlconnection.CreateCommand())
      {
        command.CommandText = $"UPDATE version set [version]=@p0,[OlbTreeDate]=@p1 where version = '{version}'";
        command.Parameters.AddWithValue("@p0", version);
        command.Parameters.AddWithValue("@p1", updatedTime.ToUniversalTime());
        bUpdated = command.ExecuteNonQuery() == 1;
      }
      return bUpdated;
    }

    public bool InsertVersion(string version, DateTime updatedTime)
    {
      bool bInserted = false;
      using (var command = sqlconnection.CreateCommand())
      {
        command.CommandText = "INSERT INTO version ([version],[OlbTreeDate]) VALUES (@p0,@p1)";
        command.Parameters.AddWithValue("@p0", version);
        command.Parameters.AddWithValue("@p1", updatedTime.ToUniversalTime());
        bInserted = command.ExecuteNonQuery() == 1;
      }
      return bInserted;
    }

   }
}