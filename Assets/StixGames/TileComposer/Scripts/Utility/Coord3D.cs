using UnityEngine;

namespace StixGames.TileComposer
{
    internal struct Coord3D
    {
        // I'm going against my regular naming conventions here,
        // because holding shift for every single coordinate is annoying
        public int x, y, z;

        public static Coord3D Up => new Coord3D(0, 1, 0);
        public static Coord3D Down => new Coord3D(0, -1, 0);
        public static Coord3D Left => new Coord3D(-1, 0, 0);
        public static Coord3D Right => new Coord3D(1, 0, 0);
        public static Coord3D Forward => new Coord3D(0, 0, 1);
        public static Coord3D Back => new Coord3D(0, 0, -1);

        public static Coord3D Zero => new Coord3D(0, 0, 0);
        public static Coord3D One => new Coord3D(1, 1, 1);

        public Coord3D(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Coord3D operator +(Coord3D a, Coord3D b)
        {
            return new Coord3D(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Coord3D operator -(Coord3D a, Coord3D b)
        {
            return new Coord3D(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public Vector3 ScaleBy(Vector3 scale)
        {
            return new Vector3(x * scale.x, y * scale.y, z * scale.z);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public bool HasNegativeValue => x < 0 || y < 0 || z < 0;

        public override string ToString()
        {
            return $"({x},{y},{z})";
        }

        public bool OutOfRange(Coord3D coord3D)
        {
            return x >= coord3D.x || y >= coord3D.y || z >= coord3D.z;
        }

        public int[] ToArray()
        {
            return new[] {x, y, z};
        }

        public static Coord3D Max(Coord3D v0, Coord3D v1)
        {
            return new Coord3D(
                Mathf.Max(v0.x, v1.x),
                Mathf.Max(v0.y, v1.y),
                Mathf.Max(v0.z, v1.z));
        }
        
        public static Coord3D Min(Coord3D v0, Coord3D v1)
        {
            return new Coord3D(
                Mathf.Min(v0.x, v1.x),
                Mathf.Min(v0.y, v1.y),
                Mathf.Min(v0.z, v1.z));
        }
    }
}