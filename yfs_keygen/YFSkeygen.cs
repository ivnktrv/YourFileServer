//
// Класс YFSkeygen предназначен для генерации случайных ключей и преобразования их в массив байтов.
//  
// Основные методы:
//
// • _keygen(): Генерирует случайный ключ, состоящий из символов латинского алфавита, цифр и спецсимволов.
// • _keyBytes(): Преобразует ключ в массив байтов.
//

using System.Text;

namespace yfs_keygen;

public class YFSkeygen
{
    private const string DICT = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()[]{}|/>_+-;:?<>.,";

    /// <summary>
    /// Генерирует случайный ключ, состоящий из символов латинского алфавита, цифр и спецсимволов.
    /// </summary>
    /// <returns></returns>
    public string _keygen()
    {
        string key = "";
        for (int i = 0; i < new Random().Next(16, 64); i++)
        {
            key += DICT[new Random().Next(DICT.Length)];
        }
        return key;
    }

    /// <summary>
    /// Преобразует ключ в массив байтов.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public byte[] _keyBytes(string key)
    {
        return Encoding.ASCII.GetBytes(key);
    }
}
