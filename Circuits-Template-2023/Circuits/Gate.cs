using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuits
{
    // Base class for all logic gates
    public abstract class Gate
    {
        protected int left;   // X position of top-left corner
        protected int top;    // Y position of top-left corner

        protected const int WIDTH = 40;   // Gate width
        protected const int HEIGHT = 40;  // Gate height
        protected const int GAP = 10;     // Gap for pin placement

        protected readonly List<Pin> pins = new List<Pin>();  // Pins attached to the gate
        protected bool selected;          // Gate selection state

        // Set initial gate position
        protected Gate(int x, int y)
        {
            left = x;
            top = y;
        }

        // Gets/sets whether the gate is selected
        public virtual bool Selected
        {
            get => selected;
            set => selected = value;
        }

        // Gets X position
        public int Left => left;
        // Gets Y position
        public int Top => top;

        // Gets the list of pins
        public List<Pin> Pins => pins;

        // Returns true if (x,y) is inside gate bounds
        public virtual bool IsMouseOn(int x, int y)
        {
            return (x >= left && x < left + WIDTH) && (y >= top && y < top + HEIGHT);
        }

        // Draws all pins and gate body
        public virtual void Draw(Graphics g)
        {
            foreach (var pin in pins) pin.Draw(g);
            DrawBody(g);
        }

        // Draws the body of the gate (to be implemented by subclasses)
        protected abstract void DrawBody(Graphics g);

        // Moves gate to new position; pins are updated
        public virtual void MoveTo(int x, int y)
        {
            left = x;
            top = y;
            MoveTo();
        }

        // Repositions pins after movement (to be implemented by subclasses)
        protected abstract void MoveTo();

        // Computes gate output (to be implemented by subclasses)
        public abstract bool Evaluate();

        // Creates a copy of this gate (to be implemented by subclasses)
        public abstract Gate Clone();

        // Safely reads input pin value, shows warning if unconnected or invalid
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
            var upstreamPin = inPin.InputWire.StartPin;
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
