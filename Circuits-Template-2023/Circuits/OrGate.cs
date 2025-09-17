using System;
using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// Two-input OR with image-based body (normal and red variants).
    /// Draw order is pins first, then the image to keep pins beneath.
    /// </summary>
    public class OrGate : Gate
    {
        private static Bitmap normalImage;
        private static Bitmap selectedImage;

        public OrGate(int x, int y) : base(x, y)
        {
            try
            {
                if (normalImage == null)
                {
                    normalImage = Properties.Resources.OrGate;
                    selectedImage = Properties.Resources.OrGateRed;
                }
            }
            catch { normalImage = selectedImage = null; }

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
            bool a = EvalInputOrFalse(0, "OR", "A");
            bool b = EvalInputOrFalse(1, "OR", "B");
            return a || b;
        }

        public override bool GetOutput(int index) => index == 2 && Evaluate();

        protected override void DrawBody(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);
            var img = selected ? selectedImage : normalImage;
            if (img != null) g.DrawImage(img, rect); // image over pins
        }

        public override Gate Clone()
        {
            var copy = new OrGate(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
