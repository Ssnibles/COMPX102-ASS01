using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuits
{
    // Groups existing gates by reference so wires keep working.
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

        // Cached bounds of the group for hit-testing and adorners
        private Rectangle bounds = Rectangle.Empty;

        public Compound(int x, int y) : base(x, y)
        {
            pins.Clear(); // compound itself has no pins; children expose theirs
        }

        public IReadOnlyList<Gate> Children
        {
            get
            {
                var list = new List<Gate>(children.Count);
                foreach (var ch in children) list.Add(ch.Gate);
                return list;
            }
        }

        // Add an existing gate (no cloning) and record its offset from the group's origin.
        public void AddExisting(Gate g)
        {
            if (g == null) return;

            if (children.Count == 0)
            {
                // On first add, anchor the group to the first gate's position
                left = g.Left;
                top = g.Top;
            }

            int dx = g.Left - left;
            int dy = g.Top - top;

            children.Add(new Child(g, dx, dy));

            // Union all children pins so hit-testing for pins still works
            foreach (var p in g.Pins)
                pins.Add(p);

            RecomputeBounds();
        }

        private void RecomputeBounds()
        {
            if (children.Count == 0)
            {
                bounds = new Rectangle(left, top, WIDTH, HEIGHT);
                return;
            }

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var ch in children)
            {
                // Use child gate body rect; if a gate overrides size visually, adjust here if needed
                int x = ch.Gate.Left;
                int y = ch.Gate.Top;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x + WIDTH);
                maxY = Math.Max(maxY, y + HEIGHT);
            }

            left = minX;
            top = minY;
            bounds = Rectangle.FromLTRB(minX, minY, maxX, maxY);

            // Update offsets to reflect any change to left/top anchor
            for (int i = 0; i < children.Count; i++)
            {
                var ch = children[i];
                ch.Dx = ch.Gate.Left - left;
                ch.Dy = ch.Gate.Top - top;
                children[i] = ch;
            }
        }

        public override void MoveTo(int x, int y)
        {
            // Move all children preserving relative offsets
            left = x;
            top = y;

            foreach (var ch in children)
                ch.Gate.MoveTo(left + ch.Dx, top + ch.Dy);

            RecomputeBounds();
        }

        public override bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                foreach (var ch in children)
                    ch.Gate.Selected = value; // all or none
            }
        }

        public override bool IsMouseOn(int x, int y)
        {
            return bounds.Contains(x, y);
        }

        public override void Draw(Graphics g)
        {
            // Draw children
            foreach (var ch in children)
                ch.Gate.Draw(g);

            // Optional: group outline for feedback
            if (selected)
            {
                using (var dash = new Pen(Color.White, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    g.DrawRectangle(dash, bounds);
            }
            // Do not call base.Draw; children already drew their pins
        }

        public override bool Evaluate() => false; // composite has no value itself

        public override Gate Clone()
        {
            // Partial-credit clone: deep-clone children (no wires), preserve relative offsets
            var copy = new Compound(left, top);

            foreach (var ch in children)
            {
                var gClone = ch.Gate.Clone();
                gClone.MoveTo(copy.left + ch.Dx, copy.top + ch.Dy);
                copy.AddExisting(gClone); // AddExisting unions pins and updates bounds
            }

            copy.Selected = false;
            return copy;
        }
    }
}
