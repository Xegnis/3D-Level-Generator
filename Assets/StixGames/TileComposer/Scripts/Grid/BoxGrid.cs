using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace StixGames.TileComposer
{
    public class BoxGrid : IGrid
    {
        private readonly int width, height, length;
        private readonly Coord3D size;
        private readonly Vector3 scale;

        public BoxGrid([NotNull] float[] scale)
        {
            if (scale == null)
            {
                throw new ArgumentNullException(nameof(scale));
            }

            if (scale.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(scale));
            }

            width = 1;
            height = 1;
            length = 1;
            size = new Coord3D(width, height, length);
            this.scale = new Vector3(scale[0], scale[1], scale[2]);
        }

        public BoxGrid([NotNull] int[] size, [NotNull] float[] scale)
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

        public BoxGrid()
        {
            width = 1;
            height = 1;
            length = 1;
            size = new Coord3D(width, height, length);
            scale = Vector3.one;
        }

        public int GridSize => width * height * length;
        public int Sides => 6;
        public string[] SideNames => new[] {"Left", "Right", "Forward", "Back", "Up", "Down"};

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
                xStart = Mathf.Clamp(slice[0].Start, 0, width-1);
                xEnd = Mathf.Clamp(slice[0].End, 0, width-1);
                yStart = Mathf.Clamp(slice[1].Start, 0, height-1);
                yEnd = Mathf.Clamp(slice[1].End, 0, height-1);
                zStart = Mathf.Clamp(slice[2].Start, 0, length-1);
                zEnd = Mathf.Clamp(slice[2].End, 0, length-1);
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

            for (int z = 0; z < localLength; z++)
            {
                for (int y = 0; y < localHeight; y++)
                {
                    // Left
                    borders[0 + y * localWidth + z * localWidth * localHeight, 0] = true;

                    // Right
                    borders[localWidth-1 + y * localWidth + z * localWidth * localHeight, 1] = true;
                }
            }

            for (int y = 0; y < localHeight; y++)
            {
                for (int x = 0; x < localWidth; x++)
                {
                    // Back
                    borders[x + y * localWidth + 0 * localWidth * localHeight, 3] = true;

                    // Forward
                    borders[x + y * localWidth + (localLength-1) * localWidth * localHeight, 2] = true;
                }
            }

            for (int z = 0; z < localLength; z++)
            {
                for (int x = 0; x < localWidth; x++)
                {
                    // Down
                    borders[x + 0 * localWidth + z * localWidth * localHeight, 5] = true;

                    // Up
                    borders[x + (localHeight-1) * localWidth + z * localWidth * localHeight, 4] = true;
                }
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
                case 4:
                    return 5;
                case 5:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 5");
            }
        }

        public Vector3 GetPosition(int index)
        {
            var coord = IndexToCoord(index);

            return coord.ScaleBy(scale);
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

            var center = new Vector3(xStart + xEnd, yStart + yEnd, zStart + zEnd) * 0.5f;

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

            var center = new Vector3(xStart + xEnd, yStart + yEnd, zStart + zEnd) * 0.5f;

            Vector3 sideCenter;
            int side;
            
            switch (axis)
            {
                case 0:
                    // X Axis
                    if (isPositive)
                    {
                        // Right
                        sideCenter = new Vector3(xEnd, center.y, center.z);
                        side = 1;
                    }
                    else
                    {
                        // Left
                        sideCenter = new Vector3(xStart, center.y, center.z);
                        side = 0;
                    }
                    break;
                
                case 1:
                    // Y Axis
                    if (isPositive)
                    {
                        // Up
                        sideCenter = new Vector3(center.x, yEnd, center.z);
                        side = 4;
                    }
                    else
                    {
                        // Down
                        sideCenter = new Vector3(center.x, yStart, center.z);
                        side = 5;
                    }
                    break;
                
                case 2:
                    // Z Axis
                    if (isPositive)
                    {
                        // Forward
                        sideCenter = new Vector3(center.x, center.y, zEnd);
                        side = 2;
                    }
                    else
                    {
                        // Back
                        sideCenter = new Vector3(center.x, center.y, zStart);
                        side = 3;
                    }
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 2");
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
                
                case 2:
                    result = Vector3.forward;
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
            return SideToDirection(side).ToVector3();
        }

        public bool IsSideVisible(int side, Vector3 viewDir)
        {
            return Vector3.Dot(viewDir, GetSideNormal(side)) > 0;
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

            xEnd += 1;
            yEnd += 1;
            zEnd += 1;

            var list = new List<Vector3>();
            for (int y = yStart; y <= yEnd; y++)
            {
                for (int z = zStart; z <= zEnd; z++)
                {
                    list.Add(new Vector3(xStart, y, z));
                    list.Add(new Vector3(xEnd, y, z));
                }
            }
            for (int x = xStart; x <= xEnd; x++)
            {
                for (int z = zStart; z <= zEnd; z++)
                {
                    list.Add(new Vector3(x, yStart, z));
                    list.Add(new Vector3(x, yEnd, z));
                }
            }
            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    list.Add(new Vector3(x, y, zStart));
                    list.Add(new Vector3(x, y, zEnd));
                }
            }

            return list.Select(x => Vector3.Scale(x - new Vector3(0.5f, 0.5f, 0.5f), scale)).ToArray();
        }

        public Mesh GetSlowGenerationMesh()
        {
            var vertices = new[]
            {
                new Vector3 (0, 0, 0),
                new Vector3 (1, 0, 0),
                new Vector3 (1, 1, 0),
                new Vector3 (0, 1, 0),
                new Vector3 (0, 1, 1),
                new Vector3 (1, 1, 1),
                new Vector3 (1, 0, 1),
                new Vector3 (0, 0, 1),
            }.Select(x => Vector3.Scale(x - new Vector3(0.5f, 0.5f, 0.5f), scale)).ToArray();
            var triangles = new[]
            {
                0, 2, 1, //face front
                0, 3, 2,
                2, 3, 4, //face top
                2, 4, 5,
                1, 2, 5, //face right
                1, 5, 6,
                0, 7, 4, //face left
                0, 4, 3,
                5, 4, 7, //face back
                5, 7, 6,
                0, 6, 7, //face bottom
                0, 1, 6
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
                Vector3.Distance(vertices[0], vertices[3]),
                Vector3.Distance(vertices[0], vertices[7]));
            MeshUtility.CalculateBarycentricCoordinates(mesh, maxLength + 0.0001f);
            
            mesh.Optimize ();
            mesh.RecalculateNormals ();
            
            return mesh;
        }

        public int[] CalculateIndexOffset(Vector3 origin, Vector3 localOffset)
        {
            return new[]
            {
                (int) (localOffset.x / scale.x),
                (int) (localOffset.y / scale.y),
                (int) (localOffset.z / scale.z),
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
                        return (int)(offset.x / scale.x);
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
                        return (int)(offset.y / scale.y);
                    }
                    else
                    {
                        // Down
                        return (int)(offset.y / scale.y);
                    }
                    
                case 2:
                    if (isPositive)
                    {
                        // Forward
                        return (int)(offset.z / scale.z);
                    }
                    else
                    {
                        // Back
                        return(int)(offset.z / scale.z);
                    }
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Must be between 0 and 5");
            }
        }

        public int RotationSteps(int axis)
        {
            return 4;
        }

        public string[] RotationAxes => new[] {"X", "Y", "Z"};

        public int[] IndexToCoordinates(int index)
        {
            var coords = IndexToCoord(index);
            return new[] {coords.x, coords.y, coords.z};
        }

        public int Axes => 3;
        public string[] AxisNames => new[] {"X", "Y", "Z"};

        public T[] RotateSideArray<T>(T[] array, int axis, int steps)
        {
            var a = array.ToArray();
            steps = steps % RotationSteps(axis);

            switch (axis)
            {
                case 0:
                {
                    // X axis
                    for (int i = 0; i < steps; i++)
                    {
                        // Save forward
                        var temp = a[2];
                        a[2] = a[4]; // Up to Forward
                        a[4] = a[3]; // Back to Up
                        a[3] = a[5]; // Down to Back
                        a[5] = temp; // Forward to Down
                    }

                    break;
                }
                case 1:
                {
                    // Y axis
                    for (int i = 0; i < steps; i++)
                    {
                        // Save back
                        var temp = a[3];
                        a[3] = a[1]; // Right to Back
                        a[1] = a[2]; // Forward to Right
                        a[2] = a[0]; // Left to Forward
                        a[0] = temp; // Back to Left
                    }

                    break;
                }
                case 2:
                {
                    // Z axis
                    for (int i = 0; i < steps; i++)
                    {
                        // Save Left
                        var temp = a[0];
                        a[0] = a[4]; // Up to Left
                        a[4] = a[1]; // Right to Up
                        a[1] = a[5]; // Down to Right
                        a[5] = temp; // Left to Down
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis));
            }

            return a;
        }

        public Quaternion GetRotation(int axis, int steps)
        {
            switch (axis)
            {
                case 0:
                    return Quaternion.Euler(90 * steps, 0, 0);
                case 1:
                    return Quaternion.Euler(0, 90 * steps, 0);
                case 2:
                    return Quaternion.Euler(0, 0, 90 * steps);
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis));
            }
        }

        private Coord3D SideToDirection(int side)
        {
            switch (side)
            {
                case 0:
                    return Coord3D.Left;
                case 1:
                    return Coord3D.Right;
                case 2:
                    return Coord3D.Forward;
                case 3:
                    return Coord3D.Back;
                case 4:
                    return Coord3D.Up;
                case 5:
                    return Coord3D.Down;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Must be between 0 and 5");
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
    }
}