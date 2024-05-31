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
              _____
              | __ \  __    __               _____    __       ___
              | -- |  \ \  / /               | ___| * | | ___ / __|  ___               ___
              |____|   \ \/ /___ __  __   ___| |__ __ | |/ . \\__ \ / . \  ___ __  __ / . \  ___
             ________   |  |/   \| | | | / _|| ___|| || || __/   \ \| __/ / _| \ \/ / | __/ / _|
            /_______/|  |  || | || \_/ || /  | |   | || || |_ ___/ || |_ | /    \  /  | |_ | /
            |____::| |  |__|\___/ \___/ |_|  |_|   |_||_|\___||___/ \___||_|     \/   \___||_|
            |"_____|/
                                                  BETA 0.4

                             [1] Создать сервер     [2] Подключиться к серверу
            
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
