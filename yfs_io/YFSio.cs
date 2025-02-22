//
// Класс YFSio предназначен для выполнения операций ввода-вывода в файловой системе и взаимодействия по сети.
// Он включает в себя следующие основные методы:
//
// • getFiles: Асинхронно получает список файлов и директорий в указанной
// директории и отправляет их по сокету.
//
// • uploadFile: Асинхронно загружает файл на сервер, с возможностью шифрования.
//
// • downloadFile: Асинхронно скачивает файл с сервера
// и сохраняет его в указанную директорию.
//
// • getFileInfo: Асинхронно получает информацию о файле и отправляет её по сокету.
//
// • writeKeytableFile: Записывает ключ шифрования для файла в таблицу ключей.
//
// • readConfigFile: Читает конфигурационный файл и возвращает данные в виде словаря.
//
// • clearTerminal: Очищает терминал.
//

using System.Net;
using System.Net.Sockets;
using System.Text;
using yfs_security;
using yfs_net;

namespace yfs_io;

public class YFSio
{
    YFSsec sec = new();
    YFSnet net = new();

    private readonly int PACKET_SIZE = 512;

    /// <summary>
    /// Получение списка файлов и директорий в указанной директории
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="dirName"></param>
    /// <returns></returns>
    public async Task getFiles(Socket __socket, string dirName)
    {
        try
        {
            string[] dirs = Directory.GetDirectories(dirName);
            string[] files = Directory.GetFiles(dirName);

            foreach (string getDir in dirs)
            {
                await net.sendDataAsync(__socket, "(DIR) " + new DirectoryInfo(getDir).Name);
            }

            foreach (string getFile in files)
            {
                await net.sendDataAsync(__socket, Path.GetFileName(getFile));
            }

            await net.sendDataAsync(__socket, ":END_OF_LIST");
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Директория не найдена: {dirName}");
            await net.sendDataAsync(__socket, $"\nДиректории {dirName} не существует. Чтобы выйти, смените директорию на /");
            await net.sendDataAsync(__socket, ":END_OF_LIST");

        }
        catch (IOException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Некорректное имя директории: {dirName}");
            await net.sendDataAsync(__socket, $"Некорректное имя директории: {dirName}. Чтобы выйти, смените директорию на /");
            await net.sendDataAsync(__socket, ":END_OF_LIST");
        }
    }

    /// <summary>
    /// Загрузка файла на сервер / клиент
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="file"></param>
    /// <param name="folder"></param>
    /// <param name="isServer"></param>
    /// <param name="encryptFile"></param>
    /// <returns></returns>
    public async Task uploadFile(Socket __socket, string file, 
        string? folder = null, bool isServer = false, bool encryptFile = false)
    {
        try
        {
            string path;
            if (folder == null)
                path = file;
            else
                path = $"{folder}/{file}";

            string keyFile = "";
            if (!isServer && encryptFile)
            {
                keyFile = sec.encryptFile(path);
                file = file + ".enc";
                path += ".enc";
            }

            BinaryReader b = new(File.Open(path, FileMode.Open));
            
            if (isServer)
                Console.WriteLine($"[{DateTime.Now}] [...] Подготовка к отправке файла: {file}");

            // ----- ОТПРАВКА ИНФОРМАЦИИ О ФАЙЛЕ -----

            byte[] getFileName = Encoding.UTF8.GetBytes(Path.GetFileName(file));
            byte[] getFileName_arrLength = { (byte)getFileName.Length };
            byte[] getFileLength = BitConverter.GetBytes(b.BaseStream.Length);
            long packetCount = b.BaseStream.Length / PACKET_SIZE;
            int remainderBytes = (int)(b.BaseStream.Length % PACKET_SIZE);
            byte[] getPacketCount = BitConverter.GetBytes(packetCount);
            byte[] getRemainderBytes = BitConverter.GetBytes(remainderBytes);
            b.Close();
            byte[] getFileChecksum = Encoding.UTF8.GetBytes(sec.checksumFileSHA256(path));

            await __socket.SendAsync(getFileName_arrLength);
            await __socket.SendAsync(getFileName);
            await __socket.SendAsync(getFileLength);
            await __socket.SendAsync(getFileChecksum);
            await __socket.SendAsync(getPacketCount);
            await __socket.SendAsync(getRemainderBytes);

            // ------------------------------------------

            if (isServer)
                Console.WriteLine($"[{DateTime.Now}] [...] Файл отправляется: {file}");
            
            BinaryReader br = new(File.Open(path, FileMode.Open));
            br.BaseStream.Position = 0;

            long c = 0;
            for (long i = 0; i <= packetCount; i++)
            {
                byte[] data = br.ReadBytes(PACKET_SIZE);
                await __socket.SendAsync(data);
                if (!isServer && c % 8192 == 0)
                {
                    clearTerminal();
                    Console.WriteLine($"[UPLOAD] {Path.GetFileName(path)} [{br.BaseStream.Position / 1024} kb / {br.BaseStream.Length / 1024} kb]");
                }
                c += 128;
            }
            if (remainderBytes != 0)
            {
                byte[] sendRemainderBytes = br.ReadBytes(remainderBytes);
                await __socket.SendAsync(sendRemainderBytes);
            }
            br.Close();

            Console.WriteLine(isServer ? $"[{DateTime.Now}] [...] Проверка контрольной суммы"
            : "\n[...] Проверка контрольной суммы");

            byte[] uploadedFileChecksum = new byte[64];
            await __socket.ReceiveAsync(uploadedFileChecksum);

            if (Encoding.UTF8.GetString(getFileChecksum) != Encoding.UTF8.GetString(uploadedFileChecksum))
            {
                if (!isServer)
                {
                    Console.Write($"""
                    [!] Хеши не совпадают. Возможно, файл при отправки на сервер был повреждён.

                    SHA-256:
                        {Encoding.UTF8.GetString(getFileChecksum)} (отправленный файл) 
                                        !=
                        {Encoding.UTF8.GetString(uploadedFileChecksum)} (файл на сервере)
            
                    [1] Удалить файл  [2] Оставить
                    
                    => 
                    """);
                    ConsoleKeyInfo key = Console.ReadKey();

                    if (key.Key == ConsoleKey.NumPad1 || key.Key == ConsoleKey.D1)
                    {
                        byte[] sendAnswer = { 1 };
                        await __socket.SendAsync(sendAnswer);
                    }

                    else
                    {
                        byte[] sendAnswer = { 0 };
                        await __socket.SendAsync(sendAnswer);
                    }
                }
                else
                {
                    Console.WriteLine($"""
                    [{DateTime.Now}] [-] Хеши не совпадают

                    SHA-256:
                        {Encoding.UTF8.GetString(getFileChecksum)} (отправленный файл)
                                        !=
                        {Encoding.UTF8.GetString(uploadedFileChecksum)} (файл на клиенте)
                    
                    """);
                }
            }

            else
            {
                if (isServer)
                {
                    Console.WriteLine($"""
                    [{DateTime.Now}] [+] Хеши совпадают

                    SHA-256:
                        {Encoding.UTF8.GetString(getFileChecksum)} (отправленный файл)
                                        =
                        {Encoding.UTF8.GetString(uploadedFileChecksum)} (файл на клиенте)
                   
                    """);
                }
            }

            if (!isServer && encryptFile)
            {
                writeKeytableFile(__socket, keyFile, file.Remove(0, 1));
                File.Delete(path);
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Файл не найден: {file}");
            byte[] getFileName_arrLengthZero = { 0 };
            await __socket.SendAsync(getFileName_arrLengthZero);
        }
        catch (IOException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Некорректное имя файла: {file}. Отменяю отправку");
            byte[] getFileName_arrLengthZero = { 0 };
            await __socket.SendAsync(getFileName_arrLengthZero);
        }
    }

    /// <summary>
    /// Скачивание файла с сервера / клиента
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="saveFolder"></param>
    /// <param name="isServer"></param>
    /// <returns></returns>
    public async Task downloadFile(Socket __socket, string saveFolder, 
        bool isServer = false)
    {
        byte[] getFileNameArrayLength = new byte[1];
        await __socket.ReceiveAsync(getFileNameArrayLength);

        if (getFileNameArrayLength[0] == 0)
        {
            Console.WriteLine(isServer ? $"[{DateTime.Now}] [i] Клиент пытался отправить не существующий у себя файл" : "[-] Файл не найден");
            return;
        }

        byte[] getFileName = new byte[getFileNameArrayLength[0]];
        await __socket.ReceiveAsync(getFileName);

        byte[] getFileLength = new byte[8];
        await __socket.ReceiveAsync(getFileLength);

        byte[] getFileChecksum = new byte[64];
        await __socket.ReceiveAsync(getFileChecksum);

        byte[] getCountPacket = new byte[8];
        await __socket.ReceiveAsync(getCountPacket);

        byte[] getReaminderBytes = new byte[4];
        await __socket.ReceiveAsync(getReaminderBytes);

        string savePath = $"{saveFolder}/{Encoding.UTF8.GetString(getFileName)}";
        using BinaryWriter br = new(File.Open(savePath, FileMode.OpenOrCreate));
        long fLength = BitConverter.ToInt64(getFileLength);
        long packetCount = BitConverter.ToInt64(getCountPacket);
        int remainderBytes = BitConverter.ToInt32(getReaminderBytes);

        if (isServer)
            Console.WriteLine($"[{DateTime.Now}] [i] Принимаю файл: {Encoding.UTF8.GetString(getFileName)}");

        long c = 0;
        for (long i = 0; i < packetCount; i++)
        {
            byte[] receiveData = new byte[PACKET_SIZE];
            await __socket.ReceiveAsync(receiveData);
            br.Write(receiveData);
            if (!isServer && c % 8192 == 0)
            {
                clearTerminal();
                Console.WriteLine($"[DOWNLOAD] {Encoding.UTF8.GetString(getFileName)} [{br.BaseStream.Position / 1024} кб / {fLength / 1024} кб]");
            }
            c += 128;
        }
        if (remainderBytes != 0)
        {
            byte[] receiveRemainderBytes = new byte[remainderBytes];
            await __socket.ReceiveAsync(receiveRemainderBytes);
            br.Write(receiveRemainderBytes);
        }
        br.Close();
        
        string downloadedFileChecksum = sec.checksumFileSHA256(savePath);
        byte[] downloadedFileChecksum_bytes = Encoding.UTF8.GetBytes(downloadedFileChecksum);
        await __socket.SendAsync(downloadedFileChecksum_bytes);

        Console.WriteLine(isServer ? $"[{DateTime.Now}] [...] Проверка контрольной суммы" 
            : "\n[...] Проверка контрольной суммы");
        if (downloadedFileChecksum != Encoding.UTF8.GetString(getFileChecksum))
        {
            if (!isServer)
            {
                Console.Write($"""
                [!] Хеши не совпадают. Возможно, файл при скачивании был повреждён.

                SHA-256:
                    {downloadedFileChecksum} (скачанный файл) 
                                !=
                    {Encoding.UTF8.GetString(getFileChecksum)} (файл на сервере)
            
                [1] Удалить файл  [2] Оставить
            
                => 
                """);
                ConsoleKeyInfo key = Console.ReadKey();

                if (key.Key == ConsoleKey.NumPad1 || key.Key == ConsoleKey.D1)
                {
                    File.Delete(savePath);
                }

                else if (key.Key == ConsoleKey.NumPad2 || key.Key == ConsoleKey.D2) { }
            }
            else
            {
                Console.WriteLine($"""
                    [{DateTime.Now}] [-] Хеши не совпадают

                    SHA-256:
                        {downloadedFileChecksum} (скачанный файл)
                                    !=
                        {Encoding.UTF8.GetString(getFileChecksum)} (файл на клиенте)
                    
                    """);

                byte[] wrongHash = Encoding.UTF8.GetBytes(downloadedFileChecksum);
                await __socket.SendAsync(wrongHash);

                byte[] getAnswer = new byte[1];
                await __socket.ReceiveAsync(getAnswer);

                if (getAnswer[0] == 1)
                {
                    File.Delete(savePath);
                    Console.WriteLine($"[{DateTime.Now}] [+] Клиент решил удалить файл: {savePath}");
                }
                else
                    Console.WriteLine($"[{DateTime.Now}] [+] Клиент решил оставить файл: {savePath}");
            }
        }
        else
        {
            if (isServer)
            {
                Console.WriteLine($"""
                [{DateTime.Now}] [+] Хеши совпадают

                SHA-256:
                    {downloadedFileChecksum} (скачанный файл)
                                    =
                    {Encoding.UTF8.GetString(getFileChecksum)} (файл на клиенте)
                
                """);
            }
        }
        if (!isServer && Encoding.UTF8.GetString(getFileName)[^4..] == ".enc")
        {
            try
            {
                Dictionary<string, string> getKey = readConfigFile($"{((IPEndPoint)__socket.RemoteEndPoint).Address}.keytable");
                sec.decryptFile(savePath, getKey[Encoding.UTF8.GetString(getFileName)]);
                File.Delete(savePath);
            }
            catch (KeyNotFoundException)
            {
                Console.Write("[-] Ключ от данного файла не найден в таблице сохранённых ключей (.keytable). Если у вас есть ключ от этого файла, введите в это поле: ");
                string key = Console.ReadLine();
                if (key != "")
                {
                    sec.decryptFile(savePath, key);
                    File.Delete(savePath);
                }
            }
        }
    }

    /// <summary>
    /// Получение информации о файле
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public async Task getFileInfo(Socket __socket, string path)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(path);
            string data = $"""
            Имя: {fileInfo.Name}
            Дата создания: {fileInfo.CreationTime}
            Размер: {(fileInfo.Length > 1048576 ?
                  Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2) + " мб"
                : Math.Round(fileInfo.Length / 1024.0, 2) + " кб")}
            SHA256: {sec.checksumFileSHA256(path)}
            """;

            byte[] sendData = Encoding.UTF8.GetBytes(data);
            await __socket.SendAsync(sendData);
        }
        catch (FileNotFoundException)
        {
            string fileNotFound = "Файл не найден";
            byte[] b = Encoding.UTF8.GetBytes(fileNotFound);
            await __socket.SendAsync(b);
        }
    }

    /// <summary>
    /// Запись ключа шифрования в таблицу ключей
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="key"></param>
    /// <param name="file"></param>
    public void writeKeytableFile(Socket __socket, string key, string file)
    {
        List<string> lines = new List<string>();
        IPEndPoint getIPAddres = __socket.RemoteEndPoint as IPEndPoint;
        string keyTableFile = $"{getIPAddres.Address}.keytable";

        if (File.Exists(keyTableFile))
        {
            string[] getLines = File.ReadAllLines(keyTableFile);
            lines = getLines.ToList();
            foreach (string line in lines.ToList())
            {
                if (line.Contains(Path.GetFileName(file)))
                {
                    int index = lines.IndexOf(line);
                    lines.RemoveAt(index);
                }
            }
            File.WriteAllText(keyTableFile, string.Empty);
        }

        using FileStream fs = new(keyTableFile, FileMode.Append);
        
        foreach (string s in lines.ToArray())
        {
            fs.Write(Encoding.UTF8.GetBytes(s+"\n"));
        }
        
        fs.Write(Encoding.UTF8.GetBytes($"{Path.GetFileName(file)}={key}\n"));  
    }

    /// <summary>
    /// Чтение конфигурационного файла
    /// </summary>
    /// <param name="configFile"></param>
    /// <returns></returns>
    public Dictionary<string, string>? readConfigFile(string configFile)
    {
        Dictionary<string, string>? data = [];
        try
        {
            foreach (string line in File.ReadAllLines(configFile))
            {
                string _key = "";
                string _value = "";

                foreach (char _char in line)
                {
                    if (_char != '=')
                        _key += _char;
                    else
                        break;
                }
                foreach (char _char in line.Reverse())
                {
                    if (_char != '=')
                        _value += _char;
                    else
                        break;
                }

                char[] _reverseStringValue = _value.ToCharArray();
                Array.Reverse(_reverseStringValue);

                data.Add(_key, new string(_reverseStringValue));
            }

            return data;
        }
        catch (FileNotFoundException)
        {
            data.Add("useStartupFile", "no");
            return data;
        }
    }

    /// <summary>
    /// Очистка терминала
    /// </summary>
    public void clearTerminal()
    {
        Console.Clear();
        Console.WriteLine("\x1b[3J");
    }
}
