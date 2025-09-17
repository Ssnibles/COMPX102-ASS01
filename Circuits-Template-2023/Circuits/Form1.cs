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
        protected Wire currentWire = null; // selected wire
        protected Gate newGate = null;
        protected int mouseX = -1, mouseY = -1;

        // Group-in-place builder
        protected Compound newCompound = null;

        // UI feedback (overlays only; banner removed)
        private readonly Pen groupDashPen = new Pen(Color.Gold, 2f) { DashStyle = DashStyle.Dash };
        private readonly Brush groupFill = new SolidBrush(Color.FromArgb(40, Color.Gold));

        // Drag-on-press gate movement
        private const int DRAG_THRESHOLD = 2;
        private int downX, downY;
        private bool draggingGate = false;
        private Gate pressedGate = null;
        private bool pressedWasInputSource = false;

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

        private Gate FindGateAt(int x, int y)
        {
            for (int i = gatesList.Count - 1; i >= 0; i--)
            {
                if (gatesList[i].IsMouseOn(x, y))
                    return gatesList[i];
            }
            return null;
        }

        private void RecomputeAndRepaint()
        {
            EvaluateAllLamps();
            Invalidate(); // request redraw after logic changes [web:7]
        }

        private bool IsGrouping => newCompound != null;

        private Rectangle BoundsOfGates(IReadOnlyList<Gate> gs)
        {
            if (gs == null || gs.Count == 0) return Rectangle.Empty;
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var g in gs)
            {
                minX = Math.Min(minX, g.Left);
                minY = Math.Min(minY, g.Top);
                maxX = Math.Max(maxX, g.Left + 40);
                maxY = Math.Max(maxY, g.Top + 40);
            }
            return Rectangle.FromLTRB(minX, minY, maxX, maxY);
        }

        private void CancelGrouping()
        {
            if (!IsGrouping) return;
            foreach (var g in newCompound.Children) g.Selected = false;
            newCompound = null;
            Cursor = Cursors.Default;
            Invalidate(); // repaint to clear overlays [web:7]
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete)
            {
                DeleteSelection();
                return true;
            }
            if (keyData == Keys.Escape)
            {
                CancelGrouping();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void DeleteSelection()
        {
            if (current != null)
            {
                for (int i = wiresList.Count - 1; i >= 0; i--)
                {
                    var w = wiresList[i];
                    if (ReferenceEquals(w.FromPin.Owner, current) || ReferenceEquals(w.ToPin.Owner, current))
                    {
                        w.ToPin.InputWire = null;
                        wiresList.RemoveAt(i);
                    }
                }
                gatesList.Remove(current);
                current = null;
                Invalidate(); // repaint after deletion [web:7]
                return;
            }

            if (currentWire != null)
            {
                currentWire.ToPin.InputWire = null;
                wiresList.Remove(currentWire);
                currentWire = null;
                Invalidate(); // repaint after deletion [web:7]
            }
        }

        private void toolStripLabelDelete_Click(object sender, EventArgs e)
        {
            DeleteSelection(); // toolbar/menu delete handler [web:126]
        }

        private void toolStripLabelClear_Click(object sender, EventArgs e)
        {
            foreach (var w in wiresList)
                w.ToPin.InputWire = null;

            wiresList.Clear();
            gatesList.Clear();

            current = null;
            currentWire = null;
            newGate = null;
            newCompound = null;
            startPin = null;

            Invalidate(); // repaint empty canvas [web:7]
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            mouseX = e.X;
            mouseY = e.Y;

            if (startPin != null)
            {
                currentX = e.X;
                currentY = e.Y;
                Invalidate(); // live wire rubber-band [web:7]
                return;
            }

            if (current != null && startX >= 0 && startY >= 0 && pressedGate == current)
            {
                if (!draggingGate && (Math.Abs(e.X - downX) > DRAG_THRESHOLD || Math.Abs(e.Y - downY) > DRAG_THRESHOLD))
                    draggingGate = true;

                current.MoveTo(currentX + (e.X - startX), currentY + (e.Y - startY));
                Invalidate(); // move feedback [web:7]
                return;
            }

            if (newGate != null)
            {
                currentX = e.X;
                currentY = e.Y;
                Invalidate(); // preview follows mouse [web:7]
                return;
            }

            Invalidate(); // hover adorners [web:7]
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (currentWire != null) { currentWire.Selected = false; currentWire = null; }

            var pinHit = findPin(e.X, e.Y);
            if (pinHit != null)
            {
                startPin = pinHit;
                pressedGate = null;
                draggingGate = false;
                return;
            }

            var hitGate = FindGateAt(e.X, e.Y);
            if (hitGate != null)
            {
                if (IsGrouping && !(hitGate is Compound))
                {
                    newCompound.AddExisting(hitGate);
                    hitGate.Selected = true;
                    Invalidate(); // overlay feedback [web:7]
                    return;
                }

                if (current != null && current != hitGate) current.Selected = false;
                current = hitGate;
                current.Selected = true;

                startX = e.X; startY = e.Y;
                currentX = current.Left; currentY = current.Top;
                downX = e.X; downY = e.Y;

                pressedGate = current;
                draggingGate = false;
                pressedWasInputSource = current is InputSource;

                return;
            }

            pressedGate = null;
            draggingGate = false;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (startPin != null)
            {
                var endPin = findPin(e.X, e.Y);
                if (endPin != null)
                {
                    Pin input, output;

                    if (startPin.IsOutput) { input = endPin; output = startPin; }
                    else { input = startPin; output = endPin; }

                    if (input.IsInput && output.IsOutput)
                    {
                        if (input.InputWire == null)
                        {
                            var newWire = new Wire(output, input);
                            input.InputWire = newWire;
                            wiresList.Add(newWire);
                            RecomputeAndRepaint(); // recompute after wiring [web:7]
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
                Invalidate(); // end of wire action [web:7]
            }
            else
            {
                if (pressedGate != null && !draggingGate && pressedWasInputSource)
                {
                    ((InputSource)pressedGate).Toggle();
                    RecomputeAndRepaint(); // update lamp state [web:7]
                }
            }

            startX = -1; startY = -1;
            currentX = 0; currentY = 0;
            pressedGate = null;
            draggingGate = false;
            pressedWasInputSource = false;
        }

        private void toolStripButtonAnd_Click(object sender, EventArgs e) { newGate = new AndGate(0, 0); }
        private void toolStripButtonOr_Click(object sender, EventArgs e) { newGate = new OrGate(0, 0); }
        private void toolStripButtonNot_Click(object sender, EventArgs e) { newGate = new NotGate(0, 0); }
        private void toolStripButtonInput_Click(object sender, EventArgs e) { newGate = new InputSource(0, 0); }
        private void toolStripButtonOutputLamp_Click(object sender, EventArgs e) { newGate = new OutputLamp(0, 0); }

        private void toolStripLabelEvaluate_Click(object sender, EventArgs e)
        {
            RecomputeAndRepaint(); // evaluate lamps + repaint [web:7]
        }

        private void toolStripLabelClone_Click(object sender, EventArgs e)
        {
            if (current != null) newGate = current.Clone(); // preview a clone [web:7]
        }

        private void toolStripLabelStartGroup_Click(object sender, EventArgs e)
        {
            newCompound = new Compound(0, 0);
            Cursor = Cursors.Cross;
            Invalidate(); // show overlays only (no banner) [web:7]
        }

        private void toolStripLabelEndGroup_Click(object sender, EventArgs e)
        {
            if (newCompound != null && newCompound.Children.Count > 0)
            {
                foreach (var child in newCompound.Children)
                {
                    child.Selected = false;
                    gatesList.Remove(child);
                }

                gatesList.Add(newCompound);
                current = newCompound;
                current.Selected = true;

                Cursor = Cursors.Default;
                newCompound = null;

                Invalidate(); // redraw to show grouped unit [web:7]
            }
            else
            {
                CancelGrouping();
            }
        }

        private void EvaluateAllLamps()
        {
            foreach (var gate in gatesList)
            {
                if (gate is OutputLamp lamp)
                    lamp.Evaluate();
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var gate in gatesList)
                gate.Draw(e.Graphics);

            foreach (var wire in wiresList)
                wire.Draw(e.Graphics);

            if (startPin != null)
                e.Graphics.DrawLine(Pens.White, startPin.X, startPin.Y, currentX, currentY);

            if (newGate != null)
            {
                newGate.MoveTo(currentX, currentY);
                newGate.Draw(e.Graphics);
            }

            // Grouping overlays (banner removed)
            if (newCompound != null)
            {
                // Outline hovered gate that would be added
                Gate hover = null;
                for (int i = gatesList.Count - 1; i >= 0; i--)
                {
                    var g = gatesList[i];
                    if (!(g is Compound) && g.IsMouseOn(mouseX, mouseY))
                    {
                        hover = g;
                        break;
                    }
                }
                if (hover != null)
                {
                    var r = new Rectangle(hover.Left, hover.Top, 40, 40);
                    e.Graphics.DrawRectangle(groupDashPen, r); // standard rectangle draw [web:191]
                }

                // Outline & fill current group bounds
                var bounds = BoundsOfGates(newCompound.Children);
                if (!bounds.IsEmpty)
                {
                    e.Graphics.FillRectangle(groupFill, bounds);
                    e.Graphics.DrawRectangle(groupDashPen, bounds); // standard rectangle draw [web:191]
                }
            }

            int mx = mouseX, my = mouseY;
            if (mx < 0 || my < 0)
            {
                var p = PointToClient(Cursor.Position);
                mx = p.X; my = p.Y; // screen -> client for reliable hover coords [web:26]
            }

            foreach (var gate in gatesList)
                foreach (var pin in gate.Pins)
                    pin.DrawSnapHover(e.Graphics, mx, my);
        }

        private void ClearSelectionVisuals()
        {
            if (current != null) { current.Selected = false; current = null; }
            if (currentWire != null) { currentWire.Selected = false; currentWire = null; }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (newGate != null)
            {
                newGate.MoveTo(e.X, e.Y);
                gatesList.Add(newGate);
                newGate = null;
                Invalidate(); // placed [web:7]
                return;
            }

            if (pressedGate != null || draggingGate)
                return;

            ClearSelectionVisuals();

            foreach (var gate in gatesList)
            {
                if (gate.IsMouseOn(e.X, e.Y))
                {
                    current = gate;
                    current.Selected = true;

                    if (IsGrouping && !(gate is Compound))
                    {
                        newCompound.AddExisting(gate);
                        Invalidate(); // overlay feedback [web:7]
                    }
                    else if (gate is InputSource src)
                    {
                        src.Toggle();
                        RecomputeAndRepaint(); // evaluate + repaint [web:7]
                    }
                    else
                    {
                        Invalidate(); // selection [web:7]
                    }
                    return;
                }
            }

            const int tol = 6;
            for (int i = wiresList.Count - 1; i >= 0; i--)
            {
                var w = wiresList[i];
                if (w.HitTest(e.X, e.Y, tol))
                {
                    currentWire = w;
                    currentWire.Selected = true;
                    Invalidate(); // red wire [web:7]
                    return;
                }
            }
        }
    }
}
