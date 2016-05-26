using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using UserInterface.Shared; // Folder containing helper method for the project

namespace UserInterface
{
    /// <summary>
    /// Window to record audio
    /// </summary>
    public partial class RecordingWindow : Window
    {
        // event to indicate that window is closed without audio selection
        public event EventHandler ClosedWithoutResult;

        // event to indicate that window is closed fine
        public event EventHandler ClosedWithResult;

        // private fields
        private bool isRecordingOn; // is recording on or off
        private readonly int selectedRecordingDevice = 0; // index of the audio device
        private int samplesCount; // counter of received audio samples ( changed to 0 every 800 samples)
        private float min_sample; // min and max audio samples
        private float max_sample;
        private string filePath; // path of the file in which to store audio data
        private bool isFileSelected; // flag
        private int sampleRate { get; set; } = 8000; // 8 khz
        
        // private objects
        private WaveIn waveIn; // Recording buffer
        private VisualizeAudioData visualizer; // Class instance to draw audio data on polygon
        private RecordWAVandMP3 recorder; // Class instance to save recorded data into WAV and MP3 files
        

        public RecordingWindow()
        {
            DataContext = this; // for binding

            InitializeComponent();

            // set window coordinates
            SetWindowOnScreen();

            // Check if recording is on when closing the window
            Closing += new CancelEventHandler(RecordingWindow_Closing);

            // Event handler to rewrite tooltip 
            ChosenFileLabel.TextChanged += ChosenFileLabel_TextChanged;

            // Set window controls and fields
            SetDefaultComponents();
        }

        // event handler to change tool tip text
        private void ChosenFileLabel_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ChosenFileLabel.ToolTip = ChosenFileLabel.Text;
        }

        // Method to check if recording is on when closing the window
        private void RecordingWindow_Closing(object sender, CancelEventArgs e)
        {
            if (isRecordingOn)
            {
                MessageBox.Show("Finish recording first!");
                e.Cancel = true;
                return;
            }

            // check if file was selected
            if (isFileSelected)
            {
                MainWindow.SpeechFile = TransformFilePath.GetCanonicalName(filePath, ".wav"); // remember file name in main window
                MainWindow.FullSpeechFile = filePath;
                ClosedWithResult(this, e);
            }
            else
            {
                ClosedWithoutResult(this, e);
            }
        }

        /// <summary>
        /// Setting default properties
        /// </summary>
        private void SetDefaultComponents()
        {
            sampleRateTextBox.Text = sampleRate.ToString();
            isRecordingOn = false;
            min_sample = float.MaxValue;
            max_sample = float.MinValue;
            samplesCount = 0;
            filePath = "";
            DoneButton.IsEnabled = false;
            isFileSelected = false;
        }

        /// <summary>
        /// Method to start/stop recording from the main microphone
        /// </summary>
        private void Record_button_Click(object sender, RoutedEventArgs e)
        {
            // In case user whants to start a recording session
            if (!isRecordingOn)
            {

                // Ask user in which file to store audio data
                string newfilePath = getFileName();
                if (newfilePath == "")
                {
                    return;
                }; // dialog canceled OR failed

                filePath = newfilePath;

                // Set some UI stuff
                ChosenFileLabel.Text = "File: " + TransformFilePath.GetCanonicalName(filePath, ".wav");
                StartRecodingButton.Content = "Stop recording";
                StartRecodingButton.BorderBrush = Brushes.Purple;
                StartRecodingButton.Foreground = Brushes.Purple;
                waveForm.Points.Clear();

                // Set the input settings of the audio recorder
                waveIn = new WaveIn();      
                waveIn.DeviceNumber = selectedRecordingDevice;
                waveIn.DataAvailable += waveIn_DataAvailable; // event to handle packs of recorded data
                waveIn.RecordingStopped += waveIn_RecordingStopped; // event to notify user about finished recording
                int channels = 1; // mono
                waveIn.WaveFormat = new WaveFormat(sampleRate, channels);

                // set some helpers
                recorder = new RecordWAVandMP3(filePath, waveIn.WaveFormat); // helper to save recorded data
                visualizer = new VisualizeAudioData(waveForm); // helper to visualize recorded data
                
                // Starting recording
                waveIn.StartRecording();
                isRecordingOn = !isRecordingOn;

            }
            // In case user whants to stop the recording
            else
            {
                // Set some UI stuff
                StartRecodingButton.Content = "Start recording";
                StartRecodingButton.BorderBrush = Brushes.Blue;
                StartRecodingButton.Foreground = Brushes.Blue;

                // stopping the recording
                waveIn.StopRecording();

                // Recording is in progress no more
                isRecordingOn = !isRecordingOn;
            }
        }

        // Method to get file name in which to store audio from user
        string getFileName()
        { 
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = "AudioSample"; // Default file name
            dlg.DefaultExt = ".wav"; // Default file extension
            dlg.Filter = "Wave audio (.wav)|*.wav"; // Filter files by extension
            dlg.InitialDirectory =TransformFilePath.GetParentDirectoryPath() + @"\Audio\Speech";

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                return dlg.FileName;
            }
            else {
                return "";
            }
        }

        // Event hadler called when the recording is no more
        private void waveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            MessageBox.Show("Recording Stopped");

            // disposing helper
            recorder.RecordingFinished();


            // enable next button
            isFileSelected = true;
            DoneButton.IsEnabled = true;
        }

        /// <summary>
        /// Handling recorded data
        /// </summary>
        /// <param name="sender"> WaveIn object </param>
        /// <param name="e"> Recorded bytes </param>
        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            // process sample data
            if (isRecordingOn)
            {
                recorder.WriteBytes(e.Buffer); // writing bytes into .mp3 and .wav files
            }

            // visualise samples every 800 samples

            // coding samples into float-point 32 bit 
            for (int index = 0; index < e.BytesRecorded; index += 2, samplesCount++)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) |
                                        e.Buffer[index + 0]);
                float sample32 = sample / 32768f;

                // check if new sample is min or max for current sample's group
                if (sample32 < min_sample) min_sample = sample32;
                if (sample32 > max_sample) max_sample = sample32;

                // check if current sapmle's group has more than 1600 members (800 pairs), if so, visualize it
                if(samplesCount >= 800)
                {
                    visualizer.VisualiseSamples(min_sample, max_sample);

                    // Set new sample's group properties
                    samplesCount = 0;
                    min_sample = float.MaxValue;
                    max_sample = float.MinValue;
                }                
            }
        }

        // Method to get audio sample from an already existing item
        private void OpenAudio_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".wav"; // Default file extension
            dlg.Filter = "Wave audio (.wav)|*.wav"; // Filter files by extension
            dlg.InitialDirectory = TransformFilePath.GetParentDirectoryPath() + @"\Audio\Speech";

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {                
                filePath = dlg.FileName;
                ChosenFileLabel.Text = "File: " + filePath.Substring(filePath.LastIndexOf('\\') + 1);
                isFileSelected = true;

                // enabe done button
                DoneButton.IsEnabled = true;

                // set ui stuff
                SelectAudioButton.BorderBrush = Brushes.Purple;
                SelectAudioButton.Foreground = Brushes.Purple;
            }
        }

        // Metho to go to the next step
        private void Done_button_Click(object sender, RoutedEventArgs e)
        {
            // save data into storage
            MainWindow.storage.StoreFileData(NoiseLibrary.Storage.Category.SPEECH, filePath);

            Close();
        }

        // center the window
        private void SetWindowOnScreen()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = Width;
            double windowHeight = Height;
            Left = (screenWidth / 2) - (windowWidth / 2);
            Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private void sampleRateTextBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            // checking value in textbox            
            int rate;
            if (!int.TryParse(sampleRateTextBox.Text, out rate))
            {
                MessageBox.Show("Only 'int' values allowed!");
                sampleRateTextBox.Text = sampleRate.ToString();
                return;
            }
            sampleRate = rate;
        }

        private void sampleRateTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((e.Key == System.Windows.Input.Key.Enter) || (e.Key == System.Windows.Input.Key.Tab))
            {
                DoneButton.Focus(); // pass focus to another control
            }
        }
    }
}
