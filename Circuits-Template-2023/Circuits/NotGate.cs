using System;
using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// Single-input NOT (inverter) with image-based body (normal and red variants).
    /// </summary>
    public class NotGate : Gate
    {
        private static Bitmap normalImage;
        private static Bitmap selectedImage;

        public NotGate(int x, int y) : base(x, y)
        {
            try
            {
                if (normalImage == null)
                {
                    normalImage = Properties.Resources.NotGate;
                    selectedImage = Properties.Resources.NotGateRed;
                }
            }
            catch { normalImage = selectedImage = null; }

            pins.Add(new Pin(this, true, HEIGHT / 2));
            pins.Add(new Pin(this, false, HEIGHT / 2));
            LayoutPins();
        }

        protected override void LayoutPins()
        {
            if (pins.Count >= 2)
            {
                pins[0].X = left - GAP; pins[0].Y = top + HEIGHT / 2;
                pins[1].X = left + WIDTH + GAP; pins[1].Y = top + HEIGHT / 2;
            }
        }

        public override bool Evaluate()
        {
            bool a = EvalInputOrFalse(0, "NOT", "In");
            return !a;
        }

        public override bool GetOutput(int index) => index == 1 && Evaluate();

        protected override void DrawBody(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);
            var img = selected ? selectedImage : normalImage;
            if (img != null) g.DrawImage(img, rect); // image over pins
        }

        public override Gate Clone()
        {
            var copy = new NotGate(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
