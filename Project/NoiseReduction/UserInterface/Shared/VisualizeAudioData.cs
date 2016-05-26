using System.Windows;
using System.Windows.Shapes;

namespace UserInterface
{
    /// <summary>
    /// Utility class to visualize audio data on polygon
    /// </summary>
    class VisualizeAudioData
    {
        // polygon control fields from outer space
        private Polygon waveForm;
        private double xScale = 1;
        private uint renderPosition = 0;
        private uint blankZone = 20;

        // properties to set some settings
        public double XScale { set { this.xScale = value; } }
        public uint RenderPosition { set { this.renderPosition = value; } }
        public uint BlankZone { set { this.blankZone = value; } }

        /// <summary>
        /// New instance of audio data visualizer
        /// </summary>
        /// <param name="polygonControl"> Control on which to draw </param>
        public VisualizeAudioData(Polygon polygonControl)
        {
            waveForm = polygonControl;
        }

        /// <summary>
        /// Method to visulize audio data 
        /// </summary>
        /// <param name="minValue"> Min audio sample </param>
        /// <param name="maxValue"> Max audio sample </param>
        /// <remarks> Called every 'N' samples </remarks>
        public void VisualiseSamples(float minValue, float maxValue, bool checkEndOfThePolygon = true)
        {
            int visiblePixels = (int)(waveForm.ActualWidth / xScale);

            if (visiblePixels > 0)
            {
                // Draw new point
                CreatePoint(maxValue, minValue);

                if (checkEndOfThePolygon)
                {

                    // Check if we reached the right bound
                    if (renderPosition > visiblePixels)
                    {
                        renderPosition = 0;
                    }

                    // If polygon has little space left - erasing data from the left corner
                    int erasePosition = (int)((renderPosition + blankZone) % visiblePixels);
                    if (erasePosition < waveForm.Points.Count / 2)
                    {
                        double yPos = SampleToYPosition(0);
                        waveForm.Points[erasePosition] = new Point(erasePosition * xScale, yPos);
                        waveForm.Points[GetBottomIndex(erasePosition)] = new Point(erasePosition * xScale, yPos);
                    }
                }
            }
        }

        /// <summary>
        /// Method to draw point on polygon based on raw float-point 32 bit data
        /// </summary>
        private void CreatePoint(float topValue, float bottomValue)
        {
            // Calculate coordinates
            double topYPos = SampleToYPosition(topValue );
            double bottomYPos = SampleToYPosition(bottomValue);
            double xPos = renderPosition * xScale;
            

            // Check if we have any space left on the polygon
            if (renderPosition >= waveForm.Points.Count / 2)
            {
                int insertPos = waveForm.Points.Count / 2;
                waveForm.Points.Insert(insertPos, new Point(xPos, topYPos));
                waveForm.Points.Insert(insertPos + 1, new Point(xPos, bottomYPos));
            }
            // if not, change values, starting from the left
            else
            {
                waveForm.Points[(int)renderPosition] = new Point(xPos, topYPos);
                waveForm.Points[GetBottomIndex((int)renderPosition)] = new Point(xPos, bottomYPos);
            }
            renderPosition++;
        }

        /// <summary>
        /// Method to draw points on polygon from raw data
        /// </summary>
        /// <param name="n">One 2 in every 'n' samples will be visualized</param>
        public void VisualizeFile(short[] audioFile, int n)
        {
            // visualise samples every n samples
            int samplesCount = 0;
            float min_sample = float.MaxValue;
            float max_sample = float.MinValue;

            // coding samples into float-point 32 bit 
            for (int index = 0; index < audioFile.Length - 1; index += 1, samplesCount++)
            {
                short sample = audioFile[index];
                float sample32 = sample / 32768f;

                // check if new sample is min or max for current sample's group
                if (sample32 < min_sample) min_sample = sample32;
                if (sample32 > max_sample) max_sample = sample32;

                // check if current sapmle's group has more than 1600 members (800 pairs), if so, visualize it
                if (samplesCount >=  n )
                {
                    VisualiseSamples(min_sample, max_sample, false);

                    // Set new sample's group properties
                    samplesCount = 0;
                    min_sample = float.MaxValue;
                    max_sample = float.MinValue;
                }
            }
        }


        /// <summary>
        /// Method to get bottom Point index(minimum sample)
        /// </summary> 
        private int GetBottomIndex(int renderPosition)
        {
            return waveForm.Points.Count - renderPosition - 1;
        }

        /// <summary>
        /// Translate samples into 'y' coordinates (samples are maxed to 1.0f)
        /// </summary> 
        private double SampleToYPosition(float topValue)
        {
            return topValue * (waveForm.ActualHeight / 2) + waveForm.ActualHeight / 2;
        }
    }
}
