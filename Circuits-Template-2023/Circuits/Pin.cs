using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Circuits
{
    // Pin (input or output) on a logic gate
    public class Pin
    {
        // Pin tip position
        protected int x, y;
        // True if input, false if output
        protected bool input;
        // Length of visual stub
        protected int length;
        // Owning gate
        protected Gate owner;
        // Connected wire (only for inputs)
        protected Wire connection;
        // Snap/hover radius in pixels
        public int SnapRadius { get; set; } = 12;

        // Create new pin with owner and type
        public Pin(Gate gate, bool input, int length)
        {
            this.owner = gate;
            this.input = input;
            this.length = length;
        }

        public bool IsInput => input;
        public bool IsOutput => !input;
        public Gate Owner => owner;

        // Wire connected to this input (ignored for outputs)
        public Wire InputWire
        {
            get { return connection; }
            set { if (input) connection = value; }
        }

        // Pin tip X position
        public int X { get => x; set => x = value; }
        // Pin tip Y position
        public int Y { get => y; set => y = value; }

        // True if mouse is within snap radius of pin tip
        public bool IsMouseOn(int mouseX, int mouseY)
        {
            int dx = mouseX - x;
            int dy = mouseY - y;
            int r = SnapRadius;
            return dx * dx + dy * dy <= r * r;
        }

        // Draw pin as a short line
        public void Draw(Graphics g)
        {
            Brush brush = Brushes.DarkGray;
            if (input)
                g.FillRectangle(brush, x - 1, y - 1, length, 3);
            else
                g.FillRectangle(brush, x - length + 1, y - 1, length, 3);
        }

        // Draw blue hover ring if mouse is within snap radious
        public bool DrawHover(Graphics g, int mouseX, int mouseY)
        {
            int dx = mouseX - x;
            int dy = mouseY - y;
            int r = SnapRadius;
            bool hovering = dx * dx + dy * dy <= r * r;
            if (!hovering) return false;
            var old = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.DeepSkyBlue, 2.0f))
                g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);
            g.SmoothingMode = old;
            return true;
        }

        public override string ToString()
            => input ? $"InPin({x},{y})" : $"OutPin({x},{y})";
    }
}
