using System;

namespace NoiseLibrary
{
    public class NoiseAdder
    {
        /// <summary>
        /// Represents the amount of noise in the resulting audio wave
        /// </summary>
        private double factor;

        /// <summary>
        /// Creates new instance of NoiseAdder
        /// </summary>
        /// <param name="factor">Noise factor in the result</param>
        public NoiseAdder(double factor = 1)
        {
            this.factor = factor;
        }

        /// <summary>
        /// Adds noise to speech, 
        /// </summary>
        /// <param name="speech">Speech audio signal</param>
        /// <param name="noise">Noise audio signal</param>
        /// <returns>Combined audio waves, but the lenght is cut up by the shortest parameter</returns>
        public short[] AddNoise(short[] speech, short[] noise) {

            // Calculate the shortest lenght
            int length = (speech.Length > noise.Length) ? noise.Length : speech.Length;

            // Summing up audio waves

            short[] speechWithNoise = new short[length];

            // Randomize noise
            Random rand = new Random();
            int noise_index = rand.Next(noise.Length - length + 1);

            for (int i = 0; i < length; i++, noise_index++)
            {
                double randFactor = (rand.NextDouble() + 0.5) * factor;
                int raw_short = (speech[i]) + (short)(noise[noise_index] * factor);
                
                
                // check if short is too high or too low
                if (raw_short > short.MaxValue) raw_short = short.MaxValue;
                if (raw_short < short.MinValue) raw_short = short.MinValue;

                // new audio sample
                speechWithNoise[i] = (short)raw_short;
            }
            
            return speechWithNoise;

        }
    }

}
