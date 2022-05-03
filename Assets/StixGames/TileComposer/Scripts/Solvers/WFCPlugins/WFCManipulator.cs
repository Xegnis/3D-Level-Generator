using System.Collections.Generic;

namespace StixGames.TileComposer.Solvers.WFCPlugins
{
    /// <summary>
    /// Returned by WFC plugins to make changes to the solver
    /// </summary>
    public class WFCManipulator
    {
        public readonly List<(int, int)> Block = new List<(int, int)>();
        public readonly List<(int, int)> SetTiles = new List<(int, int)>();

        public bool IsFailure { get; private set; }
        
        /// <summary>
        /// Set a tile to a specific type. If the type is already blocked it will cause a generation failure.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="variation"></param>
        public void SetTile(int index, int variation)
        {
            SetTiles.Add((index, variation));
        }
        
        public void BlockTile(int index, int variation)
        {
            Block.Add((index, variation));
        }
        
        public void BlockTiles(int index, params int[] variations)
        {
            foreach (var variation in variations)
            {
                Block.Add((index, variation));
            }
        }

        /// <summary>
        /// Tell the solver that the current simulation state is a failure.
        /// If there is no failure recovery enabled, it will abort generation.
        /// </summary>
        public void CauseGenerationFailure()
        {
            IsFailure = true;
        }
    }
}