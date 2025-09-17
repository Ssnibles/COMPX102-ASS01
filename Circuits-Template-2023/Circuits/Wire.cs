using System;
using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// Simple line segment connecting an output pin (FromPin) to an input pin (ToPin).
    /// Provides a small tolerance-based hit test so it can be selected and deleted.
    /// </summary>
    public class Wire
    {
        protected bool selected;
        protected Pin fromPin, toPin;

        public Wire(Pin from, Pin to) { fromPin = from; toPin = to; }

        public bool Selected { get => selected; set => selected = value; }
        public Pin FromPin => fromPin;
        public Pin ToPin => toPin;

        /// <summary>
        /// Distance-to-segment hit-test with a square tolerance.
        /// Works well for simple editor interactions.
        /// </summary>
        public bool HitTest(int x, int y, int tolerance = 5)
        {
            float x1 = fromPin.X, y1 = fromPin.Y;
            float x2 = toPin.X, y2 = toPin.Y;
            float px = x, py = y;

            // Quick reject by an expanded bounding box.
            float minX = Math.Min(x1, x2) - tolerance;
            float maxX = Math.Max(x1, x2) + tolerance;
            float minY = Math.Min(y1, y2) - tolerance;
            float maxY = Math.Max(y1, y2) + tolerance;
            if (px < minX || px > maxX || py < minY || py > maxY) return false;

            // Project point onto the segment and clamp to
            float dx = x2 - x1, dy = y2 - y1, len2 = dx * dx + dy * dy;
            if (len2 <= float.Epsilon)
            {
                float d2 = (px - x1) * (px - x1) + (py - y1) * (py - y1);
                return d2 <= tolerance * tolerance;
            }
            float t = ((px - x1) * dx + (py - y1) * dy) / len2;
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;

            // Closest point and squared distance.
            float cx = x1 + t * dx, cy = y1 + t * dy;
            float ddx = px - cx, ddy = py - cy;
            return (ddx * ddx + ddy * ddy) <= tolerance * tolerance;
        }

        /// <summary>
        /// Draw the wire as a 3px line; red when selected.
        /// </summary>
        public void Draw(Graphics g)
        {
            var pen = new Pen(selected ? Color.Red : Color.White, 3);
            g.DrawLine(pen, fromPin.X, fromPin.Y, toPin.X, toPin.Y);
        }
    }
}
