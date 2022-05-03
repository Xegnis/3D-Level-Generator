using System;

namespace StixGames.TileComposer
{
    [Serializable]
    [Flags]
    public enum ConnectionType
    {
        In = 1,
        Out = 2,
        Bidirectional = 3
    }
    
    public static class ConnectionTypeUtility
    {
        public static int GetIndex(this ConnectionType type)
        {
            return Array.IndexOf(Enum.GetValues(typeof(ConnectionType)), type);
        }

        public static ConnectionType FromIndex(int index)
        {
            return (ConnectionType) Enum.GetValues(typeof(ConnectionType)).GetValue(index);
        }
    }
}