using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuits
{
    /// <summary>
    /// Base class for all gates on the canvas
    /// Holds a position (left and top) and a fixed body size used by image gates
    /// Owns a concrete list of Pin objects (inputs and outputs)
    /// Encapsulates a small drawing template
    /// draw pins first (so they sit under images) then draw the gate body
    /// Exposes Evaluate/Clone so logic and copy behaviour live with the gate
    /// </summary>
    public abstract class Gate
    {
        // Body origin (top-left) in client coordinates.
        protected int left;
        protected int top;

        // Standard body dimensions for image-based gates.
        protected const int WIDTH = 40;
        protected const int HEIGHT = 40;

        // Horizontal gap used when positioning pins either side of the body.
        protected const int GAP = 10;

        // Pins that belong to this gate.
        protected readonly List<Pin> pins = new List<Pin>();

        // Selection affects outlines and interaction behaviour
        protected bool selected;

        protected Gate(int x, int y)
        {
            left = x;
            top = y;
        }

        // Flag used by the form to render selection rings
        public virtual bool Selected
        {
            get => selected;
            set => selected = value;
        }

        // Convenience accessors for layout and hit test
        public int Left => left;
        public int Top => top;

        // External code uses this to draw hovers on pins etc
        public List<Pin> Pins => pins;

        /// <summary>
        /// Simple hit-test against the body rectangle.
        /// Compound overrides to use the union of its children
        /// </summary>
        public virtual bool IsMouseOn(int x, int y)
        {
            return (left <= x && x < left + WIDTH) && (top <= y && y < top + HEIGHT);
        }

        /// <summary>
        /// Template: draw pins FIRST (under the body), then draw the body
        /// This intentionally fixes the odd appearance of pins over images
        /// </summary>
        public virtual void Draw(Graphics g)
        {
            foreach (var p in pins) p.Draw(g);   // pins go underneath

            DrawBody(g);                         // then render images/shapes over the pins
        }

        /// <summary>
        /// Subclasses render the visual body (image or shapes and outlines)
        /// </summary>
        protected abstract void DrawBody(Graphics g);

        /// <summary>
        /// Move the body origin and request the subclass re-layout its pins
        /// Virtual so Compound can move all children in one go
        /// </summary>
        public virtual void MoveTo(int x, int y)
        {
            left = x;
            top = y;
            LayoutPins();
        }

        /// <summary>
        /// Subclasses place their pins relative to (left, top)
        /// </summary>
        protected abstract void LayoutPins();

        /// <summary>
        /// Compute the gate's boolean result
        /// Lamps recurse into upstream gates; input sources return internal state
        /// </summary>
        public abstract bool Evaluate();

        /// <summary>
        /// Optional legacy-style output accessor used by some code paths
        /// </summary>
        public virtual bool GetOutput(int index) => false;

        /// <summary>
        /// Prototype-style cloning used by Copy and Compound cloning
        /// Returns a new, unselected instance with fresh pins.
        /// </summary>
        public abstract Gate Clone();

        /// <summary>
        /// Evaluate an input pin (by index) safely.
        /// - Warn if the input is not wired.
        /// - Assume false when missing or invalid.
        /// </summary>
        protected bool EvalInputOrFalse(int pinIndex, string gateName, string pinLabel)
        {
            if (pinIndex < 0 || pinIndex >= pins.Count)
            {
                MessageBox.Show($"{gateName}: Invalid pin index {pinIndex}.", "Evaluate error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var inPin = pins[pinIndex];
            if (inPin.InputWire == null)
            {
                MessageBox.Show($"{gateName}: Input {pinLabel} is not connected; assuming false.", "Unconnected input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var upstreamPin = inPin.InputWire.FromPin;
            var upstreamGate = upstreamPin.Owner as Gate;
            if (upstreamGate == null)
            {
                MessageBox.Show($"{gateName}: Upstream gate not found; assuming false.", "Evaluate error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return upstreamGate.Evaluate();
        }
    }
}
