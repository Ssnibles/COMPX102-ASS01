using System;
using System.Drawing;

namespace Circuits
{
    public class AndGate : Gate
    {
        private static Bitmap normalImage;
        private static Bitmap selectedImage;

        public AndGate(int x, int y) : base(x, y)
        {
            try
            {
                if (normalImage == null)
                {
                    normalImage = Properties.Resources.AndGate;
                    selectedImage = Properties.Resources.AndGateRed;
                }
            }
            catch
            {
                normalImage = null;
                selectedImage = null;
            }

            pins.Clear();
            pins.Add(new Pin(this, true, 20));
            pins.Add(new Pin(this, true, HEIGHT - 20));
            pins.Add(new Pin(this, false, HEIGHT / 2));
            MoveTo(x, y);
        }

        public override void MoveTo(int x, int y)
        {
            left = x;
            top = y;

            if (pins.Count >= 3)
            {
                pins[0].X = x - GAP;
                pins[0].Y = y + 10;

                pins[1].X = x - GAP;
                pins[1].Y = y + HEIGHT - 10;

                pins[2].X = x + WIDTH + GAP;
                pins[2].Y = y + HEIGHT / 2;
            }
        }

        public override bool Evaluate()
        {
            bool a = EvalInputOrFalse(0, "AND", "A");
            bool b = EvalInputOrFalse(1, "AND", "B");
            return a && b;
        }

        public override bool GetOutput(int index) => index == 2 && Evaluate();

        public override void Draw(Graphics paper)
        {
            base.Draw(paper);

            Bitmap imageToUse = selected ? selectedImage : normalImage;
            if (imageToUse != null)
            {
                Rectangle destRect = new Rectangle(left, top, WIDTH, HEIGHT);
                paper.DrawImage(imageToUse, destRect);
            }
        }

        // NEW: clone with same position; pins are fresh via constructor
        public override Gate Clone()
        {
            var copy = new AndGate(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
