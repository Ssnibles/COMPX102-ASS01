using System.Drawing;

namespace Circuits
{
    public class NotGate : Gate
    {
        private static Bitmap normalImage;
        private static Bitmap selectedImage;

        public NotGate(int x, int y) : base(x, y)
        {
            // Load images once if not already loaded
            if (normalImage == null)
            {
                normalImage = Properties.Resources.NotGate;     // AndGate.png
                selectedImage = Properties.Resources.NotGateRed; // AndGateRed.png
            }

            pins.Add(new Pin(this, true, 20));   // Input pin
            pins.Add(new Pin(this, false, 20));  // Output pin
            MoveTo(x, y);
        }

        // Override MoveTo to handle 2 pins instead of 3
        public override void MoveTo(int x, int y)
        {
            left = x;
            top = y;

            if (pins.Count >= 2)
            {
                // Input pin on left center
                pins[0].X = x - GAP;
                pins[0].Y = y + HEIGHT / 2;

                // Output pin on right center (after the circle)
                pins[1].X = x + WIDTH + GAP;
                pins[1].Y = y + HEIGHT / 2;
            }
        }

        public override void Draw(Graphics paper)
        {
            base.Draw(paper);

            // Choose the appropriate image based on selection state
            Bitmap imageToUse = selected ? selectedImage : normalImage;

            if (imageToUse != null)
            {
                // Draw the image scaled to the gate size
                Rectangle rect = new Rectangle(left, top, WIDTH, HEIGHT);
                paper.DrawImage(imageToUse, rect);
            }
        }
    }
}
