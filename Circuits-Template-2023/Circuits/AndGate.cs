using System;
using System.Drawing;

namespace Circuits
{
    // Two input AND gate with inputs left and output right
    public class AndGate : Gate
    {
        private static Bitmap normalImage;    // image for unselected state
        private static Bitmap selectedImage;  // image for selected state

        public AndGate(int x, int y) : base(x, y)
        {
            try
            {
                if (normalImage == null)
                {
                    normalImage = Properties.Resources.AndGate;        // load default image
                    selectedImage = Properties.Resources.AndGateRed;   // load selected image
                }
            }
            catch
            {
                normalImage = selectedImage = null;  // if load fails, images are null
            }

            // add two input pins and one output pin
            pins.Add(new Pin(this, true, 20));
            pins.Add(new Pin(this, true, 20));
            pins.Add(new Pin(this, false, 20));
            MoveTo();  // Position pins
        }

        // Position input and output pins on gate
        protected override void MoveTo()
        {
            if (pins.Count >= 3)
            {
                pins[0].X = left - GAP; pins[0].Y = top + 10;         // Input A
                pins[1].X = left - GAP; pins[1].Y = top + HEIGHT - 10; // Input B
                pins[2].X = left + WIDTH + GAP; pins[2].Y = top + HEIGHT / 2; // Output
            }
        }

        // Compute AND of two inputs
        public override bool Evaluate()
        {
            bool a = EvalInputOrFalse(0, "AND", "A");
            bool b = EvalInputOrFalse(1, "AND", "B");
            return a && b;
        }

        // Draw gate with selected or default image
        protected override void DrawBody(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);
            var img = selected ? selectedImage : normalImage;
            if (img != null) g.DrawImage(img, rect);
        }

        // Create a copy of this gate at same position, not selected
        public override Gate Clone()
        {
            var copy = new AndGate(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
