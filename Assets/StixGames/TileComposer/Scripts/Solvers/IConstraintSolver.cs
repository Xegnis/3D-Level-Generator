using System.Threading.Tasks;

namespace StixGames.TileComposer
{
    public interface IConstraintSolver
    {
        int ParallelInstances { get; set; }
        
        bool SupportsSlowGeneration { get; }
        float Timeout { get; set; }

        SolverResult CalculateModel();
        Task<SolverResult> CalculateModelAsync();
        SolverResult Step();
        TileVariation[][] GetModel();
        void Reset();
    }
}