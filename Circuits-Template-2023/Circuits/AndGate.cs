using System;
using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// Two-input AND with image-based body (normal and red variants).
    /// Pins are positioned left/left/right and drawn first, then the image is drawn on top.
    /// </summary>
    public class AndGate : Gate
    {
        private static Bitmap normalImage;
        private static Bitmap selectedImage;

        public AndGate(int x, int y) : base(x, y)
        {
            // Cache resource lookups so multiple gates share the same bitmaps.
            try
            {
                if (normalImage == null)
                {
                    normalImage = Properties.Resources.AndGate;
                    selectedImage = Properties.Resources.AndGateRed;
                }
            }
            catch { normalImage = selectedImage = null; }

            // Inputs A/B on the left; one output on the right middle.
            pins.Add(new Pin(this, true, 20));
            pins.Add(new Pin(this, true, HEIGHT - 20));
            pins.Add(new Pin(this, false, HEIGHT / 2));
            LayoutPins();
        }

        protected override void LayoutPins()
        {
            if (pins.Count >= 3)
            {
                pins[0].X = left - GAP; pins[0].Y = top + 10;
                pins[1].X = left - GAP; pins[1].Y = top + HEIGHT - 10;
                pins[2].X = left + WIDTH + GAP; pins[2].Y = top + HEIGHT / 2;
            }
        }

        public override bool Evaluate()
        {
            bool a = EvalInputOrFalse(0, "AND", "A");
            bool b = EvalInputOrFalse(1, "AND", "B");
            return a && b;
        }

        public override bool GetOutput(int index) => index == 2 && Evaluate();

        /// <summary>
        /// Draw the currently selected or normal image.
        /// Pins are already drawn by the template before this call.
        /// </summary>
        protected override void DrawBody(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);
            var img = selected ? selectedImage : normalImage;
            if (img != null) g.DrawImage(img, rect); // image over pins
        }

        public override Gate Clone()
        {
            var copy = new AndGate(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
