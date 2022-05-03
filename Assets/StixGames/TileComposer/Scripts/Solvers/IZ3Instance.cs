using Microsoft.Z3;

namespace StixGames.TileComposer
{
    public interface IZ3Instance
    {
        BoolExpr[] Assertions { get; }
        Model Model { get; }
        
        string ReasonUnknown { get; }

        void Assert(params BoolExpr[] constraints);
        Status Check(params Expr[] assumptions);
        void Set(string name, uint value);
    }

    public class Z3SolverInstance : IZ3Instance
    {
        private readonly Solver solver;

        public Z3SolverInstance(Solver solver)
        {
            this.solver = solver;
        }

        public BoolExpr[] Assertions => solver.Assertions;
        public Model Model => solver.Model;
        public string ReasonUnknown => solver.ReasonUnknown;

        public void Assert(params BoolExpr[] constraints)
        {
            solver.Assert(constraints);
        }

        public Status Check(params Expr[] assumptions)
        {
            return solver.Check(assumptions);
        }

        public void Set(string name, uint value)
        {
            solver.Set(name, value);
        }

        public override string ToString() => solver.ToString();
    }

    public class Z3OptimizerInstance : IZ3Instance
    {
        private readonly Optimize solver;

        public BoolExpr[] Assertions => solver.Assertions;
        public Model Model => solver.Model;

        public string ReasonUnknown => solver.ReasonUnknown;
        
        public Z3OptimizerInstance(Optimize solver)
        {
            this.solver = solver;
        }

        public void Assert(params BoolExpr[] constraints)
        {
            solver.Assert(constraints);
        }

        public Status Check(params Expr[] assumptions)
        {
            return solver.Check(assumptions);
        }

        public void Set(string name, uint value)
        {
            solver.Set(name, value);
        }
    }
}