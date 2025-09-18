using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Circuits
{
    // Lamp with one input; glows when on, dark bulb when off.
    public class OutputLamp : Gate
    {
        private bool lampOn;

        public OutputLamp(int x, int y) : base(x, y)
        {
            pins.Add(new Pin(this, true, 20));
            MoveTo();
        }

        protected override void MoveTo()
        {
            if (pins.Count == 1)
            {
                pins[0].X = left - GAP;
                pins[0].Y = top + HEIGHT / 2;
            }
        }

        public override bool Evaluate()
        {
            if (pins[0].InputWire == null)
            {
                MessageBox.Show("LAMP: Input is not connected; lamp will be off.", "Unconnected input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lampOn = false;
                return lampOn;
            }
            var srcPin = pins[0].InputWire.StartPin;
            var srcGate = srcPin.Owner as Gate;
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

        protected override void DrawBody(Graphics g)
        {
            var body = new Rectangle(left, top, WIDTH, HEIGHT);
            var framePen = selected ? Pens.Red : Pens.Black;

            g.DrawRectangle(framePen, body);

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
                    var r1 = new Rectangle(bulbRect.X - 6, bulbRect.Y - 6, bulbRect.Width + 12, bulbRect.Height + 12);
                    var r2 = new Rectangle(bulbRect.X - 3, bulbRect.Y - 3, bulbRect.Width + 6, bulbRect.Height + 6);
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

            g.DrawEllipse(framePen, bulbRect);
            g.SmoothingMode = prev;
        }

        public override Gate Clone()
        {
            var copy = new OutputLamp(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
