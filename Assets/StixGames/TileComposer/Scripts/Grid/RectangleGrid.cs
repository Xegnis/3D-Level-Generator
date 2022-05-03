using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace StixGames.TileComposer
{
    public class RectangleGrid : IGrid
    {
        private readonly int width, height;
        private readonly Coord2D size;
        private readonly Vector2 scale;

        public RectangleGrid()
        {
            width = 1;
            height = 1;
            scale = Vector2.one;
        }

        public RectangleGrid([NotNull] float[] scale)
        {
            if (scale == null)
            {
                throw new ArgumentNullException(nameof(scale));
            }

            if (scale.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(scale));
            }

            width = 1;
            height = 1;
            size = new Coord2D(width, height);
            this.scale = new Vector3(scale[0], scale[1]);
        }

        public RectangleGrid([NotNull] int[] size, [NotNull] float[] scale)
        {
            if (size == null)
            {
                throw new ArgumentNullException(nameof(size));
            }

            if (scale == null)
            {
                throw new ArgumentNullException(nameof(scale));
            }

            if (size.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (scale.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(scale));
            }

            width = size[0];
            height = size[1];
            this.size = new Coord2D(width, height);
            this.scale = new Vector2(scale[0], scale[1]);
        }

        public int GridSize => width * height;
        public int Sides => 4;
        public string[] SideNames => new[] {"Left", "Right", "Up", "Down"};

        public int GetNeighbor(int current, int side)
        {
            var coord = IndexToCoord(current);
            var direction = SideToDirection(side);

            var index = CoordToIndex(coord + direction);

            if (index < 0 || index >= GridSize)
            {
                return -1;
            }
            else
            {
                return index;
            }
        }

        public int[] SliceToIndices(Slice[] slice, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, width);
                xEnd = MathExtensions.Modulo(slice[0].End, width);
                yStart = MathExtensions.Modulo(slice[1].Start, height);
                yEnd = MathExtensions.Modulo(slice[1].End, height);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, width - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, width - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, height - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, height - 1);
            }

            var localWidth = Mathf.Max(xEnd - xStart + 1, 0);
            var localHeight = Mathf.Max(yEnd - yStart + 1, 0);
            var indices = new int[localWidth * localHeight];
            var index = 0;
            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    indices[index] = CoordToIndex(x, y);
                    index++;
                }
            }

            return indices;
        }

        public bool[,] SliceBorderSides(Slice[] slice, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, width);
                xEnd = MathExtensions.Modulo(slice[0].End, width);
                yStart = MathExtensions.Modulo(slice[1].Start, height);
                yEnd = MathExtensions.Modulo(slice[1].End, height);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, width - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, width - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, height - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, height - 1);
            }

            var localWidth = Mathf.Max(xEnd - xStart + 1, 0);
            var localHeight = Mathf.Max(yEnd - yStart + 1, 0);

            var borders = new bool[localWidth * localHeight, Sides];

            for (int y = 0; y < localHeight; y++)
            {
                // Left
                borders[0 + y * localWidth, 0] = true;

                // Right
                borders[localWidth - 1 + y * localWidth, 1] = true;
            }

            for (int x = 0; x < localWidth; x++)
            {
                // Up
                borders[x + (localHeight - 1) * localWidth, 2] = true;

                // Down
                borders[x + 0 * localWidth, 3] = true;
            }

            return borders;
        }

        public int GetNeighborSide(int side)
        {
            switch (side)
            {
                case 0:
                    return 1;
                case 1:
                    return 0;
                case 2:
                    return 3;
                case 3:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 3");
            }
        }

        public Vector3 GetPosition(int index)
        {
            return IndexToCoord(index).ToVector2();
        }

        public Quaternion GetTileRotation(int index)
        {
            return Quaternion.identity;
        }

        public Vector3 GetSliceCenter(Slice[] slice, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, width);
                xEnd = MathExtensions.Modulo(slice[0].End, width);
                yStart = MathExtensions.Modulo(slice[1].Start, height);
                yEnd = MathExtensions.Modulo(slice[1].End, height);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, width - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, width - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, height - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, height - 1);
            }

            var center = new Vector3(xStart + xEnd, yStart + yEnd) * 0.5f;

            return Vector3.Scale(center, scale);
        }

        public Vector3 GetSliceBorderCenter(Slice[] slice, int axis, bool isPositive, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, width);
                xEnd = MathExtensions.Modulo(slice[0].End, width);
                yStart = MathExtensions.Modulo(slice[1].Start, height);
                yEnd = MathExtensions.Modulo(slice[1].End, height);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, width - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, width - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, height - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, height - 1);
            }

            var center = new Vector3(xStart + xEnd, yStart + yEnd) * 0.5f;

            Vector3 sideCenter;
            int side;
            switch (axis)
            {
                case 0:
                    // X Axis
                    if (isPositive)
                    {
                        // Right
                        sideCenter = new Vector3(xEnd, center.y);
                        side = 1;
                    }
                    else
                    {
                        // Left
                        sideCenter = new Vector3(xStart, center.y);
                        side = 0;
                    }

                    break;

                case 1:
                    // Y Axis
                    if (isPositive)
                    {
                        // Up
                        sideCenter = new Vector3(center.x, yEnd);
                        side = 2;
                    }
                    else
                    {
                        // Down
                        sideCenter = new Vector3(center.x, yStart);
                        side = 3;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 1");
            }

            var tileCenterToSide = SideToDirection(side).ScaleBy(scale) * 0.5f;
            return Vector3.Scale(sideCenter, scale) + tileCenterToSide;
        }

        public Vector3 GetSliceBorderNormal(Slice[] slice, int axis, bool isPositive, bool wrapAround = true)
        {
            Vector3 result;
            switch (axis)
            {
                case 0:
                    result = Vector3.right;
                    break;

                case 1:
                    result = Vector3.up;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 1");
            }

            return result * (isPositive ? 1 : -1);
        }

        public Vector3 GetSideCenter(int side)
        {
            return SideToDirection(side).ScaleBy(scale) * 0.5f;
        }

        public Vector3 GetSideNormal(int side)
        {
            return SideToDirection(side).ToVector2();
        }

        public bool IsSideVisible(int side, Vector3 localViewDir)
        {
            return true;
        }

        public Vector3[] GetSliceFrame(Slice[] slice, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            var localWidth = Mathf.Max(1, width);
            var localHeight = Mathf.Max(1, height);

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, localWidth);
                xEnd = MathExtensions.Modulo(slice[0].End, localWidth);
                yStart = MathExtensions.Modulo(slice[1].Start, localHeight);
                yEnd = MathExtensions.Modulo(slice[1].End, localHeight);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, localWidth - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, localWidth - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, localHeight - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, localHeight - 1);
            }

            xEnd += 1;
            yEnd += 1;

            var list = new List<Vector3>();
            for (int x = xStart; x <= xEnd; x++)
            {
                list.Add(new Vector3(x, yStart));
                list.Add(new Vector3(x, yEnd));
            }

            for (int y = yStart; y <= yEnd; y++)
            {
                list.Add(new Vector3(xStart, y));
                list.Add(new Vector3(xEnd, y));
            }

            return list.Select(x => Vector3.Scale(x - new Vector3(0.5f, 0.5f, 0.5f), scale)).ToArray();
        }

        public Mesh GetSlowGenerationMesh()
        {
            var vertices = new[]
            {
                new Vector3(0, 0),
                new Vector3(0, 1),
                new Vector3(1, 0),
                new Vector3(1, 1),
            }.Select(x => Vector3.Scale(x - new Vector3(0.5f, 0.5f, 0.5f), scale)).ToArray();
            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = new[]
                {
                    0, 1, 2,
                    1, 3, 2
                }
            };

            var maxLength = Mathf.Max(Vector3.Distance(vertices[0], vertices[1]),
                Vector3.Distance(vertices[0], vertices[2]));
            MeshUtility.CalculateBarycentricCoordinates(mesh, maxLength + 0.0001f);
            
            mesh.Optimize();
            mesh.RecalculateNormals();

            return mesh;
        }

        public int[] CalculateIndexOffset(Vector3 origin, Vector3 localOffset)
        {
            return new[]
            {
                (int) (localOffset.x / scale.x),
                (int) (localOffset.y / scale.y),
            };
        }

        public int CalculateSliceOffset(int axis, bool isPositive, Vector3 origin, Vector3 offset)
        {
            switch (axis)
            {
                case 0:
                    if (isPositive)
                    {
                        // Right
                        return (int) (offset.x / scale.x);
                    }
                    else
                    {
                        // Left
                        return (int) (offset.x / scale.x);
                    }
                case 1:
                    if (isPositive)
                    {
                        // Up
                        return (int) (offset.y / scale.y);
                    }
                    else
                    {
                        // Down
                        return (int) (offset.y / scale.y);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 1");
            }
        }

        public int RotationSteps(int axis)
        {
            return 4;
        }

        public int[] IndexToCoordinates(int index)
        {
            var coords = IndexToCoord(index);
            return coords.ToArray();
        }

        public int Axes => 2;
        public string[] AxisNames => new[] {"X", "Y"};

        public string[] RotationAxes => new[] {"Rotation"};

        public T[] RotateSideArray<T>(T[] array, int axis, int steps)
        {
            var a = array.ToArray();
            steps = steps % RotationSteps(axis);

            for (int i = 0; i < steps; i++)
            {
                // Save Down
                var temp = a[3];
                a[3] = a[1]; // Right to Down
                a[1] = a[2]; // Up to Right
                a[2] = a[0]; // Left to Up
                a[0] = temp; // Down to Left
            }

            return a;
        }

        public Quaternion GetRotation(int axis, int steps)
        {
            return Quaternion.Euler(0, 0, 90 * steps);
        }

        private int CoordToIndex(int x, int y)
        {
            return CoordToIndex(new Coord2D(x, y));
        }

        private int CoordToIndex(Coord2D coord)
        {
            if (coord.HasNegativeValue || coord.OutOfRange(size))
            {
                return -1;
            }

            return coord.x + coord.y * width;
        }

        private Coord2D IndexToCoord(int index)
        {
            int x = index % width;
            int y = index / width;

            return new Coord2D(x, y);
        }

        private Coord2D SideToDirection(int side)
        {
            switch (side)
            {
                case 0:
                    return Coord2D.Left;
                case 1:
                    return Coord2D.Right;
                case 2:
                    return Coord2D.Up;
                case 3:
                    return Coord2D.Down;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 3");
            }
        }
    }
}