using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace StixGames.TileComposer
{
    public class HexagonGrid : IGrid
    {
        private readonly int width, height, length;
        private readonly Coord3D size;
        private readonly Vector3 scale;

        private readonly float hexagonWidth;
        private readonly float hexagonHeight;

        public const int Left = 0;
        public const int FrontLeft = 1;
        public const int FrontRight = 2;
        public const int Right = 3;
        public const int BackRight = 4;
        public const int BackLeft = 5;
        public const int Up = 6;
        public const int Down = 7;

        private const int YRotationAxis = 0;
        private const int ZRotationAxis = 1;

        public HexagonGrid(bool normalizeInnerRadius) : this(normalizeInnerRadius, new[] {1.0f, 1.0f, 1.0f})
        {
        }

        public HexagonGrid(bool normalizeInnerRadius, [NotNull] float[] scale) : this(normalizeInnerRadius,
            new[] {1, 1, 1}, scale)
        {
        }

        public HexagonGrid(bool normalizeInnerRadius, [NotNull] int[] size, [NotNull] float[] scale)
        {
            if (size == null)
            {
                throw new ArgumentNullException(nameof(size));
            }

            if (scale == null)
            {
                throw new ArgumentNullException(nameof(scale));
            }

            if (size.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (scale.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(scale));
            }

            width = size[0];
            height = size[1];
            length = size[2];
            this.size = new Coord3D(width, height, length);
            this.scale = new Vector3(scale[0], scale[1], scale[2]);

            if (normalizeInnerRadius)
            {
                hexagonWidth = 1f;

                // 2 / Sqrt(3)
                hexagonHeight = 2f / 1.73205080757f;
            }
            else
            {
                // Sqrt(3) / 2
                hexagonWidth = 1.73205080757f / 2f;
                hexagonHeight = 1f;
            }
        }

        public int GridSize => width * height * length;
        public int Sides => 8;

        public string[] SideNames => new[]
            {"Left", "Front Left", "Front Right", "Right", "Back Right", "Back Left", "Up", "Down"};

        public int Axes => 3;
        public string[] AxisNames => new[] {"X", "Y", "Z"};
        public string[] RotationAxes => new[] {"Y", "Z"};
        public int RotationSteps(int axis) => axis == 0 ? 6 : 2;

        public int[] SliceToIndices(Slice[] slice, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            SliceToIndexRange(slice, out var start, out var end, out var extent, wrapAround);

            var indices = new int[extent.x * extent.y * extent.z];
            var index = 0;
            for (int z = start.z; z <= end.z; z++)
            {
                for (int y = start.y; y <= end.y; y++)
                {
                    for (int x = start.x; x <= end.x; x++)
                    {
                        indices[index] = CoordToIndex(x, y, z);
                        index++;
                    }
                }
            }

            return indices;
        }

        public int[] IndexToCoordinates(int index)
        {
            var coords = IndexToCoord(index);
            return coords.ToArray();
        }

        public int GetNeighbor(int index, int side)
        {
            var c = IndexToCoord(index);
            c += SideToDirection(c, side);
            var other = CoordToIndex(c);

            if (other < 0 || other >= GridSize)
            {
                return -1;
            }

            return other;
        }

        public int GetNeighborSide(int side)
        {
            switch (side)
            {
                case Left:
                    return Right;
                case FrontLeft:
                    return BackRight;
                case FrontRight:
                    return BackLeft;
                case Right:
                    return Left;
                case BackRight:
                    return FrontLeft;
                case BackLeft:
                    return FrontRight;
                case Up:
                    return Down;
                case Down:
                    return Up;

                default:
                    throw new ArgumentException("Side must be between 0 and 7");
            }
        }

        public T[] RotateSideArray<T>(T[] array, int axis, int steps)
        {
            // Make a copy
            var a = array.ToArray();

            // Full rotations around an axis are ignored
            steps = steps % RotationSteps(axis);

            switch (axis)
            {
                case YRotationAxis:
                    for (int i = 0; i < steps; i++)
                    {
                        var temp = a[Left];
                        a[Left] = a[BackLeft];
                        a[BackLeft] = a[BackRight];
                        a[BackRight] = a[Right];
                        a[Right] = a[FrontRight];
                        a[FrontRight] = a[FrontLeft];
                        a[FrontLeft] = temp;
                    }

                    break;

                case ZRotationAxis:
                    for (int i = 0; i < steps; i++)
                    {
                        var temp = a[Left];
                        a[Left] = a[Right];
                        a[Right] = temp;

                        temp = a[FrontLeft];
                        a[FrontLeft] = a[FrontRight];
                        a[FrontRight] = temp;

                        temp = a[BackLeft];
                        a[BackLeft] = a[BackRight];
                        a[BackRight] = temp;

                        temp = a[Up];
                        a[Up] = a[Down];
                        a[Down] = temp;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "must be between 0 and 1");
            }

            return a;
        }

        public Vector3 GetPosition(int index)
        {
            var c = IndexToCoord(index);

            // Hexagons have a width offset of their full width and a height offset of 3/4 of their height
            var pos = new Vector3(c.x * hexagonWidth, c.y, c.z * hexagonHeight * 0.75f);

            // On odd rows, the hexagons are offset
            if (IsOddRow(c))
            {
                pos.x += 0.5f * hexagonWidth;
            }

            return Vector3.Scale(pos, scale);
        }

        // The grid is perfectly regular, no tile rotation necessary
        public Quaternion GetTileRotation(int index) => Quaternion.identity;

        public Quaternion GetRotation(int axis, int steps)
        {
            switch (axis)
            {
                case YRotationAxis:
                    return Quaternion.Euler(0, 60 * steps, 0);

                case ZRotationAxis:
                    return Quaternion.Euler(0, 0, 180 * steps);

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "must be between 0 and 1");
            }
        }

        public Vector3 GetSideCenter(int side)
        {
            if (side == Up || side == Down)
            {
                // Take the side normal and half it
                return Vector3.Scale(GetSideNormal(side), scale / 2);
            }

            // Take the side normal and scale it to half of a hexagon width
            return Vector3.Scale(GetSideNormal(side), scale * (hexagonWidth / 2));
        }

        public Vector3 GetSideNormal(int side)
        {
            switch (side)
            {
                case Left:
                    return new Vector3(-1, 0, 0);
                case FrontLeft:
                    return new Vector3(-0.5f, 0, 0.75f * (hexagonHeight / hexagonWidth));
                case FrontRight:
                    return new Vector3(0.5f, 0, 0.75f * (hexagonHeight / hexagonWidth));
                case Right:
                    return new Vector3(1, 0, 0);
                case BackRight:
                    return new Vector3(0.5f, 0, -0.75f * (hexagonHeight / hexagonWidth));
                case BackLeft:
                    return new Vector3(-0.5f, 0, -0.75f * (hexagonHeight / hexagonWidth));
                case Up:
                    return new Vector3(0, 1, 0);
                case Down:
                    return new Vector3(0, -1, 0);

                default:
                    throw new ArgumentException("Side must be between 0 and 7");
            }
        }

        public Vector3[] GetSliceFrame(Slice[] slice, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            // Can't return a grid, if there's no grid
            if (width == 0 || length == 0 || height == 0)
            {
                return new Vector3[0];
            }

            SliceToIndexRange(slice, out var start, out var end, out _, wrapAround);

            var lines = new List<Vector3>();

            // TODO: Remove duplicate lines, I'm quite sure they're in there! 
            for (int z = start.z; z <= end.z; z++)
            {
                for (int x = start.x; x <= end.x; x++)
                {
                    var xOffset = z % 2 == 0 ? 0 : 0.5f;
                    var rowX = x + xOffset;

                    // Vertical lines / Top to bottom
                    lines.Add(new Vector3(hexagonWidth * (rowX - 0.5f), start.y - 0.5f,
                        hexagonHeight * (z * 0.75f - 0.25f)));
                    lines.Add(new Vector3(hexagonWidth * (rowX - 0.5f), end.y + 0.5f,
                        hexagonHeight * (z * 0.75f - 0.25f)));

                    lines.Add(new Vector3(hexagonWidth * rowX, start.y - 0.5f, hexagonHeight * (z * 0.75f - 0.5f)));
                    lines.Add(new Vector3(hexagonWidth * rowX, end.y + 0.5f, hexagonHeight * (z * 0.75f - 0.5f)));

                    if (x == start.x)
                    {
                        lines.Add(new Vector3(hexagonWidth * (rowX - 0.5f), start.y - 0.5f,
                            hexagonHeight * (z * 0.75f + 0.25f)));
                        lines.Add(new Vector3(hexagonWidth * (rowX - 0.5f), end.y + 0.5f,
                            hexagonHeight * (z * 0.75f + 0.25f)));
                    }

                    if (x == end.x)
                    {
                        lines.Add(new Vector3(hexagonWidth * (rowX + 0.5f), start.y - 0.5f,
                            hexagonHeight * (z * 0.75f - 0.25f)));
                        lines.Add(new Vector3(hexagonWidth * (rowX + 0.5f), end.y + 0.5f,
                            hexagonHeight * (z * 0.75f - 0.25f)));
                    }

                    if (x == end.x || z == end.z)
                    {
                        lines.Add(new Vector3(hexagonWidth * (rowX + 0.5f), start.y - 0.5f,
                            hexagonHeight * (z * 0.75f + 0.25f)));
                        lines.Add(new Vector3(hexagonWidth * (rowX + 0.5f), end.y + 0.5f,
                            hexagonHeight * (z * 0.75f + 0.25f)));
                    }

                    if (z == end.z)
                    {
                        lines.Add(new Vector3(hexagonWidth * rowX, start.y - 0.5f,
                            hexagonHeight * (z * 0.75f + 0.5f)));
                        lines.Add(new Vector3(hexagonWidth * rowX, end.y + 0.5f,
                            hexagonHeight * (z * 0.75f + 0.5f)));
                    }

                    for (int y = start.y; y <= end.y + 1; y++)
                    {
                        // Hexagon bottoms
                        // Left Back
                        lines.Add(new Vector3(hexagonWidth * rowX, y - 0.5f, hexagonHeight * (z * 0.75f - 0.5f)));
                        lines.Add(new Vector3(hexagonWidth * (rowX - 0.5f), y - 0.5f,
                            hexagonHeight * (z * 0.75f - 0.25f)));

                        // Left
                        lines.Add(new Vector3(hexagonWidth * (rowX - 0.5f), y - 0.5f,
                            hexagonHeight * (z * 0.75f - 0.25f)));
                        lines.Add(new Vector3(hexagonWidth * (rowX - 0.5f), y - 0.5f,
                            hexagonHeight * (z * 0.75f + 0.25f)));

                        // Left Front
                        lines.Add(new Vector3(hexagonWidth * (rowX - 0.5f), y - 0.5f,
                            hexagonHeight * (z * 0.75f + 0.25f)));
                        lines.Add(new Vector3(hexagonWidth * rowX, y - 0.5f,
                            hexagonHeight * (z * 0.75f + 0.5f)));

                        if (z == start.z)
                        {
                            // Right Back
                            lines.Add(new Vector3(hexagonWidth * (rowX + 0.5f), y - 0.5f,
                                hexagonHeight * (z * 0.75f - 0.25f)));
                            lines.Add(new Vector3(hexagonWidth * rowX, y - 0.5f, hexagonHeight * (z * 0.75f - 0.5f)));
                        }

                        if (x == end.x || z == end.z)
                        {
                            // Right Front
                            lines.Add(new Vector3(hexagonWidth * (rowX + 0.5f), y - 0.5f,
                                hexagonHeight * (z * 0.75f + 0.25f)));
                            lines.Add(new Vector3(hexagonWidth * rowX, y - 0.5f, hexagonHeight * (z * 0.75f + 0.5f)));
                        }

                        if (x == end.x)
                        {
                            // Right
                            lines.Add(new Vector3(hexagonWidth * (rowX + 0.5f), y - 0.5f,
                                hexagonHeight * (z * 0.75f - 0.25f)));
                            lines.Add(new Vector3(hexagonWidth * (rowX + 0.5f), y - 0.5f,
                                hexagonHeight * (z * 0.75f + 0.25f)));
                        }

                        if (x == end.x && z % 2 != 0)
                        {
                            // Right Back
                            lines.Add(new Vector3(hexagonWidth * (rowX + 0.5f), y - 0.5f,
                                hexagonHeight * (z * 0.75f - 0.25f)));
                            lines.Add(new Vector3(hexagonWidth * rowX, y - 0.5f,
                                hexagonHeight * (z * 0.75f - 0.5f)));
                        }
                    }
                }
            }

            return lines.Select(x => Vector3.Scale(x, scale)).ToArray();
        }

        public Vector3 GetSliceCenter(Slice[] slice, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            SliceToIndexRange(slice, out var start, out var end, out _, wrapAround);

            var startXOffset = IsOddRow(start) ? 0.5f : 0;
            var endXOffset = IsOddRow(end) ? 0.5f : 0;
            var xOffset = startXOffset + endXOffset;

            var center = new Vector3(
                             (start.x + end.x + xOffset) * hexagonWidth,
                             start.y + end.y,
                             (start.z + end.z) * 0.75f * hexagonHeight) * 0.5f;

            return Vector3.Scale(center, scale);
        }

        public Vector3 GetSliceBorderCenter(Slice[] slice, int axis, bool isPositive, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            SliceToIndexRange(slice, out var start, out var end, out _, wrapAround);

            var startXOffset = IsOddRow(start) ? 0.5f : 0;
            var endXOffset = IsOddRow(end) ? 0.5f : 0;
            var xOffset = new Vector3(startXOffset + endXOffset, 0, 0);
            var hexScale = new Vector3(hexagonWidth, 1, 0.75f * hexagonHeight);

            Vector3 sideCenter = ((start + end).ToVector3() + xOffset) * 0.5f;
            switch (axis)
            {
                case 0:
                    sideCenter.x = isPositive ? end.x + 0.5f : start.x - 0.5f;
                    break;
                case 1:
                    sideCenter.y = isPositive ? end.y + 0.5f : start.y - 0.5f;
                    break;
                case 2:
                    sideCenter.z = isPositive ? end.z + 0.5f : start.z - 0.5f;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 2");
            }

            return Vector3.Scale(Vector3.Scale(sideCenter, hexScale), scale);
        }

        public Vector3 GetSliceBorderNormal(Slice[] slice, int axis, bool isPositive, bool wrapAround = true)
        {
            Vector3 normal;
            switch (axis)
            {
                case 0:
                    normal = isPositive ? new Vector3(1, 0, 0) : new Vector3(-1, 0, 0);
                    break;
                case 1:
                    normal = isPositive ? new Vector3(0, 1, 0) : new Vector3(0, -1, 0);
                    break;
                case 2:
                    normal = isPositive ? new Vector3(0, 0, 1) : new Vector3(0, 0, -1);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 2");
            }

            return normal;
        }

        public int[] CalculateIndexOffset(Vector3 origin, Vector3 localOffset)
        {
            return new[]
            {
                (int) (localOffset.x / (scale.x * hexagonWidth * 0.5f)),
                (int) (localOffset.y / (scale.y)),
                (int) (localOffset.z / (scale.z * hexagonHeight * 0.75f)),
            };
        }

        public int CalculateSliceOffset(int axis, bool isPositive, Vector3 origin, Vector3 offset)
        {
            switch (axis)
            {
                case 0:
                    return (int) (offset.x / (scale.x * hexagonWidth));

                case 1:
                    return (int) (offset.y / scale.y);

                case 2:
                    return (int) (offset.z / (scale.z * hexagonHeight * 0.75f));

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 2");
            }
        }

        public Mesh GetSlowGenerationMesh()
        {
            var vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0.25f),
                    new Vector3(0, -0.5f, 0.5f),
                    new Vector3(0.5f, -0.5f, 0.25f),
                    new Vector3(0.5f, -0.5f, -0.25f),
                    new Vector3(0, -0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, -0.25f),

                    new Vector3(-0.5f, 0.5f, 0.25f),
                    new Vector3(0, 0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.25f),
                    new Vector3(0.5f, 0.5f, -0.25f),
                    new Vector3(0, 0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f, -0.25f),
                }.Select(x =>
                    Vector3.Scale(x, new Vector3(scale.x * hexagonWidth, scale.y, scale.z * hexagonHeight)))
                .ToArray();
            var triangles = new[]
            {
                0, 2, 1,
                5, 2, 0,
                5, 3, 2,
                5, 4, 3,

                6, 7, 8,
                11, 6, 8,
                11, 8, 9,
                11, 9, 10,

                0, 1, 7,
                0, 7, 6,
                1, 2, 8,
                1, 8, 7,
                2, 3, 9,
                2, 9, 8,
                3, 4, 10,
                3, 10, 9,
                4, 5, 11,
                4, 11, 10,
                5, 0, 6,
                5, 6, 11,
            };

            var splitVertices = new List<Vector3>();
            var splitTriangles = new List<int>();

            foreach (var t in triangles)
            {
                splitTriangles.Add(splitVertices.Count);
                splitVertices.Add(vertices[t]);
            }

            var mesh = new Mesh();
            mesh.SetVertices(splitVertices);
            mesh.SetTriangles(splitTriangles, 0);

            var maxLength = Mathf.Max(Vector3.Distance(vertices[0], vertices[1]),
                Vector3.Distance(vertices[1], vertices[2]),
                Vector3.Distance(vertices[0], vertices[2]),
                Vector3.Distance(vertices[0], vertices[3]));
            MeshUtility.CalculateBarycentricCoordinates(mesh, maxLength + 0.0001f);

            mesh.Optimize();
            mesh.RecalculateNormals();

            return mesh;
        }

        public bool IsSideVisible(int side, Vector3 localViewDir)
        {
            return Vector3.Dot(localViewDir, GetSideNormal(side)) > 0;
        }

        public bool[,] SliceBorderSides(Slice[] slice, bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            SliceToIndexRange(slice, out var start, out var end, out var extent, wrapAround);

            var borders = new bool[extent.x * extent.y * extent.z, Sides];

            // X axis border
            for (int z = start.z; z <= end.z; z++)
            {
                for (int y = start.y; y <= end.y; y++)
                {
                    var localY = y - start.y;
                    var localZ = z - start.z;

                    // Left
                    var leftIndex = 0 + localY * extent.x + localZ * extent.x * extent.y;
                    borders[leftIndex, Left] = true;

                    // Even rows
                    if (z % 2 == 0)
                    {
                        borders[leftIndex, BackLeft] = true;
                        borders[leftIndex, FrontLeft] = true;
                    }

                    // Right
                    var rightIndex = extent.x - 1 + localY * extent.x + localZ * extent.x * extent.y;
                    borders[rightIndex, Right] = true;

                    // Odd rows
                    if (z % 2 != 0)
                    {
                        borders[rightIndex, FrontRight] = true;
                        borders[rightIndex, BackRight] = true;
                    }
                }
            }

            // Y axis border
            for (int z = start.z; z <= end.z; z++)
            {
                for (int x = start.x; x <= end.x; x++)
                {
                    var localX = x - start.x;
                    var localZ = z - start.z;

                    // Up
                    var upIndex = localX + (extent.y - 1) * extent.x + localZ * extent.x * extent.y;
                    borders[upIndex, Up] = true;

                    // Down
                    var downIndex = localX + 0 * extent.x + localZ * extent.x * extent.y;
                    borders[downIndex, Down] = true;
                }
            }

            // Z axis border
            for (int y = start.y; y <= end.y; y++)
            {
                for (int x = start.x; x <= end.x; x++)
                {
                    var localX = x - start.x;
                    var localY = y - start.y;

                    // Forward
                    var frontIndex = localX + localY * extent.x + (extent.z - 1) * extent.x * extent.y;
                    borders[frontIndex, FrontLeft] = true;
                    borders[frontIndex, FrontRight] = true;

                    // Back
                    var backIndex = localX + localY * extent.x + 0 * extent.x * extent.y;
                    borders[backIndex, BackLeft] = true;
                    borders[backIndex, BackRight] = true;
                }
            }

            return borders;
        }

        private int CoordToIndex(int x, int y, int z)
        {
            return CoordToIndex(new Coord3D(x, y, z));
        }

        private int CoordToIndex(Coord3D coord)
        {
            if (coord.HasNegativeValue || coord.OutOfRange(size))
            {
                return -1;
            }

            return coord.x + coord.y * width + coord.z * width * height;
        }

        private Coord3D IndexToCoord(int index)
        {
            int x = index % width;
            int y = (index % (width * height)) / width;
            int z = index / (width * height);

            return new Coord3D(x, y, z);
        }

        private Coord3D SideToDirection(Coord3D c, int side)
        {
            if (IsOddRow(c))
            {
                switch (side)
                {
                    case Left:
                        return new Coord3D(-1, 0, 0);
                    case FrontLeft:
                        return new Coord3D(0, 0, 1);
                    case FrontRight:
                        return new Coord3D(1, 0, 1);
                    case Right:
                        return new Coord3D(1, 0, 0);
                    case BackRight:
                        return new Coord3D(1, 0, -1);
                    case BackLeft:
                        return new Coord3D(0, 0, -1);
                    case Up:
                        return new Coord3D(0, 1, 0);
                    case Down:
                        return new Coord3D(0, -1, 0);

                    default:
                        throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 7");
                }
            }
            else
            {
                switch (side)
                {
                    case Left:
                        return new Coord3D(-1, 0, 0);
                    case FrontLeft:
                        return new Coord3D(-1, 0, 1);
                    case FrontRight:
                        return new Coord3D(0, 0, 1);
                    case Right:
                        return new Coord3D(1, 0, 0);
                    case BackRight:
                        return new Coord3D(0, 0, -1);
                    case BackLeft:
                        return new Coord3D(-1, 0, -1);
                    case Up:
                        return new Coord3D(0, 1, 0);
                    case Down:
                        return new Coord3D(0, -1, 0);

                    default:
                        throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 7");
                }
            }
        }

        /// <summary>
        /// Takes a slice and returns coordinates such that the box containing both start and end are exactly the slice
        /// and extent is exactly the count of rows/columns per coordinate.
        /// </summary>
        /// <param name="slice"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="extent"></param>
        /// <param name="wrapAround"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void SliceToIndexRange([NotNull] Slice[] slice, out Coord3D start, out Coord3D end, out Coord3D extent,
            bool wrapAround = true)
        {
            if (slice == null)
            {
                throw new ArgumentNullException(nameof(slice));
            }

            if (slice.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            if (wrapAround)
            {
                start.x = MathExtensions.Modulo(slice[0].Start, width);
                end.x = MathExtensions.Modulo(slice[0].End, width);
                start.y = MathExtensions.Modulo(slice[1].Start, height);
                end.y = MathExtensions.Modulo(slice[1].End, height);
                start.z = MathExtensions.Modulo(slice[2].Start, length);
                end.z = MathExtensions.Modulo(slice[2].End, length);
            }
            else
            {
                start.x = Mathf.Clamp(slice[0].Start, 0, width - 1);
                end.x = Mathf.Clamp(slice[0].End, 0, width - 1);
                start.y = Mathf.Clamp(slice[1].Start, 0, height - 1);
                end.y = Mathf.Clamp(slice[1].End, 0, height - 1);
                start.z = Mathf.Clamp(slice[2].Start, 0, length - 1);
                end.z = Mathf.Clamp(slice[2].End, 0, length - 1);
            }

            // I honestly don't remember why the Max part was necessary.
            // I think it was something about index overflows?
            extent = Coord3D.Max(end - start + Coord3D.One, Coord3D.Zero);
        }

        private bool IsOddRow(Coord3D c)
        {
            return c.z % 2 != 0;
        }
    }
}