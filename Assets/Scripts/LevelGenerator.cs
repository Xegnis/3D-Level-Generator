using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public int xSize;
    public int ySize;
    [Header("Generators")]
    public GameObject residentialGenerator;
    public GameObject commercialGenerator;
    public GameObject industrialGenerator;
    public ModelPackScriptableObject modelPack;
    public Grid2D<Node> mainGrid;

    void Start()
    {
        mainGrid = new Grid2D<Node>(xSize, ySize);
        mainGrid.Initialize();
        SpawnResidential();
        SpawnIndustrial();
        SpawnCommercial();
    }


    //Spawn the residential district near the center of the map
    void SpawnResidential ()
    {
        int xGridLocation = Random.Range(mainGrid.xSize / 4, mainGrid.xSize / 2);
        int yGridLocation = Random.Range(mainGrid.ySize / 4, mainGrid.ySize / 2);
        Vector3 worldPos = mainGrid.GridToWorld(xGridLocation, yGridLocation);
        GameObject generator = Instantiate(residentialGenerator, worldPos, Quaternion.identity);
        Generator rg = generator.GetComponent<Generator>();
        rg.Generate();
        mainGrid.Import(rg.grid, xGridLocation, yGridLocation);
    }

    void SpawnCommercial ()
    {
        int xGridLocation = Random.Range(mainGrid.xSize / 4, mainGrid.xSize / 2);
        int yGridLocation = Random.Range(mainGrid.ySize / 4, mainGrid.ySize / 2);
        Vector3 worldPos = mainGrid.GridToWorld(xGridLocation, yGridLocation);
        GameObject generator = Instantiate(commercialGenerator, worldPos, Quaternion.identity);
        Generator rg = generator.GetComponent<Generator>();
        rg.Generate();
        mainGrid.Import(rg.grid, xGridLocation, yGridLocation);
    }

    void SpawnIndustrial ()
    {
        int xGridLocation = Random.Range(mainGrid.xSize / 4, mainGrid.xSize / 2);
        int yGridLocation = Random.Range(mainGrid.ySize / 4, mainGrid.ySize / 2);
        Vector3 worldPos = mainGrid.GridToWorld(xGridLocation, yGridLocation);
        GameObject generator = Instantiate(industrialGenerator, worldPos, Quaternion.identity);
        Generator rg = generator.GetComponent<Generator>();
        rg.Generate();
        mainGrid.Import(rg.grid, xGridLocation, yGridLocation);
    }

    void SpawnOutSkirt ()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
