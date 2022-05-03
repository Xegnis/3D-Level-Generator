using UnityEngine;

namespace StixGames.TileComposer
{
    public static class MathExtensions
    {
        public static int Modulo(int x, int m)
        {
            return (x % m + m) % m;
        }

        public static Ray TransformRay(this Transform t, Ray ray)
        {
            return new Ray(t.TransformPoint(ray.origin), t.TransformDirection(ray.direction));
        }

        public static Plane ToPlane(this Ray ray)
        {
            return new Plane(ray.direction, ray.origin);
        }
    }
}
