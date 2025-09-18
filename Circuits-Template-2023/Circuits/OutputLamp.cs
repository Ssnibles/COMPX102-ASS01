using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Circuits
{
    public class OutputLamp : Gate
    {
        private bool lampOn; // true if lamp is lit

        public OutputLamp(int x, int y) : base(x, y)
        {
            pins.Add(new Pin(this, true, 20)); // add input pin
            MoveTo();
        }

        protected override void MoveTo()
        {
            if (pins.Count == 1)
            {
                pins[0].X = left - GAP;           // pin on left side
                pins[0].Y = top + HEIGHT / 2;     // pin at vertical center
            }
        }

        public override bool Evaluate()
        {
            if (pins[0].InputWire == null)
            {
                MessageBox.Show("Lamp input not connected; staying off.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lampOn = false;
                return lampOn;
            }
            var srcPin = pins[0].InputWire.StartPin;
            var srcGate = srcPin?.Owner as Gate;
            if (srcGate == null)
            {
                MessageBox.Show("Upstream gate missing; lamp off.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lampOn = false;
                return lampOn;
            }
            lampOn = srcGate.Evaluate();
            return lampOn;
        }

        protected override void DrawBody(Graphics g)
        {
            var body = new Rectangle(left, top, WIDTH, HEIGHT);
            var pen = selected ? Pens.Red : Pens.Black; // frame colour depends on selection
            g.DrawRectangle(pen, body);

            int pad = 8;
            var bulb = new Rectangle(body.X + pad, body.Y + pad, body.Width - 2 * pad, body.Height - 2 * pad); // bulb area

            var prev = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias; // smooth circles

            if (lampOn)
            {
                // glowing effect with layered circles
                using (var haloOuter = new SolidBrush(Color.FromArgb(32, 144, 238, 144)))
                using (var haloMid = new SolidBrush(Color.FromArgb(64, 144, 238, 144)))
                using (var haloInner = new SolidBrush(Color.FromArgb(96, 144, 238, 144)))
                using (var bulbFill = new SolidBrush(Color.FromArgb(200, 144, 238, 144)))
                {
                    g.FillEllipse(haloOuter, new Rectangle(bulb.X - 6, bulb.Y - 6, bulb.Width + 12, bulb.Height + 12));
                    g.FillEllipse(haloMid, new Rectangle(bulb.X - 3, bulb.Y - 3, bulb.Width + 6, bulb.Height + 6));
                    g.FillEllipse(haloInner, bulb);
                    g.FillEllipse(bulbFill, new Rectangle(bulb.X + 4, bulb.Y + 4, bulb.Width - 8, bulb.Height - 8));
                }
            }
            else
            {
                // bulb is off, just dark grey
                using (var offBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                    g.FillEllipse(offBrush, bulb);
            }

            g.DrawEllipse(pen, bulb); // bulb outline
            g.SmoothingMode = prev; // restore smoothing mode
        }

        public override Gate Clone()
        {
            var copy = new OutputLamp(left, top);
            copy.Selected = false;
            return copy;
        }
    }
}
