using System;

namespace StixGames.TileComposer
{
    [Serializable]
    public class TileSlice
    {
        public string TileType;
        public Slice[] Dimensions;

        public TileSlice()
        {
            Dimensions = new Slice[0];
        }

        public TileSlice(string tileType, Slice[] dimensions)
        {
            TileType = tileType;
            Dimensions = dimensions;
        }
    }

    [Serializable]
    public class Slice
    {
        public int Start = 0;
        public int End = -1;

        public Slice()
        {
        }

        public Slice(int start, int end)
        {
            Start = start;
            End = end;
        }
    }
}