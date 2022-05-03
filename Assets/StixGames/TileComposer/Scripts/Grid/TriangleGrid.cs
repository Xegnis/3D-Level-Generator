using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace StixGames.TileComposer
{
    public class TriangleGrid : IGrid
    {
        private readonly int width, height;
        private readonly Coord2D size;
        private readonly Vector2 scale;

        /// <summary>
        /// The height of a equilateral triangle
        /// </summary>
        private const float TriangleHeight = 0.86602540378f;

        public TriangleGrid() : this(new[] {1.0f, 1.0f})
        {
        }

        public TriangleGrid([NotNull] float[] scale) : this(new[] {1, 1}, scale)
        {
        }

        public TriangleGrid([NotNull] int[] size, [NotNull] float[] scale)
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
        public int Sides => 3;

        // Every second row has different meanings: Top, Right, Left
        public string[] SideNames => new[] {"Bottom", "Left", "Right"};

        public int GetNeighbor(int current, int side)
        {
            var coord = IndexToCoord(current);
            coord += SideToDirection(current, side);
            var index = CoordToIndex(coord);

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

            for (int y = yStart; y <= yEnd; y++)
            {
                // Left
                var leftIndex = 0 + (y - yStart) * localWidth;
                var leftSide = IsReversedTriangle(xStart, y) ? 2 : 1;
                borders[leftIndex, leftSide] = true;

                // Right
                var rightIndex = localWidth - 1 + (y - yStart) * localWidth;
                var rightSide = IsReversedTriangle(xEnd, y) ? 1 : 2;
                borders[rightIndex, rightSide] = true;
            }

            for (int x = xStart; x <= xEnd; x++)
            {
                // Up
                var upIndex = x - xStart + (localHeight - 1) * localWidth;
                if (IsReversedTriangle(x, yEnd))
                {
                    borders[upIndex, 0] = true;
                }

                // Down
                var downIndex = x - xStart + 0 * localWidth;
                if (!IsReversedTriangle(x, yStart))
                {
                    borders[downIndex, 0] = true;
                }
            }

            return borders;
        }

        public int GetNeighborSide(int side)
        {
            switch (side)
            {
                case 0:
                    // Bottom / Top
                    return 0;

                case 1:
                    // TopLeft / BottomRight
                    return 1;

                case 2:
                    // TopRight / BottomLeft
                    return 2;

                default:
                    throw new ArgumentException("Side must be between 0 and 2");
            }
        }

        public Vector3 GetPosition(int index)
        {
            var coords = IndexToCoord(index);
            var x = coords.x * 0.5f;
            var rowY = coords.y * TriangleHeight;
            var localY = IsReversedTriangle(index) ? TriangleHeight * 0.333333f : 0;

            return Vector3.Scale(new Vector3(x, rowY + localY), scale);
        }

        public Quaternion GetTileRotation(int index)
        {
            if (IsReversedTriangle(index))
            {
                return Quaternion.Euler(0, 0, 180);
            }
            else
            {
                return Quaternion.identity;
            }
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

            var center = new Vector3((xStart + xEnd) * 0.5f, (yStart + yEnd) * TriangleHeight) * 0.5f;

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

            var center = new Vector3((xStart + xEnd) * 0.5f, (yStart + yEnd + 0.333333f) * TriangleHeight) * 0.5f;

            Vector3 sideCenter;
            switch (axis)
            {
                case 0:
                    // X Axis
                    if (isPositive)
                    {
                        // Right
                        sideCenter = new Vector3(xEnd * 0.5f + 0.5f, center.y);
                    }
                    else
                    {
                        // Left
                        sideCenter = new Vector3(xStart * 0.5f - 0.5f, center.y);
                    }

                    break;

                case 1:
                    // Y Axis
                    if (isPositive)
                    {
                        // Up
                        var offset = IsEvenRow(yEnd) ? 0.666667f * TriangleHeight : 0.333333f * TriangleHeight;
                        sideCenter = new Vector3(center.x, yEnd * TriangleHeight + offset);
                    }
                    else
                    {
                        // Down
                        var offset = IsEvenRow(yStart) ? 0.333333f * TriangleHeight : 0.666667f * TriangleHeight;
                        sideCenter = new Vector3(center.x, yStart * TriangleHeight - offset);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 1");
            }

            return Vector3.Scale(sideCenter, scale);
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
            return Vector3.Scale(SideToDirection(0, side).ToVector2(), scale * 0.5f);
        }

        public Vector3 GetSideNormal(int side)
        {
            return SideToDirection(0, side).ToVector2();
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

            for (int y = yStart; y < yEnd; y++)
            {
                // TODO: Don't draw triangles independently
                for (int x = xStart; x < xEnd; x++)
                {
                    if (IsReversedTriangle(x, y))
                    {
                        // Top Line
                        list.Add(new Vector3(x * 0.5f, y + 1));
                        list.Add(new Vector3(x * 0.5f + 1, y + 1));

                        if (x == xStart)
                        {
                            // Left Line
                            list.Add(new Vector3(x * 0.5f, y + 1));
                            list.Add(new Vector3((x + 1) * 0.5f, y));
                        }

                        // Right Line
                        list.Add(new Vector3(x * 0.5f + 1, y + 1));
                        list.Add(new Vector3((x + 1) * 0.5f, y));
                    }
                    else
                    {
                        if (y == yStart)
                        {
                            // Bottom Line
                            list.Add(new Vector3(x * 0.5f, y));
                            list.Add(new Vector3(x * 0.5f + 1, y));
                        }

                        if (x == xStart)
                        {
                            // Left Line
                            list.Add(new Vector3((x + 1) * 0.5f, y + 1));
                            list.Add(new Vector3(x * 0.5f, y));
                        }

                        // Right Line
                        list.Add(new Vector3((x + 1) * 0.5f, y + 1));
                        list.Add(new Vector3(x * 0.5f + 1, y));
                    }
                }
            }

            return list.Select(x =>
                    Vector3.Scale(x - new Vector3(0.5f, 0.333333f), new Vector3(scale.x, scale.y * TriangleHeight, 1)))
                .ToArray();
        }

        public Mesh GetSlowGenerationMesh()
        {
            var vertices = new[]
                {
                    new Vector3(0, 0),
                    new Vector3(0.5f, 1),
                    new Vector3(1, 0),
                }.Select(x =>
                    Vector3.Scale(x - new Vector3(0.5f, 0.333333f), new Vector3(scale.x, scale.y * TriangleHeight, 1)))
                .ToArray();
            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = new[] {0, 1, 2}
            };

            MeshUtility.CalculateBarycentricCoordinates(mesh);

            mesh.Optimize();
            mesh.RecalculateNormals();

            return mesh;
        }

        public int[] CalculateIndexOffset(Vector3 origin, Vector3 localOffset)
        {
            return new[]
            {
                (int) (localOffset.x / (scale.x * 0.5f)),
                (int) (localOffset.y / (scale.y * TriangleHeight)),
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
                        return (int) (offset.x / (scale.x * 0.5f));
                    }
                    else
                    {
                        // Left
                        return (int) (offset.x / (scale.x * 0.5f));
                    }

                case 1:
                    if (isPositive)
                    {
                        // Up
                        return (int) (offset.y / (scale.y * TriangleHeight));
                    }
                    else
                    {
                        // Down
                        return (int) (offset.y / (scale.y * TriangleHeight));
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 1");
            }
        }

        public int RotationSteps(int axis)
        {
            return 3;
        }

        public string[] RotationAxes => new[] {"Rotation"};

        public int[] IndexToCoordinates(int index)
        {
            var coords = IndexToCoord(index);
            return coords.ToArray();
        }

        public int Axes => 2;
        public string[] AxisNames => new[] {"X", "Y"};

        public T[] RotateSideArray<T>(T[] array, int axis, int steps)
        {
            var a = array.ToArray();
            steps = steps % RotationSteps(axis);

            for (int i = 0; i < steps; i++)
            {
                // Save Right
                var temp = a[2];
                a[2] = a[1]; // Left to right
                a[1] = a[0]; // Bottom to left
                a[0] = temp; // Right to bottom
            }

            return a;
        }

        public Quaternion GetRotation(int axis, int steps)
        {
            return Quaternion.Euler(0, 0, -120 * steps);
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

        private Coord2D SideToDirection(int index, int side)
        {
            Coord2D direction;

            if (IsReversedTriangle(index))
            {
                switch (side)
                {
                    case 0:
                        // Top
                        direction = new Coord2D(0, 1);

                        break;
                    case 1:
                        // Right
                        direction = new Coord2D(1, 0);
                        break;
                    case 2:
                        // Left
                        direction = new Coord2D(-1, 0);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 2");
                }
            }
            else
            {
                switch (side)
                {
                    case 0:
                        // Bottom
                        direction = new Coord2D(0, -1);
                        break;
                    case 1:
                        // Left
                        direction = new Coord2D(-1, 0);
                        break;
                    case 2:
                        // Right
                        direction = new Coord2D(1, 0);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 2");
                }
            }

            return direction;
        }

        private bool IsReversedTriangle(int index)
        {
            if (IsEvenRow(index))
            {
                // In even rows, every odd tile is reversed
                return (index % width) % 2 == 1;
            }
            else
            {
                // In odd rows, every even triangle is reversed
                return (index % width) % 2 == 0;
            }
        }

        private bool IsReversedTriangle(int x, int y)
        {
            if (y % 2 == 0)
            {
                // In even rows, every odd tile is reversed
                return x % 2 == 1;
            }
            else
            {
                // In odd rows, every even triangle is reversed
                return x % 2 == 0;
            }
        }

        private bool IsEvenRow(int index) => (index / width) % 2 == 0;
    }
}