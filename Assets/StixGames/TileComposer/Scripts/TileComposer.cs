using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using StixGames.TileComposer;
using StixGames.TileComposer.Solvers;
using StixGames.TileComposer.Solvers.WFCPlugins;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class ModelGeneratedEventData
{
    public IGrid Grid;
    public Transform InstantiatedParent;
    public TileVariation[] Model;
    public bool RegisterUndo;
}

[Serializable]
public class ModelGeneratedEvent : UnityEvent<ModelGeneratedEventData>
{
}

namespace StixGames.TileComposer
{
    [AddComponentMenu("Stix Games/Tile Composer/Tile Composer")]
    public class TileComposer : MonoBehaviour
    {
        [Header("General")] public TileCollection TileCollection;
        public int Seed;

        [Tooltip(
            "How many different seeds does the algorithm try before giving up. Be aware that the timeout is for each try.")]
        public int MaxTries = 1;

        [Tooltip("Wave Function Collapse is typically faster, but might not be able to solve complex tilesets, " +
                 "while Z3 is very robust at the price of higher performance cost.")]
        public SolverType SolverTypeSelection = SolverType.WaveFunctionCollapse;

        [Min(1)]
        [Tooltip(
            "To speed up calculations, the solvers can try to find a solution with multiple seeds at the same time. This may not be supported by all solvers.")]
        public int ParallelInstances = 1;

        [Tooltip("The timeout in seconds after which the calculation will be aborted.")]
        public float Timeout = 60;
        
        [Header("Behaviour")]
        [Tooltip("Runs the calculations in a different thread. Doesn't work while using slow generation mode")]
        public bool GenerateAsynchronously;
        
        [Tooltip("If true, the component will generate a model in Start(). Otherwise you'll have to call TileComposer.Generate() from a script.")]
        public bool GenerateOnStart = true;

        [Tooltip("Automatically destroys this GameObject once the model is generated")]
        public bool DestroyAfterUse = false;

        [Tooltip("When enabled, the component will instantiate the finished model automatically.")]
        public bool InstantiateModelWhenGenerated = true;
        
        /// <summary>
        /// Called when a model was generated by this tile composer. When a model fails, this will not be called.
        ///
        /// The parameters are the grid for the model, the instantiated model (or null when no model was instantiated),
        /// an array of the selected tiles, register undo.
        /// </summary>
        public ModelGeneratedEvent OnModelGenerated;
        
        public UnityEvent OnModelGenerationFailed; 
        
        public enum SolverType
        {
            WaveFunctionCollapse,
            Z3Solver,
//            Cadical,
        }

        [Header("Grid Settings")] public int[] GridSize;

        [Space] [Tooltip("Block certain tile types from an area.")]
        public TileSlice[] BlockedTiles;

        [Tooltip("Tiles that are fixed to a certain tile type. This overwrites all previous tile restrictions.")]
        [FormerlySerializedAs("TileInitializers")]
        public TileSlice[] FixedTiles;

        public SATSettings SATSettings;

        public WaveFunctionCollapseSettings WFCSettings;

        [Header("Slow Generation")] 
        [Tooltip("When enabled, solvers that support slow generation (e.g. Wave Function Collapse) will show the generation process in the scene.")]
        public bool DoSlowGeneration = false;

        /// <summary>
        /// When set to true the slow generation will not be continued
        /// </summary>
        public bool IsPaused = false;
        
        /// <summary>
        /// Internal variable. Set to bool when you wish to make a single forward step
        /// </summary>
        public bool DoStep = false;
        
        [Header("Slow Generation Settings")]
        [Tooltip("Wait time between solver steps. Use a higher time step to debug details of the generation process." +
                 "\n\nAlternatively, you can pause the scene and walk through generation step by step.")]
        public float TimeStep = 0.1f;
        [Tooltip("Rendering every single solver step takes a lot of performance. If you want to accelerate the slow generation, you can allow the solver to run multiple steps before rendering again.")]
        public int SolverStepsPerStep = 1;

        [Tooltip(
            "Instead of rendering each possible tile, render a default shape that gets smaller as possibilities get eliminated")]
        [Space]
        public bool UseFastDebugMode;

        [Tooltip(
            "In order to debug empty type positions, you can replace each empty type with an object. Use the same order of empties as in the tile collection.")]
        public Transform[] EmptyDebugObjects;

        [Space]
        [Tooltip(
            "Leave empty to use the default material, or use a custom material, which should support transparency, instancing and have a _Color property. Vertex color is used for barycentric coordinates, so wireframe effects are possible.")]
        public Material SlowGenerationMaterial;

        [Tooltip("The transparency for the slow generation material.")]
        public float Transparency = 0.1f;

        private (Matrix4x4, Mesh)[] fastDebugObjectMeshList;

        private Transform spriteRenderParent;

        // These are for debugging in the slow render mode
        private TileVariation[][] slowRenderModel;
        private float slowGenerationWaitTime = Single.NegativeInfinity;
        private IGrid renderGrid;
        private List<int> errorIndexList;
        Dictionary<TileVariation, (Matrix4x4, Mesh)[]> tileMeshDictionary;
        private int tileVariationsLength;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (SlowGenerationMaterial == null)
            {
                SlowGenerationMaterial = new Material(Shader.Find("Hidden/TileComposer/DebugShader"));
            }
        }

        public async void Start()
        {
            if (!GenerateOnStart)
            {
                return;
            }
            
            if (Seed == 0)
            {
                var seedGen = new System.Random();
                for (int i = 0; i < MaxTries; i++)
                {
                    var finished = await GenerateInternal(seedGen.Next(), GenerateAsynchronously, i + 1);

                    if (finished)
                    {
                        break;
                    }
                }
            }
            else
            {
                await GenerateInternal(Seed, GenerateAsynchronously);
            }
        }

        public async void StartGeneration ()
        {
            if (Seed == 0)
            {
                var seedGen = new System.Random();
                for (int i = 0; i < MaxTries; i++)
                {
                    var finished = await GenerateInternal(seedGen.Next(), GenerateAsynchronously, i + 1);

                    if (finished)
                    {
                        break;
                    }
                }
            }
            else
            {
                await GenerateInternal(Seed, GenerateAsynchronously);
            }
        }

        public void Update()
        {
            if (slowRenderModel != null)
            {
                RenderModel(slowRenderModel, errorIndexList, renderGrid, tileMeshDictionary, tileVariationsLength);
            }
        }

        public bool Generate(bool registerUndo = false)
        {
            var seed = Seed;
            if (Seed == 0)
            {
                var seedGen = new System.Random();
                seed = seedGen.Next();
            }

            var task = GenerateInternal(seed, false, 1, registerUndo);
            task.Wait();
            
            return task.Result;
        }

        public async Task<bool> GenerateAsync(bool registerUndo = false)
        {
            var seed = Seed;
            if (Seed == 0)
            {
                var seedGen = new System.Random();
                seed = seedGen.Next();
            }

            var result = await GenerateInternal(seed, true, 1, registerUndo);
            
            return result;
        }

        /// <summary>
        /// Run the calculations.
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="tryCount"></param>
        /// <returns>Returns true if the calculations are finished, this can be because a result was found, or because failure has been proven.</returns>
        private async Task<bool> GenerateInternal(int seed, bool runAsync, int tryCount = 1, bool registerUndo = false)
        {
            var grid = TileCollection.GetGrid(GridSize);
            var variations = TileCollection.GetTileVariations();

            var random = new System.Random(seed);
            var blockedTiles = GetBlockedTiles(grid, variations);

            var input = new TileComposerInput(random, grid, variations, blockedTiles);

            IConstraintSolver solver;
            switch (SolverTypeSelection)
            {
                case SolverType.WaveFunctionCollapse:
                    // Ensure that the weights are not smaller or equal to 0
                    if (WFCSettings.RadiusSizeMultiplier < 0.001f)
                    {
                        WFCSettings.RadiusSizeMultiplier = 0.001f;
                    }

                    if (WFCSettings.BacktrackStepsMultiplier < 0.001f)
                    {
                        WFCSettings.BacktrackStepsMultiplier = 0.001f;
                    }

                    var wfc = new WaveFunctionCollapse(input, WFCSettings);
                    
                    // Add plugins to WFC
                    wfc.Plugins = GetComponents<IWFCPlugin>();
                    foreach (var plugin in wfc.Plugins)
                    {
                        plugin.Initialize(input);
                    }
                    
                    solver = wfc;
                    errorIndexList = wfc.ResetIndices;
                    break;
//                case SolverType.Cadical:
////                    var solverPath = FileAnchor.GetFilePath("WFCData", "CaDiCaL/Win32/CaDiCaL.exe");
////                    var dimacs = new DIMACSSATSolver(solverPath);
//                    var nativeCaDiCaL = new CaDiCaLSolver();
//                    solver = new GeneralSATSolver(input, SATSettings, nativeCaDiCaL);
//                    errorIndexList = new List<int>();
//                    break;
                case SolverType.Z3Solver:
                    solver = new Z3Solver(input, SATSettings);
                    errorIndexList = new List<int>();
                    break;

                default:
                    throw new InvalidEnumArgumentException();
            }

            // Set settings for all solver types
            if (ParallelInstances <= 0)
            {
                ParallelInstances = 1;
            }

            solver.ParallelInstances = ParallelInstances;
            solver.Timeout = Timeout;

            // Generation
            if (DoSlowGeneration && solver.SupportsSlowGeneration)
            {
                Debug.Log($"Start slow generation, seed: {seed}");
                StartCoroutine(SlowGeneration(solver, grid, variations));
                return true;
            }
            else
            {
                // Fast mode
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var result = runAsync ? await solver.CalculateModelAsync() : solver.CalculateModel();

                stopwatch.Stop();

                // Remove the temporary instances, if they exist
                if (spriteRenderParent != null)
                {
                    Destroy(spriteRenderParent.gameObject);
                }

                if (Seed != 0 || result == SolverResult.Success)
                {
                    Debug.Log($"Result: {result}, Seed: {seed}, Try: {tryCount}, Time: {stopwatch.Elapsed}");
                    FinishGenerating(solver, grid, result, registerUndo);

                    return true;
                }

                // Print the result and finish calculations if the failure is guaranteed, otherwise continue
                Debug.Log($"Result: {result}, Seed: {seed}, Try: {tryCount}, Time: {stopwatch.Elapsed}");
                return result == SolverResult.GuaranteedFailure;
            }
        }

        private TileVariation[][] GetBlockedTiles(IGrid grid, TileVariation[] variations)
        {
            var tiles = new List<TileVariation>[grid.GridSize];
            for (var i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new List<TileVariation>();
            }

            foreach (var blockedTile in BlockedTiles)
            {
                var blockedTiles = variations
                    .Where(x => x.TileTypeName == blockedTile.TileType)
                    .ToArray();

                var indices = grid.SliceToIndices(blockedTile.Dimensions);
                foreach (var index in indices)
                {
                    tiles[index].AddRange(blockedTiles);
                }
            }

            foreach (var fixedTile in FixedTiles)
            {
                var blockedTiles = variations
                    .Where(x => x.TileTypeName != fixedTile.TileType)
                    .ToArray();

                var indices = grid.SliceToIndices(fixedTile.Dimensions);
                foreach (var index in indices)
                {
                    tiles[index].Clear();
                    tiles[index].AddRange(blockedTiles);
                }
            }

            return tiles.Select(x => x.ToArray()).ToArray();
        }

        private IEnumerator SlowGeneration(IConstraintSolver solver, IGrid grid, TileVariation[] variations)
        {
            solver.Reset();

            renderGrid = grid;
            tileMeshDictionary = CreateTileMeshDictionary(variations);
            tileVariationsLength = variations.Length;

            SolverResult state;
            do
            {
                slowRenderModel = solver.GetModel();

                yield return new WaitUntil(NextSlowGenerationStep);

                slowGenerationWaitTime = Time.time;
                
                // Do at least 1 step
                state = solver.Step();
                for (int i = 0; i < SolverStepsPerStep - 1; i++)
                {
                    state = solver.Step();
                }
            } while (state == SolverResult.Unfinished);

            slowRenderModel = null;

            Debug.Log($"Result: {state}");

            FinishGenerating(solver, grid, state);
        }

        private bool NextSlowGenerationStep()
        {
            if (DoStep)
            {
                // If a step was requested, ignore everything else and step!
                DoStep = false;
                return true;
            }
            
            if (IsPaused)
            {
                return false;
            }

            // Regular time step, it's not paused
            return Time.time - slowGenerationWaitTime > TimeStep;
        }

        private void FinishGenerating(IConstraintSolver solver, IGrid grid, SolverResult result, bool registerUndo = false)
        {
            var model = solver.GetModel();

            Transform modelParent = null;
            if (InstantiateModelWhenGenerated)
            {
                modelParent = InstantiateModel(model, grid, registerUndo);
            }

            // OnModelGenerated will only be called when the model generates successfully
            if (result == SolverResult.Success)
            {
                var cleanedModel = model.Select(x => x.SingleOrDefault()).ToArray();
                
                // There should be exactly one tile in each grid position
                Assert.IsTrue(cleanedModel.All(x => x != null));
            
                OnModelGenerated.Invoke(new ModelGeneratedEventData
                {
                    Grid = grid,
                    InstantiatedParent = modelParent,
                    Model = cleanedModel,
                    RegisterUndo = registerUndo
                });
            }
            else
            {
                OnModelGenerationFailed.Invoke();
            }
            

            if (DestroyAfterUse)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        /// <summary>
        /// Creates a dictionary with lookups for all tile variations to their sub-meshes and local transformation matrices
        /// </summary>
        /// <param name="variations"></param>
        /// <returns></returns>
        private Dictionary<TileVariation, (Matrix4x4, Mesh)[]> CreateTileMeshDictionary(TileVariation[] variations)
        {
            var dict = new Dictionary<TileVariation, (Matrix4x4, Mesh)[]>();

            foreach (var variation in variations)
            {
                var t = variation.Tile?.transform;

                if (t == null)
                {
                    var emptyIndex = Array.IndexOf(TileCollection.EmptyTiles.Select(x => x.Name).ToArray(),
                        variation.EmptyName);
                    t = EmptyDebugObjects[emptyIndex];

                    if (t == null)
                    {
                        continue;
                    }
                }

                var meshList = CreateMeshList(t);
                dict[variation] = meshList;
            }

            // Get the slow generation mesh
            var slowGenerationMesh = renderGrid.GetSlowGenerationMesh();

            // Save mesh
            fastDebugObjectMeshList = new[] {(Matrix4x4.identity, slowGenerationMesh)};

            return dict;
        }

        private static (Matrix4x4, Mesh)[] CreateMeshList(Transform t)
        {
            var inverseParentMatrix = t.worldToLocalMatrix;
            var meshFilters = t.GetComponentsInChildren<MeshFilter>().Where(x => x.sharedMesh != null);

            var list = new List<(Matrix4x4, Mesh)>();
            foreach (var meshFilter in meshFilters)
            {
                var localToWorldMatrix = meshFilter.transform.localToWorldMatrix;
                var localToParentMatrix = inverseParentMatrix * localToWorldMatrix;

                list.Add((localToParentMatrix, meshFilter.sharedMesh));
            }

            var meshList = list.ToArray();
            return meshList;
        }

        private void RenderModel(TileVariation[][] model, IEnumerable<int> errorIndices, IGrid grid,
            Dictionary<TileVariation, (Matrix4x4, Mesh)[]> tileMeshes, int variationsCount)
        {
            Profiler.BeginSample("StixGames.TileComposer.RenderModel");

            // I'm checking if it's already set, in case setting the value triggers a recompilation of the shader
            if (!SlowGenerationMaterial.enableInstancing)
            {
                SlowGenerationMaterial.enableInstancing = true;
            }

            var parentMatrix = transform.localToWorldMatrix;

            var renderList = new Dictionary<Mesh, (List<Matrix4x4>, List<Vector4>)>();

            // There are no meshes in the tile list, try initializing the objects instead
            if (tileMeshes.Count == 0)
            {
                if (spriteRenderParent != null)
                {
                    Destroy(spriteRenderParent.gameObject);
                }

                spriteRenderParent = InstantiateModel(model, grid);
            }

            // Collect the matrices and additional infos for all meshes
            for (var i = 0; i < model.Length; i++)
            {
                var variations = model[i];

                // Ignore impossible tiles, 
                if (variations.Length == 0)
                {
                    continue;
                }

                var position = grid.GetPosition(i);
                var rotation = grid.GetTileRotation(i);
                if (UseFastDebugMode)
                {
                    if (variations.Length == 1)
                    {
                        var variation = variations[0];

                        // Ignore empty
                        if (variation == null)
                        {
                            continue;
                        }

                        // Ignore variations without meshes
                        if (!tileMeshes.ContainsKey(variation))
                        {
                            continue;
                        }

                        var worldMatrix = parentMatrix *
                                          Matrix4x4.TRS(position, rotation * variation.Rotation, Vector3.one);
                        var meshList = tileMeshes[variation];
                        var color = variations.Length == 1 ? Color.green : Color.white;

                        RenderTile(renderList, meshList, worldMatrix, color);
                    }
                    else
                    {
                        var possibilitiesLeft = (float) variations.Length / variationsCount;
                        var worldMatrix = parentMatrix * Matrix4x4.TRS(position, rotation,
                                              Vector3.one * possibilitiesLeft);
                        var meshList = fastDebugObjectMeshList;
                        var color = variations.Length == 1 ? Color.green : Color.white;

                        RenderTile(renderList, meshList, worldMatrix, color);
                    }
                }
                else
                {
                    var tileMatrix = parentMatrix * Matrix4x4.TRS(position, rotation, Vector3.one);

                    foreach (var variation in variations)
                    {
                        // Ignore empty
                        if (variation == null)
                        {
                            continue;
                        }

                        // Ignore variations without meshes
                        if (!tileMeshes.TryGetValue(variation, out var meshList))
                        {
                            continue;
                        }

                        var worldMatrix = tileMatrix * Matrix4x4.Rotate(variation.Rotation);
                        var color = variations.Length == 1 ? Color.green : Color.white;

                        RenderTile(renderList, meshList, worldMatrix, color);
                    }
                }
            }

            // Add removed tiles to the render list
            foreach (var i in errorIndexList)
            {
                var position = grid.GetPosition(i);
                var rotation = grid.GetTileRotation(i);

                var worldMatrix = parentMatrix * Matrix4x4.TRS(position, rotation, Vector3.one);
                var meshList = fastDebugObjectMeshList;

                RenderTile(renderList, meshList, worldMatrix, Color.red);
            }

            // Render them using instancing
            foreach (var meshPair in renderList)
            {
                var (matrices, colorList) = meshPair.Value;

                for (int i = 0; i < matrices.Count; i += 1023)
                {
                    var start = i;
                    var count = Math.Min(1023, matrices.Count - i);

                    var matrixRange = matrices.GetRange(start, count);
                    var colorValueRange = colorList.GetRange(start, count);
                    var properties = new MaterialPropertyBlock();
                    properties.SetVectorArray(ColorProperty, colorValueRange);
                    Graphics.DrawMeshInstanced(meshPair.Key, 0, SlowGenerationMaterial, matrixRange, properties);
                }
            }

            Profiler.EndSample();
        }

        private void RenderTile(Dictionary<Mesh, (List<Matrix4x4>, List<Vector4>)> renderList,
            (Matrix4x4, Mesh)[] meshList, Matrix4x4 worldMatrix, Color color)
        {
            foreach (var (localMatrix, mesh) in meshList)
            {
                var matrix = worldMatrix * localMatrix;
                color.a = Transparency;


                // Add the tile to the list of rendered meshes
                if (renderList.TryGetValue(mesh, out var tuple))
                {
                    var matrices = tuple.Item1;
                    var isFixedList = tuple.Item2;

                    matrices.Add(matrix);
                    isFixedList.Add(color);
                }
                else
                {
                    renderList[mesh] =
                    (
                        new List<Matrix4x4>
                        {
                            matrix
                        },
                        new List<Vector4>
                        {
                            color
                        }
                    );
                }
            }
        }

        private Transform InstantiateModel(TileVariation[][] model, IGrid grid, bool registerUndo = false)
        {
            var parent = new GameObject("Model").transform;
            var t = transform;
            parent.position = t.position;
            parent.rotation = t.rotation;
            parent.localScale = t.localScale;

#if UNITY_EDITOR
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(parent.gameObject, "Create constraint based model");
            }
#endif

            bool hasDisplayedNullWarning = false;
            for (var i = 0; i < model.Length; i++)
            {
                var variations = model[i];

                // Ignore invalid tiles
                if (variations == null)
                {
                    if (!hasDisplayedNullWarning)
                    {
                        Debug.LogWarning("Some of the tiles in the returned model were set to null, " +
                                         "which could be an indication for a bug. " +
                                         "Impossible tiles should be represented as empty arrays instead.");
                        hasDisplayedNullWarning = true;
                    }
                    
                    continue;
                }
                
                // Ignore impossible tiles, 
                if (variations.Length == 0 || variations.Length > 1)
                {
                    continue;
                }

                var position = grid.GetPosition(i);
                var rotation = grid.GetTileRotation(i);

                foreach (var variation in variations)
                {
                    // Ignore empty
                    if (variation == null)
                    {
                        continue;
                    }

                    rotation = rotation * variation.Rotation;
                    InstantiateTile(variation, parent, position, rotation, Vector3.one, registerUndo);
                }
            }

            return parent;
        }

        private void InstantiateTile(TileVariation tile, Transform parent, Vector3 position,
            Quaternion rotation, Vector3 scale, bool registerUndo = false)
        {
            // Take the appropriate empty proxy, or the real object
            Transform t = null;
            if (tile.Tile == null)
            {
                var emptyIndex = Array.IndexOf(TileCollection.EmptyTiles.Select(x => x.Name).ToArray(), tile.EmptyName);

                if (emptyIndex < EmptyDebugObjects.Length && EmptyDebugObjects[emptyIndex] != null)
                {
                    t = EmptyDebugObjects[emptyIndex];
                }
            }
            else
            {
                t = tile.Tile.transform;
            }

            if (t == null)
            {
                return;
            }

            InstantiateTile(t, parent, position, rotation, scale, registerUndo);
        }

        private void InstantiateTile(Transform t, Transform parent, Vector3 position, Quaternion rotation,
            Vector3 scale, bool registerUndo = false)
        {
            var newTile = Instantiate(t, parent);
            newTile.localPosition = position;
            newTile.localRotation = rotation;
            newTile.localScale = scale;

#if UNITY_EDITOR
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(newTile.gameObject, "Create constraint based model");
            }
#endif
        }
    }
}