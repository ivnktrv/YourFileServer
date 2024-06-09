using yfs_security;
using yfs_server;
using yfs_client;

namespace yfs;

internal class YFSmain
{
    static void Main(string[] args)
    {
        YFSserver server = new();
        YFSclient client = new();
        YFSsec sec = new();

        Console.Write("""
              _____
              | __ \  __    __               _____    __       ___
              | -- |  \ \  / /               | ___| * | | ___ / __|  ___               ___
              |____|   \ \/ /___ __  __   ___| |__ __ | |/ . \\__ \ / . \  ___ __  __ / . \  ___
             ________   |  |/   \| | | | / _|| ___|| || || __/   \ \| __/ / _| \ \/ / | __/ / _|
            /_______/|  |  || | || \_/ || /  | |   | || || |_ ___/ || |_ | /    \  /  | |_ | /
            |____::| |  |__|\___/ \___/ |_|  |_|   |_||_|\___||___/ \___||_|     \/   \___||_|
            |"_____|/
                                               VER 1.1 (09062024)

              [1] Создать сервер    [2] Подключиться к серверу    [3] Создать файл авторизации
            
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
        else if (key.Key == ConsoleKey.NumPad3 || key.Key == ConsoleKey.D3)
        {
            sec.createAuthFile();
        }
    }
}
