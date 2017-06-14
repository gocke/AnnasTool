using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.Windows.Forms;
using StegoSharp;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace AnnasTool
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private byte[] cipher = null;
        private int cipherStart = 0;

        public MainWindow()
        {
            InitializeComponent();

            cipher = LoadCipher();
            cipherStart = LoadCipherStart();
        }

        ///
        /// Encryption Decryption
        /// 


        private string Encrypt(string inputText)
        {
            string plainText = inputText;
            byte[] plainTextBytes = Encoding.ASCII.GetBytes(plainText);

            if (cipher == null || cipher.Length - cipherStart < plainTextBytes.Length)
            {
                MessageBox.Show("Cipherkey ran out, generating new");
                GenerateCipher();
            }

            byte[] currentKey = new ArraySegment<byte>(cipher, cipherStart, plainTextBytes.Length).ToArray();
            cipherStart += plainTextBytes.Length;
            StoreCipherStart(cipherStart);
            byte[] secureTextBytes = new byte[plainTextBytes.Length];

            for (int i = 0; i < plainTextBytes.Length; i++)
            {
                secureTextBytes[i] = (byte) ((currentKey[i] + plainTextBytes[i]) % 256);
            }

            string secureText = Convert.ToBase64String(secureTextBytes);
            string message = secureText = cipherStart.ToString() + "," + secureText.Length.ToString() + "," + secureText;
            return message;
        }

        private string Decrypt(string message)
        {
            string[] splitArray = message.Split(',');
            int messageStart = int.Parse(splitArray[0]);
            int messageLength = int.Parse(splitArray[1]);
            string secureText = splitArray[2];
            byte[] secureTextBytes = Convert.FromBase64String(secureText);

            if (cipher == null || cipher.Length - cipherStart < secureTextBytes.Length)
            {
                MessageBox.Show("Cipherkey ran out. You must request a new Key and Message");
                return "";
            }

            byte[] currentKey = new ArraySegment<byte>(cipher, messageStart, messageLength).ToArray();
            byte[] plainTextBytes = new byte[secureTextBytes.Length];

            for (int i = 0; i < plainTextBytes.Length; i++)
            {
                int result = (byte)(secureTextBytes[i] - currentKey[i]);
                plainTextBytes[i] = (byte) (result < 0 ? result : result + 256);
            }

            string plainText = Encoding.ASCII.GetString(plainTextBytes);
            return plainText;
        }

        private void EncryptImage(string message)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                StegoImage stegoImage = new StegoImage(openFileDialog.FileName);

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    stegoImage.EmbedPayload(message).Save(saveFileDialog.FileName);
                }
            }
        }

        private string DecryptImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                StegoImage stegoImage = new StegoImage(openFileDialog.FileName);
                byte[] extractBytes = stegoImage.ExtractBytes().ToArray();
                string unCutMessage = Encoding.Default.GetString(extractBytes);

                string[] splitArray = unCutMessage.Split(',');
                int messageStart = int.Parse(splitArray[0]);
                int messageLength = int.Parse(splitArray[1]);
                string secureText = new string(splitArray[2].Take(messageLength).ToArray());

                return messageStart + "," + messageLength + "," + secureText;
            }
            return null;
        }

        ///
        /// Utility
        /// 

        private void GenerateCipher()
        {
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                cipher = new byte[500000];
                rng.GetBytes(cipher);
            }

            File.WriteAllText("cipher.txt", Convert.ToBase64String(cipher));
            StoreCipherStart(0);
        }

        private byte[] LoadCipher()
        {
            if (File.Exists("cipher.txt"))
            {
                return Convert.FromBase64String(File.ReadAllText("cipher.txt"));
            }
            return null;
        }

        private byte[] ImportCipher()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                return Convert.FromBase64String(File.ReadAllText(openFileDialog.FileName));
            }
            return null;
        }

        private void StoreCipherStart(int tempCipherStart)
        {
            File.WriteAllText("cipherStart.txt", tempCipherStart.ToString());
        }

        private int LoadCipherStart()
        {
            if (File.Exists("cipherStart.txt"))
            {
                return int.Parse(File.ReadAllText("cipherStart.txt"));
            }
            return 0;
        }

        ///
        /// EVENTS
        /// 

        private void EncryptButton_OnClick(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = Encrypt(InputTextBox.Text);
        }

        private void EncryptImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            string message = Encrypt(InputTextBox.Text);
            EncryptImage(message);
        }

        private void DecryptButton_OnClick(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = Decrypt(OutputTextBox.Text);
        }

        private void DecryptImageButton_OnClick(object sender, RoutedEventArgs e)
        {          
            string message = DecryptImage();
            if (message != null)
            {
                OutputTextBox.Text = Decrypt(message);
            }
        }

        private void GenerateButton_OnClick(object sender, RoutedEventArgs e)
        {
            GenerateCipher();
        }

        private void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            ImportCipher();
        }
    }
}
