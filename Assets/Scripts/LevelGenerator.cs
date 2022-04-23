using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NodeType {Empty = 0, Road, Building }

public class Node
{
    public bool completed { get; set; }
    public List<Node> neighbors;
    public NodeType type { get; }

    public Node ()
    {
        completed = false;
        neighbors = new List<Node>();
        type = NodeType.Empty;
    }

    public Node (NodeType type = NodeType.Empty)
    {
        completed = false;
        neighbors = new List<Node>();
        this.type = type;
    }
}

public class LevelGenerator : MonoBehaviour
{
    [Header("Generators")]
    public GameObject residentialGenerator;
    public GameObject commercialGenerator;
    public GameObject industrialGenerator;
    public ModelPackScriptableObject modelPack;
    public Grid2D<Node> mainGrid;

    void Start()
    {
        mainGrid = new Grid2D<Node>(1024, 1024);
        mainGrid.Initialize();
        SpawnResidential();
    }

    //Spawn the residential district near the center of the map
    void SpawnResidential ()
    {
        int xGridLocation = Random.Range(mainGrid.xSize / 4, mainGrid.xSize * 3 / 4);
        int yGridLocation = Random.Range(mainGrid.ySize / 4, mainGrid.ySize * 3 / 4);
        Vector3 worldPos = mainGrid.GridToWorld(xGridLocation, yGridLocation);
        Debug.Log(worldPos);
        GameObject generator = Instantiate(residentialGenerator, worldPos, Quaternion.identity);
        Generator rg = generator.GetComponent<Generator>();
        rg.Generate();
        mainGrid.Import(rg.grid, xGridLocation, yGridLocation);
    }

    void SpawnCommercial ()
    {

    }

    void SpawnIndustrial ()
    {

    }
}
