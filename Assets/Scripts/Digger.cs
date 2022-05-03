using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Digger : MonoBehaviour
{
    public GameObject buildingPrefab;
    public GameObject commercialGenerator;
    public Grid2D<Node> grid;
    public Vector3Int defaultDirection;
    public int stepSize;
    [Header("Parameters")]
    public float chanceToTurnLeft;
    public float chanceToTurnRight;
    public float chanceToTurnAround;
    public float chanceToStamp;

    public void DigOnce()
    {
        if (Random.value < chanceToTurnLeft)
            Turn(90);
        else if (Random.value < chanceToTurnRight)
            Turn(-90);
        else if (Random.value < chanceToTurnAround)
            Turn(180);
        transform.position += transform.right * stepSize;
        if (Random.value > chanceToStamp)
            return;
        Vector2Int pos = grid.WorldToGrid(transform.position.x, transform.position.z);
        if (pos.x > grid.xSize - stepSize || pos.x < 0 || pos.y < 0 || pos.y > grid.ySize - stepSize)
        {
            Turn(90);
            return;
        }    
        if (grid.GetAt(pos.x, pos.y).completed)
        {
            return;
        }

        GameObject building = Instantiate(buildingPrefab, transform.position, Quaternion.identity);
        building.transform.SetParent(commercialGenerator.transform);
        for (int x = 0; x < stepSize; x++)
        {
            for (int y = 0; y < stepSize; y++)
            {
                grid.GetAt(pos.x + x, pos.y + y).completed = true;
            }
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
