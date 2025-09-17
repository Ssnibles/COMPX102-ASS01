using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Circuits
{
    /// <summary>
    /// Indicator lamp with one input.
    /// Shows a soft glow when on, and a dark bulb when off.
    /// Selection is indicated by a red frame and bulb ring.
    /// </summary>
    public class OutputLamp : Gate
    {
        private bool lampOn;

        public OutputLamp(int x, int y) : base(x, y)
        {
            // One input pin on the left-hand side.
            pins.Add(new Pin(this, true, 20));
            LayoutPins();
        }

        /// <summary>
        /// Place the single input pin to the left middle.
        /// </summary>
        protected override void LayoutPins()
        {
            if (pins.Count == 1)
            {
                pins[0].X = left - GAP;
                pins[0].Y = top + HEIGHT / 2;
            }
        }

        /// <summary>
        /// Evaluate upstream and cache a snapshot for drawing.
        /// </summary>
        public override bool Evaluate()
        {
            if (pins[0].InputWire == null)
            {
                MessageBox.Show("LAMP: Input is not connected; lamp will be off.", "Unconnected input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lampOn = false;
                return lampOn;
            }

            var fromPin = pins[0].InputWire.FromPin;
            var srcGate = fromPin.Owner as Gate;
            if (srcGate == null)
            {
                MessageBox.Show("LAMP: Upstream gate not found; lamp will be off.", "Evaluate error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lampOn = false;
                return lampOn;
            }

            lampOn = srcGate.Evaluate();
            return lampOn;
        }

        /// <summary>
        /// Draw a framed body and a glowing or dark bulb with anti aliased edges.
        /// </summary>
        protected override void DrawBody(Graphics g)
        {
            var body = new Rectangle(left, top, WIDTH, HEIGHT);
            var framePen = selected ? Pens.Red : Pens.Black;

            // Outer frame.
            g.DrawRectangle(framePen, body); // selection-aware frame

            // Inner bulb (with gentle glow when on).
            int pad = 8;
            var bulbRect = new Rectangle(left + pad, top + pad, WIDTH - 2 * pad, HEIGHT - 2 * pad);

            var prev = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (lampOn)
            {
                using (var haloOuter = new SolidBrush(Color.FromArgb(32, 144, 238, 144)))
                using (var haloMid = new SolidBrush(Color.FromArgb(64, 144, 238, 144)))
                using (var haloInner = new SolidBrush(Color.FromArgb(96, 144, 238, 144)))
                using (var bulbFill = new SolidBrush(Color.FromArgb(200, 144, 238, 144)))
                {
                    var r1 = Inflate(bulbRect, 6);
                    var r2 = Inflate(bulbRect, 3);
                    g.FillEllipse(haloOuter, r1);
                    g.FillEllipse(haloMid, r2);
                    g.FillEllipse(haloInner, bulbRect);
                    g.FillEllipse(bulbFill, new Rectangle(bulbRect.X + 4, bulbRect.Y + 4, bulbRect.Width - 8, bulbRect.Height - 8));
                }
            }
            else
            {
                using (var off = new SolidBrush(Color.FromArgb(40, 40, 40)))
                    g.FillEllipse(off, bulbRect);
            }

            // Bulb ring (red when selected).
            g.DrawEllipse(framePen, bulbRect);

            g.SmoothingMode = prev;
        }

        private static Rectangle Inflate(Rectangle r, int d)
        {
            return new Rectangle(r.X - d, r.Y - d, r.Width + 2 * d, r.Height + 2 * d);
        }

        public override Gate Clone()
        {
            var copy = new OutputLamp(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
