using System;

namespace StixGames.TileComposer
{
    [Serializable]
    public class ConnectorAssignment
    {
        public string Name;
        
        public ConnectionType ConnectionType;

        public ConnectorAssignment()
        {
            Name = "";
            ConnectionType = ConnectionType.Bidirectional;
        }
        public ConnectorAssignment(string connector, ConnectionType connectionType)
        {
            Name = connector;
            ConnectionType = connectionType;
        }

        public override string ToString()
        {
            switch (ConnectionType)
            {
                case ConnectionType.In:
                    return $"{Name} In";
                case ConnectionType.Out:
                    return $"{Name} Out";
                case ConnectionType.Bidirectional:
                    return Name;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected bool Equals(ConnectorAssignment other) => Name == other.Name && ConnectionType == other.ConnectionType;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((ConnectorAssignment) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (int) ConnectionType;
            }
        }
    }
}