using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// Implements an AND gate with two inputs and one output.
    /// Uses PNG images from resources for normal and selected states.
    /// </summary>
    public class AndGate : Gate
    {
        private static Bitmap normalImage;  // Normal state image
        private static Bitmap selectedImage; // Selected state image

        /// <summary>
        /// Constructor creates the specific pins and positions them.
        /// </summary>
        public AndGate(int x, int y) : base(x, y)
        {
            // Load images once if not already loaded
            if (normalImage == null)
            {
                normalImage = Properties.Resources.AndGate;     // AndGate.png
                selectedImage = Properties.Resources.AndGateRed; // AndGateRed.png
            }

            // Create two input pins and one output pin
            pins.Add(new Pin(this, true, 20));
            pins.Add(new Pin(this, true, 20));
            pins.Add(new Pin(this, false, 20));

            MoveTo(x, y);
        }

        /// <summary>
        /// Draw the AND gate using the appropriate PNG bitmap from resources.
        /// </summary>
        public override void Draw(Graphics paper)
        {
            // Draw pins first (this also sets the selected state)
            base.Draw(paper);

            // Choose the appropriate image based on selection state
            Bitmap imageToUse = selected ? selectedImage : normalImage;

            if (imageToUse != null)
            {
                // Draw the image scaled to the gate size
                Rectangle destRect = new Rectangle(left, top, WIDTH, HEIGHT);
                paper.DrawImage(imageToUse, destRect);
            }
        }
    }
}
