using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Circuits
{
    /// <summary>
    /// A lamp with one input pin and no outputs that glows when its input is high.
    /// </summary>
    public class OutputLamp : Gate
    {
        private bool lampOn = false;

        public OutputLamp(int x, int y) : base(x, y)
        {
            pins.Clear();

            // One input pin; Pin(owner, isInput, offset)
            pins.Add(new Pin(this, true, 20));

            MoveTo(x, y);
        }

        public override void MoveTo(int x, int y)
        {
            left = x;
            top = y;

            if (pins.Count >= 1)
            {
                // Input on left middle
                pins[0].X = x - GAP;
                pins[0].Y = y + HEIGHT / 2;
            }
        }

        // Read upstream signal through the wire
        private bool SampleInputHigh()
        {
            if (pins.Count == 0) return false;

            var inPin = pins[0];
            var wire = inPin.InputWire;
            if (wire == null) return false;

            // FromPin is the upstream output
            var fromPin = wire.FromPin;

            // Replace 'Owner' with whatever your Pin exposes (Owner/Parent/OwnerGate)
            var sourceGate = fromPin.Owner as Gate;
            if (sourceGate == null) return false;

            // Find the index of the upstream output pin
            int outIndex = sourceGate.Pins.IndexOf(fromPin);
            if (outIndex < 0) return false;

            return sourceGate.GetOutput(outIndex);
        }

        public override void Draw(Graphics g)
        {
            // Update lamp state
            lampOn = SampleInputHigh();

            var body = new Rectangle(left, top, WIDTH, HEIGHT);
            g.DrawRectangle(Pens.Black, body);

            // Bulb area
            int pad = 8;
            var bulbRect = new Rectangle(left + pad, top + pad, WIDTH - 2 * pad, HEIGHT - 2 * pad);

            // Smooth circles
            var prev = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (lampOn)
            {
                // Green glow with halos to kinda look like light
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

            // Draw pins last
            base.Draw(g);
        }

        private static Rectangle Inflate(Rectangle r, int d)
        {
            return new Rectangle(r.X - d, r.Y - d, r.Width + 2 * d, r.Height + 2 * d);
        }
    }
}
