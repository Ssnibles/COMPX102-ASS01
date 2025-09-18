using System;
using System.Drawing;

namespace Circuits
{
    // Simple on/off source with one output pin; short click toggles.
    public class InputSource : Gate
    {
        private bool isHigh;        // true if output is HIGH (on)

        // Creates a new input source at (x, y)
        public InputSource(int x, int y) : base(x, y)
        {
            pins.Add(new Pin(this, false, 20));   // single output pin
            MoveTo();                            // position pin
        }

        // Toggle the output state
        public void Toggle() => isHigh = !isHigh;

        // Current output value (always the stored state)
        public override bool Evaluate() => isHigh;

        // Positions the output pin at the right side, middle height
        protected override void MoveTo()
        {
            if (pins.Count == 1)
            {
                pins[0].X = left + WIDTH + GAP;  // right of gate
                pins[0].Y = top + HEIGHT / 2;    // middle height
            }
        }

        // Draws the input source: green if on, gray if off, with 1/0 label
        protected override void DrawBody(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);
            using (var fill = new SolidBrush(isHigh ? Color.LightGreen : Color.LightGray))
                g.FillRectangle(fill, rect);
            using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(isHigh ? "1" : "0", font, Brushes.Black, rect, sf);
            var pen = selected ? Pens.Red : Pens.Black;
            g.DrawRectangle(pen, rect);
        }

        // Creates a copy at same position, optionally toggled
        public override Gate Clone()
        {
            var copy = new InputSource(left, top);
            if (isHigh) copy.Toggle();
            copy.Selected = false;
            return copy;
        }
    }
}
