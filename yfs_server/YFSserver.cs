using System.Net.Sockets;
using yfs_security;
using System.Text;
using yfs_net;
using yfs_io;

namespace yfs_server;

public class YFSserver
{
    YFSnet net = new();
    YFSio  io  = new();
    YFSsec sec = new();
    
    private string rootDir;
    private string setDir;
    private string setIP;
    private int setPort;

    public void run()
    {
        io.clearTerminal();
        Dictionary<string, string> startupConfig = io.readStartupFile("yfs_server.startup");
        if (startupConfig["useStartupFile"] == "no")
        {
            Console.Write("Какую папку выделить для сервера?: ");
            rootDir = Console.ReadLine();
            Console.Write("Укажите порт: ");
            setPort = int.Parse(Console.ReadLine());

            io.clearTerminal();

            setIP = net.getIP();
        }
        else if (startupConfig["useStartupFile"] == "yes")
        {
            rootDir = startupConfig["rootDir"];
            setPort = int.Parse(startupConfig["serverPort"]);
            setIP = startupConfig["useIP"];
        }

        setDir = rootDir;
        Console.WriteLine($"##### IP: {setIP}, ПОРТ: {setPort} #####\n");
        Console.WriteLine($"[{DateTime.Now}] [i] Ожидаю подключения\n");

        while (true)
        {
            while (true)
            {
                Socket socket = net.createServer(setIP, setPort);
                Socket connClient = socket.Accept();

                bool auth = sec.checkAuthData(connClient);
                if (auth)             // если логин и пароль верны, отправляем 1 (данные верны)
                {
                    byte[] a = { 1 };
                    connClient.Send(a);
                }
                else       // иначе: 0 (данные неверны)
                {
                    byte[] a = { 0 };
                    connClient.Send(a);
                    connClient.Close();
                    socket.Close();
                    break;
                }


                Console.WriteLine($"[{DateTime.Now}] [i] Подключён клиент (IP: {connClient.RemoteEndPoint})");

                while (true)
                {
                    byte[] getCommand = net.getData(connClient);
                    string cmd = Encoding.UTF8.GetString(getCommand);

                    if (cmd == "list")
                    {
                        Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: отправить список файлов");
                        string[] getDirsAndFiles = io.getFiles(setDir);
                        foreach (string items in getDirsAndFiles)
                        {
                            byte[] b = Encoding.UTF8.GetBytes(items);
                            connClient.Send(b);
                        }
                    }

                    else if (cmd == "upload")
                    {
                        Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: загрузить файл на сервер");
                        io.downloadFile(connClient, setDir, isServer: true);
                    }

                    else if (cmd == "download")
                    {
                        Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: скачать файл с сервера");
                        byte[] getFileName_bytes = net.getData(connClient);
                        string getFileName = Encoding.UTF8.GetString(getFileName_bytes);

                        try
                        {
                            io.uploadFile(connClient, getFileName, folder: setDir, isServer: true);
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine($"[i] Клиент {connClient.RemoteEndPoint} отключился. Жду нового клиента");
                            connClient.Close();
                            socket.Close();
                            break;
                        }
                    }

                    else if (cmd == "delete")
                    {
                        Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: удалить файл с сервера");
                        byte[] getDelFile = net.getData(connClient);

                        File.Delete($"{setDir}/{Encoding.UTF8.GetString(getDelFile)}");
                        Console.WriteLine($"[{DateTime.Now}] [i] Файл удалён: {Encoding.UTF8.GetString(getDelFile)}");
                    }

                    else if (cmd == "cd")
                    {
                        Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: перейти в каталог");
                        byte[] getDir_bytes = net.getData(connClient);
                        string getDir = Encoding.UTF8.GetString(getDir_bytes);

                        if (getDir == "/")
                            setDir = rootDir;
                        else
                            setDir += "/" + getDir;
                    }

                    else if (cmd == "closeconn")
                    {
                        Console.WriteLine($"[{DateTime.Now}] [i] Клиент {connClient.RemoteEndPoint} отключился. Жду нового клиента");
                        connClient.Close();
                        socket.Close();
                        break;
                    }
                }
            }
        }
    }
}
