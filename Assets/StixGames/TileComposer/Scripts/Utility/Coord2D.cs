using UnityEngine;

namespace StixGames.TileComposer
{
    internal class Coord2D
    {
        // I'm going against my regular naming conventions here,
        // because holding shift for every single coordinate is annoying
        public int x, y;

        public static Coord2D Up => new Coord2D(0, 1);
        public static Coord2D Down => new Coord2D(0, -1);
        public static Coord2D Left => new Coord2D(-1, 0);
        public static Coord2D Right => new Coord2D(1, 0);

        public Coord2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static Coord2D operator +(Coord2D a, Coord2D b)
        {
            return new Coord2D(a.x + b.x, a.y + b.y);
        }

        public static Coord2D operator -(Coord2D a, Coord2D b)
        {
            return new Coord2D(a.x - b.x, a.y - b.y);
        }

        public Vector3 ScaleBy(Vector2 scale)
        {
            return new Vector3(x * scale.x, y * scale.y);
        }

        public Vector2 ToVector2()
        {
            return new Vector3(x, y);
        }

        public bool HasNegativeValue => x < 0 || y < 0;

        public override string ToString()
        {
            return $"({x},{y})";
        }

        public bool OutOfRange(Coord2D coord2D)
        {
            return x >= coord2D.x || y >= coord2D.y;
        }

        public int[] ToArray()
        {
            return new[] {x, y};
        }
    }
}