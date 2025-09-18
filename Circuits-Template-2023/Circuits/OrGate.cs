using System;
using System.Drawing;

namespace Circuits
{
    // Two‑input OR gate with a small image body; inputs left, output right.
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
            MoveTo();
        }

        protected override void MoveTo()
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

        protected override void DrawBody(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);
            var img = selected ? selectedImage : normalImage;
            if (img != null) g.DrawImage(img, rect);
        }

        public override Gate Clone()
        {
            var copy = new OrGate(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
