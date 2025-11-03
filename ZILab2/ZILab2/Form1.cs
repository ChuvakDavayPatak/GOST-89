using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Engines;

namespace ZILab2
{
    public partial class Form1 : Form
    {
        // Переменная для хранения пути выбранного файла
        private string selectedFilePath;
        public Form1()
        {
            InitializeComponent();
            passwordBox.PasswordChar = '*';
        }

        // Загрузка файла
        private void fileButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files|*.txt|All Files|*.*"; // формат файла и отображение файлов
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                selectedFilePath = openFileDialog.FileName;
                infoBox.Text = "Файл выбран " + selectedFilePath + "\n";
            }
        }

        private void encryptButton_Click(object sender, EventArgs e)
        {
            string password = passwordBox.Text; // Получение пароля из текстового поля
            if (!string.IsNullOrEmpty(selectedFilePath) && !string.IsNullOrEmpty(password)) // Проверка наличия файла и пароля
            {
                try
                {
                    // Формирование пути для зашифрованного файла
                    string outputFile = Path.Combine(Path.GetDirectoryName(selectedFilePath),
                                             Path.GetFileNameWithoutExtension(selectedFilePath) + "_encrypted" + Path.GetExtension(selectedFilePath));

                    // Вызов метода для шифрования файла
                    EncryptFile(selectedFilePath, outputFile, password);

                    // Отображение сообщения об успешном шифровании
                    MessageBox.Show("Файл успешно зашифрован! " + outputFile);
                }
                catch (Exception ex)
                {
                    // Отображение сообщения об ошибке
                    MessageBox.Show("Ошибка шифрования: " + ex.Message);
                }
            }
            else
            {
                // Сообщение о необходимости заполнения полей
                MessageBox.Show("Заполните все поля!");
            }
        }


        private void decryptButton_Click(object sender, EventArgs e)
        {
            string password = passwordBox.Text; // Получение пароля
            if (!string.IsNullOrEmpty(selectedFilePath) && !string.IsNullOrEmpty(password)) // Проверка наличия файла и пароля
            {
                try
                {
                    // Формирование пути для расшифрованного файла
                    string outputFile = Path.Combine(Path.GetDirectoryName(selectedFilePath),
                                                     Path.GetFileNameWithoutExtension(selectedFilePath) + "_decrypted" + Path.GetExtension(selectedFilePath));

                    // Вызов метода для расшифрования файла
                    DecryptFile(selectedFilePath, outputFile, password);

                    // Сообщение об успешном расшифровании
                    MessageBox.Show("Файл успешно расшифрован! " + outputFile);
                }
                catch (Exception ex)
                {
                    // Сообщение об ошибке
                    MessageBox.Show("Ошибка расшифрования: " + ex.Message);
                }
            }
            else
            {
                // Сообщение о необходимости заполнения полей
                MessageBox.Show("Заполните все поля!");
            }
        }

        // Метод для шифрования файла
        private void EncryptFile(string inputFile, string outputFile, string password)
        {
            // Преобразование пароля в массив байт (ключ)
            byte[] key = Encoding.UTF8.GetBytes(password);

            // Если ключ меньше 32 байт (256 бит), расширяем его
            if (key.Length < 32)
                Array.Resize(ref key, 32); // Подгоняем ключ до 32 байт

            // Чтение данных исходного файла
            byte[] inputBytes = File.ReadAllBytes(inputFile);

            // Шифрование данных
            byte[] encryptedBytes = Encrypt(inputBytes, key);

            // Запись зашифрованных данных в новый файл
            File.WriteAllBytes(outputFile, encryptedBytes);
        }

        // Метод для расшифрования файла
        private void DecryptFile(string inputFile, string outputFile, string password)
        {
            // Преобразование пароля в массив байт (ключ)
            byte[] key = Encoding.UTF8.GetBytes(password);

            // Если ключ меньше 32 байт, расширяем его
            if (key.Length < 32)
                Array.Resize(ref key, 32); // Подгоняем ключ до 32 байт

            // Чтение зашифрованных данных из файла
            byte[] inputBytes = File.ReadAllBytes(inputFile);

            // Расшифрование данных
            byte[] decryptedBytes = Decrypt(inputBytes, key);

            // Запись расшифрованных данных в новый файл
            File.WriteAllBytes(outputFile, decryptedBytes);
        }

        // Метод для шифрования данных с использованием ГОСТ-89
        static byte[] Encrypt(byte[] data, byte[] key)
        {
            // Создание объекта шифра ГОСТ-89тр
            Gost28147Engine engine = new Gost28147Engine();
            KeyParameter keyParam = new KeyParameter(key);

            // Инициализация движка шифра для шифрования (true)
            engine.Init(true, keyParam);

            // Добавление padding для кратности длины блока
            byte[] paddedData = AddPadding(data, engine.GetBlockSize());

            // Шифрование данных блоками
            return ProcessData(engine, paddedData);
        }

        // Расшифрование данных с использованием ГОСТ-89
        static byte[] Decrypt(byte[] data, byte[] key)
        {
            // Создание объекта шифра ГОСТ-89
            Gost28147Engine engine = new Gost28147Engine();
            KeyParameter keyParam = new KeyParameter(key);

            // Инициализация движка шифра для расшифрования (false)
            engine.Init(false, keyParam);

            // Расшифрование данных блоками
            byte[] decryptedData = ProcessData(engine, data);

            // Удаление padding после расшифрования
            return RemovePadding(decryptedData, engine.GetBlockSize());
        }

        // Метод для обработки данных блоками
        static byte[] ProcessData(Gost28147Engine engine, byte[] data)
        {
            int blockSize = engine.GetBlockSize(); // Размер блока шифра
            byte[] output = new byte[data.Length]; // Массив для хранения результата
            int offset = 0;

            // Обработка данных по блокам
            while (offset < data.Length)
            {
                engine.ProcessBlock(data, offset, output, offset);
                offset += blockSize;
            }

            return output; // Возврат зашифрованных/расшифрованных данных
        }

        // Метод для добавления padding
        private static byte[] AddPadding(byte[] data, int blockSize)
        {
            // Вычисление размера padding для кратности длины блока
            int paddingSize = blockSize - (data.Length % blockSize);
            byte[] paddedData = new byte[data.Length + paddingSize]; // Новый массив с padding

            // Копируем исходные данные в новый массив
            Array.Copy(data, paddedData, data.Length);

            // Заполнение padding байтами
            for (int i = data.Length; i < paddedData.Length; i++)
            {
                paddedData[i] = (byte)paddingSize;
            }

            return paddedData; // Возвращаем данные с добавленным padding
        }

        // Метод для удаления padding
        private static byte[] RemovePadding(byte[] data, int blockSize)
        {
            int paddingSize = data[data.Length - 1]; // Размер padding (последний байт данных)

            // Проверка корректности padding
            if (paddingSize > blockSize || paddingSize <= 0)
            {
                throw new Exception("Ошибка.");
            }

            // Копируем данные без padding
            byte[] unpaddedData = new byte[data.Length - paddingSize];
            Array.Copy(data, unpaddedData, unpaddedData.Length);

            return unpaddedData; // Возвращаем данные без padding
        }
    }
}
