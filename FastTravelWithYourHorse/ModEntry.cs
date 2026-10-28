using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.GameData.Minecarts;
using StardewValley.Locations;

namespace FastTravelWithYourHorse
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        private static ModConfig Config;
        public static ModEntry Instance { get; private set; } = null!;
        private static Point busStopDestinationTile = new(23, 10);
        private static Point desertDestinationTile = new(22, 28);

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = this.Helper.ReadConfig<ModConfig>();
            Instance = this;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

            try
            {
                Harmony harmony = new(ModManifest.UniqueID);
                harmony.Patch(
                   original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.MinecartWarp)),
                   postfix: new HarmonyMethod(typeof(ModEntry), nameof(MinecraftWarp_Postfix))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(IslandLocation), nameof(IslandLocation.answerDialogue)),
                   postfix: new HarmonyMethod(typeof(ModEntry), nameof(AnswerDialogue_Postfix))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(BusStop), "playerReachedBusDoor"),
                   prefix: new HarmonyMethod(typeof(ModEntry), nameof(BusStop_PlayerReachedBusDoor_Prefix))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Desert), "playerReachedBusDoor"),
                   prefix: new HarmonyMethod(typeof(ModEntry), nameof(Desert_PlayerReachedBusDoor_Prefix))
                );
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }
        }

        /*********
        ** Private methods
        *********/

        /// <summary>
        /// Warps the player's horse to the selected Parrot Express destination after the dialogue choice is made.
        /// </summary>
        /// <param name="__instance">The island location where the dialogue was answered.</param>
        /// <param name="answer">The player's selected dialogue response.</param>
        private static void AnswerDialogue_Postfix(IslandLocation __instance, Response answer)
        {
            // Checking if the parrot express has been disabled in the settings.
            if (!Config.EnableParrotExpress) return;
            // Checking if no parrot express option wasn't selected.
            if (!answer.responseKey.StartsWith("Go")) return;

            Farmer farmer = Game1.player;
            Horse? horse = GetHorse(farmer, __instance);

            // Checking if no horse is found in the same area as the player.
            if (horse == null) return;

            try
            {
                KeyValuePair<string, KeyValuePair<string, Point>> islandDestination = ParrotPlatform.GetDestinations()
                    .FirstOrDefault(destination => ("Go" + destination.Key) == answer.responseKey);

                // Teleports horse to the given destination.
                TeleportHorse(horse, farmer.Tile, islandDestination.Value.Key, islandDestination.Value.Value);
            }
            catch (Exception) { PrintLog(GetLocalizedString("log.error.horse_warp", answer.responseText)); }
        }

        /// <summary>
        /// Warps the player's horse to the selected minecart destination after the minecart warp.
        /// </summary>
        /// <param name="__instance">The location from which the minecart warp was initiated.</param>
        /// <param name="destination">The destination selected for the minecart warp.</param>
        private static void MinecraftWarp_Postfix(GameLocation __instance, MinecartDestinationData destination)
        {
            // Checking if minecarts have been disabled in the settings.
            if (!Config.EnableMinecarts) return;
            // Checking if there is currently a festival on the given destination.
            if (IsFestivalAtDestination(destination.TargetLocation)) return;

            Farmer farmer = Game1.player;
            Horse? horse = GetHorse(farmer, __instance);

            // Checking if no horse is found in the same area as the player.
            if (horse == null) return;

            // Teleports horse to the given destination.
            try { TeleportHorse(horse, farmer.Tile, destination.TargetLocation, destination.TargetTile); }
            catch (Exception) { PrintLog(GetLocalizedString("log.error.horse_warp", destination.DisplayName)); }
        }

        /// <summary>
        /// Warps the player's horse from the bus stop to the desert when the player reaches the bus door.
        /// </summary>
        /// <param name="__instance">The bus stop location where the player reached the bus door.</param>
        private static void BusStop_PlayerReachedBusDoor_Prefix(BusStop __instance) => PlayerReachedBusDoor_Prefix(__instance, desertDestinationTile, "Desert");

        /// <summary>
        /// Warps the player's horse from the desert to the bus stop when the player reaches the bus door.
        /// </summary>
        /// <param name="__instance">The desert location where the player reached the bus door.</param>
        private static void Desert_PlayerReachedBusDoor_Prefix(Desert __instance) => PlayerReachedBusDoor_Prefix(__instance, busStopDestinationTile, "BusStop");

        /// <summary>
        /// Warps the player's horse to the specified bus destination when bus travel is enabled and the horse is nearby.
        /// </summary>
        /// <param name="gameLocation">The location where the player reached the bus door.</param>
        /// <param name="destinationTile">The tile to use as the horse's destination.</param>
        /// <param name="destinationName">The name of the destination location.</param>
        private static void PlayerReachedBusDoor_Prefix(GameLocation gameLocation, Point destinationTile, string destinationName)
        {
            // Checking if the bus to Calico Desert has been disabled in the settings.
            if (!Config.EnableBus) return;

            Farmer farmer = Game1.player;
            Horse? horse = GetHorse(farmer, gameLocation);

            // Checking if no horse is found in the same area as the player.
            if (horse == null) return;

            // Teleports horse to the given destination.
            try { TeleportHorse(horse, farmer.Tile, destinationName, destinationTile); }
            catch (Exception) { PrintLog(GetLocalizedString("log.error.horse_warp", destinationName)); }
        }


        /// <summary>
        /// Determines whether a festival is taking place at the specified destination today.
        /// </summary>
        /// <param name="locationName">The name of the destination location.</param>
        /// <returns><see langword="true"/> if today's festival is at the destination; otherwise, <see langword="false"/>.</returns>
        private static bool IsFestivalAtDestination(string locationName) => Game1.whereIsTodaysFest == locationName;

        /// <summary>
        /// Warps the horse to a destination when it is nearby and the destination is accessible under the current settings.
        /// </summary>
        /// <param name="horse">The horse to warp.</param>
        /// <param name="farmerTile">The tile of the farmer who owns the horse.</param>
        /// <param name="destinationName">The name of the destination location.</param>
        /// <param name="destinationTile">The destination tile used to calculate a safe arrival position.</param>
        private static void TeleportHorse(Horse horse, Vector2 farmerTile, String destinationName, Point destinationTile)
        {
            float maxDistance = 4;
            bool isIndoorsAllowed = false;
            float horseToPlayerDistance = Vector2.Distance(farmerTile, horse.Tile);

            // Loading user settings.
            try { maxDistance = Config.MaxHorseDistance; }
            catch (Exception) { PrintLog(GetLocalizedString("log.error.loading_settings", "MaxHorseDistance", maxDistance)); }
            try { isIndoorsAllowed = Config.AllowIndoorsLocations; }
            catch (Exception) { PrintLog(GetLocalizedString("log.error.loading_settings", "IndoorsAllowed", isIndoorsAllowed)); }

            try
            {
                GameLocation destination = Game1.getLocationRequest(destinationName).Location;

                // Checking if all conditions are met.
                bool isHorseNearby = horseToPlayerDistance <= maxDistance;
                bool isDestinationAccessible = destination.IsOutdoors || (!destination.IsOutdoors && isIndoorsAllowed);

                // Checking if the horse can be warped.
                if (isHorseNearby && isDestinationAccessible)
                {
                    // Warping the horse to its destination
                    try { Game1.warpCharacter(horse, destinationName, GetHorseArrivalTile(destination, destinationTile)); }
                    catch (Exception) { PrintLog(GetLocalizedString("log.error.horse_warp", destinationName)); }
                }
            }
            catch (Exception) { PrintLog(GetLocalizedString("log.error.outdoor_destination", destinationName)); }
        }

        /// <summary>
        /// Determines whether a horse can be placed at the specified destination, accounting for the horse's two-tile
        /// horizontal footprint and the player's position.
        /// </summary>
        /// <param name="location">The GameLocation in which placement is attempted; used to check tile occupancy.</param>
        /// <param name="horseDestination">Tile coordinate for the horse's primary (left) tile; the adjacent right tile is also checked.</param>
        /// <param name="playerTile">The player's current tile coordinate; placement is disallowed if either horse tile equals this tile.</param>
        /// <returns>true if both the specified tile and the tile immediately to its right are unoccupied and do not overlap the
        /// player's tile; otherwise, false.</returns>
        private static bool CanHorseBePlaced(GameLocation location, Vector2 horseDestination, Vector2 playerTile)
        {
            // Checking the destination tile and one tile to the right, since horses take up two horizontal tiles.
            Vector2 horseDestination2 = new(horseDestination.X + 1, horseDestination.Y);

            // Other players' horses
            bool isTileBlockedByHorse = IsTileBlockedByHorse(location, horseDestination, horseDestination2);
            // Player destination
            bool isTileBlockedByPlayer = horseDestination.Equals(playerTile) || horseDestination2.Equals(playerTile);

            // Farm buildings
            bool isTileBlockedByBuilding = IsTileOccupied(location, horseDestination, horseDestination2, CollisionMask.Buildings);
            // Chairs, carpets
            bool isTileBlockedByFurniture = IsTileOccupied(location, horseDestination, horseDestination2, CollisionMask.Furniture);
            // Machines
            bool isTileBlockedByObject = IsTileOccupied(location, horseDestination, horseDestination2, CollisionMask.Objects);
            // Shrubs, trees
            bool isTileBlockedByTerrainFeature = IsTileOccupied(location, horseDestination, horseDestination2, CollisionMask.TerrainFeatures);

            return !isTileBlockedByHorse && !isTileBlockedByPlayer && !isTileBlockedByBuilding
                && !isTileBlockedByFurniture && !isTileBlockedByObject && !isTileBlockedByTerrainFeature;
        }

        /// <summary>
        /// Determines whether either of the horse's destination tiles is occupied by the specified type of object or terrain.
        /// </summary>
        /// <param name="location">The location in which to check tile occupancy.</param>
        /// <param name="horseDestination">The horse's first destination tile.</param>
        /// <param name="horseDestination2">The horse's adjacent second destination tile.</param>
        /// <param name="mask">The collision categories to check.</param>
        /// <returns><see langword="true"/> if either tile is occupied for the specified collision categories; otherwise, <see langword="false"/>.</returns>
        private static bool IsTileOccupied(GameLocation location, Vector2 horseDestination, Vector2 horseDestination2, CollisionMask mask) => location.IsTileOccupiedBy(horseDestination, mask) || location.IsTileOccupiedBy(horseDestination2, mask);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location"></param>
        /// <param name="horseDestination"></param>
        /// <param name="horseDestination2"></param>
        /// <returns></returns>
        private static bool IsTileBlockedByHorse(GameLocation location, Vector2 horseDestination, Vector2 horseDestination2)
        {
            foreach (NPC character in location.characters)
            {
                if (character is Horse)
                {
                    return character.Tile.Equals(horseDestination) || character.Tile.Equals(horseDestination2);
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates an open arrival tile for the horse near the destination tile.
        /// </summary>
        /// <param name="locationId">The name of the destination location.</param>
        /// <param name="destinationTile">The tile associated with the destination.</param>
        /// <returns>An available tile position for the horse in the destination location.</returns>
        private static Vector2 GetHorseArrivalTile(GameLocation locationId, Point destinationTile)
        {
            Vector2 horseDestination = destinationTile.ToVector2();

            // Sets a default tile for the horse, based on the destination.
            switch (locationId.ToString())
            {
                case "StardewValley.Locations.Beach":
                    horseDestination.Y += 2;
                    break;

                case "StardewValley.Locations.Town":
                    // Place the horse on the right side for the minecart destination outside Clint.
                    horseDestination.X += destinationTile.Equals(new Point(105, 80)) ? 1 : -2;
                    break;

                case "StardewValley.Locations.Forest":
                case "StardewValley.Locations.Mountain":
                case "StardewValley.Locations.Railroad":
                case "StardewValley.Locations.Desert":
                    horseDestination.X -= 2;
                    break;

                case "StardewValley.Locations.Mine":
                    horseDestination.X -= 2;
                    horseDestination.Y += 2;
                    break;

                case "StardewValley.Locations.BusStop":
                case "StardewValley.Locations.IslandWest":
                case "StardewValley.Locations.Woods":
                    horseDestination.X += 1;
                    break;

                default:
                    horseDestination.Y += 1;
                    break;
            }

            // Finds an unoccupied tile near the given horse destination tile.
            return GetAvailableHorseTile(horseDestination, destinationTile.ToVector2(), locationId);
        }

        /// <summary>
        /// Finds an available tile near the proposed horse position, or returns the original destination tile if none is available.
        /// </summary>
        /// <param name="horseTile">The proposed tile position for the horse.</param>
        /// <param name="playerTile">The original destination tile to use if no alternate tile is available.</param>
        /// <param name="location">The location in which to check for occupied tiles.</param>
        /// <returns>An available alternate tile, or <paramref name="horseTile"/> if none is found.</returns>
        private static Vector2 GetAvailableHorseTile(Vector2 horseTile, Vector2 playerTile, GameLocation location)
        {
            const float verticalAdjustment = 1f;
            const float horisontalAdjustment = 2f;

            // Listing a prioritized list of suggested coordinates.
            List<Vector2> suggestedCoordinates = new() {
                new Vector2(horseTile.X, horseTile.Y),
                // One tile down
                new Vector2(horseTile.X, horseTile.Y + verticalAdjustment),
                // One tile to the left
                new Vector2(horseTile.X - horisontalAdjustment, horseTile.Y),
                // One tile to the right
                new Vector2(horseTile.X + horisontalAdjustment, horseTile.Y),
                // One tile to the left and one tile down
                new Vector2(horseTile.X - horisontalAdjustment, horseTile.Y + verticalAdjustment),
                // One tile to the right and one tile down
                new Vector2(horseTile.X + horisontalAdjustment, horseTile.Y + verticalAdjustment),
                // Two tiles down (from the player)
                new Vector2(playerTile.X, playerTile.Y + verticalAdjustment * 2),
            };

            // Iterates all suggested coordinates to find a tile to warp the horse.
            try { return suggestedCoordinates.First(tile => CanHorseBePlaced(location, tile, playerTile)); }
            catch (Exception) { return horseTile; }
        }

        /// <summary>
        /// Finds the farmer's horse among the characters in the specified location.
        /// </summary>
        /// <param name="farmer">The farmer whose horse to find.</param>
        /// <param name="location">The location whose characters to search.</param>
        /// <returns>The farmer's horse if it is present in the location; otherwise, <see langword="null"/>.</returns>
        private static Horse? GetHorse(Farmer farmer, GameLocation location)
        {
            try
            {
                /* Snippet borrowed from Object.cs */

                // Iterating all characters in the current location.
                foreach (NPC character in location.characters)
                {
                    // Returns the player horse if one can be found.
                    if (character is Horse curHorse && curHorse.getOwner() == farmer)
                    {
                        return curHorse;
                    }
                }
            }
            catch (Exception) { }

            // Either the horse is not in the current location or the player doesn't own a horse.
            return null;
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
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => GetLocalizedString("config.title.traveling_methods")
            );

            // Adding minecart option.
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.enable_minecarts.title"),
                tooltip: () => GetLocalizedString("config.enable_minecarts.tooltip"),
                getValue: () => Config.EnableMinecarts,
                setValue: value => Config.EnableMinecarts = value
            );

            // Adding parrot express option.
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.enable_parrot_express.title"),
                tooltip: () => GetLocalizedString("config.enable_parrot_express.tooltip"),
                getValue: () => Config.EnableParrotExpress,
                setValue: value => Config.EnableParrotExpress = value
            );

            // Adding bus option.
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.enable_bus.title"),
                tooltip: () => GetLocalizedString("config.enable_bus.tooltip"),
                getValue: () => Config.EnableBus,
                setValue: value => Config.EnableBus = value
            );

            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => GetLocalizedString("config.title.other_settings")
            );

            // Adding distance option.
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.max_horse_distance.title"),
                tooltip: () => GetLocalizedString("config.max_horse_distance.tooltip"),
                getValue: () => Config.MaxHorseDistance,
                setValue: value => Config.MaxHorseDistance = value,
                min: 1,
                max: 10
            );

            // Adding indoors option.
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => GetLocalizedString("config.allow_indoors_locations.title"),
                tooltip: () => GetLocalizedString("config.allow_indoors_locations.tooltip"),
                getValue: () => Config.AllowIndoorsLocations,
                setValue: value => Config.AllowIndoorsLocations = value
            );

            // Adding warning text.
            configMenu.AddParagraph(
                mod: this.ModManifest,
                text: () => GetLocalizedString("config.allow_indoors_locations.warning")
            );

        }

        /// <summary>
        /// Writes an informational log message if logging is enabled in the configuration.
        /// </summary>
        /// <param name="message">The message to write to the log.</param>
        private static void PrintLog(string message) => Instance.Monitor.Log(message, LogLevel.Info);

        /// <summary>
        /// Retrieves a localized string for the specified key and formats it with the provided tokens, if any.
        /// </summary>
        /// <param name="key">The key that identifies the localized string to retrieve. Cannot be null or empty.</param>
        /// <param name="tokens">An object containing replacement values to format the localized string. May be null if no formatting is
        /// required.</param>
        /// <returns>A localized and formatted string corresponding to the specified key. If the key is not found, the original
        /// key is returned.</returns>
        private static string GetLocalizedString(string key, params object[] tokens) => String.Format(Instance.Helper.Translation.Get(key), tokens);

    }
}