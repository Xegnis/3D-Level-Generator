using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace StixGames.TileComposer
{
    public class PrismGrid : IGrid
    {
        private readonly int width, height, length;
        private readonly Coord3D size;
        private readonly Vector3 scale;

        /// <summary>
        /// The height of a equilateral triangle
        /// </summary>
        private const float TriangleHeight = 0.86602540378f;

        public PrismGrid() : this(new[] {1.0f, 1.0f, 1.0f})
        {
        }

        public PrismGrid([NotNull] float[] scale) : this(new[] {1, 1, 1}, scale)
        {
        }

        public PrismGrid([NotNull] int[] size, [NotNull] float[] scale)
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
        }

        public int GridSize => width * height * length;
        public int Sides => 5;

        // Every second row has different meanings: Forward, Right, Left, Up Down
        public string[] SideNames => new[] {"Back", "Left", "Right", "Up", "Down"};

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

            if (slice.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;
            int zStart;
            int zEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, width);
                xEnd = MathExtensions.Modulo(slice[0].End, width);
                yStart = MathExtensions.Modulo(slice[1].Start, height);
                yEnd = MathExtensions.Modulo(slice[1].End, height);
                zStart = MathExtensions.Modulo(slice[2].Start, length);
                zEnd = MathExtensions.Modulo(slice[2].End, length);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, width - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, width - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, height - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, height - 1);
                zStart = Mathf.Clamp(slice[2].Start, 0, length - 1);
                zEnd = Mathf.Clamp(slice[2].End, 0, length - 1);
            }

            var localWidth = Mathf.Max(xEnd - xStart + 1, 0);
            var localHeight = Mathf.Max(yEnd - yStart + 1, 0);
            var localLength = Mathf.Max(zEnd - zStart + 1, 0);
            var indices = new int[localWidth * localHeight * localLength];
            var index = 0;
            for (int z = zStart; z <= zEnd; z++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    for (int x = xStart; x <= xEnd; x++)
                    {
                        indices[index] = CoordToIndex(x, y, z);
                        index++;
                    }
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

            if (slice.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;
            int zStart;
            int zEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, width);
                xEnd = MathExtensions.Modulo(slice[0].End, width);
                yStart = MathExtensions.Modulo(slice[1].Start, height);
                yEnd = MathExtensions.Modulo(slice[1].End, height);
                zStart = MathExtensions.Modulo(slice[2].Start, length);
                zEnd = MathExtensions.Modulo(slice[2].End, length);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, width - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, width - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, height - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, height - 1);
                zStart = Mathf.Clamp(slice[2].Start, 0, length - 1);
                zEnd = Mathf.Clamp(slice[2].End, 0, length - 1);
            }

            var localWidth = Mathf.Max(xEnd - xStart + 1, 0);
            var localHeight = Mathf.Max(yEnd - yStart + 1, 0);
            var localLength = Mathf.Max(zEnd - zStart + 1, 0);

            var borders = new bool[localWidth * localHeight * localLength, Sides];

            // X axis borders
            for (int z = zStart; z <= zEnd; z++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    var localY = y - yStart;
                    var localZ = z - zStart;

                    // Left
                    var leftIndex = 0 + localY * localWidth + localZ * localHeight * localWidth;
                    var leftSide = IsReversedTriangle(xStart, y, z) ? 2 : 1;
                    borders[leftIndex, leftSide] = true;

                    // Right
                    var rightIndex = localWidth - 1 + localY * localWidth + localZ * localHeight * localWidth;
                    var rightSide = IsReversedTriangle(xEnd, y, z) ? 1 : 2;
                    borders[rightIndex, rightSide] = true;
                }
            }

            // Y axis borders
            for (int z = zStart; z <= zEnd; z++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    var localX = x - xStart;
                    var localZ = z - zStart;

                    // Up
                    var upIndex = localX + (localHeight - 1) * localWidth + localZ * localHeight * localWidth;
                    borders[upIndex, 3] = true;

                    // Down
                    var downIndex = localX + 0 * localWidth + localZ * localHeight * localWidth;
                    borders[downIndex, 4] = true;
                }
            }

            // Z axis borders
            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    var localX = x - xStart;
                    var localY = y - yStart;

                    // Forward
                    var upIndex = localX + localY * localWidth + (localLength - 1) * localHeight * localWidth;
                    if (IsReversedTriangle(x, y, zEnd))
                    {
                        borders[upIndex, 0] = true;
                    }

                    // Back
                    var downIndex = localX + localY * localWidth + 0 * localHeight * localWidth;
                    if (!IsReversedTriangle(x, y, zStart))
                    {
                        borders[downIndex, 0] = true;
                    }
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

                case 3:
                    // Up
                    return 4;

                case 4:
                    // Down
                    return 3;

                default:
                    throw new ArgumentException("Side must be between 0 and 4");
            }
        }

        public Vector3 GetPosition(int index)
        {
            var coords = IndexToCoord(index);
            var x = coords.x * 0.5f;
            var y = coords.y;
            var rowZ = coords.z * TriangleHeight;
            var localZ = IsReversedTriangle(index) ? TriangleHeight * 0.333333f : 0;

            return Vector3.Scale(new Vector3(x, y, rowZ + localZ), scale);
        }

        public Quaternion GetTileRotation(int index)
        {
            if (IsReversedTriangle(index))
            {
                return Quaternion.Euler(0, 180, 0);
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

            if (slice.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slice));
            }

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;
            int zStart;
            int zEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, width);
                xEnd = MathExtensions.Modulo(slice[0].End, width);
                yStart = MathExtensions.Modulo(slice[1].Start, height);
                yEnd = MathExtensions.Modulo(slice[1].End, height);
                zStart = MathExtensions.Modulo(slice[2].Start, length);
                zEnd = MathExtensions.Modulo(slice[2].End, length);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, width - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, width - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, height - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, height - 1);
                zStart = Mathf.Clamp(slice[2].Start, 0, length - 1);
                zEnd = Mathf.Clamp(slice[2].End, 0, length - 1);
            }

            var center = new Vector3((xStart + xEnd) * 0.5f, yStart + yEnd, (zStart + zEnd) * TriangleHeight) * 0.5f;

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

            var localWidth = Mathf.Max(1, width);
            var localHeight = Mathf.Max(1, height);
            var localLength = Mathf.Max(1, length);

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;
            int zStart;
            int zEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, localWidth);
                xEnd = MathExtensions.Modulo(slice[0].End, localWidth);
                yStart = MathExtensions.Modulo(slice[1].Start, localHeight);
                yEnd = MathExtensions.Modulo(slice[1].End, localHeight);
                zStart = MathExtensions.Modulo(slice[2].Start, localLength);
                zEnd = MathExtensions.Modulo(slice[2].End, localLength);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, localWidth - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, localWidth - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, localHeight - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, localHeight - 1);
                zStart = Mathf.Clamp(slice[2].Start, 0, localLength - 1);
                zEnd = Mathf.Clamp(slice[2].End, 0, localLength - 1);
            }

            var center = new Vector3((xStart + xEnd) * 0.5f, yStart + yEnd,
                             (zStart + zEnd + 0.333333f) * TriangleHeight) * 0.5f;

            Vector3 sideCenter = center;
            switch (axis)
            {
                case 0:
                    // X Axis
                    if (isPositive)
                    {
                        // Right
                        sideCenter.x = xEnd * 0.5f + 0.5f;
                    }
                    else
                    {
                        // Left
                        sideCenter.x = xStart * 0.5f - 0.5f;
                    }

                    break;

                case 1:
                    // Y Axis
                    if (isPositive)
                    {
                        // Right
                        sideCenter.y = yStart - 0.5f;
                    }
                    else
                    {
                        // Left
                        sideCenter.y = yEnd + 0.5f;
                    }

                    break;

                case 2:
                    // Z Axis
                    if (isPositive)
                    {
                        // Forward
                        var offset = IsEvenRow(zEnd) ? 0.666667f * TriangleHeight : 0.333333f * TriangleHeight;
                        sideCenter.z = zEnd * TriangleHeight + offset;
                    }
                    else
                    {
                        // Back
                        var offset = IsEvenRow(zStart) ? 0.333333f * TriangleHeight : 0.666667f * TriangleHeight;
                        sideCenter.z = zStart * TriangleHeight - offset;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 2");
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

                case 2:
                    result = Vector3.forward;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 2");
            }

            return result * (isPositive ? 1 : -1);
        }

        public Vector3 GetSideCenter(int side)
        {
            if (side == 3 || side == 4)
            {
                return Vector3.Scale(GetSideNormal(side), scale / 2);
            }

            return Vector3.Scale(GetSideNormal(side), (TriangleHeight / 3) * scale);
        }

        public Vector3 GetSideNormal(int side)
        {
            Vector3 result;
            switch (side)
            {
                // Back
                case 0:
                    result = new Vector3(0, 0, -1);
                    break;

                // Left
                case 1:
                    result = new Vector3(-TriangleHeight, 0, 0.5f).normalized;
                    break;

                // Right
                case 2:
                    result = new Vector3(TriangleHeight, 0, 0.5f).normalized;
                    break;

                // Up
                case 3:
                    result = new Vector3(0, 1, 0);
                    break;

                // Down
                case 4:
                    result = new Vector3(0, -1, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 5");
            }

            return result;
        }

        public bool IsSideVisible(int side, Vector3 localViewDir)
        {
            return Vector3.Dot(localViewDir, GetSideNormal(side)) > 0;
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

            var localWidth = Mathf.Max(1, width);
            var localHeight = Mathf.Max(1, height);
            var localLength = Mathf.Max(1, length);

            int xStart;
            int xEnd;
            int yStart;
            int yEnd;
            int zStart;
            int zEnd;

            if (wrapAround)
            {
                xStart = MathExtensions.Modulo(slice[0].Start, localWidth);
                xEnd = MathExtensions.Modulo(slice[0].End, localWidth);
                yStart = MathExtensions.Modulo(slice[1].Start, localHeight);
                yEnd = MathExtensions.Modulo(slice[1].End, localHeight);
                zStart = MathExtensions.Modulo(slice[2].Start, localLength);
                zEnd = MathExtensions.Modulo(slice[2].End, localLength);
            }
            else
            {
                xStart = Mathf.Clamp(slice[0].Start, 0, localWidth - 1);
                xEnd = Mathf.Clamp(slice[0].End, 0, localWidth - 1);
                yStart = Mathf.Clamp(slice[1].Start, 0, localHeight - 1);
                yEnd = Mathf.Clamp(slice[1].End, 0, localHeight - 1);
                zStart = Mathf.Clamp(slice[2].Start, 0, localLength - 1);
                zEnd = Mathf.Clamp(slice[2].End, 0, localLength - 1);
            }

            var list = new List<Vector3>();
            for (int z = zStart; z <= zEnd; z++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    for (int x = xStart; x <= xEnd; x++)
                    {
                        // TODO: Don't draw prisms independently
                        if (IsReversedTriangle(x, y, z))
                        {
                            // Forward Line
                            if (y == yStart)
                            {
                                list.Add(new Vector3(x * 0.5f, y, z + 1));
                                list.Add(new Vector3(x * 0.5f + 1, y, z + 1));
                            }

                            list.Add(new Vector3(x * 0.5f, y + 1, z + 1));
                            list.Add(new Vector3(x * 0.5f + 1, y + 1, z + 1));

                            if (x == xStart)
                            {
                                // Left Line
                                if (y == yStart)
                                {
                                    list.Add(new Vector3(x * 0.5f, y, z + 1));
                                    list.Add(new Vector3((x + 1) * 0.5f, y, z));
                                }

                                list.Add(new Vector3(x * 0.5f, y + 1, z + 1));
                                list.Add(new Vector3((x + 1) * 0.5f, y + 1, z));
                            }

                            // Right Line
                            if (y == yStart)
                            {
                                list.Add(new Vector3(x * 0.5f + 1, y, z + 1));
                                list.Add(new Vector3((x + 1) * 0.5f, y, z));
                            }

                            list.Add(new Vector3(x * 0.5f + 1, y + 1, z + 1));
                            list.Add(new Vector3((x + 1) * 0.5f, y + 1, z));

                            // Back vertical line
                            list.Add(new Vector3(x * 0.5f, y, z + 1));
                            list.Add(new Vector3(x * 0.5f, y + 1, z + 1));

                            if (y == yEnd && x == xStart)
                            {
                                // Left vertical line
                                list.Add(new Vector3(x * 0.5f, y, z + 1));
                                list.Add(new Vector3(x * 0.5f, y + 1, z + 1));
                            }

                            // Right vertical line
                            list.Add(new Vector3(x * 0.5f + 1, y, z + 1));
                            list.Add(new Vector3(x * 0.5f + 1, y + 1, z + 1));
                        }
                        else
                        {
                            if (z == zStart)
                            {
                                // Back Line
                                if (y == yStart)
                                {
                                    list.Add(new Vector3(x * 0.5f, y, z));
                                    list.Add(new Vector3(x * 0.5f + 1, y, z));
                                }

                                list.Add(new Vector3(x * 0.5f, y + 1, z));
                                list.Add(new Vector3(x * 0.5f + 1, y + 1, z));
                            }

                            if (x == xStart)
                            {
                                // Left Line
                                if (y == yStart)
                                {
                                    list.Add(new Vector3((x + 1) * 0.5f, y, z + 1));
                                    list.Add(new Vector3(x * 0.5f, y, z));
                                }

                                list.Add(new Vector3((x + 1) * 0.5f, y + 1, z + 1));
                                list.Add(new Vector3(x * 0.5f, y + 1, z));
                            }

                            // Right Line
                            if (y == yStart)
                            {
                                list.Add(new Vector3((x + 1) * 0.5f, y, z + 1));
                                list.Add(new Vector3(x * 0.5f + 1, y, z));
                            }

                            list.Add(new Vector3((x + 1) * 0.5f, y + 1, z + 1));
                            list.Add(new Vector3(x * 0.5f + 1, y + 1, z));

                            // Forward vertical line
                            if (x == 0 && xEnd == 0 && z == 0 && zEnd == 0)
                            {
                                list.Add(new Vector3((x + 1) * 0.5f, y, z + 1));
                                list.Add(new Vector3((x + 1) * 0.5f, y + 1, z + 1));
                            }

                            if (z == zStart)
                            {
                                // Left vertical line
                                list.Add(new Vector3(x * 0.5f, y, z));
                                list.Add(new Vector3(x * 0.5f, y + 1, z));

                                // Right vertical line
                                list.Add(new Vector3(x * 0.5f + 1, y, z));
                                list.Add(new Vector3(x * 0.5f + 1, y + 1, z));
                            }
                        }
                    }
                }
            }

            return list.Select(x =>
                    Vector3.Scale(x - new Vector3(0.5f, 0.5f, 0.333333f),
                        new Vector3(scale.x, scale.y, scale.z * TriangleHeight)))
                .ToArray();
        }

        public Mesh GetSlowGenerationMesh()
        {
            var vertices = new[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(0.5f, 0, 1),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0.5f, 1, 1),
                    new Vector3(1, 1, 0),
                }.Select(x =>
                    Vector3.Scale(x - new Vector3(0.5f, 0.5f, 0.333333f),
                        new Vector3(scale.x, scale.y, scale.z * TriangleHeight)))
                .ToArray();
            var triangles = new[]
            {
                0, 2, 1,
                3, 4, 5,
                0, 3, 2,
                2, 3, 5,
                0, 1, 3,
                4, 3, 1,
                2, 5, 1,
                5, 4, 1
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

        public int[] CalculateIndexOffset(Vector3 origin, Vector3 localOffset)
        {
            return new[]
            {
                (int) (localOffset.x / (scale.x * 0.5f)),
                (int) (localOffset.y / (scale.y * 1.0f)),
                (int) (localOffset.z / (scale.z * TriangleHeight)),
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
                        return (int) (offset.y / scale.y);
                    }
                    else
                    {
                        // Down
                        return (int) (offset.y / scale.y);
                    }

                case 2:
                    if (isPositive)
                    {
                        // Forward
                        return (int) (offset.z / (scale.z * TriangleHeight));
                    }
                    else
                    {
                        // Back
                        return (int) (offset.z / (scale.z * TriangleHeight));
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 2");
            }
        }

        public int[] IndexToCoordinates(int index)
        {
            var coords = IndexToCoord(index);
            return coords.ToArray();
        }

        public int Axes => 3;

        public string[] AxisNames => new[] {"X", "Y", "Z"};

        public string[] RotationAxes => new[] {"Y", "Z"};

        public int RotationSteps(int axis)
        {
            return axis == 0 ? 3 : 2;
        }

        public T[] RotateSideArray<T>(T[] array, int axis, int steps)
        {
            var a = array.ToArray();
            steps = steps % RotationSteps(axis);

            switch (axis)
            {
                case 0:
                    // Y axis
                    for (int i = 0; i < steps; i++)
                    {
                        // Save Right
                        var temp = a[2];
                        a[2] = a[1]; // Left to right
                        a[1] = a[0]; // Bottom to left
                        a[0] = temp; // Right to bottom
                    }

                    break;

                case 1:
                    // Z axis
                    for (int i = 0; i < steps; i++)
                    {
                        var temp = a[3]; // Save Up
                        a[3] = a[4]; // Down to up
                        a[4] = temp; // Up to down

                        temp = a[1]; // Save left
                        a[1] = a[2]; // Right to left
                        a[2] = temp; // Left to right
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "must be between 0 and 1");
            }

            return a;
        }

        public Quaternion GetRotation(int axis, int steps)
        {
            switch (axis)
            {
                case 0:
                    return Quaternion.Euler(0, 120 * steps, 0);

                case 1:
                    return Quaternion.Euler(0, 0, 180 * steps);

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "must be between 0 and 1");
            }
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

        private Coord3D SideToDirection(int index, int side)
        {
            Coord3D direction;

            if (IsReversedTriangle(index))
            {
                switch (side)
                {
                    case 0:
                        // Top
                        direction = new Coord3D(0, 0, 1);
                        break;
                    case 1:
                        // Right
                        direction = new Coord3D(1, 0, 0);
                        break;
                    case 2:
                        // Left
                        direction = new Coord3D(-1, 0, 0);
                        break;

                    case 3:
                        // Up
                        direction = new Coord3D(0, 1, 0);
                        break;

                    case 4:
                        // Down
                        direction = new Coord3D(0, -1, 0);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 4");
                }
            }
            else
            {
                switch (side)
                {
                    case 0:
                        // Bottom
                        direction = new Coord3D(0, 0, -1);
                        break;
                    case 1:
                        // Left
                        direction = new Coord3D(-1, 0, 0);
                        break;
                    case 2:
                        // Right
                        direction = new Coord3D(1, 0, 0);
                        break;
                    case 3:
                        // Up
                        direction = new Coord3D(0, 1, 0);
                        break;

                    case 4:
                        // Down
                        direction = new Coord3D(0, -1, 0);
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

        private bool IsReversedTriangle(int x, int y, int z)
        {
            if (z % 2 == 0)
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

        private bool IsEvenRow(int index) => (index / (width * height)) % 2 == 0;
    }
}