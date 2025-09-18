using System;
using System.Drawing;

namespace Circuits
{
    // Straight wire from an output pin (start) to an input pin (end), no wire selection state
    public class Wire
    {
        protected Pin startPin, endPin;

        public Wire(Pin from, Pin to) { startPin = from; endPin = to; }

        public Pin StartPin => startPin;
        public Pin EndPin => endPin;

        // Draw a 3 pixel white line for the wire
        public void Draw(Graphics g)
        {
            using (var pen = new Pen(Color.White, 3))
            {
                g.DrawLine(pen, startPin.X, startPin.Y, endPin.X, endPin.Y);
            }
        }
    }
}
