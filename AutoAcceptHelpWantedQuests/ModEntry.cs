using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Quests;

namespace AutoAcceptHelpWantedQuests
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        /*********
        ** Private methods
        *********/
        /// <summary>The method called after a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Checking if there is a quest available.
            if (Game1.questOfTheDay != null)
            {
                // Checking if the mod is enabled.
                if (this.Config.Enable && !HasMaxFriendship(GetQuestGiverName(Game1.questOfTheDay)))
                {
                    // Adding the quest to the quest log.
                    AddQuestToQuestLog();

                    // Checking if notifications are enabled.
                    if (this.Config.ShowNotifications)
                    {
                        // Displaying a notification on the screen.
                        Game1.addHUDMessage(new HUDMessage(GetLocalizedString("notification"), 2));
                    }
                }
            }
        }

        /// <summary>
        /// Adds the current daily quest to the player's quest log and marks it as active.
        /// </summary>
        private void AddQuestToQuestLog()
        {
            // Extracted from Billboard::receiveLeftClick

            // Checking if sounds are enabled.
            if (this.Config.PlaySound) { Game1.playSound("newArtifact"); }

            Game1.questOfTheDay.dailyQuest.Value = true;
            Game1.questOfTheDay.dayQuestAccepted.Value = Game1.Date.TotalDays;
            Game1.questOfTheDay.accepted.Value = true;
            Game1.questOfTheDay.canBeCancelled.Value = true;
            Game1.questOfTheDay.daysLeft.Value = 2;
            Game1.player.questLog.Add(Game1.questOfTheDay);
            Game1.player.acceptedDailyQuest.Set(newValue: true);
        }


        /// <summary>
        /// Determines the maximum possible friendship level, in hearts, that the player can achieve with the specified
        /// NPC.
        /// </summary>
        /// <param name="questGiverName">The name of the NPC whose maximum friendship level is to be determined. Cannot be null or empty.</param>
        /// <returns>The maximum number of hearts that can be reached with the specified NPC. Returns 14 for current partners or
        /// roommates, 8 for datable NPCs who are not partners, and 10 for all others.</returns>
        private static int GetMaximumFriendship(string questGiverName)
        {
            Game1.player.friendshipData.TryGetValue(questGiverName, out Friendship? friendship);
            NPC.TryGetData(questGiverName, out CharacterData? cd);

            bool IsDatable = cd?.CanBeRomanced ?? false;
            bool IsDivorced = (friendship?.IsDivorced() ?? false);
            bool IsPartner = (friendship?.IsDating() ?? false)
                || (friendship?.IsMarried() ?? false)
                || (friendship?.IsEngaged() ?? false);
            bool IsRoommate = (friendship?.IsRoommate() ?? false);

            // For existing partners or roommates (max 14 hearts).
            if (IsPartner || IsRoommate) { return 14; }
            // For potential partners (max 8 hearts).
            if (IsDatable && !IsPartner) { return 8; }
            // For everyone else (max 10 hearts).
            return 10;
        }

        /// <summary>
        /// Determines whether the specified villager has reached the maximum possible friendship level with the player.
        /// </summary>
        /// <param name="villagerName">The name of the villager whose friendship level is to be checked. Cannot be null or empty.</param>
        /// <returns>true if the friendship level with the specified villager is at or above the maximum; otherwise, false.</returns>
        private bool HasMaxFriendship(string villagerName)
        {
            // Returning false if friendship should be ignored.
            if (!this.Config.IgnoreFriends) { return false; }

            Game1.player.friendshipData.TryGetValue(villagerName, out Friendship? friendship);
            int CurrentFriendship = friendship?.Points ?? -1;
            int MaxFriendship = GetMaximumFriendship(villagerName);

            // Returns true if friendship is max hearts.
            return CurrentFriendship >= MaxFriendship;
        }

        /// <summary>
        /// Retrieves the name of the quest giver associated with the specified quest.
        /// </summary>
        /// <param name="quest">The quest for which to obtain the quest giver's name. Must not be null.</param>
        /// <returns>A string containing the name of the quest giver. Returns an empty string if the quest type is not recognized
        /// or the quest giver's name cannot be determined.</returns>
        private static string GetQuestGiverName(Quest quest)
        {
            switch (quest.questType.Value)
            {
                case 3: // Item Delivery
                    if (quest is ItemDeliveryQuest itemDeliverQuest && itemDeliverQuest.target.Value != null)
                    {
                        return itemDeliverQuest.target.Value;
                    }
                    break;
                case 4: // Slay Monsters 
                    if (quest is SlayMonsterQuest slayQuest && slayQuest.target.Value != null)
                    {
                        return slayQuest.target.Value;
                    }
                    break;
                case 5: // Saying 'Hello'
                    return "Emily";
                case 7: // Fishing
                    if (quest is FishingQuest fishQuest && fishQuest.target.Value != null)
                    {
                        return fishQuest.target.Value;
                    }
                    break;
                case 10: // Gathering
                    if (quest is ResourceCollectionQuest resourceQuest && resourceQuest.target.Value != null)
                    {
                        return resourceQuest.target.Value;
                    }
                    break;
            }
            return "";
        }

        /// <summary>
        /// Handles the event that occurs when the game is launched, registering the mod's configuration options with
        /// the Generic Mod Config Menu if it is available.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data associated with the game launched event.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // Adding mod enabling option.
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.enable.title"),
                tooltip: () => GetLocalizedString("config.enable.tooltip"),
                getValue: () => this.Config.Enable,
                setValue: value => this.Config.Enable = value
            );

            // Adding notification option.
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.show_notifications.title"),
                tooltip: () => GetLocalizedString("config.show_notifications.tooltip"),
                getValue: () => this.Config.ShowNotifications,
                setValue: value => this.Config.ShowNotifications = value
            );

            // Adding sound option.
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.play_sound.title"),
                tooltip: () => GetLocalizedString("config.play_sound.tooltip"),
                getValue: () => this.Config.PlaySound,
                setValue: value => this.Config.PlaySound = value
            );

            // Adding friendship option.
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.ignore_friends.title"),
                tooltip: () => GetLocalizedString("config.ignore_friends.tooltip"),
                getValue: () => this.Config.IgnoreFriends,
                setValue: value => this.Config.IgnoreFriends = value
            );
        }

        /// <summary>
        /// Retrieves a localized string for the specified key and formats it with the provided tokens, if any.
        /// </summary>
        /// <param name="key">The key that identifies the localized string to retrieve. Cannot be null or empty.</param>
        /// <param name="tokens">An object containing replacement values to format the localized string. May be null if no formatting is
        /// required.</param>
        /// <returns>A localized and formatted string corresponding to the specified key. If the key is not found, the original
        /// key is returned.</returns>
        private string GetLocalizedString(string key, params object[] tokens) => String.Format(this.Helper.Translation.Get(key), tokens);
    }
}