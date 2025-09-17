using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuits
{
    public abstract class Gate
    {
        protected int left;
        protected int top;

        protected const int WIDTH = 40;
        protected const int HEIGHT = 40;
        protected const int GAP = 10;

        protected List<Pin> pins = new List<Pin>();
        protected bool selected = false;

        public Gate(int x, int y)
        {
            left = x;
            top = y;
        }

        public virtual bool Selected
        {
            get => selected;
            set => selected = value;
        }

        public int Left => left;
        public int Top => top;
        public List<Pin> Pins => pins;

        // Make hit-testing virtual so composites can override with real bounds
        public virtual bool IsMouseOn(int x, int y)
        {
            return (left <= x && x < left + WIDTH) && (top <= y && y < top + HEIGHT);
        }

        public virtual void Draw(Graphics paper)
        {
            foreach (var pin in pins)
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

        public abstract bool Evaluate();   // subclasses must implement value calc [web:80]

        public virtual bool GetOutput(int index) => false;

        public abstract Gate Clone();      // per-gate prototype clone, simple and explicit [web:80]

        // Shared helper for evaluation that warns and assumes false if unconnected
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
