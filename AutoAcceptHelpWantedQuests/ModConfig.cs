namespace AutoAcceptHelpWantedQuests
{
    public sealed class ModConfig
    {
        public bool Enable { get; set; }
        public bool ShowNotifications { get; set; }
        public bool PlaySound { get; set; }
        public bool IgnoreFriends { get; set; }

        public ModConfig()
        {
            Enable = true;
            ShowNotifications = true;
            PlaySound = false;
            IgnoreFriends = false;
        }
    }
}
