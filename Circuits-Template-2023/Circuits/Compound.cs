using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuits
{
    /// <summary>
    /// A compound group that holds references to existing gates.
    /// Moving the compound moves all children by stored offsets.
    /// Selection is “all or none”, propagated to the children.
    /// Wires keep working because child gates and pins are the same objects.
    /// </summary>
    public class Compound : Gate
    {
        private sealed class Child
        {
            public Gate Gate;
            public int Dx;
            public int Dy;
            public Child(Gate g, int dx, int dy) { Gate = g; Dx = dx; Dy = dy; }
        }

        private readonly List<Child> children = new List<Child>();

        // Cached union of child body rectangles for a predictable hit box.
        private Rectangle bounds = Rectangle.Empty;

        public Compound(int x, int y) : base(x, y) { }

        /// <summary>
        /// Children gates (read-only copy).
        /// </summary>
        public IReadOnlyList<Gate> Children
        {
            get
            {
                var list = new List<Gate>(children.Count);
                foreach (var ch in children) list.Add(ch.Gate);
                return list;
            }
        }

        /// <summary>
        /// Add an existing gate (by reference) and record its offset from the group origin.
        /// Pins are unioned into this gate's pin list for hover adorners.
        /// </summary>
        public void AddExisting(Gate g)
        {
            if (g == null) return;

            if (children.Count == 0) { left = g.Left; top = g.Top; }

            int dx = g.Left - left;
            int dy = g.Top - top;

            children.Add(new Child(g, dx, dy));
            pins.AddRange(g.Pins); // handy so pin hovers still render whilst grouping

            RecomputeBounds();
        }

        // Compute the union of child bounds, and update stored offsets after any movement.
        private void RecomputeBounds()
        {
            if (children.Count == 0)
            {
                bounds = new Rectangle(left, top, WIDTH, HEIGHT);
                return;
            }

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var ch in children)
            {
                minX = Math.Min(minX, ch.Gate.Left);
                minY = Math.Min(minY, ch.Gate.Top);
                maxX = Math.Max(maxX, ch.Gate.Left + WIDTH);
                maxY = Math.Max(maxY, ch.Gate.Top + HEIGHT);
            }

            left = minX; top = minY;
            bounds = Rectangle.FromLTRB(minX, minY, maxX, maxY);

            for (int i = 0; i < children.Count; i++)
            {
                var ch = children[i];
                ch.Dx = ch.Gate.Left - left;
                ch.Dy = ch.Gate.Top - top;
                children[i] = ch;
            }
        }

        public override bool IsMouseOn(int x, int y) => bounds.Contains(x, y);

        public override bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                foreach (var ch in children) ch.Gate.Selected = value;
            }
        }

        /// <summary>
        /// Draw children, then (when selected) a dashed outline of the group bounds.
        /// </summary>
        public override void Draw(Graphics g)
        {
            foreach (var ch in children) ch.Gate.Draw(g);
            if (selected)
            {
                using (var dash = new Pen(Color.White, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    g.DrawRectangle(dash, bounds);
            }
        }

        protected override void DrawBody(Graphics g) { } // No need the children draw themselves
        protected override void LayoutPins() { } // Pins are owened by the children

        /// <summary>
        /// Move the whole group and re-layout the cached bounds and offsets.
        /// </summary>
        public override void MoveTo(int x, int y)
        {
            left = x; top = y;
            foreach (var ch in children) ch.Gate.MoveTo(left + ch.Dx, top + ch.Dy);
            RecomputeBounds();
        }

        public override bool Evaluate() => false; // compound itself is not a logic gate

        /// <summary>
        /// Clone children (without wires) and preserve relative offsets.
        /// </summary>
        public override Gate Clone()
        {
            var copy = new Compound(left, top);
            foreach (var ch in children)
            {
                var gClone = ch.Gate.Clone();
                gClone.MoveTo(copy.left + ch.Dx, copy.top + ch.Dy);
                copy.AddExisting(gClone);
            }
            copy.Selected = false;
            return copy;
        }
    }
}
