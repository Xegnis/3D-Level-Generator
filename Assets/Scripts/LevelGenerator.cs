using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NodeType {Empty = 0, Road, Building }

public class Node
{
    public bool completed { get; set; }
    public List<Node> neighbors;
    public NodeType type { get; set; }

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
    const int MIN_X_SIZE = 6;
    const int MIN_Y_SIZE = 6;
    public static LevelGenerator lg;
    public int xSize;
    public int ySize;
    public GameObject roadLeftPrefab;
    public GameObject roadEmptyPrefab;
    [Header("Generators")]
    public GameObject digger;
    public GameObject residentialGenerator;
    public GameObject commercialGenerator;
    public GameObject industrialGenerator;
    public Grid2D<Node> mainGrid;

    bool _roadGenerated, _allGenerated;

    void Start()
    {
        lg = this;
        mainGrid = new Grid2D<Node>(xSize, ySize);
        mainGrid.Initialize();
        /*SpawnResidential();
        SpawnIndustrial();
        SpawnCommercial();*/
        DigRoad();
    }

    void DigRoad ()
    {
        Vector2Int pos = new Vector2Int(Random.Range(mainGrid.xSize * 3 / 7, mainGrid.xSize * 4 / 7), 0);
        GameObject d = Instantiate(digger, mainGrid.GridToWorld(pos.x, pos.y), Quaternion.identity);
        d.GetComponent<Digger>().grid = mainGrid;
    }

    public void GenerateRoad ()
    {
        for (int x = 0; x < mainGrid.xSize; x++)
        {
            for (int y = 0; y < mainGrid.ySize; y++)
            {
                if (mainGrid.GetAt(x, y).type == NodeType.Road)
                {
                    if (x > 0 && mainGrid.GetAt(x - 1, y).type != NodeType.Road)
                    {
                        GameObject road = Instantiate(roadLeftPrefab, mainGrid.GridToWorld(x, y) + new Vector3(0.5f, 0, 0.5f), Quaternion.identity);
                        road.transform.SetParent(transform);
                    }
                    else if (x < mainGrid.xSize - 1 && mainGrid.GetAt(x + 1, y).type != NodeType.Road)
                    {
                        GameObject road = Instantiate(roadLeftPrefab, mainGrid.GridToWorld(x, y) + new Vector3(0.5f, 0, 0.5f), Quaternion.identity);
                        road.transform.Rotate(new Vector3(0, 180f, 0));
                        road.transform.SetParent(transform);
                    }
                    else if (y > 0 && mainGrid.GetAt(x, y - 1).type != NodeType.Road)
                    {
                        GameObject road = Instantiate(roadLeftPrefab, mainGrid.GridToWorld(x, y) + new Vector3(0.5f, 0, 0.5f), Quaternion.identity);
                        road.transform.Rotate(new Vector3(0, -90f, 0));
                        road.transform.SetParent(transform);
                    }
                    else if (y < mainGrid.ySize - 1 && mainGrid.GetAt(x, y + 1).type != NodeType.Road)
                    {
                        GameObject road = Instantiate(roadLeftPrefab, mainGrid.GridToWorld(x, y) + new Vector3(0.5f, 0, 0.5f), Quaternion.identity);
                        road.transform.Rotate(new Vector3(0, 90f, 0));
                        road.transform.SetParent(transform);
                    }
                    else
                    {
                        GameObject road = Instantiate(roadEmptyPrefab, mainGrid.GridToWorld(x, y) + new Vector3(0.5f, 0, 0.5f), Quaternion.identity);
                        road.transform.SetParent(transform);
                    }
                }
            }
        }
        GameObject ground = Instantiate(roadEmptyPrefab, mainGrid.GridToWorld(mainGrid.xSize / 2, mainGrid.ySize / 2) + new Vector3(0.5f, -0.5f, 0.5f), Quaternion.identity);
        ground.transform.localScale = new Vector3(mainGrid.xSize, 0, mainGrid.ySize);
        _roadGenerated = true;
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

    void SpawnIndustrial (int x, int y, int xSize, int ySize)
    {
        Vector3 worldPos = mainGrid.GridToWorld(x, y);
        GameObject generator = Instantiate(industrialGenerator, worldPos, Quaternion.identity);
        Generator rg = generator.GetComponent<Generator>();
        rg.xSize = xSize;
        rg.ySize = ySize;
        rg.Generate();
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
        if (!_roadGenerated || _allGenerated)
            return;
        Vector2Int pos = new Vector2Int(-1, -1);
        for (int x = 0; x < mainGrid.xSize; x++)
        {
            for (int y = 0; y < mainGrid.ySize; y++)
            {
                if (x == mainGrid.xSize - 1 && y == mainGrid.ySize - 1)
                {
                    _allGenerated = true;
                    Debug.Log("All Done");
                    return;
                }
                if (!mainGrid.GetAt(x, y).completed)
                {
                    pos = new Vector2Int(x, y);
                    Debug.Log("Working on " + pos);
                    break;
                }
            }
            if (pos.x != -1)
                break;
        }
        int xSize = 0, ySize = 0;
        for (int x = pos.x; x < mainGrid.xSize; x++)
        {
            if (mainGrid.GetAt(x, pos.y).completed)
            {
                xSize = x - pos.x;
                break;
            }
            xSize = x - pos.x;
        }
        for (int y = pos.y; y < mainGrid.ySize; y++)
        {
            if (mainGrid.GetAt(pos.x, y).completed)
            {
                ySize = y - pos.y;
                break;
            }
            ySize = y - pos.y;
        }
        Debug.Log("X Size: " + xSize + ", Y Size: " + ySize);
        for (int x = 0; x <= xSize; x++)
        {
            for (int y = 0; y <= ySize; y++)
            {
                mainGrid.GetAt(x + pos.x, y + pos.y).completed = true;
                Debug.Log("Marked");
            }
        }
        if (xSize >= MIN_X_SIZE && ySize >= MIN_Y_SIZE)
        {
            SpawnIndustrial(pos.x, pos.y, xSize, ySize);
        }
    }
}
