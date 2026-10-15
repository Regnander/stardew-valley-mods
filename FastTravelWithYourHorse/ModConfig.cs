namespace FastTravelWithYourHorse
{
    public sealed class ModConfig
    {
        public bool EnableMinecarts { get; set; }
        public bool EnableParrotExpress { get; set; }
        public int MaxHorseDistance { get; set; }
        public bool IndoorsAllowed { get; set; }

        public ModConfig()
        {
            EnableMinecarts = true;
            EnableParrotExpress = true;
            MaxHorseDistance = 4;
            IndoorsAllowed = false;
        }
    }
}
