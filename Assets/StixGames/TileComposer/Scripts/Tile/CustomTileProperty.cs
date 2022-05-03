using System;

namespace StixGames.TileComposer
{
    [Serializable]
    public class CustomTileProperty
    {
        public string Name;
        public int IntValue;

        public CustomTileProperty()
        {
        }

        public CustomTileProperty(string propertyName)
        {
            Name = propertyName;
        }
    }
}