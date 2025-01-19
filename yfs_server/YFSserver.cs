using System.Net.Sockets;
using yfs_security;
using System.Text;
using yfs_net;
using yfs_io;
using System.Net;

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
        #region чтение файла .startup
        io.clearTerminal();
        Dictionary<string, string>? startupConfig = io.readConfigFile($"yfs_{Environment.MachineName}.startup");

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
        #endregion
        
        setDir = rootDir;

        io.clearTerminal();
        Console.WriteLine($"##### IP: {setIP}, PORT: {setPort} #####\n");
        Console.WriteLine($"[{DateTime.Now}] [i] Ожидаю подключения");

        while (true)
        {
            while (true)
            {
                Socket socket = net.createServer(setIP, setPort);
                Socket connClient = socket.Accept();

                try
                {
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
                }
                catch (SocketException) { }

                Console.WriteLine($"\n[{DateTime.Now}] [i] Подключён клиент (IP: {net.HideIP(((IPEndPoint)connClient.RemoteEndPoint).Address)}***)");

                while (true)
                {
                    byte[] getCommand = net.getData(connClient);
                    string cmd = Encoding.UTF8.GetString(getCommand);

                    if (cmd == "list")
                    {
                        //Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: отправить список файлов");
                        io.getFiles(connClient, setDir);
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
                            Console.WriteLine($"[i] Клиент {net.HideIP(((IPEndPoint)connClient.RemoteEndPoint).Address)} отключился. Жду нового клиента");
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

                        if (getDir == "/" || getDir == ".." || getDir.Contains(".."))
                            setDir = rootDir;
                        else
                            setDir += "/" + getDir;
                    }

                    else if (cmd == "fileinfo")
                    {
                        //Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: отправить информацию о файле");
                        string file = Encoding.UTF8.GetString(net.getData(connClient));
                        io.getFileInfo(connClient, setDir+'/'+file);
                    }

                    else if (cmd == "closeconn")
                    {
                        Console.WriteLine($"[{DateTime.Now}] [i] Клиент {net.HideIP(((IPEndPoint)connClient.RemoteEndPoint).Address)}*** отключился. Жду нового клиента");
                        connClient.Close();
                        socket.Close();
                        break;
                    }
                }
            }
        }
    }
}
