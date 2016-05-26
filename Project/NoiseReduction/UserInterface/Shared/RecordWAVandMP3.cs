using NAudio.Lame;
using NAudio.Wave;

namespace UserInterface.Shared
{
    /// <summary>
    /// Helper class to save recorded data
    /// </summary>
    public class RecordWAVandMP3
    {
        // Recorders
        private LameMP3FileWriter MP3writer; // mp3 writer
        private WaveFileWriter WAVwriter; // wav writer
        
        /// <summary>
        /// Instantiates writers
        /// </summary>
        /// <param name="filePath">Path of the file in which to store audio</param>
        /// <param name="waveFormat">NAudio object</param>
        /// <remarks>
        /// Call RecordingFinished() after work with class is done
        /// </remarks>
        public RecordWAVandMP3(string filePath,WaveFormat waveFormat)
        {
            WAVwriter = new WaveFileWriter(filePath, waveFormat);
        }

        /// <summary>
        /// Method to write bytes into wav files, assuming they are in the right format
        /// </summary>
        /// <param name="array">Recorded bytes</param>
        /// <param name="offset">Bytes offset</param>
        public void WriteBytes(byte[] array, int offset = 0)
        {
            WAVwriter.Write(array, offset, array.Length);
        }

        /// <summary>
        /// Method to write shorts into wav files, assuming they are in the right format
        /// </summary>
        /// <param name="array">Recorded shorts</param>
        /// <param name="offset">Shorts offset</param>
        public void WriteShorts(short[] array, int offset = 0)
        {
             WAVwriter.WriteSamples(array, offset, array.Length);
        }

        /// <summary>
        /// Method to release memory and close streams
        /// </summary>
        public void RecordingFinished()
        {
            //MP3writer.Dispose();
            WAVwriter.Dispose();

            //MP3writer.Close();
            WAVwriter.Close();
        }
    }
}
