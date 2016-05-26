using Microsoft.Win32;
using NoiseLibrary;
using System;
using System.Xml.Serialization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using UserInterface.Shared;
using Accord.Neuro.Learning; // for network training
using AForge.Neuro; // for network training
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Media;

namespace UserInterface
{

    /// <summary>
    /// Class to create the Log of the program and read it 
    /// </summary>
    [Serializable]
    public class Log
    {
        /// <summary>
        /// Nested class - elements of the current log
        /// </summary>           
        [Serializable]
        public class LogElement
        {
            // labels to find element in the log

            string label;
            public string Label { get { return label; } set { label = value; } }
            string labelEnd;
            public string LabelEnd { get { return labelEnd; } set { labelEnd = value; } }

            // label content
            string content;
            public string Content { get { return content; } set { content = value; } }

            public LogElement() { } // constructor for serialization

            /// <summary>
            /// New Log Element
            /// </summary>
            /// <param name="label">Log label</param>
            /// <param name="content">log element content</param>
            public LogElement(string label, string content)
            {
                this.label = "<" + label + ">";
                this.labelEnd = "</" + label + ">";
                this.content = content;
            }
        }

        [XmlArray]
        public LogElement[] logElements; // log array of elements

        private int el_counter = 0; // number of elements in the log

        public Log() { } // for serialization

        /// <summary>
        /// New log
        /// </summary>
        /// <param name="logPath">File path of the file that represents the program log</param>
        public Log(int capacity)
        {
            logElements = new LogElement[capacity];
        }

        /// <summary>
        /// Method to add new element into the log
        /// </summary>
        /// <param name="content"></param>
        /// <param name="label"></param>
        public void addElement(string content, string label)
        {
            logElements[el_counter++] = new LogElement(label, content);
        }

        // Method to remember Log elements 
        public static void serializeLog(Log log, string xmlPath)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Log));
            using (FileStream fs = new FileStream(xmlPath, FileMode.Create))
            {
                using (TextWriter sw = new StreamWriter(fs))
                {
                    ser.Serialize(sw, log);
                }
            }
        }

        // Method to recover Log elements from .xml 
        public static Log deserializeLog(string xmlPath)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Log));
            Log log;
            try
            {
                using (FileStream fs = new FileStream(xmlPath, FileMode.Open))
                {
                    using (TextReader sw = new StreamReader(fs))
                    {
                        log = (Log)ser.Deserialize(sw);
                    }
                }
                return log;
            }
            catch (FileNotFoundException fnfe)
            {
                // no log found
                return null;
            }
        }

    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Class fields and properties

        // RecordingWindow
        private RecordingWindow recordingWindow; // instance of a class
        private bool isRecordingWindowActive = false; // is window has been called
        private bool isSpeechChosen = false; // is window closed with result


        // NoiseChoosingWindow
        private NoiseChoosingWindow noiseChoosingWindow; // instance of a class
        private bool isNoiseWindowActive = false; // is window has been called
        private bool isNoiseChosen = false; // is window closed with result

        // File paths
        public static string SpeechFile { get; set; } = "Speech File"; // default speech label
        public static string FullSpeechFile { get; set; }

        public static string NoiseFile { get; set; } = "Noise File"; // default noise label
        public static string FullNoiseFile { get; set; }

        public static string NoiseWithSpeechFile { get; set; } = "Mixed File"; // default mixed label
        public static string FullNoiseWithSpeechFile { get; set; }

        public static string RecoveredSpeechFile { get; set; } = "Recovered Speech File"; // default mixed label
        public static string FullRecoveredSpeechFile { get; set; }

        // Helper classes
        private RecordWAVandMP3 recorder; // to store audio in files
        public static readonly Storage storage = new Storage(); // to store data in program

        // Helper fields
        protected double Fraction { get; set; } = 0.1; // fraction of a noise in speech
        protected bool isNoiseAdded = false; // is noise already added to speech
        protected readonly string xmlFullPath = TransformFilePath.GetParentDirectoryPath() + "\\Log.xml"; // xml file path
        protected int audioSaveHz = 19980;

        // Vizualization and sound playing
        private string speechFileForVizualizaion = "";
        private string noiseFileForVizualization = "";
        SoundPlayer soundplayer;
        // Timer visualTimer;

        // Network teaching parameters
        private int shuffleTimer = 20;
        private int saveTimer = 100; // once in a ___ epoches of learning the network parameters will be saved
        private double alpha = 1.0;
        private double delimiter = 32768;
        private const double substractor = 0.0;
        private const double outpDelimiter = 32767 + 32768;
        private const double outpSubstractor = 32768;
        private int[] sampleSize = { 12, 10, 5, 1 };
        private int iterationCount = 500;
        private double learningRateplus = 1.2;
        private double learningRateminus = 0.5;
        private string networkFileName = ""; // network for training
        private bool furtherTraining = false; // indicates if some network was selected for further training
        private double error = 0;
        private volatile bool pauseTraining = false; // pause training
        private volatile bool stopTraining = false; // stop training
        private delegate void UpdateUiFromTraining(string ItMessage, string ErrorMessage, double testC); // update ui from training thread
        private delegate void DisEnUiFromTraining(bool disable); // disable and enable ui stuff at the begining and at the end of the training
        private delegate void VizualizeRecSpeech(); // vizualize recovered speech
        private int sampleLength = 25000; // how many audio units are in training set

        // Log 
        private readonly int LOG_CAPACITY = 3; // number of elements in log (file path)
        Log log;

        #endregion

        // Main Constructor
        public MainWindow()
        {
            DataContext = this; // for binding

            InitializeComponent();

            // recovering data from log
            recoverPreviousSession();

            // setup neural stuff
            setupNeuralTraining();
        }

        #region Recording Window

        // Method to call new Recording Window
        private void CallRecord_button_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecordingWindowActive)
            {
                isRecordingWindowActive = !isRecordingWindowActive;
                recordingWindow = new RecordingWindow();
                recordingWindow.ClosedWithResult += RecordingWindow_ClosedWithResult;
                recordingWindow.ClosedWithoutResult += RecordingWindow_ClosedWithoutResult;
                recordingWindow.Show();
                recordingWindow.SelectAudioButton.Focus();
            }
            else
            {
                recordingWindow.SelectAudioButton.Focus();
            }
        }

        // Recording is over but no file had been selected
        private void RecordingWindow_ClosedWithoutResult(object sender, EventArgs e)
        {
            isRecordingWindowActive = false;
            MessageBox.Show("Recording window closed without audio file selection");
        }

        // Set some UI stuff after recording is over
        private void RecordingWindow_ClosedWithResult(object sender, EventArgs e)
        {
            isRecordingWindowActive = false;
            RecordingWindow_ClosedWithResult_UpdateUI();
        }
        private void RecordingWindow_ClosedWithResult_UpdateUI()
        {
            isSpeechChosen = true;
            speechTextBox.Text = SpeechFile;
            speechTextBox.BorderBrush = Brushes.Green;

            // check if all setup is done
            if (isNoiseChosen)
            {
                AddNoiseButton.IsEnabled = true;
                AddNoiseButton.BorderBrush = Brushes.Green;
                multTextBox.BorderBrush = Brushes.Green;
                FractionTextBox.BorderBrush = Brushes.Green;
            }
        }

        #endregion

        #region Noise Window

        // Method to call Noise chooser
        private void CallNoise_button_Click(object sender, RoutedEventArgs e)
        {
            if (!isNoiseWindowActive)
            {
                isNoiseWindowActive = !isNoiseWindowActive;
                noiseChoosingWindow = new NoiseChoosingWindow();
                noiseChoosingWindow.ClosedWithResult += NoiseChoosingWindow_ClosedWithResult;
                noiseChoosingWindow.ClosedWithoutResult += NoiseChoosingWindow_ClosedWithoutResult;
                noiseChoosingWindow.Show();
                noiseChoosingWindow.SelectNoiseButton.Focus();
            }
            else
            {
                noiseChoosingWindow.SelectNoiseButton.Focus();
            }
        }

        // Noise file was not selected
        private void NoiseChoosingWindow_ClosedWithoutResult(object sender, EventArgs e)
        {
            isNoiseWindowActive = false;
            MessageBox.Show("Noise window closed without audio file selection");
        }

        // Set some UI stuff after noise choosing is over
        private void NoiseChoosingWindow_ClosedWithResult(object sender, EventArgs e)
        {
            isNoiseWindowActive = false;
            NoiseChoosingWindow_ClosedWithResult_UpdateUI();
        }

        private void NoiseChoosingWindow_ClosedWithResult_UpdateUI()
        {
            isNoiseChosen = true;
            noiseTextBox.Text = NoiseFile;
            noiseTextBox.BorderBrush = Brushes.Green;
            plusTextBox.BorderBrush = Brushes.Green;

            // check if setup is done
            if (isSpeechChosen)
            {
                AddNoiseButton.IsEnabled = true;
                AddNoiseButton.BorderBrush = Brushes.Green;
                multTextBox.BorderBrush = Brushes.Green;
                FractionTextBox.BorderBrush = Brushes.Green;
            }
        }

        #endregion

        #region Main Window

        // Method to setuf neural stuff
        private void setupNeuralTraining()
        {
            FractionTextBox.Text = Fraction.ToString();
            SampleSizeTextBox.Text = sampleSize[0].ToString() + ", " + sampleSize[1].ToString() + ", " + sampleSize[2].ToString() + ", " + sampleSize[3].ToString();
            AlphaTextBox.Text = alpha.ToString();
            IterationsTextBox.Text = iterationCount.ToString();
            LearningRateTextBox.Text = learningRateplus.ToString() + "; " + learningRateminus.ToString();
            DelimiterTextBox.Text = delimiter.ToString();
            SaveNetworkTextBox.Text = saveTimer.ToString();
            ShuffleTextBox.Text = shuffleTimer.ToString();
        }

        // Method to call window to add noise
        private void AddNoise_button_Click(object sender, RoutedEventArgs e)
        {
            // ask user for a file name (noise with speech file path)
            FullNoiseWithSpeechFile = getFileName(true, "\\Speech with Noise", "AudioSample", "Chose noisy file");

            if (FullNoiseWithSpeechFile == "")
            {
                return;
            }

            // create new instance of noise adder
            NoiseAdder noiseAdder = new NoiseAdder(Fraction);

            // adding & remembering noise
            storage.StoreRawData(Storage.Category.SPEECHWITHNOISE, noiseAdder.AddNoise(storage.Speech, storage.Noise));

            // saving new data            
            recorder = new RecordWAVandMP3(FullNoiseWithSpeechFile, new NAudio.Wave.WaveFormat(audioSaveHz, 1)); /// ... kHz, 1 channel
            recorder.WriteShorts(storage.SpeechWithNoise);
            recorder.RecordingFinished();

            // update some ui stuff
            eqTextBox.BorderBrush = Brushes.Green;
            mixedTextBox.BorderBrush = Brushes.Green;
            NoiseWithSpeechFile = TransformFilePath.GetCanonicalName(FullNoiseWithSpeechFile, ".wav");
            mixedTextBox.Text = NoiseWithSpeechFile;

            // flag
            isNoiseAdded = true;

            RemoveNoiseButton.IsEnabled = true;

        } // AddNoiseButton_Click()        

        // Remember new fraction and update some UI stuff
        private void FractionTextBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            // checking value in textbox            
            double fraction;
            if (!double.TryParse(FractionTextBox.Text, out fraction))
            {
                MessageBox.Show("Only 'double' values allowed!");
                FractionTextBox.Text = Fraction.ToString();
                return;
            }
            Fraction = fraction;
        }

        //  Fraction text box text analysis
        private void FractionTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((e.Key == System.Windows.Input.Key.Enter) || (e.Key == System.Windows.Input.Key.Tab))
            {
                mixedTextBox.Focus();
            }
        }

        // Saving session settings in the log
        private void SaveSession_button_Click(object sender, RoutedEventArgs e)
        {
            log = new Log(LOG_CAPACITY);

            // adding log elements
            if (isSpeechChosen)
            {
                // remembering file names in log
                log.addElement(FullSpeechFile, "speech");
            }
            else
            {
                // extracting text from textboxes and updating them to match file names
                log.addElement(TransformFilePath.UpdateFileName(speechTextBox.Text), "speech");
            }
            if (isNoiseChosen)
            {
                log.addElement(FullNoiseFile, "noise");
            }
            else
            {
                log.addElement(TransformFilePath.UpdateFileName(noiseTextBox.Text), "noise");
            }
            if (isNoiseAdded)
            {
                log.addElement(FullNoiseWithSpeechFile, "mixed");
            }
            else
            {
                log.addElement(TransformFilePath.UpdateFileName(mixedTextBox.Text), "mixed");
            }

            // writing the log
            Log.serializeLog(log, xmlFullPath);
        }

        // focus record button
        private void speechTextBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RecordingButton.Focus();
        }

        // focus noise button
        private void noiseTextBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NoiseButton.Focus();
        }

        // Sample size textbox value changed
        private void SampleSizeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // checking value in textbox
            string sss = SampleSizeTextBox.Text; // sample size string
            string[] sssa = sss.Split(',');  // sample size string array

            // check if sss is 'ss' or 'ss, ss, ss, ss'
            if (sssa.Length == 1)
            {
                // checking value in textbox            
                int ssi;  // sample size int
                if (!int.TryParse(sss, out ssi))
                {
                    MessageBox.Show("Only 'int' values allowed!");
                    SampleSizeTextBox.Text = sampleSize[0].ToString() + ", " + sampleSize[1].ToString() + ", " + sampleSize[2].ToString() + ", " + sampleSize[3].ToString();
                    return;
                }
                sampleSize[0] = sampleSize[1] = sampleSize[2] = sampleSize[3] = ssi;
            }
            else if (sssa.Length == 4)
            {
                // check every value is sssa

                int ssi;  // sample size int
                if (!int.TryParse(sssa[0].Trim(), out ssi))
                {
                    MessageBox.Show("Only 'int' values allowed!");
                    SampleSizeTextBox.Text = sampleSize[0].ToString() + ", " + sampleSize[1].ToString() + ", " + sampleSize[2].ToString() + ", " + sampleSize[3].ToString();
                    return;
                }
                int tempInp = ssi;
                if (!int.TryParse(sssa[1].Trim(), out ssi))
                {
                    MessageBox.Show("Only 'int' values allowed!");
                    SampleSizeTextBox.Text = sampleSize[0].ToString() + ", " + sampleSize[1].ToString() + ", " + sampleSize[2].ToString() + ", " + sampleSize[3].ToString();
                    return;
                }
                int tempH1 = ssi;
                if (!int.TryParse(sssa[2].Trim(), out ssi))
                {
                    MessageBox.Show("Only 'int' values allowed!");
                    SampleSizeTextBox.Text = sampleSize[0].ToString() + ", " + sampleSize[1].ToString() + ", " + sampleSize[2].ToString() + ", " + sampleSize[3].ToString();
                    return;
                }
                int tempH2 = ssi;
                if (!int.TryParse(sssa[3].Trim(), out ssi))
                {
                    MessageBox.Show("Only 'int' values allowed!");
                    SampleSizeTextBox.Text = sampleSize[0].ToString() + ", " + sampleSize[1].ToString() + ", " + sampleSize[2].ToString() + ", " + sampleSize[3].ToString();
                    return;
                }
                int tempOutp = ssi;

                /*if (tempInp != tempOutp)
                {
                    MessageBox.Show("Input nodes length must match ouptput nodes length");
                    SampleSizeTextBox.Text = sampleSize[0].ToString() + ", " + sampleSize[1].ToString() + ", " + sampleSize[2].ToString() + ", " + sampleSize[3].ToString();
                    return;
                }
                else
                {*/
                sampleSize[0] = tempInp;
                sampleSize[1] = tempH1;
                sampleSize[2] = tempH2;
                sampleSize[3] = tempOutp;
                //}
            }
            else
            {
                MessageBox.Show("Wrong SampleSize length number (only 1 and 4 allowed)");
                SampleSizeTextBox.Text = sampleSize[0].ToString() + ", " + sampleSize[1].ToString() + ", " + sampleSize[2].ToString() + ", " + sampleSize[3].ToString();
                return;
            }
        }

        private void SampleSizeTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AlphaTextBox.Focus();
            }
        }

        // Iterations textbox value changed
        private void IterationsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // checking value in textbox            
            int it;
            if (!int.TryParse(IterationsTextBox.Text, out it))
            {
                MessageBox.Show("Only 'int' values allowed!");
                IterationsTextBox.Text = iterationCount.ToString();
                return;
            }
            iterationCount = it;
        }

        private void IterationsTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (LearningRateTextBox.IsEnabled)
                {
                    LearningRateTextBox.Focus();
                }
                else
                {
                    ShuffleTextBox.Focus();
                }
            }

        }

        // Shuffle textbox value changed
        private void ShuffleTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // checking value in textbox            
            int sh;
            if (!int.TryParse(ShuffleTextBox.Text, out sh))
            {
                MessageBox.Show("Only 'int' values allowed!");
                ShuffleTextBox.Text = shuffleTimer.ToString();
                return;
            }
            shuffleTimer = sh;
        }

        private void ShuffleTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SaveNetworkTextBox.Focus();
            }
        }

        // Save network textbox value changed
        private void SaveNetworkTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // checking value in textbox            
            int sv;
            if (!int.TryParse(SaveNetworkTextBox.Text, out sv))
            {
                MessageBox.Show("Only 'int' values allowed!");
                SaveNetworkTextBox.Text = saveTimer.ToString();
                return;
            }
            saveTimer = sv;
        }

        private void SaveNetworkTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (RemoveNoiseButton.IsEnabled)
                {
                    RemoveNoiseButton.Focus();
                }
                else
                {
                    PausePlayTrainingButton.Focus();
                }
            }
        }

        // Alpha textbox value changed
        private void AlphaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // checking value in textbox            
            double alph;
            if (!double.TryParse(AlphaTextBox.Text, out alph))
            {
                MessageBox.Show("Only 'double' values allowed!");
                AlphaTextBox.Text = alpha.ToString();
                return;
            }
            alpha = alph;
        }

        private void AlphaTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                IterationsTextBox.Focus();
            }
        }

        // Learning rate textbox value changed
        private void LearningRateTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // checking value in textbox            
            double lr1, lr2;

            string[] lrd = LearningRateTextBox.Text.Split(';');

            bool badParse = (!double.TryParse(lrd[0].Trim(), out lr1)) | (!double.TryParse(lrd[1].Trim(), out lr2));

            if (badParse)
            {
                MessageBox.Show("Only 'double' values allowed!");
                LearningRateTextBox.Text = learningRateplus.ToString() + "; " + learningRateminus.ToString();
                return;
            }

            if (lr1 <= 1)
            {
                MessageBox.Show("eta+ must be higher than 1!");
                LearningRateTextBox.Text = learningRateplus.ToString() + "; " + learningRateminus.ToString();
                return;
            }
            else if ((lr2 <= 0) || (lr2 >= 1))
            {
                MessageBox.Show("eta- must be in (0, 1)");
                LearningRateTextBox.Text = learningRateplus.ToString() + "; " + learningRateminus.ToString();
                return;
            }


            learningRateplus = lr1;
            learningRateminus = lr2;
        }

        private void LearningRateTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                DelimiterTextBox.Focus();
            }
        }

        // Delimiter textbox value changed
        private void DelimiterTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // checking value in textbox            
            double dl;
            if (!double.TryParse(DelimiterTextBox.Text, out dl))
            {
                MessageBox.Show("Only 'double' values allowed!");
                DelimiterTextBox.Text = delimiter.ToString();
                return;
            }
            delimiter = dl;
        }

        private void DelimiterTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ShuffleTextBox.Focus();
            }
        }

        // Upload network for training
        private void UploadNetworkForTrainigButton_Click(object sender, RoutedEventArgs e)
        {
            // get network
            string netFileName = getFileName(false, "\\Networks", "Some network", "Choose Network", ".bin");
            if (netFileName == "")
            {
                return;
            }

            // save network name
            networkFileName = netFileName;
            furtherTraining = true;

            // set some ui stuff
            SampleSizeTextBox.IsEnabled = false;
            SampleSizeTextBox.ToolTip = "You cannot modify this field with selected network";
            Network net = Network.Load(netFileName);
            if (net.Layers.Length == 2)
            {
                SampleSizeTextBox.Text = net.InputsCount.ToString() + ", " + net.Layers[0].Neurons.Length + ", 0, " + net.Layers[1].Neurons.Length;
            }
            else
            {
                SampleSizeTextBox.Text = net.InputsCount.ToString() + ", " + net.Layers[0].Neurons.Length + ", " + net.Layers[1].Neurons.Length + ", " + net.Layers[2].Neurons.Length;
            }

        }

        // Clear training parameters
        private void ClearNetworkTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            // training parameters
            furtherTraining = false;
            networkFileName = "";
            alpha = 0.125;
            sampleSize[0] = 1;
            sampleSize[1] = 10;
            sampleSize[2] = 0;
            sampleSize[3] = 1;
            iterationCount = 500;
            learningRateplus = 1.2;
            learningRateminus = 0.5;
            delimiter = 8.0;
            saveTimer = 100;
            shuffleTimer = 20;

            // ui
            ShuffleTextBox.Text = shuffleTimer.ToString();
            SaveNetworkTextBox.Text = saveTimer.ToString();
            FractionTextBox.Text = Fraction.ToString();
            SampleSizeTextBox.Text = sampleSize[0].ToString() + ", " + sampleSize[1].ToString() + ", " + sampleSize[2].ToString() + ", " + sampleSize[3].ToString();
            AlphaTextBox.Text = alpha.ToString();
            IterationsTextBox.Text = iterationCount.ToString();
            LearningRateTextBox.Text = learningRateplus.ToString() + "; " + learningRateminus.ToString();
            DelimiterTextBox.Text = delimiter.ToString();
            SampleSizeTextBox.IsEnabled = true;
        }

        #endregion

        #region Helpers

        // Method to get file name in which to store nwc audio from user
        string getFileName(bool save, string directory = "", string defaultName = "AudioSample", string title = "", string extension = ".wav")
        {
            bool? result;
            FileDialog dlg;

            if (save)
            {
                dlg = new SaveFileDialog();
                dlg.FileName = defaultName; // Default file name
                if (title != "")
                    dlg.Title = title;

                dlg.DefaultExt = extension; // Default file extension
                dlg.Filter = "File (" + extension + ")|*" + extension; // Filter files by extension
                dlg.InitialDirectory = TransformFilePath.GetParentDirectoryPath() + @"\Audio" + directory;

                // Show save file dialog box
                result = dlg.ShowDialog();
            }
            else
            {
                dlg = new OpenFileDialog();
                dlg.FileName = defaultName; // Default file name
                if (title != "")
                    dlg.Title = title;

                dlg.DefaultExt = extension; // Default file extension
                dlg.Filter = "File (" + extension + ")|*" + extension; // Filter files by extension
                dlg.InitialDirectory = TransformFilePath.GetParentDirectoryPath() + @"\Audio" + directory;

                // Show save file dialog box
                result = dlg.ShowDialog();
            }

            // Process save file dialog box results
            if (result == true)
            {
                return dlg.FileName;
            }
            else {
                return "";
            }
        }

        /// <summary>
        /// Reading log and setting up the program 
        /// </summary>
        private void recoverPreviousSession()
        {
            // set window coordinates
            CenterWindowOnScreen();

            log = Log.deserializeLog(xmlFullPath);

            // check if log has been created
            if (log == null) return;

            // check if log has entries
            if (log.logElements.Length == 0) return;

            // read the log
            for (int i = 0; i < log.logElements.Length; i++)
            {
                if (log.logElements[i].Label == "<speech>")
                {
                    if (SpeechFile != TransformFilePath.GetCanonicalName(log.logElements[i].Content, ".wav"))
                    {
                        SpeechFile = TransformFilePath.GetCanonicalName(log.logElements[i].Content, ".wav");
                        FullSpeechFile = log.logElements[i].Content;

                        try
                        {
                            // storing data
                            storage.StoreFileData(Storage.Category.SPEECH, FullSpeechFile);

                            // updating ui
                            RecordingWindow_ClosedWithResult_UpdateUI();
                        }
                        catch (FileNotFoundException fnfe)
                        {
                            // no log file found
                        }
                    }
                    continue;
                }
                if (log.logElements[i].Label == "<noise>")
                {
                    if (NoiseFile != TransformFilePath.GetCanonicalName(log.logElements[i].Content, ".wav"))
                    {
                        NoiseFile = TransformFilePath.GetCanonicalName(log.logElements[i].Content, ".wav");
                        FullNoiseFile = log.logElements[i].Content;

                        try
                        {
                            // storing data
                            storage.StoreFileData(Storage.Category.NOISE, FullNoiseFile);

                            // updating ui
                            NoiseChoosingWindow_ClosedWithResult_UpdateUI();
                        }
                        catch (FileNotFoundException fnfe)
                        {
                            // no log file found
                        }

                    }
                    continue;
                }
                if (log.logElements[i].Label == "<mixed>")
                {
                    if (NoiseWithSpeechFile != TransformFilePath.GetCanonicalName(log.logElements[i].Content, ".wav"))
                    {
                        NoiseWithSpeechFile = TransformFilePath.GetCanonicalName(log.logElements[i].Content, ".wav");
                        FullNoiseWithSpeechFile = log.logElements[i].Content;

                        try
                        {
                            // adding & remembering noise
                            storage.StoreFileData(Storage.Category.SPEECHWITHNOISE, FullNoiseWithSpeechFile);

                            // update some ui stuff
                            eqTextBox.BorderBrush = Brushes.Green;
                            mixedTextBox.BorderBrush = Brushes.Green;
                            NoiseWithSpeechFile = TransformFilePath.GetCanonicalName(FullNoiseWithSpeechFile, ".wav");
                            mixedTextBox.Text = NoiseWithSpeechFile;

                            // flag
                            isNoiseAdded = true;
                        }
                        catch (FileNotFoundException fnfe)
                        {
                            // no log file found
                        }
                    }
                    continue;
                }

                // log did not contain any of the default labels
                throw new Exception("Log error - no matching labels");
            } // for()

            if ((isNoiseAdded) && (isSpeechChosen))
            {
                RemoveNoiseButton.IsEnabled = true;
            }

            // clear log
            Log.serializeLog(new Log(0), xmlFullPath);


        } // recoverPreviousSession()

        // center the window
        private void CenterWindowOnScreen()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = Width;
            double windowHeight = Height;
            Left = (screenWidth / 2) - (windowWidth / 2);
            Top = (screenHeight / 2) - (windowHeight / 2);
        }

        #endregion

        #region neuralStuff

        // Update ui stuff from training thread
        private void UpdateText(string error, string iteration, double testC)
        {
            ErrorTextBox.Text = error;
            ItCountTextBox.Text = iteration;
            TrainingProgressBar.Dispatcher.Invoke(() => TrainingProgressBar.Value = testC, DispatcherPriority.Background);
        }

        // vizualize the result of a one training epoch
        private void VizulizeRecoveredSpeech()
        {
            clearedSpeechErrorTextBox.Text = "Error = ";
            ClearedSpeechButton.IsEnabled = false; // block 'play' button for recovered speech

            // clear polygon and vizualize data
            clearedSpeechPolygon.Points.Clear();
            VisualizeAudioData visualizer = new VisualizeAudioData(clearedSpeechPolygon); // helper to visualize recorded data
            visualizer.VisualizeFile(storage.RecoveredSpeech, storage.RecoveredSpeech.Length / ((int)clearedSpeechPolygon.ActualWidth));
        }

        // Disable or enable ui form training thread
        private void DisEnUi(bool disable)
        {
            if (disable)
            {
                // training starts

                RemoveNoiseButton.IsEnabled = false; // block this button while network training is running
                StopTrainingButton.IsEnabled = true;
                PausePlayTrainingButton.IsEnabled = true; // unlock specific for training buttons

                // block training ui
                AlphaTextBox.IsEnabled = false;
                AddNoiseButton.IsEnabled = false;
                DelimiterTextBox.IsEnabled = false;
                SampleSizeTextBox.IsEnabled = false;
                LearningRateTextBox.IsEnabled = false;
                ClearNetworkTrainingButton.IsEnabled = false;
                UploadNetworkForTrainigButton.IsEnabled = false;


                // clear ui stuff
                UpdateText("Error = ", "It = ", 0);
            }
            else
            {
                // training ends

                RemoveNoiseButton.IsEnabled = true; // unblock this button 
                PausePlayTrainingButton.IsEnabled = false;

                stopTraining = false;
                pauseTraining = false;

                // ulock training ui
                AlphaTextBox.IsEnabled = true;
                AddNoiseButton.IsEnabled = true;
                DelimiterTextBox.IsEnabled = true;
                SampleSizeTextBox.IsEnabled = true;
                LearningRateTextBox.IsEnabled = true;
                ClearNetworkTrainingButton.IsEnabled = true;
                UploadNetworkForTrainigButton.IsEnabled = true;

            }
        }

        // Train network
        private void RemoveNoiseButton_Click(object sender, RoutedEventArgs e)
        {

            #region alg

            Action<object> training = (object obj) =>
            {
                // Disable and enable some ui stuff
                TrainingProgressBar.Dispatcher.Invoke(new DisEnUiFromTraining(DisEnUi), new object[] { true });

                Network network; // network to train

                // check if some network was selected for further training
                if (furtherTraining)
                {
                    try
                    {
                        network = Network.Load(networkFileName);  // load network
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("No such network: " + networkFileName);
                        return;
                    }

                    sampleSize[0] = network.InputsCount; // remember input and output layers nodes count
                    sampleSize[1] = network.Layers[1].InputsCount;
                    if (network.Layers.Length > 2)
                    {
                        sampleSize[2] = network.Layers[2].InputsCount;
                        sampleSize[3] = network.Layers[2].Neurons.Length;
                    }
                    else
                    {
                        sampleSize[2] = 0;
                        sampleSize[3] = network.Layers[1].Neurons.Length;
                    }
                }
                else
                {
                    // if not: create new network

                    if (sampleSize[2] == 0)
                    {
                        network = new ActivationNetwork(new SigmoidFunction(alpha),
                            sampleSize[0], // inputs in the network
                            sampleSize[1], // first layer 
                            sampleSize[3] // output layer
                        );
                    }
                    else
                    {
                        network = new ActivationNetwork(new SigmoidFunction(alpha),
                            sampleSize[0], // inputs in the network
                            sampleSize[1], // first layer 
                            sampleSize[2], // second layer
                            sampleSize[3] // output layer
                        );
                    }
                }

                double[][] inp;
                double[][] outp;
                int totalIndex = 0; // start training set at a _
                                    // input == output
                if (sampleSize[0] == sampleSize[3])
                {
                    inp = new double[storage.SpeechWithNoise.Length / sampleSize[0]][];
                    outp = new double[storage.Speech.Length / sampleSize[3]][];
                    for (int i = 0; i < inp.Length; i++)
                    {
                        inp[i] = new double[sampleSize[0]];
                        outp[i] = new double[sampleSize[3]];
                        for (int j = 0; j < sampleSize[0]; j++)
                        {
                            inp[i][j] = (storage.SpeechWithNoise[totalIndex] - substractor) / delimiter;
                            outp[i][j] = (storage.Speech[totalIndex] + outpSubstractor) / outpDelimiter;
                            totalIndex += 1; // increment by one audio point

                        }
                    }
                }
                else
                {
                    inp = new double[storage.SpeechWithNoise.Length - sampleSize[0] + 1][];
                    outp = new double[inp.Length][];
                    for (int i = 0; i < inp.Length; i++)
                    {
                        inp[i] = new double[sampleSize[0]];
                        outp[i] = new double[sampleSize[3]];
                        int intIndex = totalIndex;
                        for (int j = 0; j < sampleSize[0]; j++)
                        {
                            inp[i][j] = (storage.SpeechWithNoise[intIndex++] - substractor) / delimiter;
                        }
                        for (int j = 0; j < sampleSize[3]; j++)
                        {
                            outp[i][j] = (storage.Speech[totalIndex + sampleSize[0] - 1] + outpSubstractor) / outpDelimiter;
                        }
                        totalIndex += 1; // increment by one audio point
                    }

                }

                ParallelResilientBackpropagationLearning teacher = new ParallelResilientBackpropagationLearning((ActivationNetwork)network); // teaching algorithm from Accord lib
                teacher.IncreaseFactor = learningRateplus;
                teacher.DecreaseFactor = learningRateminus;

                int testC = 0; // number of learning epoches

                while (testC != iterationCount)
                {
                    // check if training is paused
                    if (pauseTraining)
                    {
                        while (true)
                        {
                            if ((!pauseTraining) || (stopTraining))
                            {
                                break;
                            }
                        }
                    }

                    // check if training is stopped
                    if (stopTraining)
                    {
                        break;
                    }

                    // run learning epoch
                    try
                    {
                        error = teacher.RunEpoch(inp, outp);
                        testC++;

                        // setup Ui stuff                    
                        TrainingProgressBar.Dispatcher.Invoke(new UpdateUiFromTraining(UpdateText), new object[] { string.Format("Error = {0:0.000000000000}", error), string.Format("It = {0}/{1}", testC, iterationCount), testC * 100.0 / iterationCount });


                        // save some networks
                        if (testC % saveTimer == 0)
                        {
                            // save network parameters
                            string path = TransformFilePath.GetParentDirectoryPath() + "\\Audio" + "\\Networks" + "\\PRprop Sample size " + sampleSize[0].ToString()
                                + "," + sampleSize[1].ToString() + "," + sampleSize[2].ToString() + "," + sampleSize[3].ToString() + "  Alpha " + alpha.ToString() +
                                "  It " + testC.ToString() + " Del " + delimiter.ToString() + " LR+ " + learningRateplus.ToString() + " LR- " + learningRateminus.ToString() + "  Noise " + Fraction.ToString() + "  Error " + error.ToString() + ".bin";

                            if (furtherTraining)
                            {
                                path.Insert(path.IndexOf(".") - 1, "+");
                            }

                            network.Save(path);


                        } // end of if ()

                        short[] res;
                        if (sampleSize[0] == sampleSize[3])
                        {
                            res = new short[inp.GetLength(0) * sampleSize[0]];
                        }
                        else
                        {
                            res = new short[inp.GetLength(0)];
                        }
                        int count = 0;
                        for (int i = 0; i < inp.GetLength(0); i++)
                        {
                            double[] tempRes = network.Compute(inp[i]);
                            for (int j = 0; j < tempRes.Length; j++)
                            {
                                res[count++] = (short)(tempRes[j] * outpDelimiter - outpSubstractor);
                            }
                        }

                        // save speech
                        storage.StoreRawData(Storage.Category.RECOVEREDSPEECH, res);

                        // vizual and stuff
                        TrainingProgressBar.Dispatcher.Invoke(new VizualizeRecSpeech(VizulizeRecoveredSpeech), new object[] { });



                        if (testC % shuffleTimer == 0)
                        {
                            // generate next input with random noise
                            totalIndex = 0;
                            NoiseAdder na = new NoiseAdder(Fraction);
                            short[] randomSpeechWithNoise = na.AddNoise(storage.Speech, storage.Noise);

                            if (sampleSize[0] == sampleSize[3])
                            {
                                for (int i = 0; i < inp.Length; i++)
                                {
                                    inp[i] = new double[sampleSize[0]];
                                    outp[i] = new double[sampleSize[3]];
                                    for (int j = 0; j < sampleSize[0]; j++)
                                    {
                                        inp[i][j] = (randomSpeechWithNoise[totalIndex] - substractor) / delimiter;
                                        totalIndex += 1; // increment by one audio point

                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < inp.Length; i++)
                                {
                                    inp[i] = new double[sampleSize[0]];
                                    int intIndex = totalIndex;
                                    for (int j = 0; j < sampleSize[0]; j++)
                                    {
                                        inp[i][j] = (randomSpeechWithNoise[intIndex++] - substractor) / delimiter;
                                    }
                                    totalIndex += 1; // increment by one audio point
                                }

                            }
                        } // end of if() shuffle samples
                    }
                    catch (OutOfMemoryException)
                    {
                        MessageBox.Show("Network is too large, not enough memory");
                        break;
                    }



                }  // end of for() sampleSizes

                // Disable and enable some ui stuff
                TrainingProgressBar.Dispatcher.Invoke(new DisEnUiFromTraining(DisEnUi), new object[] { false });

            };

            #endregion

            // start training
            Task t1 = new Task(training, "training");
            t1.Start();


        } // RemoveNoiseButton_Click()

        // Check network
        private void CheckNetworkButton_Click(object sender, RoutedEventArgs e)
        {
            // get network
            string netFileName = getFileName(false, "\\Networks", "Some network", "Choose Network", ".bin");
            if (netFileName == "")
            {
                return;
            }
            Network network;
            try
            {
                network = Network.Load(netFileName); // load trained network form '.bin' file
            }
            catch (IOException)
            {
                MessageBox.Show("No such network: " + netFileName);
                return;
            }

            int localSampleSize = network.InputsCount; // number of input for this particular network

            // prepare input
            int outputSize;
            if (network.Layers.Length == 2)
            {
                outputSize = network.Layers[1].Neurons.Length;
            }
            else if (network.Layers.Length == 3)
            {
                outputSize = network.Layers[2].Neurons.Length;
            }
            else
            {
                MessageBox.Show("Unknown network structure");
                return;
            }

            double[][] inp;
            short[] res;

            // prepare input for a neural net
            if (localSampleSize == outputSize)
            {
                inp = new double[storage.NoiseForVizualization.Length / localSampleSize][];
                int totalIndex = 0;
                for (int i = 0; i < inp.GetLength(0); i++)
                {
                    inp[i] = new double[localSampleSize];
                    for (int j = 0; j < localSampleSize; j++)
                    {
                        inp[i][j] = (storage.NoiseForVizualization[totalIndex++] - substractor) / delimiter; // Check if delimiter is right here!

                    }
                }
                res = new short[localSampleSize * inp.GetLength(0)];
            }

            else
            {
                inp = new double[storage.NoiseForVizualization.Length - localSampleSize + 1][];
                int totalIndex = 0;
                for (int i = 0; i < inp.Length; i++)
                {
                    int ind = totalIndex;
                    inp[i] = new double[localSampleSize];
                    for (int j = 0; j < localSampleSize; j++)
                    {
                        inp[i][j] = (storage.NoiseForVizualization[ind++] - substractor) / delimiter; // Check if delimiter is right here!

                    }
                    totalIndex++;
                }
                res = new short[inp.GetLength(0)];
            }

            double error = 0; // Just error
            int count = 0;

            // calculating recovered speech
            for (int i = 0; i < inp.GetLength(0); i++)
            {
                double[] tempRes = network.Compute(inp[i]);
                for (int j = 0; j < tempRes.Length; j++)
                {
                    res[count] = (short)(tempRes[j] * outpDelimiter - outpSubstractor);
                    if ((storage.SpeechForVizualization != null) && (storage.SpeechForVizualization.Length == storage.NoiseForVizualization.Length))
                    {
                        error += Math.Abs(storage.SpeechForVizualization[count] - res[count]);
                    }
                    count++;
                }
            }

            error /= count;

            // rememver recovered audio file
            storage.StoreRawData(Storage.Category.RECOVEREDSPEECH, res);

            FullRecoveredSpeechFile = getFileName(true, "\\Recovered Speech", TransformFilePath.GetCanonicalName(noiseFileForVizualization, ".wav"), "Save recovered speech");

            if (FullRecoveredSpeechFile == "")
                return;

            // saving new data in ".wav"
            recorder = new RecordWAVandMP3(FullRecoveredSpeechFile, new NAudio.Wave.WaveFormat(audioSaveHz, 1)); /// 8 kHz, 1 channel
            recorder.WriteShorts(storage.RecoveredSpeech);
            recorder.RecordingFinished();

            // clear polygon and vizualize data
            clearedSpeechPolygon.Points.Clear();
            VisualizeAudioData visualizer = new VisualizeAudioData(clearedSpeechPolygon); // helper to visualize recorded data
            visualizer.VisualizeFile(storage.RecoveredSpeech, storage.RecoveredSpeech.Length / ((int)clearedSpeechPolygon.ActualWidth));
            
            // show error
            if ((storage.SpeechForVizualization != null) && (storage.SpeechForVizualization.Length == storage.NoiseForVizualization.Length))
            {
                clearedSpeechErrorTextBox.Text = string.Format("Error = {0:0.000000000}", error);
            }

            // open button to play sound
            ClearedSpeechButton.IsEnabled = true;
        }

        // Stop training of the network
        private void StopTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            stopTraining = true;
            StopTrainingButton.IsEnabled = false;
        }

        // Pause/ unpause training of the network
        private void PausePlayTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            if (PausePlayTrainingButton.Content.ToString() == "Pause")
            {
                PausePlayTrainingButton.Content = "Play";
                pauseTraining = true;
            }
            else if (PausePlayTrainingButton.Content.ToString() == "Play")
            {
                PausePlayTrainingButton.Content = "Pause";
                pauseTraining = false;
            }

        }
        #endregion

        #region Vizualizaion

        // upload speech file for vizualization
        private void UploadSpeechForVizualizationButton_Click(object sender, RoutedEventArgs e)
        {
            // get file name
            speechFileForVizualizaion = getFileName(false, "\\Speech", "Speech.wav", "Select speech for vizualization", ".wav");

            if (speechFileForVizualizaion == "")
            {
                VizualizeSpeechButton.IsEnabled = false;
                return; // bad input
            }

            // remember data
            storage.StoreFileData(Storage.Category.SPEECHFORVIZUALIZATION, speechFileForVizualizaion);

            // open visualization
            VizualizeSpeechButton.IsEnabled = true;

            // clear polygon and vizualize data
            cleanSpeechPolygon.Points.Clear();
            VisualizeAudioData visualizer = new VisualizeAudioData(cleanSpeechPolygon); // helper to visualize recorded data
            visualizer.VisualizeFile(storage.SpeechForVizualization, storage.SpeechForVizualization.Length / ((int)cleanSpeechPolygon.ActualWidth));
        }

        //Show content of a audio file using polygon and play it's sound
        private void VizualizeSpeechButton_Click(object sender, RoutedEventArgs e)
        {
            if (soundplayer != null)
            {
                soundplayer.Stop();
            }
            soundplayer = new SoundPlayer(speechFileForVizualizaion);
            soundplayer.Play();

            // start timer
            /*if (visualTimer != null)
            {
                visualTimer.Stop();
            }
            visualTimer = new Timer();
            visualTimer.Elapsed += speechVisualTimer_Elapsed;
            visualTimer.Start();*/
        }

        // upload speech file for vizualization and to remove noise from it
        private void UploadNoisySpeechForVizualizationButton_Click(object sender, RoutedEventArgs e)
        {
            // get file name
            noiseFileForVizualization = getFileName(false, "\\Speech with Noise", "Noisy.wav", "Select noisy file for vizualization", ".wav");

            if (noiseFileForVizualization == "")
            {
                VizualizeNoisySpeechButton.IsEnabled = false;
                CheckNetworkButton.IsEnabled = false;
                return; // bad input
            }

            // remember data
            storage.StoreFileData(Storage.Category.NOISEFORVIZUALIZATION, noiseFileForVizualization);

            // open visualization
            VizualizeNoisySpeechButton.IsEnabled = true;

            // clear polygon and vizualize data
            noisySpeechPolygon.Points.Clear();
            VisualizeAudioData visualizer = new VisualizeAudioData(noisySpeechPolygon); // helper to visualize recorded data
            visualizer.VisualizeFile(storage.NoiseForVizualization, storage.NoiseForVizualization.Length / ((int)noisySpeechPolygon.ActualWidth));

            // enable button for noise removal
            CheckNetworkButton.IsEnabled = true;
        }

        private void VizualizeNoisySpeechButton_Click(object sender, RoutedEventArgs e)
        {
            if (soundplayer != null)
            {
                soundplayer.Stop();
            }
            soundplayer = new SoundPlayer(noiseFileForVizualization);
            soundplayer.Play();
        }

        private void ClearedSpeechButton_Click(object sender, RoutedEventArgs e)
        {
            if (soundplayer != null)
            {
                soundplayer.Stop();
            }
            soundplayer = new SoundPlayer(FullRecoveredSpeechFile);
            soundplayer.Play();
        }


        #endregion

    }
}

