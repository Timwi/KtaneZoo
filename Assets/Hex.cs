using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zoo
{
    public struct Hex : IEquatable<Hex>
    {
        public static IEnumerable<Hex> LargeHexagon(int sideLength)
        {
            for (int r = -sideLength + 1; r < sideLength; r++)
                for (int q = -sideLength + 1; q < sideLength; q++)
                {
                    var hex = new Hex(q, r);
                    if (hex.Distance < sideLength)
                        yield return hex;
                }
        }
        public static readonly float WidthToHeight = Mathf.Sqrt(3) / 2;

        public static double LargeWidth(int sideLength) { return (3 * sideLength - 1) * .5; }
        public static double LargeHeight(int sideLength) { return (2 * sideLength - 1) * WidthToHeight; }

        public int Q { get; private set; }
        public int R { get; private set; }

        public Hex[] Neighbors
        {
            get
            {
                return Ut.NewArray(
                    new Hex(Q - 1, R),
                    new Hex(Q, R - 1),
                    new Hex(Q + 1, R - 1),
                    new Hex(Q + 1, R),
                    new Hex(Q, R + 1),
                    new Hex(Q - 1, R + 1));
            }
        }

        public static Hex GetDirection(int dir)
        {
            switch (dir)
            {
                case 0: return new Hex(-1, 0);
                case 1: return new Hex(0, -1);
                case 2: return new Hex(1, -1);
                case 3: return new Hex(1, 0);
                case 4: return new Hex(0, 1);
                case 5: return new Hex(-1, 1);
            }
            throw new ArgumentException("Invalid direction. Direction must be 0–5.", "dir");
        }

        public int Distance { get { return Math.Max(Math.Abs(Q), Math.Max(Math.Abs(R), Math.Abs(-Q - R))); } }

        public IEnumerable<int> GetEdges(int size)
        {
            // Don’t use ‘else’ because multiple conditions could apply
            if (Q + R == -size)
                yield return 0;
            if (R == -size)
                yield return 1;
            if (Q == size)
                yield return 2;
            if (Q + R == size)
                yield return 3;
            if (R == size)
                yield return 4;
            if (Q == -size)
                yield return 5;
        }

        public float GetCenterX(float hexWidth) { return Q * .75f * hexWidth; }
        public float GetCenterY(float hexWidth) { return (Q * .5f + R) * hexWidth * WidthToHeight; }

        public override string ToString() { return string.Format("({0}, {1})", Q, R); }

        public Hex(int q, int r) : this() { Q = q; R = r; }

        public bool Equals(Hex other) { return Q == other.Q && R == other.R; }
        public override bool Equals(object obj) { return obj is Hex && Equals((Hex) obj); }
        public static bool operator ==(Hex one, Hex two) { return one.Q == two.Q && one.R == two.R; }
        public static bool operator !=(Hex one, Hex two) { return one.Q != two.Q || one.R != two.R; }
        public override int GetHashCode() { return Q * 47 + R; }

        public static Hex operator +(Hex one, Hex two) { return new Hex(one.Q + two.Q, one.R + two.R); }
        public static Hex operator -(Hex one, Hex two) { return new Hex(one.Q - two.Q, one.R - two.R); }
        public static Hex operator *(Hex hex, int mult) { return new Hex(hex.Q * mult, hex.R * mult); }
        public static Hex operator *(int mult, Hex hex) { return new Hex(hex.Q * mult, hex.R * mult); }

        public Hex Rotate(int rotation)
        {
            switch (((rotation % 6) + 6) % 6)
            {
                case 0: return this;
                case 1: return new Hex(-R, Q + R);
                case 2: return new Hex(-Q - R, Q);
                case 3: return new Hex(-Q, -R);
                case 4: return new Hex(R, -Q - R);
                case 5: return new Hex(Q + R, -Q);
            }
            throw new ArgumentException("Rotation must be between 0 and 5.", "rotation");
        }

        public Hex Mirror(bool doMirror)
        {
            if (!doMirror)
                return this;
            return new Hex(Q, -R - Q);
        }
    }
}
