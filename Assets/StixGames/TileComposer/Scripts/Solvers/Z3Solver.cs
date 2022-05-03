using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Z3;
using UnityEngine;

namespace StixGames.TileComposer.Solvers
{
    public class Z3Solver : IConstraintSolver
    {
        public int ParallelInstances { get; set; }
        public bool SupportsSlowGeneration => false;
        public float Timeout { get; set; }

        private SATSettings settings;

        private uint TimeoutMilliseconds => (uint) Timeout * 1000;

        private readonly TileComposerInput input;

        private System.Random random => input.Random;
        private IGrid grid => input.Grid;
        private TileVariation[] variations => input.TileVariations;
        private TileVariation[][] blockedTiles => input.BlockedTiles;
        
        private int[][] blockedTileIndices => input.BlockedUniqueTileIndices;
        private Dictionary<TileVariation, int> tileIndices => input.TileIndices;
        private int[,][] tileCompatibilityMatrix => input.TileCompatibilityMatrix;

        /// <summary>
        /// A array of variations that share the same neighbor matrices
        /// </summary>
        private TileVariation[][] uniqueVariations => input.UniqueVariations;

        /// <summary>
        /// Lookup from the unique index to any one of the duplicates
        /// </summary>
        private int[] uniqueIndexToTileIndex => input.UniqueVariationRepresentatives;

        /// <summary>
        /// A lookup from the tile type to its unique id
        /// </summary>
        private int[] tileIndexToUniqueIndex => input.TileIndexToUniqueIndex;

        private TileVariation[][] model;

        private Task<Solver> solverTask;
        private Task<Status> solverCheckTask;

        public Z3Solver(TileComposerInput input, SATSettings settings)
        {
            this.input = input;
            this.settings = settings;
        }

        public SolverResult CalculateModel()
        {
            var cfg = new Dictionary<string, string>
            {
                ["parallel.enable"] = "true",
                ["timeout"] = TimeoutMilliseconds.ToString()
            };

            using (Context ctx = new Context(cfg))
            {
                // Create the list of tiles
                var tiles = new IntExpr[grid.GridSize];

                // Create the solver
                var tactic = CreateQFLIATactic(ctx);
                var solver = CreateZ3Instance(ctx, tactic);

                // Create assertions
                CreateAssertions(ctx, solver, tiles);
                //LogAssertions(solver);

                // Check the model
                var z3Result = solver.Check();
                //Debug.Log(solver.ReasonUnknown);

                // Return the result
                var result = ConvertResult(z3Result);
                if (result == SolverResult.Failure)
                {
                    Debug.Log(solver.ReasonUnknown);
                }

                if (result == SolverResult.Success)
                {
                    RetrieveModel(solver, tiles);
                }

                return result;
            }
        }

        public async Task<SolverResult> CalculateModelAsync()
        {
            var cfg = new Dictionary<string, string>
            {
                ["parallel.enable"] = "true",
                ["timeout"] = TimeoutMilliseconds.ToString()
            };

            using (Context ctx = new Context(cfg))
            {
                // Create the list of tiles
                var tiles = new IntExpr[grid.GridSize];

                // Create the solver
                var tactic = CreateQFLIATactic(ctx);
                var solver = CreateZ3Instance(ctx, tactic);

                // Create assertions
                var constraintTask = Task.Run(() => CreateAssertions(ctx, solver, tiles));
                await constraintTask;
                //LogAssertions(solver);

                // Check the model
                solverCheckTask = Task.Run(() => solver.Check());
                var z3Result = await solverCheckTask;

                // Return the result
                var result = ConvertResult(z3Result);
                if (result == SolverResult.Success)
                {
                    RetrieveModel(solver, tiles);
                }

                return result;
            }
        }

        private static IZ3Instance CreateZ3Instance(Context ctx, Tactic tactic)
        {
            return new Z3SolverInstance(tactic == null ? ctx.MkSolver() : ctx.MkSolver(tactic));
        }

        private Tactic CreateSMTTactic(Context ctx)
        {
            var simplifyParams = ctx.MkParams();
            var simplify = ctx.UsingParams(ctx.MkTactic("simplify"), simplifyParams);

            var parallelTactics = new List<Tactic>();

            for (int i = 0; i < ParallelInstances; i++)
            {
                var smtParams = ctx.MkParams();
                smtParams.Add(ctx.MkSymbol("random-seed"), (uint) random.Next());
                var smt = ctx.UsingParams(ctx.MkTactic("smt"), smtParams);

                parallelTactics.Add(smt);
            }

            var tactic = ctx.ParAndThen(simplify, ctx.ParOr(parallelTactics.ToArray()));
            return tactic;
        }

        /// <summary>
        /// Quantifier free integer difference logic
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private Tactic CreateQFIDLTactic(Context ctx)
        {
            var simplifyParams = ctx.MkParams();
            var simplify = ctx.UsingParams(ctx.MkTactic("simplify"), simplifyParams);

            var parallelTactics = new List<Tactic>();

            for (int i = 0; i < ParallelInstances; i++)
            {
                var qfliaParams = ctx.MkParams();
                qfliaParams.Add(ctx.MkSymbol("random-seed"), (uint) random.Next());
                //qfliaParams.Add(ctx.MkSymbol("restart.initial"), 100000);
                var smt = ctx.UsingParams(ctx.MkTactic("qfidl"), qfliaParams);

                parallelTactics.Add(smt);
            }

            var tactic = ctx.ParAndThen(simplify, ctx.ParOr(parallelTactics.ToArray()));
            return tactic;
        }
        
        private Tactic CreateQFLIATactic(Context ctx)
        {
            var simplifyParams = ctx.MkParams();
            var simplify = ctx.UsingParams(ctx.MkTactic("simplify"), simplifyParams);

            var parallelTactics = new List<Tactic>();

            for (int i = 0; i < ParallelInstances; i++)
            {
                var qfliaParams = ctx.MkParams();
                qfliaParams.Add(ctx.MkSymbol("random-seed"), (uint) random.Next());
                //qfliaParams.Add(ctx.MkSymbol("restart.initial"), 100000);
                var smt = ctx.UsingParams(ctx.MkTactic("qflia"), qfliaParams);

                parallelTactics.Add(smt);
            }

            var tactic = ctx.ParAndThen(simplify, ctx.ParOr(parallelTactics.ToArray()));
            return tactic;
        }

        private Tactic CreateSATBitBlastTactic(Context ctx)
        {
            var simplifyParams = ctx.MkParams();
            simplifyParams.Add(ctx.MkSymbol("arith-lhs"), true);
            simplifyParams.Add(ctx.MkSymbol("som"), true);
            var simplify = ctx.UsingParams(ctx.MkTactic("simplify"), simplifyParams);

            var normalizeBounds = ctx.MkTactic("normalize-bounds");

            var lia2pbParams = ctx.MkParams();
            lia2pbParams.Add(ctx.MkSymbol("lia2pb_max_bits"), (uint) uniqueVariations.Length);
            lia2pbParams.Add(ctx.MkSymbol("lia2pb_total_bits"), (uint) (uniqueVariations.Length * grid.GridSize));
            var lia2pb = ctx.UsingParams(ctx.MkTactic("lia2pb"), lia2pbParams);

            var pb2bv = ctx.MkTactic("pb2bv");
            var bitBlast = ctx.MkTactic("bit-blast");

            var parallelTactics = new List<Tactic>();

            for (int i = 0; i < ParallelInstances; i++)
            {
                var satParams = ctx.MkParams();
                satParams.Add(ctx.MkSymbol("random-seed"), (uint) random.Next());
                satParams.Add(ctx.MkSymbol("restart.initial"), 10000);
                var sat = ctx.UsingParams(ctx.MkTactic("sat"), satParams);

                parallelTactics.Add(sat);
            }

            var tactic = ctx.AndThen(simplify, normalizeBounds, lia2pb, pb2bv, bitBlast,
                ctx.ParOr(parallelTactics.ToArray()));
            return tactic;
        }

        private void CreateAssertions(Context ctx, IZ3Instance s, IntExpr[] tiles)
        {
            // Function to constraint tiles to the right size.
            var intConst = ctx.MkIntConst("x");

            BoolExpr SizeConstraintFunc(Expr x) =>
                (BoolExpr) (intConst >= 0 & intConst < uniqueVariations.Length).Substitute(intConst, x);

            // Create variables
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = ctx.MkIntConst($"x_{i}");
                tiles[i] = tile;

                // Check if it would require less rules to allow instead of block tiles
                if (blockedTileIndices[i].Length > uniqueVariations.Length / 2f)
                {
                    // Allow valid tiles
                    var allowed = new HashSet<int>(Enumerable.Range(0, uniqueVariations.Length));
                    allowed.ExceptWith(blockedTileIndices[i]);

                    var tileAllowAssertions = allowed.Aggregate(ctx.MkFalse(),
                        (current, index) => current | ctx.MkEq(tile, ctx.MkInt(index)));

                    // Block tiles and constrain size, then simplify rules
                    s.Assert(tileAllowAssertions.Simplify() as BoolExpr);
                }
                else
                {
                    // Block tiles
                    var tileBlockAssertions = blockedTileIndices[i]
                        .Aggregate(ctx.MkTrue(),
                            (current, index) => current & ctx.MkNot(ctx.MkEq(tile, ctx.MkInt(index))));

                    // Block tiles and constrain size, then simplify rules
                    s.Assert((tileBlockAssertions & SizeConstraintFunc(tile)).Simplify() as BoolExpr);
                }
            }

            // Create side constraints
            var start = ctx.MkIntConst("Start");
            var target = ctx.MkIntConst("Target");

            var sideFunctions = new Func<Expr, Expr, BoolExpr>[grid.Sides];
            for (int side = 0; side < grid.Sides; side++)
            {
                //s.Assert((BoolExpr) sizeConstraintFunc[start]);
                //s.Assert((BoolExpr) sizeConstraintFunc[target]);

                // Iterate all neighbors
                var sideFunctionResult = ctx.MkTrue();
                for (int uniqueIndex = 0; uniqueIndex < uniqueVariations.Length; uniqueIndex++)
                {
                    var allowedNeighbors = ctx.MkFalse();
                    var compatibleNeighbors = tileCompatibilityMatrix[uniqueIndexToTileIndex[uniqueIndex], side];

                    // TODO: Check if the solver is faster at removing duplicates than me
                    var neighborSet = new HashSet<int>();
                    foreach (var compatibleNeighbor in compatibleNeighbors)
                    {
                        var uniqueNeighbor = tileIndexToUniqueIndex[compatibleNeighbor];

                        // Skip neighbors that were already visited
                        if (neighborSet.Contains(uniqueNeighbor))
                        {
                            continue;
                        }

                        neighborSet.Add(uniqueNeighbor);

                        // Enforce that one of the neighbors must be supported
                        allowedNeighbors |= ctx.MkEq(target, ctx.MkInt(uniqueNeighbor));
                    }

                    var isCurrentVariation = ctx.MkEq(start, ctx.MkInt(uniqueIndex));
                    sideFunctionResult &= ctx.MkImplies(isCurrentVariation, allowedNeighbors);
                }

                // Assert the side constraint, to give the function meaning
                var simplifiedFunc = sideFunctionResult.Simplify();
                sideFunctions[side] = (a, b) =>
                    (BoolExpr) simplifiedFunc.Substitute(new Expr[] {start, target}, new[] {a, b});
            }

            // Iterate all tiles
            var randomTileOrder = Enumerable.Range(0, tiles.Length);

            if (settings.RandomizeConstraintOrder)
            {
                randomTileOrder = randomTileOrder.OrderBy(x => random.Next());
            }

            foreach (int tileIndex in randomTileOrder)
            {
                // Iterate all tile sides
                var sideConstraints = ctx.MkTrue();
                for (int side = 0; side < grid.Sides; side++)
                {
                    int neighborIndex = grid.GetNeighbor(tileIndex, side);

                    // Skip border constraints
                    if (neighborIndex < 0)
                    {
                        continue;
                    }

                    // Apply the constraints for these two tiles
                    var sideFunction = sideFunctions[side];
                    sideConstraints &= sideFunction(tiles[tileIndex], tiles[neighborIndex]);
                }

                // Apply constraints for this tile
                s.Assert(sideConstraints);
            }

            // Additional constraints
            var groupCountFunctions = new Dictionary<string, BoolExpr[]>();

            // Include weighting in calculations
            if (settings.WeightPriority > 0.0001f)
            {
                // Get weights for each type
                var indexLookup = tileIndices
                    .Select(x => (x.Key.TileTypeName, x.Key.VariationWeight, tileIndexToUniqueIndex[x.Value]))
                    .Distinct()
                    .ToLookup(x => x.TileTypeName);

                var atLeastConditions = new List<(BoolExpr[], double)>();
                var totalWeight = 0.0;

                // Iterate through all tile groups and create the individual conditions we want to count
                foreach (var group in indexLookup)
                {
                    // Create a function that checks if a tile if part of the selected group
                    var isGroup = CreateGroupCountFunction(ctx, group.Select(x => x.Item3));

                    // Calculate the average weight of this group
                    var groupWeight = 0.0;
                    foreach (var elem in group)
                    {
                        groupWeight += elem.VariationWeight;
                    }

                    totalWeight += groupWeight;

                    // Apply the function on the whole grid, then add it to the cache for later constraints
                    BoolExpr[] conditions = FuncOverAllTiles(tiles, isGroup);
                    groupCountFunctions[group.Key] = conditions;

                    // Add the conditions and the average weight to the list
                    atLeastConditions.Add((conditions, groupWeight));
                }

                foreach (var (conditions, weight) in atLeastConditions)
                {
                    s.Assert(
                        ctx.MkAtLeast(conditions,
                            (uint) (settings.WeightPriority * grid.GridSize * (weight / totalWeight))));
                }
            }

            // Percentage constraints
            foreach (var constraint in settings.PercentageConstraints)
            {
                BoolExpr[] countConditions;
                if (groupCountFunctions.ContainsKey(constraint.Name))
                {
                    countConditions = groupCountFunctions[constraint.Name];
                }
                else
                {
                    var indices = tileIndices
                        .Where(x => x.Key.TileTypeName == constraint.Name)
                        .Select(x => tileIndexToUniqueIndex[x.Value])
                        .Distinct();
                    Func<Expr, BoolExpr> isGroup = CreateGroupCountFunction(ctx, indices);

                    // Apply the function on the whole grid, then add it to the cache for later constraints
                    countConditions = FuncOverAllTiles(tiles, isGroup);
                    groupCountFunctions[constraint.Name] = countConditions;
                }

                // Create the conditions, as specified by the constraints
                switch (constraint.Comparison)
                {
                    case Comparison.Less:
                        s.Assert(ctx.MkAtMost(countConditions,
                            (uint) Mathf.CeilToInt(constraint.Value * grid.GridSize)));
                        break;
                    case Comparison.Equal:
                        s.Assert(ctx.MkAtMost(countConditions,
                            (uint) Mathf.CeilToInt(constraint.Value * grid.GridSize)));
                        s.Assert(ctx.MkAtLeast(countConditions,
                            (uint) Mathf.CeilToInt(constraint.Value * grid.GridSize)));
                        break;
                    case Comparison.Greater:
                        s.Assert(ctx.MkAtLeast(countConditions,
                            (uint) Mathf.CeilToInt(constraint.Value * grid.GridSize)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Absolute constraints
            foreach (var constraint in settings.AbsoluteConstraints)
            {
                BoolExpr[] countConditions;
                if (groupCountFunctions.ContainsKey(constraint.Name))
                {
                    countConditions = groupCountFunctions[constraint.Name];
                }
                else
                {
                    var indices = tileIndices
                        .Where(x => x.Key.TileTypeName == constraint.Name)
                        .Select(x => tileIndexToUniqueIndex[x.Value])
                        .Distinct();
                    Func<Expr, BoolExpr> isGroup = CreateGroupCountFunction(ctx, indices);

                    // Apply the function on the whole grid, then add it to the cache for later constraints
                    countConditions = FuncOverAllTiles(tiles, isGroup);
                    groupCountFunctions[constraint.Name] = countConditions;
                }

                // Create the conditions, as specified by the constraints
                switch (constraint.Comparison)
                {
                    case Comparison.Less:
                        s.Assert(ctx.MkAtMost(countConditions, (uint) (constraint.Value - 1)));
                        break;
                    case Comparison.Equal:
                        s.Assert(ctx.MkPBEq(Enumerable.Repeat(1, countConditions.Length).ToArray(), countConditions, constraint.Value));
                        break;
                    case Comparison.Greater:
                        s.Assert(ctx.MkAtLeast(countConditions, (uint) (constraint.Value + 1)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            // Custom property constraints
            foreach (var constraint in settings.CustomPropertyConstraints)
            {
                // Collect the values and conditions for each unique custom value
                var customPropertyValues = tileIndices.Where(x => x.Key.Tile != null)
                    .ToLookup(x => x.Key.Tile.CustomProperties.Single(y => y.Name == constraint.Name).IntValue,
                        x => tileIndexToUniqueIndex[x.Value]);

                var values = new List<int>();
                var groups = new List<BoolExpr>();
                foreach (var customPropertyValue in customPropertyValues)
                {
                    var entries = customPropertyValue.Distinct();

                    var groupFunction = CreateGroupCountFunction(ctx, entries);
                    
                    values.AddRange(Enumerable.Repeat(customPropertyValue.Key, tiles.Length));
                    groups.AddRange(FuncOverAllTiles(tiles, groupFunction));
                }
                
                // Create the conditions, as specified by the constraints
                switch (constraint.Comparison)
                {
                    case Comparison.Less:
                        s.Assert(ctx.MkPBLe(values.ToArray(), groups.ToArray(), constraint.Value - 1));
                        break;
                    case Comparison.Equal:
                        s.Assert(ctx.MkPBEq(values.ToArray(), groups.ToArray(), constraint.Value));
                        break;
                    case Comparison.Greater:
                        s.Assert(ctx.MkPBGe(values.ToArray(), groups.ToArray(), constraint.Value + 1));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static Func<Expr, BoolExpr> CreateGroupCountFunction(Context ctx, IEnumerable<int> tileIndices)
        {
            // Check if the current tile is equal to one of the tileIndices
            var currentTile = ctx.MkIntConst(ctx.MkSymbol(0));
            var isGroupFunction = tileIndices.Aggregate(ctx.MkFalse(),
                    (current, index) => current | ctx.MkEq(currentTile, ctx.MkInt(index)))
                .Simplify();
            return tile => (BoolExpr) isGroupFunction.Substitute(currentTile, tile);
        }

        private BoolExpr[] FuncOverAllTiles(IntExpr[] tiles, Func<Expr, BoolExpr> func)
        {
            // Create the conditions for each grid tile
            var conditions = new BoolExpr[grid.GridSize];
            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];

                conditions[i] = func(tile);
            }

            return conditions;
        }

        private static SolverResult ConvertResult(Status z3Result)
        {
            SolverResult result;
            switch (z3Result)
            {
                case Status.UNSATISFIABLE:
                    result = SolverResult.GuaranteedFailure;
                    break;
                case Status.UNKNOWN:
                    result = SolverResult.Failure;
                    break;
                case Status.SATISFIABLE:
                    result = SolverResult.Success;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        private void RetrieveModel(IZ3Instance s, IntExpr[] tiles)
        {
            var satModel = s.Model;

            model = new TileVariation[tiles.Length][];

            for (int i = 0; i < tiles.Length; i++)
            {
                var uniqueIndex = ((IntNum) satModel.Evaluate(tiles[i])).Int;
                var variationGroup = uniqueVariations[uniqueIndex];
                var variation = variationGroup[random.Next(variationGroup.Length)];
                model[i] = new[] {variation};
            }
        }

        private static void LogAssertions(IZ3Instance s)
        {
            var builder = new StringBuilder();
            foreach (var assertion in s.Assertions)
            {
                builder.AppendLine(assertion.ToString());
            }

            File.WriteAllText(Path.Combine(Application.dataPath, "assertions.txt"), builder.ToString());
        }

        public SolverResult Step()
        {
            throw new System.NotImplementedException();
        }

        public TileVariation[][] GetModel()
        {
            // Return empty model
            if (model == null)
            {
                var tiles = new TileVariation[grid.GridSize][];

                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i] = new TileVariation[0];
                }

                return tiles;
            }

            // Return real model
            return model;
        }

        public void Reset()
        {
            model = null;
        }
    }
}