using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D; // optional for global smoothing

namespace Circuits
{
    public partial class Form1 : Form
    {
        // Mouse position recorded on mouse down (used for drag)
        protected int startX, startY;

        // Currently dragging wire start pin (null if not dragging wire)
        protected Pin startPin = null;

        // Saved coordinates of gate before dragging
        protected int currentX, currentY;

        // Collection of all gates in the current circuit
        protected List<Gate> gatesList = new List<Gate>();

        // Collection of all wires in the current circuit
        protected List<Wire> wiresList = new List<Wire>();

        // Currently selected gate or null
        protected Gate current = null;

        // Gate being newly inserted and dragged before placement
        protected Gate newGate = null;

        // Track latest mouse position in client coordinates for hover rendering
        protected int mouseX = -1, mouseY = -1;

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        /// <summary>
        /// Finds a pin near the given (x,y), or returns null if none.
        /// Uses each pin’s SnapRadius for hit testing.
        /// </summary>
        public Pin findPin(int x, int y)
        {
            foreach (var gate in gatesList)
            {
                foreach (var pin in gate.Pins)
                {
                    if (pin.IsMouseOn(x, y))
                        return pin;
                }
            }
            return null;
        }

        /// <summary>
        /// Handles mouse movement events and tracks mouse for hover ring.
        /// </summary>
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            // Client-relative mouse for this control
            mouseX = e.X;
            mouseY = e.Y; // MouseEventArgs.X/Y are client-coordinates for the control raising MouseMove

            if (startPin != null)
            {
                currentX = e.X;
                currentY = e.Y;
                Invalidate();
            }
            else if (startX >= 0 && startY >= 0 && current != null)
            {
                current.MoveTo(currentX + (e.X - startX), currentY + (e.Y - startY));
                Invalidate();
            }
            else if (newGate != null)
            {
                currentX = e.X;
                currentY = e.Y;
                Invalidate();
            }
            else
            {
                Invalidate();
            }
        }

        /// <summary>
        /// Handles mouse button release.
        /// </summary>
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (startPin != null)
            {
                // Attempt to connect wire from startPin to a nearby pin
                Pin endPin = findPin(e.X, e.Y);
                if (endPin != null)
                {
                    Pin input, output;

                    if (startPin.IsOutput)
                    {
                        input = endPin;
                        output = startPin;
                    }
                    else
                    {
                        input = startPin;
                        output = endPin;
                    }

                    // Validate connection direction
                    if (input.IsInput && output.IsOutput)
                    {
                        if (input.InputWire == null)
                        {
                            Wire newWire = new Wire(output, input);
                            input.InputWire = newWire;
                            wiresList.Add(newWire);
                        }
                        else
                        {
                            MessageBox.Show("That input is already used.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Error: you must connect an output pin to an input pin.");
                    }
                }

                startPin = null;
                Invalidate();
            }

            // Reset drag tracking variables
            startX = -1;
            startY = -1;
            currentX = 0;
            currentY = 0;
        }

        /// <summary>
        /// Event handler for pressing the AND gate button.
        /// </summary>
        private void toolStripButtonAnd_Click(object sender, EventArgs e)
        {
            newGate = new AndGate(0, 0);
        }

        private void toolStripButtonOr_Click(object sender, EventArgs e)
        {
            newGate = new OrGate(0, 0);
        }

        private void toolStripButtonNot_Click(object sender, EventArgs e)
        {
            newGate = new NotGate(0, 0);
        }

        private void toolStripButtonInput_Click(object sender, EventArgs e)
        {
            newGate = new InputSource(0, 0);
        }

        private void toolStripButtonOutputLamp_Click(object sender, EventArgs e)
        {
            newGate = new OutputLamp(0, 0);
        }

        /// <summary>
        /// Paint handler draws gates, wires, dragged elements, and snap-hover indicators.
        /// </summary>
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // Optional global smoothing for nicer lamps
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw all gates
            foreach (var gate in gatesList)
            {
                gate.Draw(e.Graphics);
            }

            // Draw all wires
            foreach (var wire in wiresList)
            {
                wire.Draw(e.Graphics);
            }

            // Draw wire rubberband if dragging a wire
            if (startPin != null)
            {
                e.Graphics.DrawLine(Pens.White,
                    startPin.X, startPin.Y,
                    currentX, currentY);
            }

            // Draw the new gate being inserted and dragged (if any)
            if (newGate != null)
            {
                newGate.MoveTo(currentX, currentY);
                newGate.Draw(e.Graphics);
            }

            // Fallback if no MouseMove yet: convert screen mouse to client coords
            int mx = mouseX, my = mouseY;
            if (mx < 0 || my < 0)
            {
                var p = PointToClient(Cursor.Position);
                mx = p.X; my = p.Y; // Convert screen → client for this control
            }

            // Hover ring: uses the same SnapRadius as IsMouseOn for each pin
            foreach (var gate in gatesList)
            {
                foreach (var pin in gate.Pins)
                {
                    pin.DrawSnapHover(e.Graphics, mx, my);
                }
            }
        }

        /// <summary>
        /// Handles mouse button press for selecting gates or starting wire drags.
        /// </summary>
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (current == null)
            {
                // Try starting wire drag if clicking on a pin (uses pin.SnapRadius)
                startPin = findPin(e.X, e.Y);
            }
            else if (current.IsMouseOn(e.X, e.Y))
            {
                // Begin dragging the currently selected gate
                startX = e.X;
                startY = e.Y;
                currentX = current.Left;
                currentY = current.Top;
            }
        }

        /// <summary>
        /// Handles mouse clicks for selecting/deselecting gates and placing new gates.
        /// InputSource toggles on selection.
        /// </summary>
        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            // Deselect current gate if any
            if (current != null)
            {
                current.Selected = false;
                current = null;
                Invalidate();
            }

            // If inserting a new gate, place it at click location
            if (newGate != null)
            {
                newGate.MoveTo(e.X, e.Y);
                gatesList.Add(newGate);
                newGate = null;
                Invalidate();
            }
            else
            {
                // Try selecting a gate under the mouse
                foreach (var gate in gatesList)
                {
                    if (gate.IsMouseOn(e.X, e.Y))
                    {
                        gate.Selected = true;
                        current = gate;

                        // Toggle InputSource on selection
                        if (gate is InputSource src)
                        {
                            src.Toggle();
                        }

                        Invalidate(); // request redraw with new state
                        break;
                    }
                }
            }
        }
    }
}
