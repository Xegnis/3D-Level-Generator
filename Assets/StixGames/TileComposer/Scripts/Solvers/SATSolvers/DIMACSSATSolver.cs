using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Debug = UnityEngine.Debug;

namespace StixGames.TileComposer.Solvers.SATSolvers
{
    public class DIMACSSATSolver : ISATSolver
    {
        private string solverPath;
        
        private List<int[]> clauses = new List<int[]>();

        public int NextVariable() => ++VariableCount;

        public int VariableCount { get; private set; }
        public int[] SelectedVariables { get; private set; }

        public DIMACSSATSolver(string solverPath)
        {
            this.solverPath = solverPath;
        }

        public void Assert(IEnumerable<int> clause)
        {
            clauses.Add(clause.ToArray());
        }

        public void Assert(params int[] clause)
        {
            clauses.Add(clause);
        }

        public SolverResult Solve(int mainVariables, int timeout)
        {
            var input = GenerateDIMACS();

            return RunSATProcess(input, mainVariables, timeout);
        }

        private string GenerateDIMACS()
        {
            var b = new StringBuilder();

            b.AppendLine($"p cnf {VariableCount} {clauses.Count}");

            foreach (var clause in clauses)
            {
                foreach (var variable in clause)
                {
                    b.Append(variable);
                    b.Append(' ');
                }

                b.Append('0').AppendLine();
            }

            return b.ToString();
        }
        
        private SolverResult RunSATProcess(string satInput, int mainVariables, int timeout)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;

            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.FileName = Path.GetFullPath(solverPath);
            startInfo.Arguments = $"-t {timeout}";

            var process = Process.Start(startInfo);

            process.StandardInput.Write(satInput);
            process.StandardInput.Close();

            var satOutput = process.StandardOutput.ReadToEnd();
            Debug.Log(satOutput);

            var lines = satOutput.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.RemoveEmptyEntries
            );

            var resultLine = lines.Single(x => x.StartsWith("s"));

            if (resultLine.Contains("UNSATISFIABLE"))
            {
                return SolverResult.GuaranteedFailure;
            }
            
            if (!resultLine.Contains("SATISFIABLE"))
            {
                return SolverResult.Failure;
            }

            var variableLines = lines.Where(x => x.StartsWith("v"));
            var variables = new List<int>();
            foreach (var variableLine in variableLines)
            {
                var values = variableLine.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                var lineVariables = values
                    .Where(x => x != "v" && !x.StartsWith("-"))
                    .Select(int.Parse)
                    .Where(x => x > 0 && x <= mainVariables);
                
                variables.AddRange(lineVariables);
            }

            SelectedVariables = variables.ToArray();
            
            return SolverResult.Success;
        }
        
        public void Reset()
        {
            clauses.Clear();
            VariableCount = 0;
        }
    }
}