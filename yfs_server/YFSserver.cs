//
// Класс YFSserver предназначен для управления сервером, который обрабатывает подключения
// клиентов и выполняет различные команды.
// 
// Команды, которые принимает сервер: 
//
// • list: Получение списка файлов и директорий в текущей директории.
// • upload: Загрузка файла на сервер.
// • download: Скачивание файла с сервера.
// • deletefile: Удаление файла с сервера.
// • cd: Переход в указанную директорию.
// • createdir: Создание новой директории.
// • deletedir: Удаление директории.
// • fileinfo: Получение информации о файле.
// • closeconn: Закрытие соединения с клиентом.
//

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

        Socket socket = net.createServer(setIP, setPort);

        while (true)
        {
            while (true)
            {
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
                        connClient.Dispose();
                        //socket.Close();
                        break;
                    }
                }
                catch (SocketException) { }

                Console.WriteLine($"\n[{DateTime.Now}] [i] Подключён клиент (IP: {sec.HideIP(((IPEndPoint)connClient.RemoteEndPoint).Address)}***)");

                Task.Run(async () =>
                {
                    while (true)
                    {
                        byte[] getCommand = await net.getDataAsync(connClient);
                        string cmd = Encoding.UTF8.GetString(getCommand);

                        if (cmd == "list")
                        {
                            await io.getFiles(connClient, setDir);
                        }

                        else if (cmd == "upload")
                        {
                            Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: загрузить файл на сервер");
                            await io.downloadFile(connClient, setDir, isServer: true);
                        }

                        else if (cmd == "download")
                        {
                            Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: скачать файл с сервера");
                            byte[] getFileName_bytes = await net.getDataAsync(connClient);
                            string getFileName = Encoding.UTF8.GetString(getFileName_bytes);

                            try
                            {
                                await io.uploadFile(connClient, getFileName, folder: setDir, isServer: true);
                            }
                            catch (SocketException)
                            {
                                Console.WriteLine($"[i] Клиент {sec.HideIP(((IPEndPoint)connClient.RemoteEndPoint).Address)} отключился. Жду нового клиента");
                                connClient.Close();
                                socket.Close();
                                break;
                            }
                        }

                        else if (cmd == "deletefile")
                        {
                            Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: удалить файл с сервера");
                            byte[] getDelFile = await net.getDataAsync(connClient);
                            string delFile = Encoding.UTF8.GetString(getDelFile);

                            if (delFile == "deletefile:abort")
                            {
                                Console.WriteLine($"[{DateTime.Now}] [i] Удаление файла отменено");
                            }
                            else
                            {
                                if (File.Exists($"{setDir}/{delFile}"))
                                {
                                    File.Delete($"{setDir}/{delFile}");
                                    Console.WriteLine($"[{DateTime.Now}] [i] Файл удалён: {delFile}");
                                }
                                else
                                {
                                    Console.WriteLine($"[{DateTime.Now}] [-] Файл не найден: {delFile}");
                                }
                            }
                        }

                        else if (cmd == "cd")
                        {
                            Console.WriteLine($"[{DateTime.Now}] [i] Получена команда: перейти в директорию");
                            byte[] getDir_bytes = await net.getDataAsync(connClient);
                            string getDir = Encoding.UTF8.GetString(getDir_bytes);

                            if (getDir == "/" || getDir == ".." || getDir.Contains(".."))
                                setDir = rootDir;
                            else
                                setDir += "/" + getDir;
                        }

                        else if (cmd == "createdir")
                        {
                            byte[] getDirName_bytes = await net.getDataAsync(connClient);
                            string getDirName = Encoding.UTF8.GetString(getDirName_bytes);
                            Directory.CreateDirectory(setDir + '/' + getDirName);
                            Console.WriteLine($"[{DateTime.Now}] [i] Создана директория: {getDirName}");
                        }

                        else if (cmd == "deletedir")
                        {
                            byte[] getDirName_bytes = await net.getDataAsync(connClient);
                            string getDirName = Encoding.UTF8.GetString(getDirName_bytes);
                            if (getDirName == "deletedir:abort") 
                            {
                                Console.WriteLine($"[{DateTime.Now}] [i] Удаление директории отменено");
                            }
                            else
                            {
                                if (Directory.Exists(setDir + '/' + getDirName))
                                {
                                    Directory.Delete(setDir + '/' + getDirName, true);
                                    Console.WriteLine($"[{DateTime.Now}] [i] Директория удалена: {getDirName}");
                                }
                                else
                                {
                                    Console.WriteLine($"[{DateTime.Now}] [-] Директория не найдена: {getDirName}");
                                }
                            }
                        }

                        else if (cmd == "fileinfo")
                        {
                            string file = Encoding.UTF8.GetString(await net.getDataAsync(connClient));
                            await io.getFileInfo(connClient, setDir + '/' + file);
                        }

                        else if (cmd == "closeconn")
                        {
                            Console.WriteLine($"[{DateTime.Now}] [i] Клиент {sec.HideIP(((IPEndPoint)connClient.RemoteEndPoint).Address)}*** отключился. Жду нового клиента");
                            connClient.Close();
                            break;
                        }
                    }
                });
            }
        }
    }
}
