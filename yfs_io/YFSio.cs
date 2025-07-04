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
using System.IO;

namespace yfs_io;

public class YFSio
{
    YFSsec sec = new();
    YFSnet net = new();

    private readonly int PACKET_SIZE = 2048;

    /// <summary>
    /// Получение списка файлов и директорий в указанной директории
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="dirName"></param>
    /// <returns></returns>
    public void getFiles(Socket __socket, string dirName)
    {
        try
        {
            string[] dirs = Directory.GetDirectories(dirName);
            string[] files = Directory.GetFiles(dirName);

            foreach (string getDir in dirs)
            {
                net.sendData(__socket, "(DIR) " + new DirectoryInfo(getDir).Name);
            }

            foreach (string getFile in files)
            {
                net.sendData(__socket, Path.GetFileName(getFile));
            }

            net.sendData(__socket, ":END_OF_LIST");
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Директория не найдена: {dirName}");
            net.sendData(__socket, $"\nДиректории {dirName} не существует. Чтобы выйти, смените директорию на /");
            net.sendData(__socket, ":END_OF_LIST");

        }
        catch (IOException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Некорректное имя директории: {dirName}");
            net.sendData(__socket, $"Некорректное имя директории: {dirName}. Чтобы выйти, смените директорию на /");
            net.sendData(__socket, ":END_OF_LIST");
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
    public void uploadFile(Socket __socket, string file, 
        string? folder = null, bool isServer = false, bool encryptFile = false)
    {
        string path = folder == null ? file : $"{folder}/{file}";
        string keyFile = "";
        try
        {
            if (!isServer && encryptFile)
            {
                keyFile = sec.encryptFile(path);
                file = file + ".enc";
                path += ".enc";
            }
            if (!isServer)
                Console.WriteLine($"[DEBUG] Путь к файлу для отправки: {path}");
            Console.WriteLine($"[{DateTime.Now}] [LOG] Старт отправки файла: {path}");
            using (BinaryReader br = new(File.Open(path, FileMode.Open)))
            {
                long fLength = br.BaseStream.Length;
                byte[] getFileName = Encoding.UTF8.GetBytes(Path.GetFileName(file));
                byte[] getFileName_arrLength = { (byte)getFileName.Length };
                byte[] getFileLength = BitConverter.GetBytes(fLength);
                long packetCount = fLength / PACKET_SIZE;
                int remainderBytes = (int)(fLength % PACKET_SIZE);
                byte[] getPacketCount = BitConverter.GetBytes(packetCount);
                byte[] getRemainderBytes = BitConverter.GetBytes(remainderBytes);
                byte[] getFileChecksum = Encoding.UTF8.GetBytes(sec.checksumFileSHA256(path));
                Console.WriteLine($"[{DateTime.Now}] [LOG] Отправка метаданных файла: имя={file}, размер={fLength}, пакетов={packetCount}, остаток={remainderBytes}");
                __socket.Send(getFileName_arrLength);
                __socket.Send(getFileName);
                __socket.Send(getFileLength);
                __socket.Send(getFileChecksum);
                __socket.Send(getPacketCount);
                __socket.Send(getRemainderBytes);
                if (!isServer)
                    Console.WriteLine($"[INFO] Файл: {file}; Размер: {fLength} байт; Количество пакетов: {packetCount}; Остаток: {remainderBytes} байт\n");
                for (long i = 0; i <= packetCount; i++)
                {
                    byte[] data = br.ReadBytes(PACKET_SIZE);
                    __socket.Send(data);
                    Console.WriteLine($"[{DateTime.Now}] [LOG] Отправлен пакет №{i} ({data.Length} байт)");
                }
                if (remainderBytes != 0)
                {
                    br.BaseStream.Position = br.BaseStream.Length - remainderBytes;
                    byte[] sendRemainderBytes = br.ReadBytes(remainderBytes);
                    __socket.Send(sendRemainderBytes);
                    Console.WriteLine($"[{DateTime.Now}] [LOG] Отправлены остаточные байты: {remainderBytes}");
                }
                Console.WriteLine(isServer ? $"[{DateTime.Now}] [...] Проверка контрольной суммы"
                    : "\n[...] Проверка контрольной суммы");
                byte[] uploadedFileChecksum = new byte[64];
                __socket.Receive(uploadedFileChecksum);
                Console.WriteLine($"[{DateTime.Now}] [LOG] Получена контрольная сумма с другой стороны: {Encoding.UTF8.GetString(uploadedFileChecksum)}");
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
                            __socket.Send(sendAnswer);
                        }
                        else
                        {
                            byte[] sendAnswer = { 0 };
                            __socket.Send(sendAnswer);
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
                Console.WriteLine($"[{DateTime.Now}] [LOG] Отправка файла завершена: {path}");
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Файл не найден: {path}");
            byte[] getFileName_arrLengthZero = { 0 };
            __socket.Send(getFileName_arrLengthZero);
            // Ждём ответ от сервера, чтобы не зависнуть
            byte[] serverResponse = new byte[1];
            __socket.Receive(serverResponse);
            return;
        }
        catch (IOException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Некорректное имя файла: {path}. Отменяю отправку");
            byte[] getFileName_arrLengthZero = { 0 };
            __socket.Send(getFileName_arrLengthZero);
            // Ждём ответ от сервера, чтобы не зависнуть
            byte[] serverResponse = new byte[1];
            __socket.Receive(serverResponse);
            return;
        }
        Console.ReadLine();
    }

    /// <summary>
    /// Скачивание файла с сервера / клиента
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="saveFolder"></param>
    /// <param name="isServer"></param>
    /// <returns></returns>
    public void downloadFile(Socket __socket, string saveFolder, bool isServer = false)
    {
        byte[] getFileNameArrayLength = new byte[1];
        __socket.ReceiveAsync(getFileNameArrayLength, SocketFlags.None);
        if (getFileNameArrayLength[0] == 0)
        {
            Console.WriteLine(isServer ? $"[{DateTime.Now}] [i] Клиент пытался отправить не существующий у себя файл" : "[-] Файл не найден");
            // Отправляем клиенту байт-ответ, чтобы он не зависал
            __socket.Send(new byte[] { 1 });
            return;
        }
        byte[] getFileName = new byte[getFileNameArrayLength[0]];
        __socket.Receive(getFileName, SocketFlags.None);
        byte[] getFileLength = new byte[8];
        __socket.Receive(getFileLength, SocketFlags.None);
        byte[] getFileChecksum = new byte[64];
        __socket.Receive(getFileChecksum, SocketFlags.None);
        byte[] getCountPacket = new byte[8];
        __socket.Receive(getCountPacket, SocketFlags.None);
        byte[] getReaminderBytes = new byte[4];
        __socket.Receive(getReaminderBytes, SocketFlags.None);
        string savePath = $"{saveFolder}/{Encoding.UTF8.GetString(getFileName)}";
        long fLength = BitConverter.ToInt64(getFileLength);
        long packetCount = BitConverter.ToInt64(getCountPacket);
        int remainderBytes = BitConverter.ToInt32(getReaminderBytes);
        string fileChecksum = Encoding.UTF8.GetString(getFileChecksum);
        Console.WriteLine($"[{DateTime.Now}] [LOG] Старт загрузки файла: {savePath}");
        if (isServer)
            Console.WriteLine($"[{DateTime.Now}] [i] Принимаю файл: {Encoding.UTF8.GetString(getFileName)}");
        using (FileStream fs = new(savePath, FileMode.Create, FileAccess.Write, FileShare.None, PACKET_SIZE))
        {
            for (long i = 0; i < packetCount; i++)
            {
                byte[] receiveData = new byte[PACKET_SIZE];
                __socket.Receive(receiveData, SocketFlags.None);
                fs.Write(receiveData, 0, receiveData.Length);
                Console.WriteLine($"[{DateTime.Now}] [LOG] Получен пакет №{i} ({receiveData.Length} байт)");
            }
            if (remainderBytes != 0)
            {
                byte[] receiveRemainderBytes = new byte[remainderBytes];
                __socket.Receive(receiveRemainderBytes, SocketFlags.None);
                fs.Write(receiveRemainderBytes, 0, receiveRemainderBytes.Length);
                Console.WriteLine($"[{DateTime.Now}] [LOG] Получены остаточные байты: {remainderBytes}");
            }
            fs.Close();
        }
        string downloadedFileChecksum = sec.checksumFileSHA256(savePath);
        byte[] downloadedFileChecksum_bytes = Encoding.UTF8.GetBytes(downloadedFileChecksum);
        __socket.Send(downloadedFileChecksum_bytes, SocketFlags.None);
        Console.WriteLine($"[{DateTime.Now}] [LOG] Отправлена контрольная сумма: {downloadedFileChecksum}");
        Console.WriteLine(isServer ? $"[{DateTime.Now}] [...] Проверка контрольной суммы" : "\n[...] Проверка контрольной суммы");
        if (downloadedFileChecksum != fileChecksum)
        {
            if (!isServer)
            {
                Console.Write($"""
                [!] Хеши не совпадают. Возможно, файл при скачивании был повреждён.

                SHA-256:
                    {downloadedFileChecksum} (скачанный файл) 
                                !=
                    {fileChecksum} (файл на сервере)
            
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
                        {fileChecksum} (файл на клиенте)
                    
                    """);
                byte[] wrongHash = Encoding.UTF8.GetBytes(downloadedFileChecksum);
                __socket.Send(wrongHash, SocketFlags.None);
                byte[] getAnswer = new byte[1];
                __socket.Receive(getAnswer, SocketFlags.None);
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
                    {fileChecksum} (файл на клиенте)
                
                """);
            }
        }
        Console.WriteLine($"[{DateTime.Now}] [LOG] Загрузка файла завершена: {savePath}");
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
        Console.ReadLine();
    }

    /// <summary>
    /// Получение информации о файле
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public void getFileInfo(Socket __socket, string path)
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
            __socket.Send(sendData);
        }
        catch (FileNotFoundException)
        {
            string fileNotFound = "Файл не найден";
            byte[] b = Encoding.UTF8.GetBytes(fileNotFound);
            __socket.Send(b);
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
