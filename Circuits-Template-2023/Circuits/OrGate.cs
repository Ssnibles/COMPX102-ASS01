using System;
using System.Drawing;

namespace Circuits
{
    // Two-input OR gate, inputs left, output right
    public class OrGate : Gate
    {
        private static Bitmap normalImage;    // default gate image
        private static Bitmap selectedImage;  // image when selected

        // Constructor: initialise position and load images
        public OrGate(int x, int y) : base(x, y)
        {
            try
            {
                if (normalImage == null)
                {
                    normalImage = Properties.Resources.OrGate;      // load default image
                    selectedImage = Properties.Resources.OrGateRed; // load selected image
                }
            }
            catch { normalImage = selectedImage = null; }  // set null if load fails

            pins.Add(new Pin(this, true, 20));   // input A
            pins.Add(new Pin(this, true, 20));   // input B
            pins.Add(new Pin(this, false, 20));  // output

            MoveTo();  // position pins
        }

        // Position pins relative to gate
        protected override void MoveTo()
        {
            if (pins.Count >= 3)
            {
                pins[0].X = left - GAP; pins[0].Y = top + 10;           // input 1
                pins[1].X = left - GAP; pins[1].Y = top + HEIGHT - 10;  // input 2
                pins[2].X = left + WIDTH + GAP; pins[2].Y = top + HEIGHT / 2; // output
            }
        }

        // Evaluate OR logic of inputs A and B
        public override bool Evaluate()
        {
            bool a = EvalInputOrFalse(0, "OR", "A");
            bool b = EvalInputOrFalse(1, "OR", "B");
            return a || b;
        }

        // Draw gate body with selected or default image
        protected override void DrawBody(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);
            var img = selected ? selectedImage : normalImage;
            if (img != null) g.DrawImage(img, rect);
        }

        // Create a copy of this gate at same position, unselected
        public override Gate Clone()
        {
            var copy = new OrGate(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
