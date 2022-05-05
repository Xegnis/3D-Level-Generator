using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Digger : MonoBehaviour
{
    public Grid2D<Node> grid;
    public GameObject diggerPrefab;
    public static int minStepSize = 4;
    public static int counter = 0;

    Vector3[] directions = { Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

    [Header("Parameters")]
    public int stepSize;
    public Vector3 dir;
    public float chanceToSpawnNewDigger = 0.005f;

    float _currentChance;

    void Start()
    {
        counter++;
    }

    void OnDestroy()
    {
        counter--;
        if (counter == 0)
        {
            LevelGenerator.lg.GenerateRoad();
        }
    }

    public void DigOnce()
    {
        Vector2Int pos = grid.WorldToGrid(transform.position.x, transform.position.z);
        if (transform.position.x < 0 || (pos.x + stepSize) >= grid.xSize || transform.position.z < 0 || (pos.y + stepSize) >= grid.ySize)
        {
            Destroy(gameObject);
        }
        for (int x = 0; x < stepSize; x++)
        {
            for (int y = 0; y < stepSize; y++)
            {
                Node node = grid.GetAt(pos.x + x, pos.y + y);
                if (node.completed != true)
                {
                    node.completed = true;
                    _currentChance += chanceToSpawnNewDigger;
                    node.type = NodeType.Road;
                }
            }
        }
        transform.position += dir;
    }

    public void SpawnNewDigger ()
    {
        GameObject diggerObj = Instantiate(diggerPrefab, transform.position, Quaternion.identity);
        Digger digger = diggerObj.GetComponent<Digger>();
        digger.grid = grid;
        do
        {
            digger.dir = directions[Random.Range(0, directions.Length)];
        }
        while (digger.dir.Equals(dir));
        digger.stepSize = Random.Range(minStepSize, stepSize - 1);
        if (digger.dir.Equals(-dir))
            digger.stepSize = stepSize;
    }

    void Update()
    {
        DigOnce();
        if (Random.value < _currentChance)
        {
            SpawnNewDigger();
            _currentChance = 0;
        }
    }

    //counterclockwise
    void Turn(float degree)
    {
        transform.Rotate(new Vector3(0, -degree, 0));
    }

    /*bool CanTurn(float degree)
    {
        Vector3Int newDir = Turn(degree);
        if (_xPos + newDir.x * stepSize < 0 || _xPos + newDir.x * stepSize > grid.xSize - stepSize)
            return false;
        if (_yPos + newDir.y * stepSize < 0 || _yPos + newDir.y * stepSize > grid.ySize - stepSize)
            return false;
        return true;
    }*/
}
