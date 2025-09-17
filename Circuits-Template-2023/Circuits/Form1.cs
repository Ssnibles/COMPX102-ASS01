using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Circuits
{
    /// <summary>
    /// Main canvas/controller for the simple logic editor.
    /// Refactored event flow uses a small state machine:
    /// Idle, DragWire (rubber band), DragGate (hold-to-drag), Preview (floating new gate).
    /// Drawing follows normal WinForms patterns: update model, call Invalidate(), and paint in OnPaint.
    /// </summary>
    public partial class Form1 : Form
    {
        // Model collections: top-level gates and wires.
        private readonly List<Gate> gates = new List<Gate>();
        private readonly List<Wire> wires = new List<Wire>();

        // Current selection.
        private Gate selectedGate;
        private Wire selectedWire;

        // Floating preview gate (when inserting or cloning).
        private Gate previewGate;

        // Active compound being built (grouping mode).
        private Compound buildingGroup;

        // Latest mouse position in client coordinates (for hover).
        private int mouseX = -1, mouseY = -1;

        // UI finite-state machine to simplify interactions.
        private enum UiState { Idle, DragWire, DragGate, Preview }
        private UiState state = UiState.Idle;

        // Drag bookkeeping.
        private Pin wireStart;           // start pin for rubber-band wire
        private Point gateStartPos;      // selected gate origin at drag begin
        private Point mouseDownPos;      // mouse at MouseDown
        private int currentX, currentY;  // rubber-band endpoint during wire drag

        // Lightweight visual resources.
        private readonly Pen groupDash = new Pen(Color.Gold, 2f) { DashStyle = DashStyle.Dash };
        private readonly Brush groupFill = new SolidBrush(Color.FromArgb(40, Color.Gold));

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true; // reduce flicker when repainting
        }

        // --- Utility helpers --------------------------------------------------

        /// <summary>
        /// Schedules a repaint (WinForms will call Paint later).
        /// </summary>
        private void RequestRepaint() => Invalidate(); // request, not force, a redraw

        /// <summary>
        /// Topmost gate under a point (scan from end for natural z-order).
        /// </summary>
        private Gate HitGate(int x, int y)
        {
            for (int i = gates.Count - 1; i >= 0; i--)
                if (gates[i].IsMouseOn(x, y)) return gates[i];
            return null;
        }

        /// <summary>
        /// First pin under a point (for wire starts/ends).
        /// </summary>
        private Pin HitPin(int x, int y)
        {
            foreach (var g in gates)
                foreach (var p in g.Pins)
                    if (p.IsMouseOn(x, y)) return p;
            return null;
        }

        /// <summary>
        /// Union bounds of several gates, for compound overlay.
        /// </summary>
        private Rectangle BoundsOf(IReadOnlyList<Gate> gs)
        {
            if (gs == null || gs.Count == 0) return Rectangle.Empty;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var g in gs)
            {
                minX = Math.Min(minX, g.Left);
                minY = Math.Min(minY, g.Top);
                maxX = Math.Max(maxX, g.Left + 40);
                maxY = Math.Max(maxY, g.Top + 40);
            }
            return Rectangle.FromLTRB(minX, minY, maxX, maxY);
        }

        // --- Keyboard shortcuts ----------------------------------------------

        /// <summary>
        /// Delete removes the selected gate (and its attached wires) or the selected wire.
        /// Escape cancels grouping.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete) { DeleteSelection(); return true; } // tidy-up and repaint
            if (keyData == Keys.Escape) { CancelGrouping(); return true; }  // reset cursor and overlays
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void DeleteSelection()
        {
            if (selectedGate != null)
            {
                // Remove any wires attached to the selected gate (cleanly detaching inputs).
                for (int i = wires.Count - 1; i >= 0; i--)
                {
                    var w = wires[i];
                    if (ReferenceEquals(w.FromPin.Owner, selectedGate) || ReferenceEquals(w.ToPin.Owner, selectedGate))
                    {
                        w.ToPin.InputWire = null;
                        wires.RemoveAt(i);
                    }
                }
                gates.Remove(selectedGate);
                selectedGate = null;
                RequestRepaint(); // show the cleared canvas
                return;
            }

            if (selectedWire != null)
            {
                selectedWire.ToPin.InputWire = null;
                wires.Remove(selectedWire);
                selectedWire = null;
                RequestRepaint(); // redraw without the removed wire
            }
        }

        private void ClearAll()
        {
            // Disconnect all inputs first, then clear lists and state.
            foreach (var w in wires) w.ToPin.InputWire = null;
            wires.Clear();
            gates.Clear();
            selectedGate = null;
            selectedWire = null;
            previewGate = null;
            buildingGroup = null;
            state = UiState.Idle;
            wireStart = null;
            Cursor = Cursors.Default; // ensure cursor is restored on a full reset
            RequestRepaint(); // repaint empty scene
        }

        // --- Toolbar/menu handlers (wire these in the designer) ---------------

        private void toolStripButtonAnd_Click(object s, EventArgs e) => StartPreview(new AndGate(0, 0));   // floating preview
        private void toolStripButtonOr_Click(object s, EventArgs e) => StartPreview(new OrGate(0, 0));    // floating preview
        private void toolStripButtonNot_Click(object s, EventArgs e) => StartPreview(new NotGate(0, 0));   // floating preview
        private void toolStripButtonInput_Click(object s, EventArgs e) => StartPreview(new InputSource(0, 0)); // floating preview
        private void toolStripButtonOutputLamp_Click(object s, EventArgs e) => StartPreview(new OutputLamp(0, 0));  // floating preview

        private void toolStripLabelEvaluate_Click(object s, EventArgs e) { EvaluateLamps(); RequestRepaint(); } // recompute + repaint

        private void toolStripLabelClone_Click(object s, EventArgs e)
        {
            if (selectedGate != null) StartPreview(selectedGate.Clone()); // prototype clone into preview
        }

        private void toolStripLabelDelete_Click(object s, EventArgs e) => DeleteSelection(); // parity with Delete key
        private void toolStripLabelClear_Click(object s, EventArgs e) => ClearAll();        // clear everything

        private void toolStripLabelStartGroup_Click(object s, EventArgs e)
        {
            buildingGroup = new Compound(0, 0); // group by reference so wires keep working
            Cursor = Cursors.Cross;             // visible feedback for grouping mode
            RequestRepaint();                   // overlays will draw in Paint
        }

        private void toolStripLabelEndGroup_Click(object s, EventArgs e)
        {
            if (buildingGroup != null && buildingGroup.Children.Count > 0)
            {
                // Replace the individual children with a single compound.
                foreach (var child in buildingGroup.Children) { child.Selected = false; gates.Remove(child); }
                gates.Add(buildingGroup);
                SelectGate(buildingGroup);
            }

            // Always exit grouping mode and restore the cursor, even if nothing was added.
            buildingGroup = null;
            Cursor = Cursors.Default; // restore standard pointer
            RequestRepaint();         // repaint without overlays
        }

        // --- Core actions -----------------------------------------------------

        private void StartPreview(Gate g)
        {
            previewGate = g;
            state = UiState.Preview;  // new gate follows the cursor until placed
            RequestRepaint();         // schedule a redraw
        }

        private void EvaluateLamps()
        {
            foreach (var g in gates)
                if (g is OutputLamp lamp) lamp.Evaluate(); // cascades through upstream gates
        }

        private void CancelGrouping()
        {
            if (buildingGroup == null) return;
            foreach (var g in buildingGroup.Children) g.Selected = false; // tidy selection
            buildingGroup = null;
            Cursor = Cursors.Default;  // back to normal pointer
            RequestRepaint();          // remove overlays
        }

        private void SelectGate(Gate g)
        {
            if (selectedGate != null) selectedGate.Selected = false;
            selectedGate = g;
            if (selectedGate != null) selectedGate.Selected = true;
        }

        private void SelectWire(Wire w)
        {
            if (selectedWire != null) selectedWire.Selected = false;
            selectedWire = w;
            if (selectedWire != null) selectedWire.Selected = true;
        }

        // --- Mouse events -----------------------------------------------------

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDownPos = new Point(e.X, e.Y);

            // Start a wire if pressing on a pin.
            var p = HitPin(e.X, e.Y);
            if (p != null)
            {
                wireStart = p;
                state = UiState.DragWire;    // rubber-band begins
                RequestRepaint();            // show the line immediately
                return;
            }

            // If grouping, add the gate under the pointer on press.
            var gHit = HitGate(e.X, e.Y);
            if (buildingGroup != null && gHit != null && !(gHit is Compound))
            {
                buildingGroup.AddExisting(gHit);
                gHit.Selected = true;        // keep it visibly included
                RequestRepaint();            // refresh overlays
                return;
            }

            // Else begin dragging a gate under the pointer.
            if (gHit != null)
            {
                SelectGate(gHit);
                gateStartPos = new Point(selectedGate.Left, selectedGate.Top);
                state = UiState.DragGate;    // hold-to-drag
                return;
            }

            // Otherwise stay in current state (Preview if placing).
            state = (previewGate != null) ? UiState.Preview : UiState.Idle;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            mouseX = e.X; mouseY = e.Y;

            switch (state)
            {
                case UiState.DragWire:
                    currentX = e.X; currentY = e.Y;           // update rubber-band end
                    RequestRepaint();                          // redraw the line
                    break;

                case UiState.DragGate:
                    if (selectedGate != null)
                        selectedGate.MoveTo(gateStartPos.X + (e.X - mouseDownPos.X),
                                            gateStartPos.Y + (e.Y - mouseDownPos.Y));
                    RequestRepaint();                          // smooth dragging
                    break;

                case UiState.Preview:
                    if (previewGate != null)
                    {
                        previewGate.MoveTo(e.X, e.Y);          // float with the cursor
                        RequestRepaint();                      // keep preview live
                    }
                    break;

                default:
                    RequestRepaint();                          // update hover adorners
                    break;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (state == UiState.DragWire)
            {
                var end = HitPin(e.X, e.Y);
                if (end != null && wireStart != null)
                {
                    Pin input, output;
                    if (wireStart.IsOutput) { input = end; output = wireStart; }
                    else { input = wireStart; output = end; }

                    if (input.IsInput && output.IsOutput)
                    {
                        if (input.InputWire == null)
                        {
                            var w = new Wire(output, input);
                            input.InputWire = w;   // connect to the input
                            wires.Add(w);
                            EvaluateLamps();       // reflect immediately
                        }
                        else MessageBox.Show("That input is already used.");
                    }
                    else MessageBox.Show("Error: you must connect an output pin to an input pin.");
                }

                wireStart = null;
                state = UiState.Idle;
                RequestRepaint();                      // final wire drawn
                return;
            }

            if (state == UiState.DragGate)
            {
                // A short press without movement toggles an InputSource.
                if (selectedGate is InputSource src &&
                    Math.Abs(e.X - mouseDownPos.X) < 2 &&
                    Math.Abs(e.Y - mouseDownPos.Y) < 2)
                {
                    src.Toggle();
                    EvaluateLamps();                  // recompute lamps
                }
                state = UiState.Idle;
                RequestRepaint();                     // final position shown
                return;
            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            // Place a previewed gate with a click.
            if (previewGate != null && state == UiState.Preview)
            {
                previewGate.MoveTo(e.X, e.Y);
                gates.Add(previewGate);
                previewGate = null;
                state = UiState.Idle;
                RequestRepaint();                     // show newly placed gate
                return;
            }

            // When idle, allow selecting a wire with a small tolerance.
            if (state == UiState.Idle)
            {
                SelectGate(null);
                const int tol = 6;
                for (int i = wires.Count - 1; i >= 0; i--)
                {
                    if (wires[i].HitTest(e.X, e.Y, tol))
                    {
                        SelectWire(wires[i]);
                        RequestRepaint();             // highlight in red
                        return;
                    }
                }
                SelectWire(null);
                RequestRepaint();                     // clear any wire selection
            }
        }

        // --- Painting ---------------------------------------------------------

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Gates first: their Draw() now renders pins beneath the body (images on top).
            foreach (var g in gates) g.Draw(e.Graphics); // DrawImage/DrawRectangle happen here

            // Then draw wires so connections are always visible above gate bodies.
            foreach (var w in wires) w.Draw(e.Graphics);

            // Rubber-band line when dragging a wire.
            if (state == UiState.DragWire && wireStart != null)
                e.Graphics.DrawLine(Pens.White, wireStart.X, wireStart.Y, currentX, currentY);

            // Floating preview is drawn on top for clarity.
            if (state == UiState.Preview && previewGate != null)
                previewGate.Draw(e.Graphics);

            // Grouping overlays: dashed outline of the hover target and current group bounds.
            if (buildingGroup != null)
            {
                var hover = HitGate(mouseX, mouseY);
                if (hover != null && !(hover is Compound))
                    e.Graphics.DrawRectangle(groupDash, new Rectangle(hover.Left, hover.Top, 40, 40)); // simple outline

                var b = BoundsOf(buildingGroup.Children);
                if (!b.IsEmpty)
                {
                    e.Graphics.FillRectangle(groupFill, b);
                    e.Graphics.DrawRectangle(groupDash, b); // readable bounds box
                }
            }

            // Hover adorners for pins use the latest client coordinates.
            int mx = mouseX, my = mouseY;
            if (mx < 0 || my < 0)
            {
                var p = PointToClient(Cursor.Position);
                mx = p.X; my = p.Y; // ensure we work in client coords
            }
            foreach (var g in gates)
                foreach (var p in g.Pins)
                    p.DrawSnapHover(e.Graphics, mx, my);
        }
    }
}
