using System.IO;
using NAudio.Wave;

namespace NoiseLibrary
{
    /// <summary>
    /// Class to store various audio samples
    /// </summary>
    public class Storage
    {
        public enum Category
        {
            SPEECH, NOISE, SPEECHWITHNOISE, RECOVEREDSPEECH, SPEECHFORVIZUALIZATION, NOISEFORVIZUALIZATION
        }

        private short[] speechForVizualization;
        public short[] SpeechForVizualization { get { return speechForVizualization; } }

        private short[] noiseForVizualization;
        public short[] NoiseForVizualization { get { return noiseForVizualization; } }

        /// <summary>
        /// User records audio with a sampling rate of 19.98 KHz (or 8 khz) and random lenght
        /// </summary>
        private short[] speech;
        public short[] Speech { get { return speech; } }

        /// <summary>
        /// Noise, all noises are 235 seconds long and have a sampling rate of 19.98 KHz
        /// </summary>
        private short[] noise;
        public short[] Noise { get { return noise; } }

        /// <summary>
        /// Speech with noise
        /// </summary>
        private short[] speechWithNoise;
        public short[] SpeechWithNoise { get { return speechWithNoise; } }

        /// <summary>
        /// Speech with noise after neural noise reduction
        /// </summary>
        private short[] recoveredSpeech;
        public short[] RecoveredSpeech { get { return recoveredSpeech; } }

        /// <summary>
        /// Method to store data from file in this object
        /// </summary>
        /// <param name="categoty">In which category to save data</param>
        /// <param name="filePath">File path from which to read data (FULL)</param>
        public void StoreFileData(Category category, string filePath)
        {
            using (WaveFileReader reader = new WaveFileReader(filePath))
            {
                // Assert.AreEqual(16, reader.WaveFormat.BitsPerSample, "Only works with 16 bit audio");
                byte[] buffer = new byte[reader.Length];
                int read = reader.Read(buffer, 0, buffer.Length);
                switch (category)
                {
                    case Category.SPEECH:
                        speech = new short[read / 2];
                        System.Buffer.BlockCopy(buffer, 0, speech, 0, read);
                        break;
                    case Category.NOISE:
                        noise = new short[read / 2];
                        System.Buffer.BlockCopy(buffer, 0, noise, 0, read);
                        break;
                    case Category.SPEECHWITHNOISE:
                        speechWithNoise = new short[read / 2];
                        System.Buffer.BlockCopy(buffer, 0, speechWithNoise, 0, read);
                        break;
                    case Category.RECOVEREDSPEECH:
                        recoveredSpeech = new short[read / 2];
                        System.Buffer.BlockCopy(buffer, 0, recoveredSpeech, 0, read);
                        break;
                    case Category.SPEECHFORVIZUALIZATION:
                        speechForVizualization = new short[read / 2];
                        System.Buffer.BlockCopy(buffer, 0, speechForVizualization, 0, read);
                        break;
                    case Category.NOISEFORVIZUALIZATION:
                        noiseForVizualization = new short[read / 2];
                        System.Buffer.BlockCopy(buffer, 0, noiseForVizualization, 0, read);
                        break;
                    default:
                        throw new System.Exception("No such category in Storage class");
                }
            
            }
        }

        /// <summary>
        /// Method to store data from raw data
        /// </summary>
        /// <param name="category">In which category to save data</param>
        /// <param name="data">Raw audio data</param>
        public void StoreRawData(Category category, short[] data)
        {
            switch (category)
            {
                case Category.SPEECH:
                    speech = new short[data.Length];
                    data.CopyTo(speech, 0);
                    break;
                case Category.NOISE:
                    noise = new short[data.Length];
                    data.CopyTo(noise, 0);
                    break;
                case Category.SPEECHWITHNOISE:
                    speechWithNoise = new short[data.Length];
                    data.CopyTo(speechWithNoise, 0);
                    break;
                case Category.RECOVEREDSPEECH:
                    recoveredSpeech = new short[data.Length];
                    data.CopyTo(recoveredSpeech, 0);
                    break;
                case Category.NOISEFORVIZUALIZATION:
                    noiseForVizualization = new short[data.Length];
                    data.CopyTo(noiseForVizualization, 0);
                    break;
                case Category.SPEECHFORVIZUALIZATION:
                    speechForVizualization = new short[data.Length];
                    data.CopyTo(speechForVizualization, 0);
                    break;
                default:
                    throw new System.Exception("No such category in Storage class");
            }
        }
        
    }
}
