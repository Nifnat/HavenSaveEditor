using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace HavenGenderChanger
{
    public partial class MainWindow : Window
    {
        private string selectedFilePath;
        private dynamic jsonData;
        private dynamic inkStoryStateJson;

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

                // Create a backup of the file
                string backupFilePath = selectedFilePath + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak";
                File.Copy(selectedFilePath, backupFilePath, true);

                // Deserialize JSON file
                string decryptedJson = DecryptFile(selectedFilePath);
                string decryptedFilePath = selectedFilePath + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".decrypted";
                File.WriteAllText(decryptedFilePath, decryptedJson);
                jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject(decryptedJson);

                string serializedJsonBefore = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);
                string jsonFilePath = selectedFilePath + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jsonbefore";
                File.WriteAllText(jsonFilePath, serializedJsonBefore);

                // Display selected values from the JSON on the UI
                DisplayValuesFromJson();

                // Enable the gender dropdown
                genderDropdown.IsEnabled = true;
            }
        }

        private void DisplayValuesFromJson()
        {
            inkStoryStateJsonTextBox.Text = ReadInkStoryStateJson((string)jsonData.storyData.inkStoryStateJson);
            charactersGenderTextBox.Text = ReadGenderPair("CharactersGender");
            genderPairTextBox.Text = jsonData.coreData.__genderPair;
        }

        private string ReadInkStoryStateJson(string inkStoryStateJson)
        {
            // Deserialize the inkStoryStateJson string
            dynamic inkStoryStateObject = Newtonsoft.Json.JsonConvert.DeserializeObject(inkStoryStateJson);

            // Read the desired value
            return (string)inkStoryStateObject.variablesState.CharactersGender;
        }

        private string ReadGenderPair(string genderPair)
        {
            foreach (var variable in jsonData.inkVariables.values)
            {
                if (variable._value.name == genderPair)
                {
                    return (string)variable._value.stringValue;
                }
            }
            return null;
        }

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

        private void UpdateJsonBasedOnDropdownSelection(string selectedValue)
        {
            // Update specific values in the JSON based on the dropdown selection
            switch (selectedValue)
            {
                case "WM":
                    jsonData.coreData.__genderPair = "0";
                    jsonData.storyData.inkStoryStateJson = UpdateInkStoryStateJson(jsonData.storyData.inkStoryStateJson, "CharactersGender", "WM");
                    UpdateInkVariableValue("CharactersGender", "^WM");
                    break;
                case "MM":
                    jsonData.coreData.__genderPair = "1";
                    jsonData.storyData.inkStoryStateJson = UpdateInkStoryStateJson(jsonData.storyData.inkStoryStateJson, "CharactersGender", "MM");
                    UpdateInkVariableValue("CharactersGender", "^MM");
                    break;
                case "WW":
                    jsonData.coreData.__genderPair = "2";
                    jsonData.storyData.inkStoryStateJson = UpdateInkStoryStateJson((string)jsonData.storyData.inkStoryStateJson, "CharactersGender", "WW");
                    UpdateInkVariableValue("CharactersGender", "^WW");
                    break;
            }
        }

        private string UpdateInkStoryStateJson(string inkStoryStateJson, string variableName, string newValue)
        {
            // Deserialize the inkStoryStateJson string
            dynamic inkStoryStateObject = Newtonsoft.Json.JsonConvert.DeserializeObject(inkStoryStateJson);

            // Update the nested value
            inkStoryStateObject.variablesState.CharactersGender = newValue;

            // Serialize the updated object back to a JSON string
            string updatedInkStoryStateJson = Newtonsoft.Json.JsonConvert.SerializeObject(inkStoryStateObject);

            return updatedInkStoryStateJson;
        }

        private void UpdateInkVariableValue(string variableName, string newValue)
        {
            foreach (var variable in jsonData.inkVariables.values)
            {
                if (variable._value.name == variableName)
                {
                    variable._value.stringValue = newValue;
                    break;
                }
            }
        }

        private void DropdownSelectionChanged(object sender, RoutedEventArgs e)
        {
            string selectedValue = ((ComboBoxItem)genderDropdown.SelectedItem).Content.ToString();
            UpdateJsonBasedOnDropdownSelection(selectedValue);
            // Display selected values from the JSON on the UI
            DisplayValuesFromJson();
            // Enable the save button
            saveButton.IsEnabled = true;
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            // Serialize JSON data and save to file
            string serializedJson = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);
            File.WriteAllText(selectedFilePath, serializedJson);
            MessageBox.Show("File saved successfully.");
        }
    }
}
