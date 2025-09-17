using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Circuits
{
    public partial class Form1 : Form
    {
        protected int startX, startY;
        protected Pin startPin = null;
        protected int currentX, currentY;
        protected List<Gate> gatesList = new List<Gate>();
        protected List<Wire> wiresList = new List<Wire>();
        protected Gate current = null;
        protected Gate newGate = null;
        protected int mouseX = -1, mouseY = -1;

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

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

        private void RecomputeAndRepaint()
        {
            EvaluateAllLamps();   // run evaluation cascade into lamps
            Invalidate();         // force repaint so visuals update now
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            mouseX = e.X;
            mouseY = e.Y;

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

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (startPin != null)
            {
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

                    if (input.IsInput && output.IsOutput)
                    {
                        if (input.InputWire == null)
                        {
                            Wire newWire = new Wire(output, input);
                            input.InputWire = newWire;
                            wiresList.Add(newWire);

                            // Evaluate immediately after making a new connection
                            RecomputeAndRepaint();
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

            startX = -1;
            startY = -1;
            currentX = 0;
            currentY = 0;
        }

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

        // Evaluate toolbar button: run all lamp evaluations, then repaint
        private void toolStripButtonEvaluate_Click(object sender, EventArgs e)
        {
            RecomputeAndRepaint(); // evaluates lamps and calls Invalidate
        }

        // If using a label instead of a button, delegate to the same action
        private void toolStripLabelEvaluate_Click(object sender, EventArgs e)
        {
            RecomputeAndRepaint();
        }

        private void EvaluateAllLamps()
        {
            foreach (var gate in gatesList)
            {
                if (gate is OutputLamp lamp)
                {
                    lamp.Evaluate(); // lamp updates its stored on/off here
                }
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var gate in gatesList)
            {
                gate.Draw(e.Graphics);
            }

            foreach (var wire in wiresList)
            {
                wire.Draw(e.Graphics);
            }

            if (startPin != null)
            {
                e.Graphics.DrawLine(Pens.White, startPin.X, startPin.Y, currentX, currentY);
            }

            if (newGate != null)
            {
                newGate.MoveTo(currentX, currentY);
                newGate.Draw(e.Graphics);
            }

            int mx = mouseX, my = mouseY;
            if (mx < 0 || my < 0)
            {
                var p = PointToClient(Cursor.Position);
                mx = p.X; my = p.Y;
            }

            foreach (var gate in gatesList)
            {
                foreach (var pin in gate.Pins)
                {
                    pin.DrawSnapHover(e.Graphics, mx, my);
                }
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (current == null)
            {
                startPin = findPin(e.X, e.Y);
            }
            else if (current.IsMouseOn(e.X, e.Y))
            {
                startX = e.X;
                startY = e.Y;
                currentX = current.Left;
                currentY = current.Top;
            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (current != null)
            {
                current.Selected = false;
                current = null;
                Invalidate();
            }

            if (newGate != null)
            {
                newGate.MoveTo(e.X, e.Y);
                gatesList.Add(newGate);
                newGate = null;
                Invalidate();
            }
            else
            {
                foreach (var gate in gatesList)
                {
                    if (gate.IsMouseOn(e.X, e.Y))
                    {
                        gate.Selected = true;
                        current = gate;

                        // Toggle InputSource on selection and recompute immediately
                        if (gate is InputSource src)
                        {
                            src.Toggle();
                            RecomputeAndRepaint(); // evaluate + repaint after toggle
                        }
                        else
                        {
                            Invalidate();
                        }

                        break;
                    }
                }
            }
        }
    }
}
