using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuits
{
    public class Compound : Gate
    {
        // Stores a gate and its offset from compound origin
        private class Child
        {
            public Gate Gate;
            public int Dx;  // horizontal offset
            public int Dy;  // vertical offset
            public Child(Gate g, int dx, int dy) { Gate = g; Dx = dx; Dy = dy; }
        }

        private readonly List<Child> children = new List<Child>(); // list of child gates
        private Rectangle bounds = Rectangle.Empty;               // cached bounds

        public Compound(int x, int y) : base(x, y) { }

        // Read-only access to all child gates
        public IReadOnlyList<Gate> Children
        {
            get
            {
                var list = new List<Gate>(children.Count);
                foreach (var ch in children) list.Add(ch.Gate);
                return list;
            }
        }

        // Add an existing gate, store its offset from compound origin
        public void AddExisting(Gate g)
        {
            if (g == null) return;
            int dx = g.Left - left;
            int dy = g.Top - top;
            children.Add(new Child(g, dx, dy));
            pins.AddRange(g.Pins); // include child pins for interaction
            RecomputeBounds();
        }

        // Compute the bounding rectangle of all children and update offsets
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

        // Selection cascades to all children
        public override bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                foreach (var ch in children) ch.Gate.Selected = value;
            }
        }

        // Draw all children and outline if selected
        public override void Draw(Graphics g)
        {
            foreach (var ch in children) ch.Gate.Draw(g);
            if (selected)
            {
                using (var dash = new Pen(Color.White, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    g.DrawRectangle(dash, bounds);
            }
        }

        // Compound has no body
        protected override void DrawBody(Graphics g) { }

        // Move compound and all children, then update bounds
        public override void MoveTo(int x, int y)
        {
            left = x; top = y;
            foreach (var ch in children) ch.Gate.MoveTo(left + ch.Dx, top + ch.Dy);
            RecomputeBounds();
        }

        // Compound has no logic output
        public override bool Evaluate() => false;

        // Copy compound with all children, preserving relative positions
        public override Gate Clone()
        {
            int originLeft = left, originTop = top;
            var copy = new Compound(originLeft, originTop);
            foreach (var ch in children)
            {
                Gate clone = ch.Gate.Clone();
                clone.MoveTo(originLeft + ch.Dx, originTop + ch.Dy);
                copy.AddExisting(clone);
            }
            copy.Selected = false;
            return copy;
        }
    }
}
