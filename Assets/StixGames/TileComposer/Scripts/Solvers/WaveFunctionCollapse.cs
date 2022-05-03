using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using StixGames.TileComposer.Solvers.WFCPlugins;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace StixGames.TileComposer.Solvers
{
    /// <summary>
    /// The basic wave function collapse algorithm is based on https:// github.com/mxgmn/WaveFunctionCollapse and https:// github.com/math-fehr/fast-wfc
    /// </summary>
    public class WaveFunctionCollapse : IConstraintSolver
    {
        private WaveFunctionCollapseSettings settings;

        public readonly List<int> ResetIndices = new List<int>();

        private readonly Stack<List<(int, int, bool)>> backtrackStack = new Stack<List<(int, int, bool)>>();

        private const double Epsilon = 1e-6;

        private readonly Dictionary<TileVariation, int> tileVariationIndices;

        /// <summary>
        /// tileCompatibility[currentTile, side] returns a list of all compatible tiles
        /// </summary>
        private readonly int[,][] tileCompatibility;

        private readonly int[,] initialCompatibleNeighbors;

        private readonly HashSet<int>[] blockedVariationSets;

        /// <summary>
        /// possibleNeighbors[input.gridPos, variation, opposingSide] returns the number of neighboring variations that would support variation from side (opposite of the one in the index).
        /// Encoding the compatible neighbors like this, enables setting the borders of the input.grid for specific requirements
        /// Using the opposing direction, lets us remove calculations for the opposing direction in the propagation step
        /// </summary>
        private int[,,] compatibleNeighbors;

        /// <summary>
        /// possibleTiles[input.gridPos, variation] returns if the variation is allowed at this tile
        /// </summary>
        private bool[,] allowedTiles;

        /// <summary>
        /// A list of failure weights for each tile, (weight, acceleration, last access)
        /// </summary>
        private (float, float, int)[] failureWeights;

        private float failureDecayPower;
        private float failureAccelerationDecayPower;
        private int timeStep;

        private int failureIndex;

        // Entropy calculation constants and memoisation 
        private readonly double[] weights, weightLogWeights;

        private readonly int[] allowedTileCount;
        private readonly double sumOfWeights, sumOfWeightLogWeights, startEntropy;
        private readonly double[] sumsOfWeights, sumsOfWeightsLogWeights, entropies;

        /// <summary>
        /// A queue of all variations that were changed. (input.gridPos, variation, isBacktracking)
        /// </summary>
        private readonly Queue<(int, int, bool)> propagationQueue = new Queue<(int, int, bool)>();

        private readonly TileComposerInput input;
        private readonly int[,] neighborList;

        public int ParallelInstances { get; set; }
        public bool SupportsSlowGeneration => true;
        public float Timeout { get; set; }

        public IWFCPlugin[] Plugins;

        private IGrid grid => input.Grid;
        private int gridSize => grid.GridSize;
        
        /// <summary>
        /// Used to tell plugins how the grid has changed
        /// </summary>
        private readonly List<WFCChange> changes = new List<WFCChange>();
        
        public WaveFunctionCollapse(TileComposerInput input, WaveFunctionCollapseSettings settings)
        {
            this.input = input;
            this.settings = settings;
            tileVariationIndices = input.TileVariations.Select((x, i) => new {x, i}).ToDictionary(x => x.x, x => x.i);
            tileCompatibility = TileUtility.CreateTileCompatibilityMatrix(input.TileVariations, grid);
            initialCompatibleNeighbors = CreateInitialCompatibleNeighborsArray();

            blockedVariationSets = input.BlockedTiles.Select(x => new HashSet<int>(x.Select(y => tileVariationIndices[y])))
                .ToArray();

            // Initialize entropy calculation
            weights = input.TileVariations.Select(x => Math.Max(x.VariationWeight, 0.0001)).ToArray();
            weightLogWeights = weights.Select(x => x * Math.Log(x)).ToArray();

            sumOfWeights = weights.Sum();
            sumOfWeightLogWeights = weightLogWeights.Sum();

            startEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

            neighborList = new int[gridSize, grid.Sides];

            for (int i = 0; i < gridSize; i++)
            {
                for (int side = 0; side < grid.Sides; side++)
                {
                    neighborList[i, side] = grid.GetNeighbor(i, side);
                }
            }

            allowedTileCount = new int[gridSize];
            sumsOfWeights = new double[gridSize];
            sumsOfWeightsLogWeights = new double[gridSize];
            entropies = new double[gridSize];

            failureWeights = new (float, float, int)[gridSize];
        }

        public SolverResult CalculateModel()
        {
            Reset();

            var state = CalculationLoop();

            return state;
        }

        public async Task<SolverResult> CalculateModelAsync()
        {
            Reset();

            var state = await Task.Run(CalculationLoop);

            return state;
        }

        private SolverResult CalculationLoop()
        {
            var timer = new Stopwatch();
            timer.Start();

            SolverResult state;
            do
            {
                state = Step();
            } while (state == SolverResult.Unfinished && timer.Elapsed < TimeSpan.FromSeconds(Timeout));

            timer.Stop();

            // If the algorithm went over the timeout, return failure
            return state == SolverResult.Unfinished ? SolverResult.Failure : state;
        }

        public void Reset()
        {
            // Create a valid backtrack step, which will be discarded after resetting
            backtrackStack.Push(new List<(int, int, bool)>());

            // Set the start state
            Clear();

            Propagate();

            // Throw away the changes from initializing, to make them permanent for backtracking
            backtrackStack.Pop();
        }

        public SolverResult Step()
        {
            Profiler.BeginSample("StixGames.TileComposer.Step");

            // Calculate the failure decay, in case it was changed
            failureDecayPower = Mathf.Log(2) / settings.FailureDecay;
            failureAccelerationDecayPower = Mathf.Log(2) / settings.FailureAccelerationDecay;

            // Clear the debug information for all positions that were cleared
            ResetIndices.Clear();

            // Add a new list for the current backtrace changes
            backtrackStack.Push(new List<(int, int, bool)>());

            var isValid = ExecutePlugins();
            changes.Clear();

            ObserveResult result;
            if (isValid)
            {
                result = Observe();
            }
            else
            {
                // A plugin registered a failure
                result = ObserveResult.Failure;
            }

            if (result == ObserveResult.Failure)
            {
                if (settings.UseFailureRecovery)
                {
                    // There is a failure, try resetting the area around it
                    Backtrack();
                }
                else
                {
                    return SolverResult.Failure;
                }
            }

            // The model is complete!
            if (result == ObserveResult.Finished)
            {
                return SolverResult.Success;
            }

            Propagate();

            // Increment step count for calculating failure weights
            timeStep++;

            Profiler.EndSample();

            return SolverResult.Unfinished;
        }

        private bool ExecutePlugins()
        {
            var isValid = true;
            foreach (var plugin in Plugins)
            {
                try
                {
                    var manipulator = plugin.Step(allowedTiles, changes);

                    isValid &= !manipulator.IsFailure;

                    if (!isValid)
                    {
                        continue;
                    }
                    
                    foreach (var (index, variation) in manipulator.Block)
                    {
                        SetTileAllowed(index, variation, false);
                    }

                    foreach (var (index, variation) in manipulator.SetTiles)
                    {
                        SetTile(index, variation);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"The plugin \"{plugin.GetType().Name}\" had an exception and will be ignored.");
                    Debug.LogException(e);
                }
            }
            
            return isValid;
        }

        public TileVariation[][] GetModel()
        {
            var resultModel = new TileVariation[gridSize][];
            for (var index = 0; index < gridSize; index++)
            {
                resultModel[index] = new TileVariation[allowedTileCount[index]];

                // Find the valid variation
                var i = 0;
                for (var variation = 0; variation < input.TileVariations.Length; variation++)
                {
                    if (allowedTiles[index, variation])
                    {
                        resultModel[index][i] = input.TileVariations[variation];
                        i++;
                    }
                }
            }

            return resultModel;
        }

        private ObserveResult Observe()
        {
            Profiler.BeginSample("StixGames.TileComposer.Observe");

            double minEntropy = double.PositiveInfinity;

            var lowEntropyStates = new List<int>();

            for (int i = 0; i < gridSize; i++)
            {
                var tileCount = allowedTileCount[i];

                // There is an impossible state in the model
                if (tileCount <= 0)
                {
                    failureIndex = i;
                    Profiler.EndSample();
                    return ObserveResult.Failure;
                }

                // Skip tiles that are already defined
                if (tileCount == 1)
                {
                    continue;
                }

                var entropy = entropies[i];
                if (entropy < minEntropy && Math.Abs(entropy - minEntropy) > Epsilon)
                {
                    // If entropy smaller than minEntropy
                    minEntropy = entropy;
                    lowEntropyStates.Clear();
                    lowEntropyStates.Add(i);
                }
                else if (Math.Abs(entropy - minEntropy) <= Epsilon)
                {
                    // If entropy and minEntropy are approximately equal
                    lowEntropyStates.Add(i);
                }
            }

            // If the model is complete, return true
            if (lowEntropyStates.Count == 0)
            {
                Profiler.EndSample();
                return ObserveResult.Finished;
            }

            // Randomly select one of the possibilities
            var minEntropyIndex = lowEntropyStates[input.Random.Next(lowEntropyStates.Count)];

            // Randomly select a tileVariation for this tile
            double weightSample = input.Random.NextDouble() * sumsOfWeights[minEntropyIndex];
            double current = 0;
            int variationIndex = -1;
            for (int i = 0; i < input.TileVariations.Length; i++)
            {
                if (allowedTiles[minEntropyIndex, i])
                {
                    current += weights[i];

                    if (current >= weightSample)
                    {
                        variationIndex = i;
                        break;
                    }
                }
            }

            Assert.IsTrue(variationIndex >= 0);

            SetTile(minEntropyIndex, variationIndex);

            Profiler.EndSample();

            return ObserveResult.Progressing;
        }

        private void Backtrack()
        {
            Profiler.BeginSample("StixGames.TileComposer.Backtrack");

            UpdateFailureWeight(failureIndex);
            var (weight, _, _) = failureWeights[failureIndex];

            // Calculate the backtrack step count from the failure weight at the failures position
            var backtrackingSteps = settings.BacktrackSteps + weight / settings.BacktrackStepsMultiplier;

            // Backtrack
            bool stackEmpty = false;
            for (int i = 0; i < backtrackingSteps; i++)
            {
                // If the stack is empty, stop backtracking
                if (backtrackStack.Count == 0)
                {
                    break;
                }

                var backtrackedChanges = backtrackStack.Pop();

                // If the stack is empty, create a new step, to save the changes made during the current backtracking
                if (backtrackStack.Count == 0)
                {
                    backtrackStack.Push(new List<(int, int, bool)>());
                    stackEmpty = true;
                }

                foreach (var (index, variation, isAllowed) in backtrackedChanges)
                {
                    SetTileAllowed(index, variation, !isAllowed, true);
                }

                if (stackEmpty)
                {
                    break;
                }
            }

            // Reset around the failure after backtracking, so backtrack changes are overwritten, instead of backtracking messing up the data
            var failureResetRadius = (int) (settings.FailureResetRadius + weight / settings.RadiusSizeMultiplier);

            var slice = new Slice[grid.Axes];
            var coordinates = grid.IndexToCoordinates(failureIndex);
            for (var i = 0; i < slice.Length; i++)
            {
                slice[i] = new Slice
                {
                    Start = coordinates[i] - failureResetRadius,
                    End = coordinates[i] + failureResetRadius
                };
            }

            var indices = grid.SliceToIndices(slice, false);
            var borders = grid.SliceBorderSides(slice, false);
            ResetTiles(indices, borders);

            Profiler.EndSample();
        }

        private void UpdateFailureWeight(int index)
        {
            var (weight, acceleration, lastUpdate) = failureWeights[index];

            var newWeight = weight * Mathf.Exp((lastUpdate - timeStep) * failureDecayPower);
            var newAcceleration = acceleration * Mathf.Exp((lastUpdate - timeStep) * failureAccelerationDecayPower);
            failureWeights[index] = (newWeight, newAcceleration, timeStep);
        }

        private void ResetTiles(int[] indices, bool[,] borders)
        {
            Profiler.BeginSample("StixGames.TileComposer.ResetTiles");

            // Add debug information for the reset
            ResetIndices.AddRange(indices);

            // Increase the failure weights for the current area
            foreach (var index in indices)
            {
                UpdateFailureWeight(index);

                var (weight, acceleration, lastUpdate) = failureWeights[index];
                failureWeights[index] = (weight + 1 + acceleration,
                    acceleration + 1.0f / settings.FailureAccelerationMultiplier,
                    lastUpdate);
            }

            var borderNeighbors = new List<int>();

            // First pass: allow all tiles in the area, then propagate forbidden tiles from the border
            for (var i = 0; i < indices.Length; i++)
            {
                var index = indices[i];

                for (int variation = 0; variation < input.TileVariations.Length; variation++)
                {
                    // Abort if the current variations is blocked
                    if (blockedVariationSets[index].Contains(variation))
                    {
                        continue;
                    }

                    // Allow the current variation, unless it is blocked
                    SetTileAllowed(index, variation, true, false, false);

                    for (int side = 0; side < grid.Sides; side++)
                    {
                        // Compatible neighbors uses opposing sides as optimization
                        var localCompatibilitySide = grid.GetNeighborSide(side);

                        if (borders[i, side])
                        {
                            // The side is on the border of the reset area
                            var neighbor = neighborList[index, side];

                            // The current side is at the border
                            if (neighbor < 0)
                            {
                                continue;
                            }

                            borderNeighbors.Add(neighbor);

                            // If the tile is still not allowed, propagate the change
                            if (compatibleNeighbors[index, variation, localCompatibilitySide] == 0)
                            {
                                SetTileAllowed(index, variation, false);
                            }
                        }
                    }
                }
            }

            // Propagate all disallowed tiles
            Propagate();

            //Pass 2: Check the border area and propagate tiles that are now allowed again
            foreach (var neighbor in borderNeighbors)
            {
                for (int variation = 0; variation < input.TileVariations.Length; variation++)
                {
                    // Abort if the current variations is blocked
                    if (blockedVariationSets[neighbor].Contains(variation))
                    {
                        continue;
                    }

                    if (IsVariationValid(neighbor, variation))
                    {
                        SetTileAllowed(neighbor, variation, true);
                    }
                }
            }

            Profiler.EndSample();
        }

        private void Propagate()
        {
            Profiler.BeginSample("StixGames.TileComposer.Propagate");

            while (propagationQueue.Count != 0)
            {
                var (currentIndex, currentVariation, isBacktracking) = propagationQueue.Dequeue();

                // Don't propagate impossible tiles, or it will be impossible to backtrack
                if (allowedTileCount[currentIndex] == 0)
                {
                    continue;
                }

                // Iterate all directions, so increment / decrement the number of compatible neighbors for all neighbors
                for (int side = 0; side < grid.Sides; side++)
                {
                    var neighbor = neighborList[currentIndex, side];

                    // Don't do anything if the neighbor is outside the input.grid
                    if (neighbor < 0)
                    {
                        continue;
                    }

                    // Get all variations the current variation was compatible with in this direction
                    var compatible = tileCompatibility[currentVariation, side];

                    // Now add / remove all of them from the neighbor
                    foreach (var variation in compatible)
                    {
                        // TODO: Check if blocked tiles are still respected with backtracking

                        // Propagate the backtracking
                        if (isBacktracking)
                        {
                            // The tile is already allowed on this neighbor, no need to handle it further
                            if (allowedTiles[neighbor, variation])
                            {
                                continue;
                            }

                            // If the variation is allowed by all other neighbors, propagate the change
                            if (IsVariationValid(neighbor, variation))
                            {
                                SetTileAllowed(neighbor, variation, true);
                            }
                        }
                        else
                        {
                            // The variation isn't allowed by one of the neighbors any more, propagate this change
                            if (compatibleNeighbors[neighbor, variation, side] == 0)
                            {
                                SetTileAllowed(neighbor, variation, false);
                            }
                        }
                    }
                }
            }

            Profiler.EndSample();
        }

        private bool HasBlockedTiles(int index) =>
            input.BlockedTiles[index] != null && input.BlockedTiles[index].Length > 0;

        private bool IsVariationValid(int index, int variation)
        {
            // Check if the neighbor has any other neighbors that prevent the current variation
            bool propagateVariation = true;
            for (int checkSide = 0; checkSide < grid.Sides; checkSide++)
            {
                if (compatibleNeighbors[index, variation, checkSide] == 0)
                {
                    propagateVariation = false;
                    break;
                }
            }

            return propagateVariation;
        }

        private void Clear()
        {
            compatibleNeighbors = CreateCompatibleNeighborsMatrix();
            allowedTiles = new bool[gridSize, input.TileVariations.Length];

            for (var i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < input.TileVariations.Length; j++)
                {
                    // Allow all at the start
                    allowedTiles[i, j] = true;
                }
            }

            propagationQueue.Clear();

            for (int i = 0; i < gridSize; i++)
            {
                allowedTileCount[i] = weights.Length;
                sumsOfWeights[i] = sumOfWeights;
                sumsOfWeightsLogWeights[i] = sumOfWeightLogWeights;
                entropies[i] = startEntropy;
            }

            // Reset start tiles
            for (var i = 0; i < input.BlockedTiles.Length; i++)
            {
                // Skip uninitialized
                if (!HasBlockedTiles(i))
                {
                    continue;
                }

                BlockTiles(i, input.BlockedTiles[i]);
            }
        }

        /// <summary>
        /// Sets the tile to its final value and register the change for propagation
        /// </summary>
        /// <param name="index"></param>
        /// <param name="variation"></param>
        private void SetTile(int index, int variation)
        {
            // Add all disabled variations to propagation
            for (int i = 0; i < input.TileVariations.Length; i++)
            {
                if (i != variation)
                {
                    SetTileAllowed(index, i, false);
                }
            }
        }

        /// <summary>
        /// Block all tile variations in the list
        /// </summary>
        /// <param name="index"></param>
        /// <param name="blockedVariations"></param>
        private void BlockTiles(int index, TileVariation[] blockedVariations)
        {
            foreach (var blockedVariation in blockedVariations)
            {
                SetTileAllowed(index, tileVariationIndices[blockedVariation], false);
            }
        }

        private void SetTileAllowed(int index, int variation, bool isAllowed, bool isBacktracking = false,
            bool doPropagate = true)
        {
            // The variation has already been set
            if (allowedTiles[index, variation] == isAllowed)
            {
                return;
            }

            Profiler.BeginSample("StixGames.TileComposer.SetTileAllowed");

            changes.Add(new WFCChange(index, variation, isAllowed));
            
            // Change if the variation is allowed at the current index
            allowedTiles[index, variation] = isAllowed;

            var sign = isAllowed ? 1 : -1;

            allowedTileCount[index] += sign;
            sumsOfWeights[index] += weights[variation] * sign;
            sumsOfWeightsLogWeights[index] += weightLogWeights[variation] * sign;
            entropies[index] = Math.Log(sumsOfWeights[index]) - sumsOfWeightsLogWeights[index] / sumsOfWeights[index];

            // Change the allowed neighbors
            for (int side = 0; side < grid.Sides; side++)
            {
                var neighbor = neighborList[index, side];

                // Don't do anything if the neighbor is outside the input.grid
                if (neighbor < 0)
                {
                    continue;
                }

                // Get all variations the current variation was compatible with in this direction
                var compatibleVariations = tileCompatibility[variation, side];

                // Now add / remove all of them from the neighbor
                for (var i = 0; i < compatibleVariations.Length; i++)
                {
                    // Reduce the amount of compatible neighbors for the opposite side on the neighbor.
                    compatibleNeighbors[neighbor, compatibleVariations[i], side] += sign;
                }
            }

            // Add to propagation queue
            if (!isBacktracking)
            {
                backtrackStack.Peek().Add((index, variation, isAllowed));
            }

            // Don't propagate allowing tiles again, this slows down calculations more than it helps.
            if (isAllowed)
            {
                Profiler.EndSample();
                return;
            }

            if (doPropagate)
            {
                propagationQueue.Enqueue((index, variation, isAllowed));
            }

            Profiler.EndSample();
        }

        private int[,] CreateInitialCompatibleNeighborsArray()
        {
            // As the input.grid is uninitialized at the start, it will have the same possibility for all directions
            var initialNeighbors = new int[input.TileVariations.Length, grid.Sides];
            for (int current = 0; current < input.TileVariations.Length; current++)
            {
                for (int oppositeSide = 0; oppositeSide < grid.Sides; oppositeSide++)
                {
                    initialNeighbors[current, oppositeSide] =
                        tileCompatibility[current, grid.GetNeighborSide(oppositeSide)].Length;
                }
            }

            // If a variation has 0 compatible tiles in any direction, it is not possible to use this tile at all
            for (int variation = 0; variation < input.TileVariations.Length; variation++)
            {
                for (int side = 0; side < grid.Sides; side++)
                {
                    if (initialNeighbors[variation, side] == 0)
                    {
                        Debug.LogWarning(
                            $"{nameof(TileVariation)} can not be used at all, " +
                            $"as it is not compatible with any other tiles on at least one side: " +
                            $"Tile: {input.TileVariations[variation].Tile} Sides: {grid.SideNames[grid.GetNeighborSide(side)]}");
                    }
                }
            }

            return initialNeighbors;
        }

        private int[,,] CreateCompatibleNeighborsMatrix()
        {
            int[,,] neighbors = new int[gridSize, input.TileVariations.Length, grid.Sides];

            // Copy the initial value to all input.grid positions
            for (int i = 0; i < gridSize; i++)
            {
                for (int variation = 0; variation < input.TileVariations.Length; variation++)
                {
                    for (int side = 0; side < grid.Sides; side++)
                    {
                        neighbors[i, variation, side] = initialCompatibleNeighbors[variation, side];
                    }
                }
            }

            return neighbors;
        }

        private enum ObserveResult
        {
            Finished,
            Failure,
            Progressing
        }
    }
}