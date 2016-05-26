using Microsoft.Win32;
using System;
using UserInterface.Shared;
using System.Windows;
using NAudio.Wave;

namespace UserInterface
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NoiseChoosingWindow : Window
    {
        // event to indicate that window is closed without audio selection
        public event EventHandler ClosedWithoutResult;

        // event to indicate that window is closed fine
        public event EventHandler ClosedWithResult;

        private string filePath; // file from which to read noise 
        private bool isFileSelected; // show if file is selected
        private int desiredHz; // for noise/speech conversion

        public NoiseChoosingWindow()
        {
            InitializeComponent();

            // set window coordinates
            SetWindowOnScreen();

            // event on closing
            Closing += NoiseChoosingWindow_Closing;

            // set some UI stuff
            setDefaultSettings();
        }

        // method to set default settings
        private void setDefaultSettings()
        {
            DoneButton.IsEnabled = false;
            filePath = "";
            isFileSelected = false;
            desiredHz = 8000;
            desiredHzTextBox.Text = desiredHz.ToString();
        }

        // method to check if file was selected
        private void NoiseChoosingWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // check if file was selected
            if (isFileSelected)
            {
                MainWindow.NoiseFile = TransformFilePath.GetCanonicalName(filePath, ".wav"); // remember file name in main window
                MainWindow.FullNoiseFile = filePath;
                ClosedWithResult(this, e);
            }
            else
            {
                ClosedWithoutResult(this, e);
            }
        }

        // Go to the next step
        private void Done_button_Click(object sender, RoutedEventArgs e)
        {
            // save data into storage
            MainWindow.storage.StoreFileData(NoiseLibrary.Storage.Category.NOISE, filePath);

            this.Close();
        }

        private void OpenNoiseAudio_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".wav"; // Default file extension
            dlg.Filter = "Audio documents (.wav)|*.wav"; // Filter files by extension
            dlg.InitialDirectory = TransformFilePath.GetParentDirectoryPath() + @"\Audio\Noise";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                isFileSelected = true;
                filePath = dlg.FileName;
                ChosenFileLabel.Content = "File: " + TransformFilePath.GetCanonicalName(filePath, ".wav");

                // enable done button
                DoneButton.IsEnabled = true;
            }
        }


        // set the window
        private void SetWindowOnScreen()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = Width;
            double windowHeight = Height;
            Left = (screenWidth / 2) - (windowWidth / 2);
            Top = (screenHeight / 2) - (windowHeight / 2);
        }

        // Method to convert audio files
        private void ConvertAudio_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".wav"; // Default file extension
            dlg.Filter = "Audio documents (.wav)|*.wav"; // Filter files by extension
            dlg.InitialDirectory = TransformFilePath.GetParentDirectoryPath() + @"\Audio";
            dlg.Title = "Load audio to convert";

            string noiseToConvertFilePath;

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                noiseToConvertFilePath = dlg.FileName;
            }
            else
            {
                return;
            }

            // ask for destination

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".wav";
            sfd.InitialDirectory = TransformFilePath.GetParentDirectoryPath() + @"\Audio";
            sfd.Title = "Save new audio";

            string newNoise;

            // Show save file dialog box
            result = sfd.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                newNoise = sfd.FileName;
            }
            else
            {
                return;
            }

            // convert existing file into a new one with 'desired' Hz
            using (var reader = new WaveFileReader(noiseToConvertFilePath))
            {
                var newFormat = new WaveFormat(desiredHz, 16, 1);
                using (var conversionStream = new WaveFormatConversionStream(newFormat, reader))
                {
                    WaveFileWriter.CreateWaveFile(newNoise, conversionStream);
                }
            }

        }

        // Event to handle text changes
        private void desiredHzTextBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            // checking value in textbox            
            int hz;
            if (!int.TryParse(desiredHzTextBox.Text, out hz))
            {
                MessageBox.Show("Only 'int' values allowed!");
                desiredHzTextBox.Text = desiredHz.ToString();
                return;
            }
            desiredHz = hz;
        }
        
        // Event to handle text changes
        private void desiredHzTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((e.Key == System.Windows.Input.Key.Enter) || (e.Key == System.Windows.Input.Key.Tab))
            {
                DoneButton.Focus(); // pass focus to another control
            }
        }
    }
}
