using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Debug = UnityEngine.Debug;

namespace StixGames.TileComposer.Solvers.SATSolvers
{
    public class CaDiCaLSolver : ISATSolver, IDisposable
    {
        private readonly IntPtr solver;

        [DllImport("Assets/StixGames/TileComposer/Libraries/CaDiCaL/win-x64/CaDiCaL.dll")]
        private static extern IntPtr CreateSolver();

        [DllImport("Assets/StixGames/TileComposer/Libraries/CaDiCaL/win-x64/CaDiCaL.dll")]
        private static extern void DisposeSolver(IntPtr solver);

        [DllImport("Assets/StixGames/TileComposer/Libraries/CaDiCaL/win-x64/CaDiCaL.dll")]
        private static extern void SolverAdd(IntPtr solver, int lit);

        [DllImport("Assets/StixGames/TileComposer/Libraries/CaDiCaL/win-x64/CaDiCaL.dll")]
        private static extern int SolverSolve(IntPtr solver);

        [DllImport("Assets/StixGames/TileComposer/Libraries/CaDiCaL/win-x64/CaDiCaL.dll")]
        private static extern int SolverVal(IntPtr solver, int lit);

        [DllImport("Assets/StixGames/TileComposer/Libraries/CaDiCaL/win-x64/CaDiCaL.dll")]
        private static extern void SetTimeLimit(int seconds);

        public CaDiCaLSolver()
        {
            solver = CreateSolver();
        }

        private void ReleaseUnmanagedResources()
        {
            DisposeSolver(solver);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~CaDiCaLSolver()
        {
            ReleaseUnmanagedResources();
        }

        public int NextVariable() => ++VariableCount;

        public int VariableCount { get; private set; }

        public void Assert(IEnumerable<int> clause)
        {
            foreach (var lit in clause)
            {
                SolverAdd(solver, lit);
            }

            SolverAdd(solver, 0);
        }

        public void Assert(params int[] clause)
        {
            Assert(clause as IEnumerable<int>);
        }

        public SolverResult Solve(int mainVariables, int timeout)
        {
            Debug.LogWarning("Timout is not yet implemented for the current solver.");

            var solveStopwatch = new Stopwatch();
            solveStopwatch.Start();
            var rawResult = SolverSolve(solver);
            solveStopwatch.Stop();
            
            Debug.Log($"Solve time: {solveStopwatch.Elapsed}");

            switch (rawResult)
            {
                // Unsolved, aborted by limit or terminator
                case 0:
                    return SolverResult.Failure;
                
                // SAT
                case 10:
                    break;
                
                // UNSAT
                case 20:
                    return SolverResult.GuaranteedFailure;
                default:
                    Debug.LogError("Invalid return value from Native solve function.");
                    return SolverResult.Failure;
            }

            var variables = new List<int>();
            for (int i = 1; i <= mainVariables; i++)
            {
                var solverVal = SolverVal(solver, i);
                if (solverVal > 0)
                {
                    variables.Add(solverVal);
                }
            }

            SelectedVariables = variables.ToArray();
            
            
            return SolverResult.Success;
        }

        public int[] SelectedVariables { get; private set; }

        public void Reset()
        {
            throw new System.NotImplementedException("Can't reset native component.");
        }
    }
}