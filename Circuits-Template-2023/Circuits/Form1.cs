using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Circuits
{
    public partial class Form1 : Form
    {
        // All gates and wires on the canvas
        private readonly List<Gate> gates = new List<Gate>();
        private readonly List<Wire> wires = new List<Wire>();

        // Selected gate
        private Gate selectedGate;

        // A gate that follows the mouse before it is placed
        private Gate newGate;

        // A compound group being built
        private Compound buildingGroup;

        // Latest mouse position used for pin hover rings and grouping outlines
        private int mouseX = -1, mouseY = -1;

        // Wiring and dragging stuff
        private Pin wireStart;
        private int currentX, currentY;
        private Gate draggingGate;
        private Point gateStartPos;
        private Point mouseDownPos;

        // Light overlay pens/brushes for the group outlines and fill
        private readonly Pen groupOutline = new Pen(Color.Gold, 2f) { DashStyle = DashStyle.Dash };
        private readonly Brush groupFill = new SolidBrush(Color.FromArgb(40, Color.Gold));

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true; // Reduce flicker during painting and dragging
        }

        /// <summary>
        /// Find the top most gate at a point for selection
        /// </summary>
        private Gate HitGate(int x, int y)
        {
            for (int i = gates.Count - 1; i >= 0; i--)
                if (gates[i].IsMouseOn(x, y)) return gates[i];
            return null;
        }

        /// <summary>
        /// Find the first pin at a point, used to start and end wires
        /// </summary>
        private Pin HitPin(int x, int y)
        {
            foreach (var g in gates)
                foreach (var p in g.Pins)
                    if (p.IsMouseOn(x, y)) return p;
            return null;
        }

        /// <summary>
        /// Get a union rectangle around a list of gates (used for group overlay)
        /// </summary>
        private Rectangle BoundsOf(IReadOnlyList<Gate> gSelected)
        {
            if (gSelected == null || gSelected.Count == 0) return Rectangle.Empty;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var g in gSelected)
            {
                minX = Math.Min(minX, g.Left);
                minY = Math.Min(minY, g.Top);
                maxX = Math.Max(maxX, g.Left + 40);
                maxY = Math.Max(maxY, g.Top + 40);
            }
            return Rectangle.FromLTRB(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Delete removes the selected gate and its wires; Esc cancels grouping
        /// </summary>
        protected override bool ProcessCmdKey(ref Message messages, Keys key)
        {
            if (key == Keys.Delete)
            {
                DeleteSelection();
                return true;
            }
            if (key == Keys.Escape)
            {
                if (buildingGroup != null)
                {
                    foreach (var g in buildingGroup.Children) g.Selected = false;
                    buildingGroup = null;
                    Cursor = Cursors.Default;
                    Invalidate();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref messages, key);
        }

        /// <summary>
        /// Delete the selected gate and any wires attached to it, then repaint
        /// </summary>
        private void DeleteSelection()
        {
            if (selectedGate == null) return;
            // Remove any wires attached to this gate and clear the input link on the input pin
            for (int i = wires.Count - 1; i >= 0; i--)
            {
                var w = wires[i];
                if (ReferenceEquals(w.StartPin.Owner, selectedGate) || ReferenceEquals(w.EndPin.Owner, selectedGate))
                {
                    w.EndPin.InputWire = null;
                    wires.RemoveAt(i);
                }
            }
            gates.Remove(selectedGate);
            selectedGate = null;
            Invalidate();
        }

        /// <summary>
        /// Clear the canvas and reset all transient state and cursor, then repaint
        /// </summary>
        private void ClearAll()
        {
            foreach (var w in wires) w.EndPin.InputWire = null;
            wires.Clear();
            gates.Clear();

            selectedGate = null;
            newGate = null;
            buildingGroup = null;

            wireStart = null;
            draggingGate = null;
            mouseDownPos = Point.Empty;
            gateStartPos = Point.Empty;
            currentX = currentY = -1;

            Cursor = Cursors.Default;
            Invalidate();
        }

        // Toolbar stuff
        private void toolStripButtonAnd_Click(object s, EventArgs e) { newGate = new AndGate(0, 0); Invalidate(); }
        private void toolStripButtonOr_Click(object s, EventArgs e) { newGate = new OrGate(0, 0); Invalidate(); }
        private void toolStripButtonNot_Click(object s, EventArgs e) { newGate = new NotGate(0, 0); Invalidate(); }
        private void toolStripButtonInput_Click(object s, EventArgs e) { newGate = new InputSource(0, 0); Invalidate(); }
        private void toolStripButtonOutputLamp_Click(object s, EventArgs e) { newGate = new OutputLamp(0, 0); Invalidate(); }

        // Recompute all lamps after manual request, then repaint
        private void toolStripLabelEvaluate_Click(object s, EventArgs e)
        {
            foreach (var g in gates)
                if (g is OutputLamp lamp) lamp.Evaluate();
            Invalidate();
        }

        // Clone the selected gate into a preview so it follows the cursor until placed
        private void toolStripLabelClone_Click(object s, EventArgs e)
        {
            if (selectedGate != null) { newGate = selectedGate.Clone(); Invalidate(); }
        }

        private void toolStripLabelDelete_Click(object s, EventArgs e) => DeleteSelection();
        private void toolStripLabelClear_Click(object s, EventArgs e) => ClearAll();

        // Start selecting gates for a new compound; show a cross cursor as a hint
        private void toolStripLabelStartGroup_Click(object s, EventArgs e)
        {
            buildingGroup = new Compound(0, 0);
            Cursor = Cursors.Cross;
            Invalidate();
        }

        /// <summary>
        /// Replace the child gates with the new group, select it, and repaint
        /// Always restores the default cursor
        /// </summary>
        private void toolStripLabelEndGroup_Click(object s, EventArgs e)
        {
            if (buildingGroup != null && buildingGroup.Children.Count > 0)
            {
                foreach (var child in buildingGroup.Children) { child.Selected = false; gates.Remove(child); }
                gates.Add(buildingGroup);
                SelectGate(buildingGroup);
            }
            buildingGroup = null;
            Cursor = Cursors.Default;
            Invalidate();
        }

        /// <summary>
        /// Select a gate (clear old selection, set new selection state)
        /// </summary>
        private void SelectGate(Gate g)
        {
            if (selectedGate != null) selectedGate.Selected = false;
            selectedGate = g;
            if (selectedGate != null) selectedGate.Selected = true;
        }

        // === Mouse events =====================

        /// <summary>
        /// Start wiring if pressing on a pin add to group if grouping; otherwise start dragging a gate
        /// </summary>
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDownPos = new Point(e.X, e.Y);

            // Start a wire if pressing on a pin
            var p = HitPin(e.X, e.Y);
            if (p != null)
            {
                wireStart = p;
                currentX = e.X; currentY = e.Y;
                Invalidate();
                return;
            }

            // While grouping clicking a gate adds it to the group, compound gates cannot be nested
            var gHit = HitGate(e.X, e.Y);
            if (buildingGroup != null && gHit != null && !(gHit is Compound))
            {
                buildingGroup.AddExisting(gHit);
                gHit.Selected = true;
                Invalidate();
                return;
            }

            // Otherwise press on a gate to start dragging it
            if (gHit != null)
            {
                SelectGate(gHit);
                draggingGate = gHit;
                gateStartPos = new Point(gHit.Left, gHit.Top);
                return;
            }
        }

        /// <summary>
        /// Update the rubber band while wiring, move a gate while dragging; otherwise update cursor follow preview and hover
        /// </summary>
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            mouseX = e.X; mouseY = e.Y; //Latest pointer position for hover rings and overlays

            // Rubber band while wiring
            if (wireStart != null)
            {
                currentX = e.X; currentY = e.Y;
                Invalidate();
                return;
            }

            // Drag the selected gate, keeping the original offset from the grab points
            if (draggingGate != null)
            {
                draggingGate.MoveTo(gateStartPos.X + (e.X - mouseDownPos.X),
                                    gateStartPos.Y + (e.Y - mouseDownPos.Y));
                Invalidate();
                return;
            }

            // Let the preview gate follow the mouse until it is placed
            if (newGate != null)
            {
                newGate.MoveTo(e.X, e.Y);
                Invalidate();
                return;
            }

            // Nothing active, just refresh hover rings and any group overlay
            Invalidate();
        }

        /// <summary>
        /// Finish a wire if one is being drawn, stop dragging to toggle an InputSource on a short press
        /// </summary>
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            // Connect output to input if compatible and input is free then update lamps
            if (wireStart != null)
            {
                var end = HitPin(e.X, e.Y);
                if (end != null)
                {
                    Pin input, output;
                    if (wireStart.IsOutput) { input = end; output = wireStart; }
                    else { input = wireStart; output = end; }

                    if (input.IsInput && output.IsOutput)
                    {
                        if (input.InputWire == null)
                        {
                            var w = new Wire(output, input);
                            input.InputWire = w;
                            wires.Add(w);

                            // Recompute all lamps so the change is visible immediately
                            foreach (var g in gates)
                                if (g is OutputLamp lamp) lamp.Evaluate();
                        }
                        else MessageBox.Show("That input is already used.");
                    }
                    else MessageBox.Show("Error: you must connect an output pin to an input pin.");
                }

                wireStart = null;
                Invalidate();
                return;
            }

            // Stop dragging a tiny movement counts as a click to toggle an InputSource or it would just not register the click
            if (draggingGate != null)
            {
                if (selectedGate is InputSource src &&
                    Math.Abs(e.X - mouseDownPos.X) < 2 &&
                    Math.Abs(e.Y - mouseDownPos.Y) < 2)
                {
                    src.Toggle();

                    // Update lamps after a source change.
                    foreach (var g in gates)
                        if (g is OutputLamp lamp) lamp.Evaluate();
                }

                draggingGate = null;
                Invalidate();
                return;
            }
        }

        /// <summary>
        /// Place the preview gate at the pointer when not wiring or dragging
        /// </summary>
        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (newGate != null && wireStart == null && draggingGate == null)
            {
                newGate.MoveTo(e.X, e.Y);
                gates.Add(newGate);
                newGate = null;
                Invalidate();
                return;
            }
        }

        /// <summary>
        /// Draw gates (and their pins) then wires then any rubber band preview grouping overlays and finally pin hover rings
        /// </summary>
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; // smooth lines and rings

            // Draw gates first so wires sit on top of bodies
            foreach (var g in gates) g.Draw(e.Graphics);

            // Draw all wires next for clear visibility
            foreach (var w in wires) w.Draw(e.Graphics);

            // While wiring show a white rubber band from the start pin to the mouse
            if (wireStart != null)
                e.Graphics.DrawLine(Pens.White, wireStart.X, wireStart.Y, currentX, currentY);

            // Draw the floating preview last so it is clearly visible
            if (newGate != null)
                newGate.Draw(e.Graphics);

            // Grouping overlays: dashed outline on the hover target and a filled box over the group bounds
            if (buildingGroup != null)
            {
                var hover = HitGate(mouseX, mouseY);
                if (hover != null && !(hover is Compound))
                    e.Graphics.DrawRectangle(groupOutline, new Rectangle(hover.Left, hover.Top, 40, 40));

                var b = BoundsOf(buildingGroup.Children);
                if (!b.IsEmpty)
                {
                    e.Graphics.FillRectangle(groupFill, b);
                    e.Graphics.DrawRectangle(groupOutline, b);
                }
            }

            // Pin hover decorations draw a blue ring when the pointer is within the snap radius.
            int mx = mouseX, my = mouseY;
            if (mx < 0 || my < 0)
            {
                var p = PointToClient(Cursor.Position); // convert from screen to client coordinates (I didn't even know you could do this)
                mx = p.X; my = p.Y;
            }
            foreach (var g in gates)
                foreach (var p in g.Pins)
                    p.DrawHover(e.Graphics, mx, my);
        }
    }
}
