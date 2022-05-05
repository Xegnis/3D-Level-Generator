using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEditor;

namespace StixGames.TileComposer
{
    public static class DataSanitizer
    {
        public static void SanitizeTileCollection([NotNull] TileCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            // Add undo
            /*Undo.RecordObject(collection, "Tile Collection Changes");
            Undo.RecordObjects(collection.GetTiles(true), "Tile Collection Changes");*/
            
            
            if (!Enum.IsDefined(typeof(GridType), collection.GridType))
            {
                collection.GridType = default;
            }

            var grid = collection.DefaultGrid;

            if (collection.GridScale == null)
            {
                collection.GridScale = Enumerable.Repeat(1f, grid.Axes).ToArray();
            }

            if (collection.GridScale.Length != grid.Axes)
            {
                Array.Resize(ref collection.GridScale, grid.Axes);
            }

            // Empty tiles
            if (collection.EmptyTiles == null)
            {
                collection.EmptyTiles = new EmptyTile[0];
            }

            collection.EmptyTiles = collection.EmptyTiles.Where(x => x != null).ToArray();

            // Make sure names are unique
            var emptyNames = collection.EmptyTiles.Select(x => string.IsNullOrWhiteSpace(x.Name) ? "Empty" : x.Name)
                .ToList();
            var newEmptyNames = UniqueNames(emptyNames);

            for (var i = 0; i < collection.EmptyTiles.Length; i++)
            {
                collection.EmptyTiles[i].Name = newEmptyNames[i];
            }

            // Connectors
            if (collection.Connectors == null)
            {
                collection.Connectors = new ConnectorType[0];
            }

            collection.Connectors = collection.Connectors.Where(x => x != null).ToArray();

            // Make sure names are unique
            var connectorNames = collection.Connectors
                .Select(x => string.IsNullOrWhiteSpace(x.Name) ? "Connector" : x.Name)
                .ToList();
            var newConnectorNames = UniqueNames(connectorNames);

            for (var i = 0; i < collection.Connectors.Length; i++)
            {
                collection.Connectors[i].Name = newConnectorNames[i];
            }

            // Custom Properties
            if (collection.CustomValues == null)
            {
                collection.CustomValues = new string[0];
            }

            // Make sure names are unique
            var customPropertyNames = collection.CustomValues
                .Select(x => string.IsNullOrWhiteSpace(x) ? "Property" : x)
                .ToList();
            var newCustomPropertyNames = UniqueNames(customPropertyNames);

            for (var i = 0; i < collection.CustomValues.Length; i++)
            {
                collection.CustomValues[i] = newCustomPropertyNames[i];
            }

            // Sanitize children / tiles
            var tiles = collection.GetTiles(true);
            foreach (var tile in tiles)
            {
                SanitizeTile(collection, tile);
            }
        }

        /// <summary>
        /// Checks all tile fields and fixes potential errors
        /// </summary>
        public static void SanitizeTile([NotNull] TileCollection collection, [NotNull] Tile tile)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            var baseTiles = collection.GetTiles(true).Where(x => x.BaseTile == null).ToArray();
            var grid = collection.DefaultGrid;

            // Base name
            SanitizeTileType(collection, tile, baseTiles);

            // Base weight
            tile.BaseWeight = Math.Max(0.0001, tile.BaseWeight);

            // Rotation axes
            tile.RotationAxes = tile.RotationAxes.Where(x => 0 <= x && x < grid.RotationAxes.Length).ToArray();

            // Neighbors
            var neighbors = new List<NeighborCompatibiltyMatrix>();
            foreach (var type in collection.GetAllTypes(true))
            {
                // For the other types, find the same or create a new compatibilty matrix
                var existingNeighbor = tile.Neighbors?.FirstOrDefault(x => x.TileType == type);
                existingNeighbor?.SetSideCount(grid.Sides);
                neighbors.Add(existingNeighbor ?? new NeighborCompatibiltyMatrix(type, grid.Sides));
            }

            tile.Neighbors = neighbors.OrderBy(x => x.TileType).ToArray();

            // Custom properties
            tile.CustomProperties =
            (
                from property in collection.CustomValues
                let existingProperty = tile.CustomProperties?.FirstOrDefault(x => x.Name == property)
                select existingProperty ?? new CustomTileProperty(property)
            ).ToArray();

            // Connectors
            SanitizeConnectors(collection, grid, tile);
        }

        private static void SanitizeConnectors(TileCollection collection, IGrid grid, Tile tile)
        {
            if (tile.Connectors == null)
            {
                tile.Connectors = new ConnectorSide[grid.Sides];
            }
            
            if (tile.Connectors.Length != grid.Sides)
            {
                Array.Resize(ref tile.Connectors, grid.Sides);
            }

            for (var i = 0; i < tile.Connectors.Length; i++)
            {
                if (tile.Connectors[i] == null)
                {
                    tile.Connectors[i] = new ConnectorSide();
                }

                var side = tile.Connectors[i];

                if (side.Connectors == null)
                {
                    side.Connectors = new ConnectorAssignment[0];
                }

                // Remove all invalid assignments
                var connectors = side.Connectors
                    .Where(x => x?.Name != null && collection.Connectors.Any(y => y.Name == x.Name)).ToArray();

                if (side.Connectors.Length != connectors.Length)
                {
                    side.Connectors = connectors;
                }

                foreach (var assignment in side.Connectors)
                {
                    var connector = collection.Connectors.First(x => x.Name == assignment.Name);

                    if (connector.IsBidirectional)
                    {
                        assignment.ConnectionType = ConnectionType.Bidirectional;
                    }
                    else if (assignment.ConnectionType != ConnectionType.In &&
                             assignment.ConnectionType != ConnectionType.Out &&
                             assignment.ConnectionType != ConnectionType.Bidirectional)
                    {
                        assignment.ConnectionType = ConnectionType.Bidirectional;
                    }
                }
            }
        }

        private static void SanitizeTileType(TileCollection collection, Tile tile, Tile[] baseTiles)
        {
            if (tile.BaseTile == null)
            {
                if (baseTiles.Any(x => tile.TileType == x.TileType && tile != x))
                {
                    tile.TileType = UniqueTileType(collection, tile.TileType);
                }
            }
            else
            {
                tile.TileType = tile.BaseTile.TileType;
            }
        }


        public static string UniqueTileType(TileCollection collection, string newValue)
        {
            var tileTypes = collection.GetTileTypes(true);

            return UniqueName(tileTypes, newValue);
        }

        public static string UniqueName(IList<string> names, string current)
        {
            // Check if the new value is still unique
            if (names.Contains(current))
            {
                var numberRegex = new Regex(@"(.*)(\s*\(\d+\))");

                var match = numberRegex.Match(current);
                if (match.Success)
                {
                    current = match.Groups[1].Value.Trim();
                }

                string fixedName;
                var duplicateCount = 1;
                do
                {
                    fixedName = $"{current} ({duplicateCount})";
                    duplicateCount++;
                } while (names.Contains(fixedName));

                current = fixedName;
            }

            return current;
        }

        public static IList<string> UniqueNames(IList<string> names)
        {
            // Copy list
            names = names.ToList();

            for (var i = names.Count - 1; i >= 0; i--)
            {
                var currentIndex = i;
                var exList = names.Where((_, j) => currentIndex != j).ToList();

                names[i] = UniqueName(exList, names[i]);
            }

            return names;
        }
    }
}