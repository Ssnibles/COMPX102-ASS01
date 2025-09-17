using System;
using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// A gate with no inputs and one output that can be toggled between low/high.
    /// Draws a filled body and a centered "1"/"0" based on its internal state.
    /// </summary>
    public class InputSource : Gate
    {
        private bool isHigh = false; // default off/low

        public InputSource(int x, int y) : base(x, y)
        {
            pins.Clear();
            // One output pin; assuming Pin(owner, isInput, offset)
            pins.Add(new Pin(this, false, 20));
            MoveTo(x, y);
        }

        public void Toggle()
        {
            isHigh = !isHigh;
        }

        public override bool Evaluate() => isHigh;

        public override bool GetOutput(int index) => index == 0 && isHigh;

        public override void MoveTo(int x, int y)
        {
            left = x;
            top = y;

            if (pins.Count >= 1)
            {
                // Output pin on right middle
                pins[0].X = x + WIDTH + GAP;
                pins[0].Y = y + HEIGHT / 2;
            }
        }

        public override void Draw(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);

            // Fill background by state (avoid red)
            using (var fill = new SolidBrush(isHigh ? Color.LightGreen : Color.LightGray))
            {
                g.FillRectangle(fill, rect);
            }

            // Centered “1” or “0”
            using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(isHigh ? "1" : "0", font, Brushes.Black, rect, sf);
            }

            // Outline
            g.DrawRectangle(Pens.Black, rect);

            // Draw pins last
            base.Draw(g);
        }
    }
}
