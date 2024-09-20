using System.Net.Sockets;
using System.Text;
using yfs_security;

namespace yfs_io;

public class YFSio
{
    private const string KEYTABLE_FILE = ".keytable";
    YFSsec sec = new();

    public string[] getFiles(string dirName)
    {
        List<string> dirsAndFiles = [];

        try
        {
            string[] dirs = Directory.GetDirectories(dirName);
            foreach (string getDirs in dirs)
            {
                dirsAndFiles.Add($"\n{getDirs}");
            }

            string[] files = Directory.GetFiles(dirName);
            foreach (string getFiles in files)
            {
                dirsAndFiles.Add($"\n{Path.GetFileName(getFiles)}");
            }

            dirsAndFiles.Add("\n\n:END_OF_LIST"); // end of list

            return dirsAndFiles.ToArray();
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Папка не найдена: {dirName}");
            string[] dirNotFound = { $"\nПапки {dirName} не существует\n\n:END_OF_LIST" };
            return dirNotFound;
        }
    }

    public void uploadFile(Socket __socket, string file, 
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
            
            byte[] getFileName = Encoding.UTF8.GetBytes(file);
            byte[] getFileName_arrLength = { (byte)getFileName.Length };
            byte[] getFileLength = BitConverter.GetBytes(b.BaseStream.Length);
            byte[] getFileLength_arrSize = { (byte)getFileLength.Length };
            b.Close();
            byte[] getFileChecksum = Encoding.UTF8.GetBytes(sec.checksumFileSHA256(path));

            __socket.Send(getFileName_arrLength);
            __socket.Send(getFileName);
            __socket.Send(getFileLength_arrSize);
            __socket.Send(getFileLength);
            __socket.Send(getFileChecksum);
            
            BinaryReader br = new(File.Open(path, FileMode.Open));
            br.BaseStream.Position = 0;
            
            if (isServer)
                Console.WriteLine($"[{DateTime.Now}] [...] Файл отправляется: {file}");

            long c = 1024*180;
            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                byte[] readByte = { br.ReadByte() };
                __socket.Send(readByte);
                if (br.BaseStream.Position == c && !isServer)
                {
                    clearTerminal();
                    Console.WriteLine($"[UPLOAD] {Path.GetFileName(path)} [{br.BaseStream.Position / 1024} кб / {br.BaseStream.Length / 1024} кб]");
                    c += 1024*180;
                }
            }
            br.Close();

            Console.WriteLine(isServer ? $"[{DateTime.Now}] [...] Проверка контрольной суммы"
            : "[...] Проверка контрольной суммы");

            byte[] uploadedFileChecksum = new byte[64];
            __socket.Receive(uploadedFileChecksum);

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
                writeKeytableFile(keyFile, file.Remove(0,1));
                File.Delete(path);
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Файл не найден: {file}");
            byte[] getFileName_arrLengthZero = {0};
            __socket.Send(getFileName_arrLengthZero);
        }
    }

    public void downloadFile(Socket __socket, string saveFolder, 
        bool isServer = false)
    {
        byte[] getFileNameArrayLength = new byte[1];
        __socket.Receive(getFileNameArrayLength);

        if (getFileNameArrayLength[0] == 0)
        {
            Console.WriteLine("[-] Файл не найден");
            return;
        }

        byte[] getFileName = new byte[getFileNameArrayLength[0]];
        __socket.Receive(getFileName);

        byte[] getFileArrayLength = new byte[1];
        __socket.Receive(getFileArrayLength);
        
        byte[] getFileLength = new byte[getFileArrayLength[0]];
        __socket.Receive(getFileLength);

        byte[] getFileChecksum = new byte[64];
        __socket.Receive(getFileChecksum);

        string savePath = $"{saveFolder}/{Path.GetFileName(Encoding.UTF8.GetString(getFileName))}";
        using BinaryWriter br = new(File.Open(savePath, FileMode.OpenOrCreate));
        long fLength = BitConverter.ToInt64(getFileLength);

        if (isServer)
            Console.WriteLine($"[{DateTime.Now}] [i] Принимаю файл: {Path.GetFileName(Encoding.UTF8.GetString(getFileName))}");
        
        long c = 1024*180;
        while (br.BaseStream.Position != fLength)
        {
            byte[] wr = new byte[1];
            __socket.Receive(wr);
            br.Write(wr[0]);
            if (br.BaseStream.Position == c && !isServer)
            {
                clearTerminal();
                Console.WriteLine($"[DOWNLOAD] {Encoding.UTF8.GetString(getFileName)} [{br.BaseStream.Position / 1024} кб / {fLength / 1024} кб]");
                c += 1024*180;
            }
        }
        br.Close();

        string downloadedFileChecksum = sec.checksumFileSHA256(savePath);
        byte[] downloadedFileChecksum_bytes = Encoding.UTF8.GetBytes(downloadedFileChecksum);
        __socket.Send(downloadedFileChecksum_bytes);

        Console.WriteLine(isServer ? $"[{DateTime.Now}] [...] Проверка контрольной суммы" 
            : "[...] Проверка контрольной суммы");
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
                __socket.Send(wrongHash);

                byte[] getAnswer = new byte[1];
                __socket.Receive(getAnswer);

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
        if (!isServer)
        {
            try
            {
                Dictionary<string, string> getKey = readStartupFile(".keytable");
                sec.decryptFile(savePath, getKey[Path.GetFileName(Encoding.UTF8.GetString(getFileName))]);
                File.Delete(savePath);
            }
            catch (KeyNotFoundException) { }
        }
    }

    public void getFileInfo(Socket __socket, string path)
    {
        FileInfo fileInfo = new FileInfo(path);
        string data = $"""
            Имя: {fileInfo.Name}
            Дата создания: {fileInfo.CreationTime}
            Размер: {
            (fileInfo.Length > 1048576 ? 
              Math.Round(fileInfo.Length/1024.0/1024.0, 2)+" мб"
            : Math.Round(fileInfo.Length/1024.0, 2)+" кб")
            }
            SHA256: {sec.checksumFileSHA256(path)}
            """;

        byte[] sendData = Encoding.UTF8.GetBytes(data);
        __socket.Send(sendData);
    }

    public void writeKeytableFile(string key, string file)
    {
        List<string> lines = new List<string>();

        if (File.Exists(KEYTABLE_FILE))
        {
            string[] getLines = File.ReadAllLines(KEYTABLE_FILE);
            lines = getLines.ToList();
            foreach (string line in lines.ToList())
            {
                if (line.Contains(Path.GetFileName(file)))
                {
                    int index = lines.IndexOf(line);
                    lines.RemoveAt(index);
                }
            }
            File.WriteAllText(KEYTABLE_FILE, string.Empty);
        }

        using FileStream fs = new(KEYTABLE_FILE, FileMode.Append);
        
        foreach (string s in lines.ToArray())
        {
            fs.Write(Encoding.UTF8.GetBytes(s+"\n"));
        }

        
        fs.Write(Encoding.UTF8.GetBytes($"{Path.GetFileName(file)}={key}\n"));
        
    }

    public string readKeytableFile(string path)
    {
        return "";
    }

    public Dictionary<string, string>? readStartupFile(string startupFile)
    {
        Dictionary<string, string>? data = [];
        try
        {
            foreach (string line in File.ReadAllLines(startupFile))
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

    public void clearTerminal()
    {
        Console.Clear();
        Console.WriteLine("\x1b[3J");
    }
}
