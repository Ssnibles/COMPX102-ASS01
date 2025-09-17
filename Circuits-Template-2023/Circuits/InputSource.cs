using System;
using System.Drawing;

namespace Circuits
{
    public class InputSource : Gate
    {
        private bool isHigh = false;

        public InputSource(int x, int y) : base(x, y)
        {
            pins.Clear();
            pins.Add(new Pin(this, false, 20));
            MoveTo(x, y);
        }

        public void Toggle() => isHigh = !isHigh;

        public override bool Evaluate() => isHigh;

        public override bool GetOutput(int index) => index == 0 && isHigh;

        public override void MoveTo(int x, int y)
        {
            left = x;
            top = y;

            if (pins.Count >= 1)
            {
                pins[0].X = x + WIDTH + GAP;
                pins[0].Y = y + HEIGHT / 2;
            }
        }

        public override void Draw(Graphics g)
        {
            var rect = new Rectangle(left, top, WIDTH, HEIGHT);

            using (var fill = new SolidBrush(isHigh ? Color.LightGreen : Color.LightGray))
                g.FillRectangle(fill, rect);

            using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(isHigh ? "1" : "0", font, Brushes.Black, rect, sf);

            // Selection indicator: red border when selected, else black
            var borderPen = selected ? Pens.Red : Pens.Black;
            g.DrawRectangle(borderPen, rect); // draw state-dependent outline [web:191][web:196]

            base.Draw(g);
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
