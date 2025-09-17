using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Circuits
{
    /// <summary>
    /// A wire connects between two pins.
    /// That is, it connects the output pin FromPin 
    /// to the input pin ToPin.
    /// </summary>
    public class Wire
    {
        //Has the wire been selected
        protected bool selected = false;
        //The pins the wire is connected to
        protected Pin fromPin, toPin;

        public Wire(Pin from, Pin to)
        {
            fromPin = from;
            toPin = to;
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; }
        }

        public Pin FromPin { get { return fromPin; } }
        public Pin ToPin { get { return toPin; } }

        /// <summary>
        /// Hit-test the wire as a line segment with a tolerance (in pixels).
        /// Computes distance from point to segment and compares with tolerance.
        /// </summary>
        public bool HitTest(int x, int y, int tolerance = 5)
        {
            // Endpoints
            float x1 = fromPin.X, y1 = fromPin.Y;
            float x2 = toPin.X, y2 = toPin.Y;
            float px = x, py = y;

            // Fast check: bounding box grown by tolerance
            float minX = Math.Min(x1, x2) - tolerance;
            float maxX = Math.Max(x1, x2) + tolerance;
            float minY = Math.Min(y1, y2) - tolerance;
            float maxY = Math.Max(y1, y2) + tolerance;
            if (px < minX || px > maxX || py < minY || py > maxY)
                return false;

            // Compute projection of P onto segment and clamp to [0,1]
            float dx = x2 - x1;
            float dy = y2 - y1;
            float len2 = dx * dx + dy * dy;
            if (len2 <= float.Epsilon) // degenerate
            {
                float d2 = (px - x1) * (px - x1) + (py - y1) * (py - y1);
                return d2 <= tolerance * tolerance;
            }

            float t = ((px - x1) * dx + (py - y1) * dy) / len2;
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;

            float cx = x1 + t * dx;
            float cy = y1 + t * dy;

            float ddx = px - cx;
            float ddy = py - cy;
            float dist2 = ddx * ddx + ddy * ddy;

            return dist2 <= tolerance * tolerance; // within tolerance ⇒ hit [web:182]
        }

        public void Draw(Graphics paper)
        {
            Pen wire = new Pen(selected ? Color.Red : Color.White, 3);
            paper.DrawLine(wire, fromPin.X, fromPin.Y, toPin.X, toPin.Y);
        }
    }
}
