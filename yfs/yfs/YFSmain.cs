using yfs_security;
using yfs_server;
using yfs_client;
using System.Text;
using System.Diagnostics;

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
                                               VER 2.2 (03072024)

              [1] Создать сервер    [2] Подключиться к серверу    [3] Создать файл авторизации
                              [4] Создать файл конфигурации запуска сервера
            
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
        else if (key.Key == ConsoleKey.NumPad4 || key.Key == ConsoleKey.D4)
        {
            Console.Clear();
            Console.WriteLine("\x1b[3J");
            Console.WriteLine("##### СОЗДАНИЕ ФАЙЛА КОНФИГУРАЦИИ ЗАПУСКА СЕРВЕРА #####\n");
            Console.Write("Папка, которая будет выделяться для сервера: ");
            string rootDir = Console.ReadLine();
            Console.Write("Порт: ");
            string port = Console.ReadLine();
            Console.Write("IP: ");
            string ip = Console.ReadLine();

            using FileStream fs = new($"yfs_{Environment.MachineName}.startup", FileMode.Create, FileAccess.Write);
            fs.Write(Encoding.UTF8.GetBytes($"""
                useStartupFile=yes
                rootDir={rootDir}
                serverPort={port}
                useIP={ip}
                """));
        }
    
        else if (key.Key == ConsoleKey.G)
        {
            ProcessStartInfo github = new ProcessStartInfo("https://github.com/ivnktrv/YourFileServer");
            github.UseShellExecute = true;
            Process.Start(github);
        }
    }
}
