namespace StixGames.TileComposer.Solvers.WFCPlugins
{
    public struct WFCChange
    {
        public int Index;
        public int Variation;
        public bool IsAllowed;

        public WFCChange(int index, int variation, bool isAllowed)
        {
            Index = index;
            Variation = variation;
            IsAllowed = isAllowed;
        }
    }
}