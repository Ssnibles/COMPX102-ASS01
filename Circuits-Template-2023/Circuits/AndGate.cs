using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// Implements an AND gate with two inputs and one output.
    /// Inherits common behavior and properties from Gate.
    /// </summary>
    public class AndGate : Gate
    {
        /// <summary>
        /// Constructor creates the specific pins and positions them.
        /// Calls base constructor to initialise position.
        /// </summary>
        public AndGate(int x, int y) : base(x, y)
        {
            // Create two input pins (true for input)
            pins.Add(new Pin(this, true, 20));
            pins.Add(new Pin(this, true, 20));

            // Create one output pin (false for output)
            pins.Add(new Pin(this, false, 20));

            // Position the gate and pins at the specified coordinates
            MoveTo(x, y);
        }

        /// <summary>
        /// Draw the AND gate body, using a combination of ellipse and rectangle,
        /// then draw the pins via base.Draw().
        /// </summary>
        public override void Draw(Graphics paper)
        {
            // Draw the pins first, and set brush color appropriately (done in base.Draw)
            base.Draw(paper);

            // Draw the AND gate body shapes using the brush set by base.Draw
            paper.FillEllipse(brush, left, top, WIDTH, HEIGHT);
            paper.FillRectangle(brush, left, top, WIDTH / 2, HEIGHT);
        }
    }
}
