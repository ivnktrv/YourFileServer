using System.Data;
using System.Data.SQLite;

namespace yfs_db;

public class YFSdb
{
    private string DBFile;
    private SQLiteConnection DBConnect;
    private SQLiteCommand DBCommand;

    public YFSdb()
    {
        DBFile = "YFShost.db";
        DBConnect = new($"Data Source={DBFile}");
        DBCommand = new SQLiteCommand();
    }

    public void requestToDB(string request)
    {
        if (!File.Exists(DBFile))
            SQLiteConnection.CreateFile(DBFile);

        DBConnect.Open();
        DBCommand.Connection = DBConnect;
        DBCommand.CommandText = request;
        DBCommand.ExecuteNonQuery();
        DBConnect.Close();
    }

    public DataTable getAnswer()
    {
        SQLiteDataAdapter adapter = new(DBCommand);
        DataTable answer = new();
        adapter.Fill(answer);
        
        return answer;
    }
}

