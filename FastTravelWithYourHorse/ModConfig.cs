namespace FastTravelWithYourHorse
{
    public sealed class ModConfig
    {
        public bool EnableMinecarts { get; set; }
        public bool EnableParrotExpress { get; set; }
        public bool EnableBus { get; set; }
        public int MaxHorseDistance { get; set; }
        public bool AllowIndoorsLocations { get; set; }

        public ModConfig()
        {
            EnableMinecarts = true;
            EnableParrotExpress = true;
            EnableBus = true;
            MaxHorseDistance = 4;
            AllowIndoorsLocations = false;
        }
    }
}
