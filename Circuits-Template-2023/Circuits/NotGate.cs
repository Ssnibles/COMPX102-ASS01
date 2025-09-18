using System;
using System.Drawing;

namespace Circuits
{
    // NOT gate (inverter), one input pin, one output pin
    public class NotGate : Gate
    {
        // The default (unselected) and selected images
        private static Bitmap notGateNormal;
        private static Bitmap notGateRed;

        // Get image resources if not already loaded, do nothing if it fails
        public NotGate(int x, int y) : base(x, y)
        {
            try { if (notGateNormal == null) { notGateNormal = Properties.Resources.NotGate; notGateRed = Properties.Resources.NotGateRed; } }
            catch { notGateNormal = notGateRed = null; }

            // Input and output pins, middle height
            pins.Add(new Pin(this, true, HEIGHT / 2));   // Input left
            pins.Add(new Pin(this, false, HEIGHT / 2));  // Output right

            // Move the pins to their correct spots based on left/top
            MoveTo();
        }

        protected override void MoveTo()
        {
            if (pins.Count >= 2) //Place the input and output pins
            {
                pins[0].X = left - GAP; pins[0].Y = top + HEIGHT / 2;     // Input left
                pins[1].X = left + WIDTH + GAP; pins[1].Y = top + HEIGHT / 2; // Output right
            }
        }

        // Return the inverse of the input value
        public override bool Evaluate()
        {
            bool a = EvalInputOrFalse(0, "NOT", "In");
            return !a;
        }

        // Draw the gate body, either default or red if selected
        protected override void DrawBody(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);
            var img = selected ? notGateRed : notGateNormal;
            if (img != null) g.DrawImage(img, rect);
        }

        // Make a fresh copy at same position, but not selected
        public override Gate Clone()
        {
            var copy = new NotGate(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
