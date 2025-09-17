using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Circuits
{
    public class OutputLamp : Gate
    {
        private bool lampOn = false;

        public OutputLamp(int x, int y) : base(x, y)
        {
            pins.Clear();
            pins.Add(new Pin(this, true, 20));
            MoveTo(x, y);
        }

        public override void MoveTo(int x, int y)
        {
            left = x;
            top = y;

            if (pins.Count >= 1)
            {
                pins[0].X = x - GAP;
                pins[0].Y = y + HEIGHT / 2;
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

        public override void Draw(Graphics g)
        {
            var body = new Rectangle(left, top, WIDTH, HEIGHT);
            g.DrawRectangle(Pens.Black, body);

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
                {
                    g.FillEllipse(off, bulbRect);
                }
            }

            g.DrawEllipse(Pens.Black, bulbRect);
            g.SmoothingMode = prev;

            base.Draw(g);
        }

        // NEW: clone with same position and current on/off snapshot (no wires)
        public override Gate Clone()
        {
            var copy = new OutputLamp(left, top);
            // Copy visual state snapshot; no wires are cloned
            // Access to private field is allowed within the same class
            copy.lampOn = this.lampOn;
            copy.Selected = false;
            return copy;
        }

        private static Rectangle Inflate(Rectangle r, int d)
        {
            return new Rectangle(r.X - d, r.Y - d, r.Width + 2 * d, r.Height + 2 * d);
        }
    }
}
