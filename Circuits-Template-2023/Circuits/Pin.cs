using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D; // for SmoothingMode
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Circuits
{
    /// <summary>
    /// Each Pin represents an input or an output of a gate.
    /// Every Pin knows which gate it belongs to.
    /// Input pins can be connected to at most one wire.
    /// Output pins may have lots of wires pointing to them.
    /// </summary>
    public class Pin
    {
        //The x and y position of the pin
        protected int x, y;
        //The input value coming into the pin
        protected bool input;
        //How long the pin is when drawn
        protected int length;
        //The gate the pin belongs to
        protected Gate owner;
        //The wire connected to the pin
        protected Wire connection;

        // Unified snap radius (pixels) used for both hit test and drawing
        public int SnapRadius { get; set; } = 12;

        /// <summary>
        /// Initialises the object to the values passed in.
        /// </summary>
        public Pin(Gate gate, bool input, int length)
        {
            this.owner = gate;
            this.input = input;
            this.length = length;
        }

        public bool IsInput => input;
        public bool IsOutput => !input;
        public Gate Owner => owner;

        /// <summary>
        /// For input pins, this gets or sets the wire that is coming
        /// into the pin. For output pins, sets are ignored and get returns null.
        /// </summary>
        public Wire InputWire
        {
            get { return connection; }
            set { if (input) connection = value; }
        }

        /// <summary>
        /// Get or set the X position of this pin.
        /// For input pins, this is at the left hand side of the pin.
        /// For output pins, this is at the right hand side.
        /// </summary>
        public int X { get => x; set => x = value; }

        /// <summary>
        /// Get or set the Y position of this pin.
        /// </summary>
        public int Y { get => y; set => y = value; }

        /// <summary>
        /// True if (mouseX, mouseY) is within SnapRadius pixels of the pin tip.
        /// </summary>
        public bool IsMouseOn(int mouseX, int mouseY)
        {
            int dx = mouseX - x;
            int dy = mouseY - y;
            int r = SnapRadius;
            return dx * dx + dy * dy <= r * r;
        }

        /// <summary>
        /// Draws the pin body.
        /// </summary>
        public void Draw(Graphics paper)
        {
            Brush brush = Brushes.DarkGray;
            if (input)
            {
                paper.FillRectangle(brush, x - 1, y - 1, length, 3);
            }
            else
            {
                paper.FillRectangle(brush, x - length + 1, y - 1, length, 3);
            }
        }

        /// <summary>
        /// If the mouse is within SnapRadius, draws a solid circle (SnapRadius) around the tip.
        /// Returns true if hovering within the snap area.
        /// </summary>
        public bool DrawSnapHover(Graphics g, int mouseX, int mouseY)
        {
            int dx = mouseX - x;
            int dy = mouseY - y;
            int r = SnapRadius;
            bool hovering = (dx * dx + dy * dy) <= r * r;
            if (!hovering) return false;

            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var pen = new Pen(Color.DeepSkyBlue, 2.0f))
            {
                g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);
            }

            g.SmoothingMode = old;
            return true;
        }

        public override string ToString()
            => input ? $"InPin({x},{y})" : $"OutPin({x},{y})";
    }
}
