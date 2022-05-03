using System.Collections.Generic;

namespace StixGames.TileComposer.Solvers.WFCPlugins
{
    public interface IWFCPlugin
    {
        void Initialize(TileComposerInput input);
        
        /// <summary>
        /// Called for each step of the solver.
        ///
        /// You should not make changes to the inputs, only through the returned WFC manipulator.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="allowedTiles"></param>
        /// <param name="lastChanges">The changes since the last manipulator was called last time.
        /// This includes backtracking.
        /// In the first step, this includes all fixed or blocked tiles.</param>
        /// <returns></returns>
        WFCManipulator Step(bool[,] allowedTiles, IList<WFCChange> lastChanges);
    }
}