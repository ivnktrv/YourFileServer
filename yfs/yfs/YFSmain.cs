//using yfs_db;
using yfs_server;
using yfs_client;

namespace yfs;

internal class YFSmain
{
    static void Main(string[] args)
    {
        //YFSdb  db  = new();
        YFSserver server = new();
        YFSclient client = new();
        /*
        db.requestToDB("""
        CREATE TABLE IF NOT EXISTS hostInfo (
            hostname TEXT,
            ip TEXT
        )
        """);
        db.requestToDB("SELECT ");
        db.requestToDB($"INSERT INTO hostInfo VALUES ('{Dns.GetHostName()}', '{net.getIP()}')");
        */

        Console.Write("""
            1 - Создать сервер
            2 - Подключиться к серверу
            
            -> 
            """);
        ConsoleKeyInfo key = Console.ReadKey();
        
        if (key.Key == ConsoleKey.NumPad1 || key.Key == ConsoleKey.D1)
        {
            server.run();
        }
        else if (key.Key == ConsoleKey.NumPad2 || key.Key == ConsoleKey.D2)
        {
            client.run();
        }
    }
}
