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
            this.waveForm = polygonControl;
        }

        /// <summary>
        /// Method to visulize audio data 
        /// </summary>
        /// <param name="minValue"> Min audio sample </param>
        /// <param name="maxValue"> Max audio sample </param>
        /// <remarks> Called every 'N' samples </remarks>
        public void VisualiseSamples(float minValue, float maxValue)
        {
            int visiblePixels = (int)(waveForm.Width / xScale);
            if (visiblePixels > 0)
            {
                // Draw new point
                CreatePoint(maxValue, minValue);

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

        /// <summary>
        /// Method to draw point on polygon based on raw float-point 32 bit data
        /// </summary>
        private void CreatePoint(float topValue, float bottomValue)
        {
            // Calculate coordinates
            double topYPos = SampleToYPosition(topValue);
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

        // Method to get bottom Point index(minimum sample)
        private int GetBottomIndex(int renderPosition)
        {
            return waveForm.Points.Count - renderPosition - 1;
        }

        // Translate samples into 'y' coordinates (samples are maxed to 1.0f)
        private double SampleToYPosition(float topValue)
        {
            return topValue * (waveForm.Height / 2) + waveForm.Height / 2;
        }
    }
}
