using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Digger : MonoBehaviour
{
    public Grid2D<Node> grid;
    public Vector3 defaultDirection;
    [Header("Parameters")]
    public float chanceToTurnLeft;
    public float chanceToTurnRight;
    public float chanceToTurnAround;

    public Digger ()
    {

    }

    public void DigOnce ()
    {

    }
}
