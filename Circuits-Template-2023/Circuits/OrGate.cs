using System.Drawing;

namespace Circuits
{
    public class OrGate : Gate
    {
        private static Bitmap normalImage;   // Normal state image
        private static Bitmap selectedImage; // Selected state image

        public OrGate(int x, int y) : base(x, y)
        {
            if (normalImage == null)
            {
                normalImage = Properties.Resources.OrGate;      // OrGate.png
                selectedImage = Properties.Resources.OrGateRed; // OrGateRed.png
            }

            pins.Add(new Pin(this, true, 20));
            pins.Add(new Pin(this, true, 20));
            pins.Add(new Pin(this, false, 20));
            MoveTo(x, y);
        }

        public override void Draw(Graphics paper)
        {
            // Draw pins and selection adorners first
            base.Draw(paper);

            // Choose appropriate bitmap based on selection state
            Bitmap imageToUse = selected ? selectedImage : normalImage;

            if (imageToUse != null)
            {
                Rectangle destRect = new Rectangle(left, top, WIDTH, HEIGHT);
                paper.DrawImage(imageToUse, destRect);
            }
        }
    }
}
