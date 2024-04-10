using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace HavenGenderChanger
{
    public partial class MainWindow : Window
    {
        // TODO get rid of this shit, pass via contructor as needed
        private string selectedFilePath;
        private string decryptedFilePath;
        private string selectedValDropDown;
        private string decryptedJson;
        private string genderPairValue;
        private string charactersGenderValue;
        private string inkStoryStateJsonValue;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectFileButtonClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                filePathLabel.Content = selectedFilePath;

                // TODO catch IO exceptions
                // Create a backup of the file
                string parentDirectory = Directory.GetParent(Path.GetDirectoryName(selectedFilePath)).FullName;
                string grandParentDirectory = Directory.GetParent(parentDirectory).FullName;
                string backupFolderPath = Path.Combine(grandParentDirectory, "backups");
                Directory.CreateDirectory(backupFolderPath);

                string backupFileName = Path.GetFileName(selectedFilePath) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak";
                string backupFilePath = Path.Combine(backupFolderPath, backupFileName);
                File.Copy(selectedFilePath, backupFilePath, true);

                // Decrypted JSON file and save
                decryptedJson = DecryptFile(selectedFilePath);
                string decryptedFileName = Path.GetFileName(selectedFilePath) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".decrypted";
                decryptedFilePath = Path.Combine(backupFolderPath, decryptedFileName);
                File.WriteAllText(decryptedFilePath, decryptedJson);

                ProcessDecryptedFile(selectedFilePath, decryptedJson, false,"",0,"");

                // Display selected values from the JSON on the UI
                DisplayValuesFromJson();

                // Enable the gender dropdown
                genderDropdown.IsEnabled = true;
            }
        }

        private void DisplayValuesFromJson()
        {
            genderPairTextBox.Text = genderPairValue;
            inkStoryStateJsonTextBox.Text = inkStoryStateJsonValue;
            charactersGenderTextBox.Text = charactersGenderValue;
        }

    // TODO refactor to write and read method
    private void ProcessDecryptedFile(string originalFilePath, string decryptedJson, bool updateValues, string newStringValue, int newGenderPair, string newCharactersGender)
    {
        // TODO refactor
        string decryptedContent = decryptedJson;

        // Define regex patterns for matching the values
        string genderPairPattern = "\"__genderPair\":\\s*(\\d+)";
        string charactersGenderPattern = "\\^(WW|WM|MM)";
        string stringValuePattern = "\"stringValue\":\\s*\"(WW|WM|MM)\"";

        // Match and extract values using regex
        Match genderPairMatch = Regex.Match(decryptedContent, genderPairPattern);
        Match inkStoryStateJsonMatch = Regex.Match(decryptedContent, charactersGenderPattern);
        Match charactersGenderMatch = Regex.Match(decryptedContent, stringValuePattern);

        // Extract and display the matched values
        if (genderPairMatch.Success)
        {
            genderPairValue = genderPairMatch.Groups[1].Value;
            if (updateValues)
            {
                // Replace the matched value with a new value
                decryptedContent = Regex.Replace(decryptedContent, genderPairPattern, $"\"__genderPair\": {newGenderPair}");
            }
        }

        if (inkStoryStateJsonMatch.Success)
        {
            inkStoryStateJsonValue = inkStoryStateJsonMatch.Value;
            if (updateValues)
            {
                decryptedContent = Regex.Replace(decryptedContent, charactersGenderPattern, $"^{newCharactersGender}");
            }
        }

        if (charactersGenderMatch.Success)
        {
            charactersGenderValue = charactersGenderMatch.Groups[1].Value;
            if (updateValues)
            {
                decryptedContent = Regex.Replace(decryptedContent, stringValuePattern, $"\"stringValue\": \"{newStringValue}\"");
            }
        }

        // Write the modified text back to the file if updateValues is true
        if (updateValues)
        {
            File.WriteAllText(originalFilePath, decryptedContent);
        }
    }

        //TODO using BinaryFormatter to match game logic, should move to something secure
        #pragma warning disable SYSLIB0011
        private string DecryptFile(string filePath)
        {
            object obj;
            string encryptionKey = "KCLMNOP8";

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            using (ICryptoTransform cryptoTransform = new DESCryptoServiceProvider().CreateDecryptor(Encoding.ASCII.GetBytes("64bitPas"), Encoding.ASCII.GetBytes(encryptionKey)))
            using (CryptoStream cryptoStream = new CryptoStream(fileStream, cryptoTransform, CryptoStreamMode.Read))
            using (StreamReader streamReader = new StreamReader(cryptoStream))
            {
                obj = binaryFormatter.Deserialize(cryptoStream);
                return (string)obj;
            }
        }
        #pragma warning restore SYSLIB0011

        private void UpdateJsonBasedOnDropdownSelection(string selectedValue)
        {
            // Update specific values in the "JSON" based on the dropdown selection
            selectedValDropDown = selectedValue;
        }


        private void DropdownSelectionChanged(object sender, RoutedEventArgs e)
        {
            string selectedValue = ((ComboBoxItem)genderDropdown.SelectedItem).Content.ToString();
            UpdateJsonBasedOnDropdownSelection(selectedValue);
            // Display selected values from the JSON on the UI
            ProcessDecryptedFile(selectedFilePath, decryptedJson, false, "", 0, "");
            DisplayValuesFromJson();
            // Enable the save button
            saveButton.IsEnabled = true;
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            switch (selectedValDropDown)
            {
                // Woman/Man
                case "WM":
                    ProcessDecryptedFile(selectedFilePath, decryptedJson, true, "WM", 0, "WM");
                    break;
                // Man/Man
                case "MM":
                    ProcessDecryptedFile(selectedFilePath, decryptedJson, true, "MM", 1, "MM");
                    break;
                // Woman/Woman
                case "WW":
                    ProcessDecryptedFile(selectedFilePath, decryptedJson, true, "WW", 2, "WW");
                    break;
            }
        }
    }
}
