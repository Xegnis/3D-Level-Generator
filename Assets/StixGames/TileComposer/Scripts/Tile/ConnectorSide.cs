using System;
using UnityEngine.Serialization;

namespace StixGames.TileComposer
{
    [Serializable]
    public class ConnectorSide
    {
        [FormerlySerializedAs("Connector")] public ConnectorAssignment[] Connectors;
    }
}