using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class BPNode
{
    public Grid2D<Node> grid;
    public BPNode parent;
    public float currentStopChance;

    public BPNode () {}

    public BPNode (int x, int y)
    {
        grid = new Grid2D<Node>(x, y);
    }
}
public class IndustrialGenerator : Generator
{
    [Header("Industrial")]
    public GameObject tilePrefab;
    public int minXSize;
    public int minYSize;
    public float stopChance;
    public float buildingChance;

    public override void Generate()
    {
        grid = new Grid2D<Node>(xSize, ySize);
        grid.origin = transform.position;
        BPNode node = new BPNode();
        node.grid = grid;
        Split(node);
    }

    void Split (BPNode node)
    {
        bool canSplitV = CanSplitV(node);
        bool canSplitH = CanSplitH(node);
        if (canSplitV && canSplitH)
        {
            if (Random.value < 0.5f)
            {
                SplitV(node);
            }
            else
            {
                SplitH(node);
            }
        }
        else if (canSplitV)
        {
            SplitV(node);
        }
        else if (canSplitH)
        {
            SplitH(node);
        }
        else
        {
            SpawnBuildings(node);
        }
    }

    //Range between 1/3 and 2/3
    void SplitV (BPNode node)
    {
        int newXSize = Random.Range(node.grid.xSize / 3, node.grid.xSize * 2 / 3);
        BPNode leftNode = new BPNode(newXSize, node.grid.ySize);
        leftNode.grid.origin = node.grid.origin;
        leftNode.currentStopChance = node.currentStopChance + stopChance;

        BPNode rightNode = new BPNode(node.grid.xSize - newXSize, node.grid.ySize);
        //rightNode.grid.origin = node.grid.GridToWorld(node.grid.xSize - newXSize, 0);
        rightNode.grid.origin = node.grid.origin + new Vector3(newXSize * 1f, 0, 0);
        rightNode.currentStopChance = node.currentStopChance + stopChance;

        Split(leftNode);
        Split(rightNode);
        node.grid.Import(leftNode.grid, 0, 0);
        node.grid.Import(rightNode.grid, newXSize, 0);
    }

    void SplitH (BPNode node)
    {
        int newYSize = Random.Range(node.grid.ySize / 3, node.grid.ySize * 2 / 3);
        BPNode topNode = new BPNode(node.grid.xSize, newYSize);
        topNode.grid.origin = node.grid.origin;
        topNode.currentStopChance = node.currentStopChance + stopChance;

        BPNode bottomNode = new BPNode(node.grid.xSize, node.grid.ySize - newYSize);
        //bottomNode.grid.origin = node.grid.GridToWorld(0, node.grid.ySize - newYSize);
        bottomNode.grid.origin = node.grid.origin + new Vector3(0, 0, newYSize * 1f);
        bottomNode.currentStopChance = node.currentStopChance + stopChance;

        Split(topNode);
        Split(bottomNode);
        node.grid.Import(topNode.grid, 0, 0);
        node.grid.Import(bottomNode.grid, 0, newYSize);
    }

    bool CanSplitV (BPNode node)
    {
        if (Random.value < node.currentStopChance)
            return false;
        if (node.grid.xSize > 2 * node.grid.ySize)
            return true;
        if (node.grid.xSize / 2 < minXSize)
            return false;
        if (node.grid.ySize < minYSize)
            return false;
        return true;
    }

    bool CanSplitH(BPNode node)
    {
        if (Random.value < node.currentStopChance)
            return false;
        if (node.grid.ySize > 2 * node.grid.xSize)
            return true;
        if (node.grid.xSize < minXSize)
            return false;
        if (node.grid.ySize / 2 < minYSize)
            return false;
        return true;
    }

    void SpawnBuildings (BPNode node)
    {
        if (Random.value > buildingChance)
            return;
        GameObject tile = Instantiate(tilePrefab, node.grid.origin + new Vector3(1, 0, 1), Quaternion.identity);
        float yScale = Random.Range(Mathf.Min(node.grid.xSize, node.grid.ySize), Mathf.Max(node.grid.xSize, node.grid.ySize));
        tile.transform.localScale = new Vector3(node.grid.xSize - 2, ySize / 15, node.grid.ySize - 2) ;
        tile.transform.SetParent(transform);
    }

}
