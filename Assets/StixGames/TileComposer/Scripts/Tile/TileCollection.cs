using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace StixGames.TileComposer
{
    [AddComponentMenu("Stix Games/Tile Composer/Tile Collection")]
    public class TileCollection : MonoBehaviour
    {
        [Tooltip("Use examples to create tile neighbors")]
        public ExampleModel[] ExampleModels;

        [Header("Grid Settings")] public GridType GridType;
        public float[] GridScale;

        [Tooltip(
            "On a hexagon grid you can choose if you want to set the inner radius to 1, or the outer radius, depending on your tile set.")]
        public bool HexagonNormalizeInnerRadius = false;

        [Header("Tile Settings")] public EmptyTile[] EmptyTiles = {new EmptyTile("Empty", 1.0f)};
        public ConnectorType[] Connectors;

        [Header("Custom Properties")]
        [FormerlySerializedAs("CustomProperties")]  
        public string[] CustomValues;

        public string[] GetTileTypes(bool includeInactive = false)
        {
            return GetTiles(includeInactive).Select(x => x.GetOverridenTileType()).Distinct().ToArray();
        }

        public string[] GetAllTypes(bool includeInactive = false)
        {
            return EmptyTiles.Select(x => x.Name).Concat(GetTileTypes(includeInactive)).ToArray();
        }

        public IGrid DefaultGrid => GetDefaultGrid(GridType, GridScale);

        public static IGrid GetDefaultGrid(GridType gridType)
        {
            switch (gridType)
            {
                case GridType.Rectangle:
                    return new RectangleGrid();
                case GridType.Box:
                    return new BoxGrid();
                case GridType.Triangle:
                    return new TriangleGrid();
                case GridType.Prism:
                    return new PrismGrid();
                case GridType.Hexagon:
                    return new HexagonGrid(false);
                default:
                    throw new InvalidOperationException($"{nameof(gridType)} has an invalid type.");
            }
        }

        public IGrid GetDefaultGrid(GridType gridType, float[] gridScale)
        {
            switch (gridType)
            {
                case GridType.Rectangle:
                    return new RectangleGrid(gridScale);
                case GridType.Box:
                    return new BoxGrid(gridScale);
                case GridType.Triangle:
                    return new TriangleGrid(gridScale);
                case GridType.Prism:
                    return new PrismGrid(gridScale);
                case GridType.Hexagon:
                    return new HexagonGrid(HexagonNormalizeInnerRadius, gridScale);
                default:
                    throw new InvalidOperationException($"{nameof(gridType)} has an invalid type.");
            }
        }

        public IGrid GetGrid([NotNull] int[] size)
        {
            switch (GridType)
            {
                case GridType.Rectangle:
                    return new RectangleGrid(size, GridScale);
                case GridType.Box:
                    return new BoxGrid(size, GridScale);
                case GridType.Triangle:
                    return new TriangleGrid(size, GridScale);
                case GridType.Prism:
                    return new PrismGrid(size, GridScale);
                case GridType.Hexagon:
                    return new HexagonGrid(HexagonNormalizeInnerRadius, size, GridScale);
                default:
                    throw new InvalidOperationException($"{nameof(GridType)} has an invalid type.");
            }
        }

        public Tile[] GetTiles(bool includeInactive = false)
        {
            return GetComponentsInChildren<Tile>(includeInactive);
        }

        public TileVariation[] GetTileVariations(bool includeInactive = false)
        {
            // Just to be save
            UpdateTileTypeCount();

            var grid = DefaultGrid;
            var variations = new List<TileVariation>();

            var orderedTypes = GetAllTypes(includeInactive).OrderBy(x => x).ToArray();
            var tileToTileType = orderedTypes.Select((x, i) => new {x, i}).ToDictionary(x => x.x, x => x.i);
            var isEmptyType = orderedTypes.Select(x => EmptyTiles.Select(y => y.Name).Contains(x)).ToArray();

            for (var i = 0; i < EmptyTiles.Length; i++)
            {
                var emptyType = EmptyTiles[i];
                variations.Add(new TileVariation(tileToTileType[emptyType.Name], emptyType.Name, grid.Sides,
                    orderedTypes,
                    isEmptyType, Quaternion.identity, emptyType.Weight));
            }

            // Get all tiles
            var tiles = GetTiles(includeInactive);

            // Create a list of all connectors, (tileType, side) 
            var connectorInputs = new Dictionary<string, List<(int, int)>>();
            var connectorOutputs = new Dictionary<string, List<(int, int)>>();

            if (Connectors?.Length > 0)
            {
                var bidirectionalConnectors = new Dictionary<string, bool>();
                foreach (var connector in Connectors)
                {
                    bidirectionalConnectors[connector.Name] = connector.IsBidirectional;
                    connectorInputs[connector.Name] = new List<(int, int)>();
                    connectorOutputs[connector.Name] = new List<(int, int)>();
                }

                foreach (var tile in tiles)
                {
                    for (var side = 0; side < grid.Sides; side++)
                    {
                        // Get the connectors in the tile or its parents
                        var connectors = tile.GetCombinedConnectors(side);

                        foreach (var connector in connectors)
                        {
                            var tileType = tileToTileType[tile.GetOverridenTileType()];

                            // Bidirectional connectors should always be set to bidirectional,
                            // update here in case the type was changed
                            if (bidirectionalConnectors[connector.Name])
                            {
                                connector.ConnectionType = ConnectionType.Bidirectional;
                            }

                            if (connector.ConnectionType.HasFlag(ConnectionType.In))
                            {
                                connectorInputs[connector.Name].Add((tileType, side));
                            }

                            if (connector.ConnectionType.HasFlag(ConnectionType.Out))
                            {
                                connectorOutputs[connector.Name].Add((tileType, side));
                            }
                        }
                    }
                }
            }

            // A list of compressible empty sides (Empty type index, List<(tile index, side)>)
            var compressibleTileSides = new List<(int, List<(int, int)>)>();
            foreach (var compressible in EmptyTiles.Where(x => x.IsCompressible))
            {
                var emptySides = new List<(int, int)>();
                var emptyIndex = tileToTileType[compressible.Name];

                foreach (var tile in tiles)
                {
                    var neighborMatrices = tile.GetOverridenNeighborSides();
                    for (var side = 0; side < grid.Sides; side++)
                    {
                        // If the empty neighbor is allowed, add it to the list of sides
                        if (neighborMatrices[emptyIndex].Sides[side] != 0)
                        {
                            emptySides.Add((tileToTileType[tile.GetOverridenTileType()], side));
                        }
                    }
                }

                compressibleTileSides.Add((emptyIndex, emptySides));
            }

            foreach (var tile in tiles)
            {
                // Create a list for all tile variations, with the base tile as start
                var neighbors = tile.GetOverridenNeighborSides()
                    .Where(x => tileToTileType.ContainsKey(x.TileType))
                    .OrderBy(x => x.TileType).ToArray();

                // Add all tiles with the same connector as potential neighbors
                if (Connectors?.Length > 0)
                {
                    var currentTileType = tileToTileType[tile.GetOverridenTileType()];

                    for (var side = 0; side < grid.Sides; side++)
                    {
                        // Skip tiles where the connectors weren't properly generated
                        if (tile.Connectors == null || tile.Connectors.Length < grid.Sides)
                        {
                            continue;
                        }

                        // Use connectors to allow additional neighbors
                        var tileConnector = tile.GetCombinedConnectors(side);
                        foreach (var connector in tileConnector)
                        {
                            // Inputs and bidirectional are handled the same way, because bidirectional connections are in both lists anyways
                            var targetList = connector.ConnectionType == ConnectionType.Out
                                ? connectorInputs
                                : connectorOutputs;

                            // Ignore invalid connector values
                            if (!targetList.ContainsKey(connector.Name))
                            {
                                continue;
                            }

                            foreach (var (otherTileType, otherSide) in targetList[connector.Name])
                            {
                                if (!tile.CanNeighborSelf && otherTileType == currentTileType)
                                {
                                    continue;
                                }

                                neighbors[otherTileType].SetNeighborSideSupport(side, otherSide, true);
                            }
                        }
                    }
                }

                // Compress empty types
                foreach (var (emptyIndex, emptySides) in compressibleTileSides)
                {
                    for (int side = 0; side < grid.Sides; side++)
                    {
                        // Continue if there is no empty on this side
                        if (neighbors[emptyIndex].Sides[side] == 0)
                        {
                            continue;
                        }

                        // If the current tile is empty on this side,
                        // potentially allow its connection to all other side with this empty type
                        foreach (var (otherType, neighborsOriginalSide) in emptySides)
                        {
                            neighbors[otherType].SetNeighborSideSupport(side, neighborsOriginalSide, true);
                        }
                    }
                }

                // Create the original tile variation, without rotation
                var tileVariations = new List<TileVariation>
                {
                    new TileVariation(tileToTileType[tile.GetOverridenTileType()], tile, grid.Sides, neighbors)
                };

                // Iterate over all rotational axes
                foreach (var axis in tile.RotationAxes)
                {
                    // Save all current variations, all of them will be rotated together, to create new variations
                    var currentBase = tileVariations.ToList();

                    // Create variations for each rotation step
                    for (int i = 1; i < grid.RotationSteps(axis); i++)
                    {
                        foreach (var variation in currentBase)
                        {
                            tileVariations.Add(variation.CreateRotated(grid, axis, i));
                        }
                    }
                }

                // Now we have a mix of all different possible rotations, but also duplicates, so filter those, then add them to the final list
                var distinctVariations = tileVariations.Distinct().ToList();

                // Set the frequency for each tile variation
                double weight = tile.BaseWeight / distinctVariations.Count;
                foreach (var distinctVariation in distinctVariations)
                {
                    distinctVariation.VariationWeight = weight;
                }

                // Add the current tile variations to the list of all variations
                variations.AddRange(distinctVariations);
            }

            return variations.ToArray();
        }

        public void UpdateCustomPropertyCount()
        {
            UpdateCustomProperty(null, null);
        }

        public void UpdateCustomProperty(string original, string newName)
        {
            var tiles = GetTiles(true);

            foreach (var tile in tiles)
            {
                var properties = tile.CustomProperties.ToDictionary(x => x.Name, x => x);
                var newProperties = new CustomTileProperty[CustomValues.Length];

                for (var i = 0; i < CustomValues.Length; i++)
                {
                    var propertyName = CustomValues[i];

                    // If the changed property is found, replace the name
                    if (propertyName == original || propertyName == newName)
                    {
                        if (properties.ContainsKey(original))
                        {
                            var customTileProperty = properties[original];
                            customTileProperty.Name = newName;

                            newProperties[i] = customTileProperty;
                        }
                        else
                        {
                            newProperties[i] = new CustomTileProperty(newName);
                        }
                    }
                    else
                    {
                        if (properties.ContainsKey(propertyName))
                        {
                            newProperties[i] = properties[propertyName];
                        }
                        else
                        {
                            newProperties[i] = new CustomTileProperty(propertyName);
                        }
                    }
                }

                tile.CustomProperties = newProperties;
            }
        }

        public void UpdateConnector(string original, string newName)
        {
            var tiles = GetTiles(true);

            foreach (var tile in tiles)
            {
                foreach (var connectorSide in tile.Connectors)
                {
                    foreach (var connectorAssignment in connectorSide.Connectors)
                    {
                        if (connectorAssignment.Name == original)
                        {
                            connectorAssignment.Name = newName;
                        }
                    }
                }
            }
        }

        public void UpdateTileTypeCount()
        {
            UpdateTileTypes(null, null);
        }

        public void UpdateTileTypes(string original, string newValue)
        {
            var tiles = GetTiles(true);

            foreach (var tile in tiles)
            {
                var neighbors = new List<NeighborCompatibiltyMatrix>();
                foreach (var type in GetAllTypes(true))
                {
                    if (type == newValue || type == original)
                    {
                        // If the current type is the type that changed, find the original and rename it
                        var existingNeighbor = tile.Neighbors?.FirstOrDefault(x => x.TileType == original);
                        existingNeighbor?.SetSideCount(DefaultGrid.Sides);
                        neighbors.Add(existingNeighbor ?? new NeighborCompatibiltyMatrix(type, DefaultGrid.Sides));
                        neighbors[neighbors.Count - 1].TileType = newValue;
                    }
                    else
                    {
                        // For the other types, find the same or create a new compatibilty matrix
                        var existingNeighbor = tile.Neighbors?.FirstOrDefault(x => x.TileType == type);
                        existingNeighbor?.SetSideCount(DefaultGrid.Sides);
                        neighbors.Add(existingNeighbor ?? new NeighborCompatibiltyMatrix(type, DefaultGrid.Sides));
                    }
                }

                tile.Neighbors = neighbors.OrderBy(x => x.TileType).ToArray();
            }
        }

        public void UpdateNeighborCompatibility(Tile tile, IGrid grid)
        {
            // Ignore changes to sub tiles
            if (tile.BaseTile != null)
            {
                return;
            }

            var tiles = GetTiles(true).Where(x => x.BaseTile == null).ToList();

            if (tiles.Select(x => x.TileType).Distinct().Count() != tiles.Count)
            {
                Debug.LogError("You can't have multiple base tiles for the same tile type. " +
                               "Create one and base the second off the first. " +
                               "If the differences are large, create a new tile type instead.");
                return;
            }

            foreach (var otherTile in tiles)
            {
                var allowedSides = tile.Neighbors.Single(x => x.TileType == otherTile.TileType);

                for (var side = 0; side < grid.Sides; side++)
                {
                    var allowedNeighborSides = allowedSides.GetSideArray(side, grid.Sides);

                    var currentOnNeighbor = otherTile.Neighbors
                        .Select((x, i) => new {x, i})
                        .Single(x => x.x.TileType == tile.TileType).i;

                    for (int otherSide = 0; otherSide < grid.Sides; otherSide++)
                    {
                        otherTile.Neighbors[currentOnNeighbor]
                            .SetNeighborSideSupport(otherSide, side, allowedNeighborSides[otherSide]);
                    }
                }
            }
        }

        public void ClearNeighbors()
        {
            foreach (var tile in GetTiles(true))
            {
                foreach (var matrix in tile.Neighbors)
                {
                    for (var i = 0; i < matrix.Overrides.Length; i++)
                    {
                        matrix.Overrides[i] = false;
                    }

                    for (var i = 0; i < matrix.Sides.Length; i++)
                    {
                        matrix.Sides[i] = 0;
                    }
                }
            }
        }

        public void ClearConnectors()
        {
            foreach (var tile in GetTiles(true))
            {
                foreach (var connectorSide in tile.Connectors)
                {
                    connectorSide.Connectors = new ConnectorAssignment[0];
                }
            }
        }

        public void CalcNeighborCompatibilityFromExamples()
        {
            var tiles = GetTiles(true).Where(x => x.BaseTile == null).ToArray();
            var tileDictionary = tiles.ToDictionary(x => x.TileType, x => x);

            // Find all empty neighbors
            var emptyTypes = EmptyTiles.Select(x => x.Name).ToList();
            var emptyNeighborLookUp = new Dictionary<string, string[][]>();
            foreach (var tile in tiles)
            {
                var emptyTiles = tile
                    .Neighbors
                    .Where(x => emptyTypes.Contains(x.TileType)).ToArray();

                var lists = new string[DefaultGrid.Sides][];
                for (var side = 0; side < DefaultGrid.Sides; side++)
                {
                    lists[side] = emptyTiles
                        .Where(x => x.Sides[side] == ~0)
                        .Select(x => x.TileType)
                        .ToArray();
                }

                emptyNeighborLookUp[tile.TileType] = lists;
            }

            // Now clear all neighbors
            ClearNeighbors();

            // Look at the examples and allow neighbors accordingly
            foreach (var example in ExampleModels)
            {
                var grid = example.GetGrid();
                var model = example.GetExampleModel();

                for (var index = 0; index < grid.GridSize; index++)
                {
                    var currentResult = model[index];
                    if (!currentResult.HasValue)
                    {
                        continue;
                    }

                    var (currentType, currentSides) = currentResult.Value;

                    for (var side = 0; side < grid.Sides; side++)
                    {
                        var neighborIndex = grid.GetNeighbor(index, side);

                        // Skip tiles outside grid
                        if (neighborIndex < 0)
                        {
                            continue;
                        }


                        var otherResult = model[neighborIndex];
                        if (otherResult.HasValue)
                        {
                            var (otherType, otherSides) = otherResult.Value;

                            var otherSide = grid.GetNeighborSide(side);

                            // Allow the connection. We only need to do this on one side, the other one will also be visited later
                            var currentOriginalSide = currentSides[side];
                            var otherOriginalSide = otherSides[otherSide];
                            tileDictionary[currentType].Neighbors
                                .Single(x => x.TileType == otherType)
                                .SetNeighborSideSupport(currentOriginalSide, otherOriginalSide, true);
                        }
                        else
                        {
                            // This is an empty spot on the grid, allow all empties that were allowed before
                            var allowedEmpties = emptyNeighborLookUp[currentType][side];
                            foreach (var allowedEmpty in tileDictionary[currentType].Neighbors
                                .Where(x => allowedEmpties.Contains(x.TileType)))
                            {
                                allowedEmpty.Sides[side] = ~0;
                            }
                        }
                    }
                }
            }

            // Now set all sides that still have no possible neighbors to empty
            if (emptyTypes.Count > 0)
            {
                var firstEmpty = emptyTypes[0];
                foreach (var tile in tiles)
                {
                    for (var side = 0; side < DefaultGrid.Sides; side++)
                    {
                        // If there are no valid neighbor, fill it with the first empty type
                        if (tile.Neighbors.All(x => x.Sides[side] == 0))
                        {
                            tile.Neighbors.Single(x => x.TileType == firstEmpty).Sides[side] = ~0;
                        }
                    }
                }
            }
        }

        public void CalcNeighborCompatibilityFromMesh(float epsilon = 0.001f)
        {
            // Create a grid with a valid center point and at least one adjacent tile per direction
            var axes = Enumerable.Repeat(3, DefaultGrid.Axes).ToArray();
            var grid = GetGrid(axes);

            // Find the center index for later
            var centerCoordinates = Enumerable.Repeat(new Slice(1, 1), DefaultGrid.Axes).ToArray();
            var centerIndex = grid.SliceToIndices(centerCoordinates)[0];

            var tiles = GetTiles(true).Where(x => x.BaseTile == null).ToList();

            // Create a list of all lines on the border of the tiles
            var tileBorder = CreateTileBorderDict(tiles, grid, epsilon);

            // Reset all neighbors, but save empty types
            var emptyTypes = EmptyTiles.Select(x => x.Name).ToList();
            foreach (var neighbors in
                from tile in tiles
                from neighbors in tile.Neighbors
                where !emptyTypes.Contains(neighbors.TileType)
                select neighbors)
            {
                ClearSides(neighbors);
            }

            // Get all tile variations
            var tileVariations = GetTileVariations(true)
                .Where(x => x.Tile != null && x.Tile.BaseTile == null)
                .ToList();

            // Set the neighbor matrix for all tiles
            foreach (var tileVariation in tileVariations)
            {
                var current = tileBorder[tileVariation.Tile]
                    .Select(x => x.Select(y => y.Rotate(tileVariation.Rotation)).ToList()).ToList();

                foreach (var otherVariation in tileVariations)
                {
                    if (tileVariation.Tile != null &&
                        !tileVariation.Tile.CanNeighborSelf &&
                        tileVariation.Tile == otherVariation.Tile)
                    {
                        continue;
                    }

                    var other = tileBorder[otherVariation.Tile]
                        .Select(x => x.Select(y => y.Rotate(otherVariation.Rotation)).ToList()).ToList();

                    for (int side = 0; side < grid.Sides; side++)
                    {
                        var otherSide = grid.GetNeighborSide(side);
                        var originalSide = tileVariation.OriginalSides[side];
                        var otherOriginalSide = otherVariation.OriginalSides[otherSide];

                        // For comparisons, the vertices are actually on the wrong side
                        // e.g. the original vertices are on the left side of the model, but the neighboring sides are on the right side
                        // To fix this, I'm placing the vertices on an example grid and comparing them in their realistic positions
                        var currentPos = grid.GetPosition(centerIndex);
                        var currentRot = grid.GetTileRotation(centerIndex);

                        var neighborIndex = grid.GetNeighbor(centerIndex, side);
                        Assert.IsTrue(neighborIndex >= 0, "No neighbor should be outside the grid!");

                        var otherPos = grid.GetPosition(neighborIndex);
                        var otherRot = grid.GetTileRotation(neighborIndex);

                        var currentBorder = current[originalSide].Select(x => x.Transform(currentPos, currentRot))
                            .ToList();
                        var otherBorder = other[otherOriginalSide].Select(x => x.Transform(otherPos, otherRot))
                            .ToList();

                        if (currentBorder.Count == 0 || otherBorder.Count == 0)
                        {
                            continue;
                        }

                        // If all border vertices exist on the border of the other tile
                        var borderPlaneNormal = grid.GetSideNormal(side);
                        if (currentBorder.All(x =>
                                otherBorder.Exists(vertex =>
                                    AreVerticesCompatible(x, vertex, borderPlaneNormal, epsilon))) &&
                            otherBorder.All(x =>
                                currentBorder.Exists(vertex =>
                                    AreVerticesCompatible(x, vertex, borderPlaneNormal, epsilon))))
                        {
                            tileVariation.Tile.Neighbors[otherVariation.TileType]
                                .SetNeighborSideSupport(originalSide, otherOriginalSide, true);
                        }
                    }
                }

                // If the tile hasn't found a neighbor for a side, set it to empty
                if (EmptyTiles.Length > 0)
                {
                    var emptyTypeNames = EmptyTiles.Select(x => x.Name);
                    var emptyNeighbors = tileVariation.Tile.Neighbors.Where(x => emptyTypeNames.Contains(x.TileType))
                        .ToList();
                    var firstEmpty = emptyNeighbors.Single(x => x.TileType == EmptyTiles[0].Name);
                    var tileNeighbors = tileVariation.Tile.Neighbors.Where(x => !emptyTypeNames.Contains(x.TileType))
                        .ToList();

                    for (int side = 0; side < grid.Sides; side++)
                    {
                        var originalSide = tileVariation.OriginalSides[side];

                        if (tileNeighbors.All(x => x.Sides[originalSide] == 0))
                        {
                            // If no tile neighbors are set for the current side, check if any empties are set already
                            // If empties are set, leave them as is, otherwise set the first empty type
                            if (emptyNeighbors.All(x => x.Sides[originalSide] == 0))
                            {
                                // If no empty tiles are set for the current side, set the first empty
                                firstEmpty.Sides[originalSide] = ~0;
                            }
                        }
                        else
                        {
                            // There are active tile neighbors, remove empty types!
                            foreach (var emptyNeighbor in emptyNeighbors)
                            {
                                emptyNeighbor.Sides[originalSide] = 0;
                            }
                        }
                    }
                }
            }
        }

        private static bool AreVerticesCompatible(BorderVertex first, BorderVertex second, Vector3 borderPlaneNormal,
            float epsilon)
        {
            return first.IsPositionApproximately(second, epsilon) &&
                   first.AreNormalsOnSameSide(second, borderPlaneNormal, epsilon);
        }

        private static void ClearSides(NeighborCompatibiltyMatrix neighbors)
        {
            neighbors.Sides = Enumerable.Repeat(0, neighbors.Sides.Length).ToArray();
        }

        private Dictionary<Tile, List<BorderVertex>[]> CreateTileBorderDict(IEnumerable<Tile> tiles, IGrid grid,
            float epsilon = 0.05f)
        {
            var tileBorder = new Dictionary<Tile, List<BorderVertex>[]>();
            foreach (var tile in tiles)
            {
                // TODO: Fix this function when executed on a prefab
                // Set the tile position to be in the point of origin, simplifiying calculations later
                var tileTransform = tile.transform;
                var parent = tileTransform.parent;
                var position = tileTransform.position;
                var rotation = tileTransform.rotation;
                var scale = tileTransform.localScale;
                tileTransform.parent = null;
                tileTransform.position = Vector3.zero;
                tileTransform.rotation = Quaternion.identity;
                tileTransform.localScale = Vector3.one;

                // Collect all vertices in the object
                var allVertices = new List<BorderVertex>();
                var allTriangles = new List<int>();
                foreach (var meshFilter in tile.GetComponentsInChildren<MeshFilter>())
                {
                    var t = meshFilter.transform;
                    var mesh = Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;

                    var vertices = mesh.vertices;
                    var normals = mesh.normals;
                    var triangles = mesh.triangles;

                    allTriangles.AddRange(triangles.Select(x => x + allVertices.Count));
                    for (var i = 0; i < vertices.Length; i++)
                    {
                        allVertices.Add(new BorderVertex(t.TransformPoint(vertices[i]),
                            t.TransformDirection(normals[i])));
                    }
                }

                tileTransform.parent = parent;
                tileTransform.position = position;
                tileTransform.rotation = rotation;
                tileTransform.localScale = scale;

                var borders = new List<BorderVertex>[grid.Sides];
                for (int side = 0; side < grid.Sides; side++)
                {
                    var plane = grid.GetBorderPlane(side);
                    borders[side] = new List<BorderVertex>();

                    for (var i = 0; i < allTriangles.Count; i += 3)
                    {
                        // Only add vertices that are part of a triangle that lies on the border
                        var v0 = allVertices[allTriangles[i]];
                        var v1 = allVertices[allTriangles[i + 1]];
                        var v2 = allVertices[allTriangles[i + 2]];
                        var onPlane0 = PlaneInRange(plane, v0, epsilon);
                        var onPlane1 = PlaneInRange(plane, v1, epsilon);
                        var onPlane2 = PlaneInRange(plane, v2, epsilon);

                        if (onPlane0 && onPlane1 && onPlane2)
                        {
                            borders[side].Add(v0);
                            borders[side].Add(v1);
                            borders[side].Add(v2);
                        }
                        else if (onPlane0 && onPlane1)
                        {
                            borders[side].Add(v0);
                            borders[side].Add(v1);
                        }
                        else if (onPlane1 && onPlane2)
                        {
                            borders[side].Add(v1);
                            borders[side].Add(v2);
                        }
                        else if (onPlane0 && onPlane2)
                        {
                            borders[side].Add(v0);
                            borders[side].Add(v2);
                        }
                    }
                }

                tileBorder[tile] = borders;
            }

            return tileBorder;
        }

        private static bool PlaneInRange(Plane plane, BorderVertex vertex, float epsilon)
        {
            return Mathf.Abs(plane.GetDistanceToPoint(vertex.Position)) < epsilon;
        }

        private struct BorderVertex
        {
            public readonly Vector3 Position;
            public readonly Vector3 Normal;

            public BorderVertex(Vector3 position, Vector3 normal)
            {
                Position = position;
                Normal = normal;
            }

            public BorderVertex Rotate(Quaternion rotation)
            {
                return new BorderVertex(rotation * Position, rotation * Normal);
            }

            public bool IsApproximately(BorderVertex other, float epsilon = 0.001f)
            {
                var equals = IsPositionApproximately(other, epsilon);

                equals &= AreNormalsApproximately(other, epsilon);

                return equals;
            }

            private bool AreNormalsApproximately(BorderVertex other, float epsilon)
            {
                var equals = Approximately(Normal.x, other.Normal.x, epsilon);
                equals &= Approximately(Normal.y, other.Normal.y, epsilon);
                equals &= Approximately(Normal.z, other.Normal.z, epsilon);
                return equals;
            }

            public bool IsPositionApproximately(BorderVertex other, float epsilon)
            {
                var equals = Approximately(Position.x, other.Position.x, epsilon);
                equals &= Approximately(Position.y, other.Position.y, epsilon);
                equals &= Approximately(Position.z, other.Position.z, epsilon);
                return equals;
            }

            /// <summary>
            /// Returns if both normals have the same orientation, e.g. inside or outside of a model
            /// </summary>
            /// <param name="other"></param>
            /// <param name="planeNormal">A plane where the other vertex is from the positive side of the plane</param>
            /// <param name="epsilon"></param>
            /// <returns></returns>
            public bool AreNormalsOnSameSide(BorderVertex other, Vector3 planeNormal, float epsilon)
            {
                // If the normals are approximately the same, they are obviously on the same side
                if (AreNormalsApproximately(other, epsilon))
                {
                    return true;
                }

                // Get a vector that is normal to both normals
                // (this is always possible, as the normals must not be the same at this point)
                var up = Vector3.Cross(Normal, other.Normal);

                // If the normals are parallel, but not the same, they are not on the same side
                if (up.magnitude < epsilon)
                {
                    return false;
                }

                // Get a vector that is normal to the planeNormal and the up vector
                var separationNormal = Vector3.Cross(planeNormal, up);

                // If the plane normal and the up normal are parallel,
                // both normals are inside the plane, but not the same.
                // This is a bit of a strange case, so I'll assume that they are not on the same side.
                if (separationNormal.magnitude < epsilon)
                {
                    return false;
                }

                // If both normals are on the same side as the new separation plane,
                // they are oriented in the same direction.
                var separationPlane = new Plane(separationNormal, 0);
                return separationPlane.SameSide(Normal, other.Normal);
            }

            private static bool Approximately(float a, float b, float epsilon = 0.001f)
            {
                return Mathf.Abs(a - b) <= epsilon;
            }

            public BorderVertex MirrorPosition(IGrid grid, int side)
            {
                return new BorderVertex(grid.MirrorVector(side, Position), Normal);
            }

            public BorderVertex Transform(Vector3 position, Quaternion rotation)
            {
                return new BorderVertex(rotation * Position + position, rotation * Normal);
            }

            public override string ToString()
            {
                return $"{Position} -> {Normal}";
            }
        }
    }
}