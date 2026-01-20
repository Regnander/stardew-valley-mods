namespace UnlockCasksWithoutACellar
{
    public sealed class ModConfig
    {
        public bool EnableLogging { get; set; }
        public int MinimumFarmingLevel { get; set; }
        public int MinimumFishingLevel { get; set; }
        public int MinimumForagingLevel { get; set; }
        public int MinimumMiningLevel { get; set; }
        public int MinimumCombatLevel { get; set; }

        public ModConfig()
        {
            EnableLogging = true;
            MinimumFarmingLevel = 8;
            MinimumFishingLevel = 0;
            MinimumForagingLevel = 0;
            MinimumMiningLevel = 0;
            MinimumCombatLevel = 0;
        }
    }
}
