using System;
using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// Toggleable on/off input with one output pin.
    /// Draws a simple filled body, an optional icon overlay, and a centred “1/0”.
    /// Selection is indicated by a red border.
    /// </summary>
    public class InputSource : Gate
    {
        private bool isHigh;         // current logical state

        public InputSource(int x, int y) : base(x, y)
        {
            // One output pin on the right-hand side.
            pins.Add(new Pin(this, false, 20));
            LayoutPins();
        }

        /// <summary>
        /// Flip the internal state (used by click without drag).
        /// </summary>
        public void Toggle() => isHigh = !isHigh;

        public override bool Evaluate() => isHigh;

        public override bool GetOutput(int index) => index == 0 && isHigh;

        /// <summary>
        /// Place the single output pin to the right middle.
        /// </summary>
        protected override void LayoutPins()
        {
            if (pins.Count == 1)
            {
                pins[0].X = left + WIDTH + GAP;
                pins[0].Y = top + HEIGHT / 2;
            }
        }

        /// <summary>
        /// Fill the body based on state, overlay icon/text, then draw an outline.
        /// </summary>
        protected override void DrawBody(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);

            using (var fill = new SolidBrush(isHigh ? Color.LightGreen : Color.LightGray))
                g.FillRectangle(fill, rect);

            using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(isHigh ? "1" : "0", font, Brushes.Black, rect, sf);

            var pen = selected ? Pens.Red : Pens.Black;
            g.DrawRectangle(pen, rect); // selection ring
        }

        public override Gate Clone()
        {
            var copy = new InputSource(left, top);
            if (isHigh) copy.Toggle();
            copy.Selected = false;
            return copy;
        }
    }
}
