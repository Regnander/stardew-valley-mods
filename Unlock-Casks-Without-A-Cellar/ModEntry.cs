using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace UnlockCasksWithoutACellar
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
            // Checking if the player already has the cask recipe.
            if (IsRecipeUnlocked())
            {
                // Checking if the player hasn't reached the necessary skill levels, while having the cask recipe.
                if (!DoesMeetSkillRequirements())
                {
                    PrintLog(GetLocalizedString("log.requirements_not_met_unlocked", Game1.player.Name));
                }

                return;
            }

            // Checking if the player hasn't reached the necessary skill levels.
            if (!DoesMeetSkillRequirements())
            {
                // Prints a list of necessary skill levels.
                PrintLog(GetLocalizedString("log.requirements_not_met", Game1.player.Name) + Environment.NewLine + GetSkillRequirements());
            }
            else
            {
                try
                {
                    // Giving the cask recipe to the player.
                    Game1.player.craftingRecipes.Add("Cask", 0);

                    PrintLog(GetLocalizedString("log.requirements_met", Game1.player.Name));

                    // Displaying a notification to inform the player.
                    Game1.showGlobalMessage(
                        Game1.content.LoadString("Strings\\UI:LearnedRecipe",
                            Game1.content.LoadString("Strings\\UI:LearnedRecipe_crafting"),
                            new CraftingRecipe("Cask").DisplayName
                        )
                    );
                }
                catch (Exception ex) { this.Monitor.Log(ex.Message, LogLevel.Error); }
            }
        }

        /// <summary>
        /// Determines whether the player has already unlocked the Cask crafting recipe.
        /// </summary>
        /// <returns>true if the Cask recipe is present in the player's crafting recipes; otherwise, false.</returns>
        private static bool IsRecipeUnlocked() => Game1.player.craftingRecipes.ContainsKey("Cask");

        /// <summary>
        /// Determines whether the player meets the minimum skill level requirements.
        /// </summary>
        /// <returns>true if the player's skill levels are each greater than or
        /// equal to the configured minimum values; otherwise, false.</returns>
        private bool DoesMeetSkillRequirements()
        {
            return GetSkillLevel(0) >= this.Config.MinimumFarmingLevel
                && GetSkillLevel(1) >= this.Config.MinimumFishingLevel
                && GetSkillLevel(2) >= this.Config.MinimumForagingLevel
                && GetSkillLevel(3) >= this.Config.MinimumMiningLevel
                && GetSkillLevel(4) >= this.Config.MinimumCombatLevel;
        }

        /// <summary>
        /// Writes an informational log message if logging is enabled in the configuration.
        /// </summary>
        /// <param name="message">The message to write to the log.</param>
        private void PrintLog(string message)
        {
            // Checking if logging is enabled.
            if (Config.EnableLogging)
            {
                // Printing the given message to the log.
                this.Monitor.Log(message, LogLevel.Info);
            }
        }

        /// <summary>
        /// Generates a formatted string describing the player's current and required levels for each skill with a
        /// requirement.
        /// </summary>
        /// <returns>A string listing each skill with a nonzero requirement in the format "Skill: currentLevel/requiredLevel",
        /// separated by " | ". Returns an empty string if no skill requirements are set.</returns>
        private string GetSkillRequirements()
        {
            List<(string Name, int Level, int Limit)> Skills = new()
            {
                (GetSkillName(11604), GetSkillLevel(0), this.Config.MinimumFarmingLevel),
                (GetSkillName(11605), GetSkillLevel(3), this.Config.MinimumMiningLevel),
                (GetSkillName(11606), GetSkillLevel(2), this.Config.MinimumForagingLevel),
                (GetSkillName(11607), GetSkillLevel(1), this.Config.MinimumFishingLevel),
                (GetSkillName(11608), GetSkillLevel(4), this.Config.MinimumCombatLevel)
            };

            return " " + String.Join(" | ", Skills.Where(skill => skill.Limit > 0)
                .Select(skill => $"{skill.Name}: {skill.Level}/{skill.Limit}"));
        }

        /// <summary>
        /// Retrieves the localized display name of a skill based on its index.
        /// </summary>
        /// <param name="Index">The zero-based index of the skill for which to retrieve the display name.</param>
        /// <returns>A string containing the localized name of the specified skill.</returns>
        private static string GetSkillName(int Index) => Game1.content.LoadString($"Strings\\StringsFromCSFiles:SkillsPage.cs.{Index}");

        /// <summary>
        /// Retrieves the player's current level for the specified skill, excluding any temporary modifiers.
        /// </summary>
        /// <param name="Index">The zero-based index of the skill to retrieve the level for. Must correspond to a valid skill defined in the
        /// game.</param>
        /// <returns>The player's unmodified skill level for the specified skill. Returns 0 if the player has not gained any
        /// levels in the skill.</returns>
        private static int GetSkillLevel(int Index) => Game1.player.GetUnmodifiedSkillLevel(Index);

        /// <summary>
        /// Retrieves a localized string for the specified key and formats it with the provided tokens, if any.
        /// </summary>
        /// <param name="key">The key that identifies the localized string to retrieve. Cannot be null or empty.</param>
        /// <param name="tokens">An object containing replacement values to format the localized string. May be null if no formatting is
        /// required.</param>
        /// <returns>A localized and formatted string corresponding to the specified key. If the key is not found, the original
        /// key is returned.</returns>
        private string GetLocalizedString(string key, params object[] tokens) => String.Format(this.Helper.Translation.Get(key), tokens);

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

            // Adding logging option.
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.enable_logging.title"),
                tooltip: () => GetLocalizedString("config.enable_logging.tooltip"),
                getValue: () => this.Config.EnableLogging,
                setValue: value => this.Config.EnableLogging = value
            );

            // Adding header.
            configMenu.AddSubHeader(
              mod: this.ModManifest,
              text: () => GetLocalizedString("config.minimum_level.header")
            );

            // Adding notice.
            configMenu.AddParagraph(
                mod: this.ModManifest,
                text: () => GetLocalizedString("config.minimum_level.notice")
            );

            // Adding skill level sliders.
            var Skills = new List<(int, int, Func<int>, Action<int>)>
            {
                (11604, 0, () => this.Config.MinimumFarmingLevel, value => this.Config.MinimumFarmingLevel = value),
                (11605, 3, () => this.Config.MinimumMiningLevel, value => this.Config.MinimumMiningLevel = value),
                (11606, 2, () => this.Config.MinimumForagingLevel, value => this.Config.MinimumForagingLevel = value),
                (11607, 1, () => this.Config.MinimumFishingLevel, value => this.Config.MinimumFishingLevel = value),
                (11608, 4, () => this.Config.MinimumCombatLevel, value => this.Config.MinimumCombatLevel = value)
            };

            foreach (var skill in Skills)
            {
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetSkillName(skill.Item1),
                    tooltip: () => GetLocalizedString("config.minimum_level.tooltip"),
                    getValue: skill.Item3,
                    setValue: skill.Item4,
                    min: 0,
                    max: 10
                );
            }
        }
    }
}