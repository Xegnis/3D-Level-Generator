using System.Collections.Generic;

namespace StixGames.TileComposer.Solvers.SATSolvers
{
    public interface ISATSolver
    {
        /// <summary>
        /// Returns a new variable
        /// </summary>
        /// <returns></returns>
        int NextVariable();
        
        /// <summary>
        /// Returns the count of variables, which is also the number of the last variable used,
        /// because variables start at 1.
        /// </summary>
        int VariableCount { get; }
        
        void Assert(IEnumerable<int> clause);
        void Assert(params int[] clause);
        

        /// <summary>
        /// Start solving the specified assertions.
        /// </summary>
        /// <param name="mainVariables">The count of main variables that will be read from the solver.</param>
        /// <returns></returns>
        SolverResult Solve(int mainVariables, int timeout);
        
        /// <summary>
        /// After Solve was executed successfully, this will return all main variables that are true.
        /// </summary>
        int[] SelectedVariables { get; }

        void Reset();
    }
}