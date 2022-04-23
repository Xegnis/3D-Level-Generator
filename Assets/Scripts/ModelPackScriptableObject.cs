using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModelPack", menuName = "ScriptableObjects/ModelPackScriptableObject", order = 1)]
public class ModelPackScriptableObject : ScriptableObject
{
    public string modelPackName;

    public GameObject emptyPrefab;
}
