using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuits
{
    /// <summary>
    /// Base class representing a generic gate in the circuit.
    /// Contains common properties like position, pins, drawing, and selection state.
    /// </summary>
    public abstract class Gate
    {
        // Position of the gate
        protected int left;
        protected int top;

        // Fixed size of gate body
        protected const int WIDTH = 40;
        protected const int HEIGHT = 40;

        // Distance pins extend outside the gate body
        protected const int GAP = 10;

        // List of pins connected to this gate
        protected List<Pin> pins = new List<Pin>();

        // Indicates whether the gate is currently selected
        protected bool selected = false;

        public Gate(int x, int y)
        {
            left = x;
            top = y;
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; }
        }

        public int Left => left;
        public int Top => top;
        public List<Pin> Pins => pins;

        public bool IsMouseOn(int x, int y)
        {
            return (left <= x && x < left + WIDTH) && (top <= y && y < top + HEIGHT);
        }

        public virtual void Draw(Graphics paper)
        {
            foreach (Pin pin in pins)
            {
                pin.Draw(paper);
            }
        }

        public virtual void MoveTo(int x, int y)
        {
            left = x;
            top = y;

            if (pins.Count >= 3)
            {
                pins[0].X = x - GAP;
                pins[0].Y = y + GAP;

                pins[1].X = x - GAP;
                pins[1].Y = y + HEIGHT - GAP;

                pins[2].X = x + WIDTH + GAP;
                pins[2].Y = y + HEIGHT / 2;
            }
        }

        // Subclasses must compute their logical result and return it.
        public abstract bool Evaluate();

        // Optional legacy: gates without outputs return false by default; override where needed.
        public virtual bool GetOutput(int index) => false;

        // Helper to evaluate an input pin with null-check and message, assuming false if unconnected.
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

            // FromPin is the upstream output; Owner should be a Gate
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
