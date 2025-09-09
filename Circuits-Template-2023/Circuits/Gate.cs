using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// Base class representing a generic gate in the circuit.
    /// Contains common properties like position, pins, drawing, and selection state.
    /// </summary>
    public class Gate
    {
        // Position of the gate
        protected int left;
        protected int top;

        // Fixed size of gate body
        protected const int WIDTH = 40;
        protected const int HEIGHT = 40;

        // Distance pins extend outside the gate body
        protected const int GAP = 10;

        // Brushes for drawing selected and normal states
        protected Brush selectedBrush = Brushes.Red;
        protected Brush normalBrush = Brushes.LightGray;

        // The brush currently used to draw the gate body (set during drawing)
        protected Brush brush;

        // List of pins connected to this gate
        protected List<Pin> pins = new List<Pin>();

        // Indicates whether the gate is currently selected
        protected bool selected = false;

        /// <summary>
        /// Constructor initialises gate position and default brush.
        /// </summary>
        public Gate(int x, int y)
        {
            left = x;
            top = y;
            brush = normalBrush; // Initialise to avoid null reference during drawing
        }

        // Property to get/set selected state
        public bool Selected
        {
            get { return selected; }
            set { selected = value; }
        }

        // Read-only properties for position and pins list
        public int Left => left;
        public int Top => top;
        public List<Pin> Pins => pins;

        /// <summary>
        /// Determines if a point (x,y) lies within the bounding rectangle of the gate.
        /// Used for hit-testing mouse clicks.
        /// </summary>
        public bool IsMouseOn(int x, int y)
        {
            return (left <= x && x < left + WIDTH) && (top <= y && y < top + HEIGHT);
        }

        /// <summary>
        /// Draw the gate and its pins on the supplied Graphics object.
        /// The gate body drawing is the responsibility of subclasses (override Draw).
        /// </summary>
        public virtual void Draw(Graphics paper)
        {
            // Select brush color depending on selection state
            brush = selected ? selectedBrush : normalBrush;

            // Draw all pins connected to this gate
            foreach (Pin pin in pins)
            {
                pin.Draw(paper);
            }
        }

        /// <summary>
        /// Move the gate and its pins to a new position (x,y)
        /// Updates the position of pins relative to the gate body
        /// </summary>
        public void MoveTo(int x, int y)
        {
            left = x;
            top = y;

            // Update coordinates of pins only if there are at least 3 pins expected
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
    }
}
