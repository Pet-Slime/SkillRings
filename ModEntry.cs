using Microsoft.Xna.Framework;
using Netcode;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.IO;

namespace SkillRings
{
    public class ModEntry : Mod
    {
        //Initializing all of the used variables
        private IJsonAssetsApi ja;
        private ModConfig cfg;

        //Variables adding support for other mods
        private IMoreRingsApi moreRings;
        public bool hasSVE = false; //Stardew Valley Expanded
        public bool hasWMR = false; //Wear More Rings
        public bool hasCMR = false; //Combined Many Rings / Balanced Combined Many Rings

        //Fishing Rings
        public string RingLegendaryAnglerName = "Ring of the Legendary Angler";
        public string RingMadMarinerName = "Ring of the Mad Mariner";
        public string RingApprenticeFisherName = "Ring of the Apprentice Fisher";
        public int RingLegendaryAngler => this.ja.GetObjectId("Ring of the Legendary Angler");
        public int RingLegendaryAnglerDusty => this.ja.GetObjectId("Dusty Ring");
        public int RingMadMariner => this.ja.GetObjectId("Ring of the Mad Mariner");
        public int RingApprenticeFisher => this.ja.GetObjectId("Ring of the Apprentice Fisher");
        public int addedFishing = 0;
        public int addedFishingNew = 0;

        //Farming Rings
        public string RingNaturesOracleName = "Ring of Nature's Oracle";
        public string RingGreenThumbName = "Ring of the Green Thumb";
        public string RingDecentSoilName = "Ring of Decent Soil";
        public int RingNaturesOracle => this.ja.GetObjectId("Ring of Nature's Oracle");
        public int RingNaturesOracleGrassy => this.ja.GetObjectId("Grassy Ring");
        public int RingGreenThumb => this.ja.GetObjectId("Ring of the Green Thumb");
        public int RingDecentSoil => this.ja.GetObjectId("Ring of Decent Soil");
        public int addedFarming = 0;
        public int addedFarmingNew = 0;

        //Foraging Rings
        public string RingNaturalBountyName = "Ring of Natural Bounty";
        public string RingGathererName = "Ring of the Gatherer";
        public string RingDeepRootsName = "Ring of Deep Roots";
        public int RingNaturalBounty => this.ja.GetObjectId("Ring of Natural Bounty");
        public int RingNaturalBountySticky => this.ja.GetObjectId("Sticky Ring");
        public int RingGatherer => this.ja.GetObjectId("Ring of the Gatherer");
        public int RingDeepRoots => this.ja.GetObjectId("Ring of Deep Roots");
        public int addedForaging = 0;
        public int addedForagingNew = 0;

        //Mining Rings
        public string RingDwarvenLuckName = "Ring of Dwarven Luck";
        public string RingCavesName = "Ring of the Caves";
        public string RingWieldyPickName = "Ring of the Wieldy Pick";
        public int RingDwarvenLuck => this.ja.GetObjectId("Ring of Dwarven Luck");
        public int RingDwarvenLuckStone => this.ja.GetObjectId("Stone Ring");
        public int RingCaves => this.ja.GetObjectId("Ring of the Caves");
        public int RingWieldyPick => this.ja.GetObjectId("Ring of the Wieldy Pick");
        public int addedMining = 0;
        public int addedMiningNew = 0;

        //Combat Rings
        public string RingWarGodName = "Ring of the War God";
        public string RingPureStrengthName = "Ring of Pure Strength";
        public string RingSharperBladesName = "Ring of Sharper Blades";
        public int RingWarGod => this.ja.GetObjectId("Ring of the War God");
        public int RingWarGodCursed => this.ja.GetObjectId("Cursed Ring");
        public int RingPureStrength => this.ja.GetObjectId("Ring of Pure Strength");
        public int RingSharperBlades => this.ja.GetObjectId("Ring of Sharper Blades");
        public int addedCombat = 0;
        public int addedCombatNew = 0;

        //Experience Rings
        public string RingIneffableKnowledgeName = "Ring of Ineffable Knowledge";
        public string RingKnowledgeName = "Ring of Knowledge";
        public string RingInsightName = "Ring of Insight";
        public int RingIneffableKnowledge => this.ja.GetObjectId("Ring of Ineffable Knowledge");
        public int RingIneffableKnowledgeElusive => this.ja.GetObjectId("Elusive Ring");
        public int RingKnowledge => this.ja.GetObjectId("Ring of Knowledge");
        public int RingInsight => this.ja.GetObjectId("Ring of Insight");
        public double expMult = 0.0;
        public int[] oldExperiencePoints = new int[5];

        //Runs when the mod gets loaded into the game
        public override void Entry(IModHelper helper)
        {
            //Tying functions to the game
            helper.Events.GameLoop.GameLaunched += new EventHandler<GameLaunchedEventArgs>(this.onGameLaunched);
            helper.Events.GameLoop.DayStarted += new EventHandler<DayStartedEventArgs>(this.onDayStarted);
            helper.Events.GameLoop.UpdateTicked += new EventHandler<UpdateTickedEventArgs>(this.onUpdateTicked);
            helper.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(this.onButtonPressed);
            //Adding console commands to the game
            //Shows all of the tier 3 rings the game believes the player to have obtained, ideally should never have to be used but the game gets weird sometimes
            helper.ConsoleCommands.Add("checkrings", "Displays which tier 3 rings the player has gotten.\n\nUsage: checkrings", new Action<string, string[]>(this.checkRings));
            //Resets the status of the provided tier 3 ring allowing the player to obtain them again, only really useful if the player accidentally deletes a tier3 or for some reason the game thinks they've gotten it
            helper.ConsoleCommands.Add("resetring", "Resets a ring so you can obtain it again\n\nUsage: resetring <ring>\n- ring: legendaryAngler|naturesOracle|naturalBounty|dwarvenLuck|warGod|ineffableKnowledge", new Action<string, string[]>(this.resetRing));
            //Checks which of the "Broken" tier 3 rings have been sent in the mail to the player
            helper.ConsoleCommands.Add("checkmail", "Displays which tier 3 rings the player has recieved in the mail.\n\nUsage: checkmail", new Action<string, string[]>(this.checkMail));
            //Allows the player to recieve the provided "Broken" ring in the mail again
            //Note: Does not guarentee the ring will arrive the next day, the conditions must still be met and it is a 10% change each day the conditions are met to recieve it
            helper.ConsoleCommands.Add("resetmail", "Resets mail so you can obtain it again\n\nUsage: resetmail <ring>\n- ring: DustyRing|GrassyRing|CursedRing|StickyRing|StoneRing|DeepRootsRecipe|GathererRecipe", new Action<string, string[]>(this.resetMail));
            //Fixes the health of the player if it was messed up by the mod
            helper.ConsoleCommands.Add("fixhealth", "Changes max health to what it should be, take off combat rings and don't have combat buffs on\n\nUsage: fixhealth", new Action<string, string[]>(this.fixHealth));
            //Load the config file
            this.cfg = helper.ReadConfig<ModConfig>();
        }

        //Function to fix a player's health if it was messed up by a ring
        private void fixHealth(string command, string[] args)
        {
            int skillLevel = Game1.player.GetSkillLevel(4);
            int num = 100;
            for(int index = 0; index < skillLevel; ++index)
            {
                switch(index)
                {
                    case 4:
                        if(Game1.player.professions.Contains(24))
                        {
                            num += 15;
                            break;
                        }
                        break;
                    case 9:
                        if(Game1.player.professions.Contains(27))
                        {
                            num += 25;
                            break;
                        }
                        break;
                    default:
                        num += 5;
                        break;
                }
            }
            if(Game1.player.mailReceived.Contains("qiCave"))
                num += 25;
            Game1.player.maxHealth = num;
            Game1.player.health = num;
        }

        //Function handling the resetRing command
        private void resetRing(string command, string[] args)
        {
            //Makes sure the second argument is valid
            if(args[0] == "legendaryAngler" || args[0] == "naturesOracle" || args[0] == "naturalBounty" || args[0] == "dwarvenLuck" || args[0] == "warGod" || args[0] == "ineffableKnowledge")
            {
                //Checks the player's mail for the ring
                if(!Game1.player.mailReceived.Contains(args[0] + "Ring"))
                {
                    //If they've not recieved the ring the print
                    this.Monitor.Log("Player has not obtained the " + args[0] + " Ring", (LogLevel) 1);
                }
                else
                {
                    //If they have then remove the piece of mail and log it
                    Game1.player.mailReceived.Remove(args[0] + "Ring");
                    this.Monitor.Log("Successfully reset " + args[0] + " Ring", (LogLevel) 1);
                }
            }
            else
                this.Monitor.Log("Please enter one of legendaryAngler|naturesOracle|naturalBounty|dwarvenLuck|warGod|ineffableKnowledge", (LogLevel) 1);
        }

        //Function handling the resetMail command
        //Almost identical to the function above but tied to the MFM mail
        private void resetMail(string command, string[] args)
        {
            if(args[0] == "DustyRing" || args[0] == "GrassyRing" || args[0] == "CursedRing" || args[0] == "StoneRing" || args[0] == "DeepRootsRecipe" || args[0] == "GathererRecipe" || args[0] == "StickyRing")
            {
                if(!Game1.player.mailReceived.Contains("SkillRings." + args[0]))
                {
                    this.Monitor.Log("Player has not obtained the " + args[0] + " Mail", (LogLevel) 1);
                }
                else
                {
                    Game1.player.mailReceived.Remove("SkillRings." + args[0]);
                    this.Monitor.Log("Successfully reset " + args[0] + " Mail", (LogLevel) 1);
                }
            }
            else
                this.Monitor.Log("Please enter one of DustyRing|GrassyRing|CursedRing|StickyRing|StoneRing|DeepRootsRecipe|GathererRecipe", (LogLevel) 1);
        }

        //Function handling the checkRings command
        private void checkRings(string command, string[] args)
        {
            this.Monitor.Log(string.Format("legendaryAngler: {0}", Game1.player.mailReceived.Contains("legendaryAnglerRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("naturesOracle: {0}", Game1.player.mailReceived.Contains("naturesOracleRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("naturalBounty: {0}", Game1.player.mailReceived.Contains("naturalBountyRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("dwarvenLuck: {0}", Game1.player.mailReceived.Contains("dwarvenLuckRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("warGod: {0}", Game1.player.mailReceived.Contains("warGodRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("ineffableKnowledge: {0}", Game1.player.mailReceived.Contains("ineffableKnowledgeRing")), (LogLevel) 1);
        }

        //Function handling the checkMail command
        private void checkMail(string command, string[] args)
        {
            this.Monitor.Log(string.Format("DustyRing: {0}", Game1.player.mailReceived.Contains("SkillRings.DustyRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("StickyRing: {0}", Game1.player.mailReceived.Contains("SkillRings.StickyRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("GrassyRing: {0}", Game1.player.mailReceived.Contains("SkillRings.GrassyRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("CursedRing: {0}", Game1.player.mailReceived.Contains("SkillRings.CursedRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("StoneRing: {0}", Game1.player.mailReceived.Contains("SkillRings.StoneRing")), (LogLevel) 1);
            this.Monitor.Log(string.Format("DeepRootsRecipe: {0}", Game1.player.mailReceived.Contains("SkillRings.DeepRootsRecipe")), (LogLevel) 1);
            this.Monitor.Log(string.Format("GathererRecipe: {0}", Game1.player.mailReceived.Contains("SkillRings.GathererRecipe")), (LogLevel) 1);
        }

        //Runs each time the day starts
        private void onDayStarted(object sender, DayStartedEventArgs e)
        {
            //Set the mod's added values
            this.addedFishing = 0;
            this.addedFishingNew = 0;
            this.addedFarming = 0;
            this.addedFarmingNew = 0;
            this.addedForaging = 0;
            this.addedForagingNew = 0;
            this.addedMining = 0;
            this.addedMiningNew = 0;
            this.expMult = 0.0;

            //This is needed so health is properly calculated
            if(this.addedCombat == this.cfg.tier3SkillRingBoost)
            {
                Game1.player.maxHealth -= 5 * this.cfg.tier3SkillRingBoost;
                Game1.player.health -= 5 * this.cfg.tier3SkillRingBoost;
            } else if(this.addedCombat == this.cfg.tier2SkillRingBoost)
            {
                Game1.player.maxHealth -= 5 * this.cfg.tier2SkillRingBoost;
                Game1.player.health -= 5 * this.cfg.tier2SkillRingBoost;
            } else if(this.addedCombat == this.cfg.tier1SkillRingBoost)
            {
                Game1.player.maxHealth -= 5 * this.cfg.tier1SkillRingBoost;
                Game1.player.health -= 5 * this.cfg.tier1SkillRingBoost;
            }
            this.addedCombat = 0;
            this.addedCombatNew = 0;

            //Set the old experience points for each skill, I do not recall which index is which skill
            this.oldExperiencePoints[0] = Game1.player.experiencePoints[0];
            this.oldExperiencePoints[1] = Game1.player.experiencePoints[1];
            this.oldExperiencePoints[2] = Game1.player.experiencePoints[2];
            this.oldExperiencePoints[3] = Game1.player.experiencePoints[3];
            this.oldExperiencePoints[4] = Game1.player.experiencePoints[4];
            this.fixHealth("", new string[0]);
        }

        //Runs after the game is loaded and when the game actually launches
        private void onGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            //Load the jsonAssets api for getting the rings as variables
            IJsonAssetsApi api = this.Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
            this.ja = api;
            api.LoadAssets(Path.Combine(this.Helper.DirectoryPath, "assets"));

            //Check for compatible mods
            this.hasSVE = this.Helper.ModRegistry.IsLoaded("FlashShifter.SVECode");
            this.hasWMR = this.Helper.ModRegistry.IsLoaded("bcmpinc.WearMoreRings");
            this.hasCMR = this.Helper.ModRegistry.IsLoaded("Stari.CombineManyRings") || this.Helper.ModRegistry.IsLoaded("Arruda.BalancedCombineManyRings");
            if(this.hasWMR)
                this.moreRings = this.Helper.ModRegistry.GetApi<IMoreRingsApi>("bcmpinc.WearMoreRings");
        }

        //Runs when the game is saved
        public void Saving(object sender, SavingEventArgs e)
        {
            if(this.addedCombat == this.cfg.tier3SkillRingBoost)
            {
                Game1.player.maxHealth -= 5 * this.cfg.tier3SkillRingBoost;
                Game1.player.health -= 5 * this.cfg.tier3SkillRingBoost;
            }
            else if(this.addedCombat == this.cfg.tier2SkillRingBoost)
            {
                Game1.player.maxHealth -= 5 * this.cfg.tier2SkillRingBoost;
                Game1.player.health -= 5 * this.cfg.tier2SkillRingBoost;
            }
            else if(this.addedCombat == this.cfg.tier1SkillRingBoost)
            {
                Game1.player.maxHealth -= 5 * this.cfg.tier1SkillRingBoost;
                Game1.player.health -= 5 * this.cfg.tier1SkillRingBoost;
            }
            this.addedCombat = 0;
            this.addedCombatNew = 0;
        }

        //Runs every game tick, pretty sure I set it to only run the code every second, could be every 10 seconds but I've got my doubts
        private void onUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            //Makes sure the code only runs every second
            if(!Context.IsPlayerFree || !e.IsOneSecond)
                return;

            //Fishing skill
            if(this.ringEquipped(this.RingLegendaryAngler, this.RingLegendaryAnglerName))
            {
                if(this.addedFishing < this.cfg.tier3SkillRingBoost) this.addedFishingNew = this.cfg.tier3SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingMadMariner, this.RingMadMarinerName))
            {
                if(this.addedFishing < this.cfg.tier2SkillRingBoost) this.addedFishingNew = this.cfg.tier2SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingApprenticeFisher, this.RingApprenticeFisherName))
            {
                if(this.addedFishing == 0) this.addedFishingNew = this.cfg.tier1SkillRingBoost;
            }
            else this.addedFishingNew = 0;

            if(this.addedFishingNew != this.addedFishing)
            {
                Game1.player.addedFishingLevel.Value -= this.addedFishing;
                this.addedFishing = this.addedFishingNew;
                Game1.player.addedFishingLevel.Value += this.addedFishing;
            }

            //Farming skill
            if(this.ringEquipped(this.RingNaturesOracle, this.RingNaturesOracleName))
            {
                if(this.addedFarming < this.cfg.tier3SkillRingBoost) this.addedFarmingNew = this.cfg.tier3SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingGreenThumb, this.RingGreenThumbName))
            {
                if(this.addedFarming < this.cfg.tier2SkillRingBoost) this.addedFarmingNew = this.cfg.tier2SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingDecentSoil, this.RingDecentSoilName))
            {
                if(this.addedFarming == 0) this.addedFarmingNew = this.cfg.tier1SkillRingBoost;
            }
            else this.addedFarmingNew = 0;

            if(this.addedFarmingNew != this.addedFarming)
            {
                Game1.player.addedFarmingLevel.Value -= this.addedFarming;
                this.addedFarming = this.addedFarmingNew;
                Game1.player.addedFarmingLevel.Value += this.addedFarming;
            }

            //Foraging skill
            if(this.ringEquipped(this.RingNaturalBounty, this.RingNaturalBountyName))
            {
                if(this.addedForaging < this.cfg.tier3SkillRingBoost) this.addedForagingNew = this.cfg.tier3SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingGatherer, this.RingGathererName))
            {
                if(this.addedForaging < this.cfg.tier2SkillRingBoost) this.addedForagingNew = this.cfg.tier2SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingDeepRoots, this.RingDeepRootsName))
            {
                if(this.addedForaging == 0) this.addedForagingNew = this.cfg.tier1SkillRingBoost;
            }
            else this.addedForagingNew = 0;

            if(this.addedForagingNew != this.addedForaging)
            {
                Game1.player.addedForagingLevel.Value -= this.addedForaging;
                this.addedForaging = this.addedForagingNew;
                Game1.player.addedForagingLevel.Value += this.addedForaging;
            }

            //Mining skill
            if(this.ringEquipped(this.RingDwarvenLuck, this.RingDwarvenLuckName))
            {
                if(this.addedMining < this.cfg.tier3SkillRingBoost) this.addedMiningNew = this.cfg.tier3SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingCaves, this.RingCavesName))
            {
                if(this.addedMining < this.cfg.tier2SkillRingBoost) this.addedMiningNew = this.cfg.tier2SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingWieldyPick, this.RingWieldyPickName))
            {
                if(this.addedMining == 0) this.addedMiningNew = this.cfg.tier1SkillRingBoost;
            }
            else this.addedMiningNew = 0;

            if(this.addedMiningNew != this.addedMining)
            {
                Game1.player.addedMiningLevel.Value -= this.addedMining;
                this.addedMining = this.addedMiningNew;
                Game1.player.addedMiningLevel.Value += this.addedMining;
            }

            //Combat skill
            if(this.ringEquipped(this.RingWarGod, this.RingWarGodName))
            {
                if(this.addedCombat < this.cfg.tier3SkillRingBoost) this.addedCombatNew = this.cfg.tier3SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingPureStrength, this.RingPureStrengthName))
            {
                if(this.addedCombat < this.cfg.tier2SkillRingBoost) this.addedCombatNew = this.cfg.tier2SkillRingBoost;
            }
            else if(this.ringEquipped(this.RingSharperBlades, this.RingSharperBladesName))
            {
                if(this.addedCombat == 0) this.addedCombatNew = this.cfg.tier1SkillRingBoost;
            }
            else this.addedCombatNew = 0;

            if(this.addedCombatNew != this.addedCombat)
            {
                Game1.player.addedCombatLevel.Value -= this.addedCombat;
                Game1.player.maxHealth -= this.addedCombat * 5;
                if(Game1.player.health > Game1.player.maxHealth)
                    Game1.player.health = Game1.player.maxHealth;

                this.addedCombat = this.addedCombatNew;

                Game1.player.addedCombatLevel.Value += this.addedCombat;
                if(Game1.player.maxHealth == Game1.player.health)
                    Game1.player.health += this.addedCombat * 5;
                Game1.player.maxHealth += this.addedCombat * 5;
            }

            //Experience Rings
            this.expMult = 0.0;
            if(this.ringEquipped(this.RingIneffableKnowledge, this.RingIneffableKnowledgeName))
            {
                this.expMult = this.cfg.tier3ExperienceRingBoost;
            }
            else if(this.ringEquipped(this.RingKnowledge, this.RingKnowledgeName))
            {
                this.expMult = this.cfg.tier2ExperienceRingBoost;
            }
            else if(this.ringEquipped(this.RingInsight, this.RingInsightName))
            {
                this.expMult = this.cfg.tier1ExperienceRingBoost;
            }
            else this.expMult = 0.0;

            //Handling the gained experience
            //The experience rings do not multiply incoming experience, but calculate the difference between now and last itteration andadd a portion of the difference
            for(int index = 0; index < 5; ++index)
            {
                if(Game1.player.experiencePoints[index] > this.oldExperiencePoints[index])
                {
                    Game1.player.gainExperience(index, (int) (this.expMult * Game1.player.experiencePoints[index] - this.oldExperiencePoints[index]));
                    this.oldExperiencePoints[index] = Game1.player.experiencePoints[index];
                }
            }
        }

        //Handles things that a button must be pressed for
        private void onButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if(!Context.IsWorldReady)
                return;
            //If F9(debug key) is pressed
            if(this.Helper.Input.IsDown((SButton) 120))
            {
                this.Monitor.Log(string.Format("Cursor At X:{0} Y:{1} \n Player at {2}", e.Cursor.GrabTile.X, e.Cursor.GrabTile.Y, Game1.currentLocation?.Name), (LogLevel) 1);
                if(this.hasWMR)
                {
                    foreach(Item allRing in this.moreRings.GetAllRings(Game1.player))
                        this.Monitor.Log("Ring: " + allRing.Name, (LogLevel) 1);
                }
            }

            //Decide whether to watch Right Click of the A button on a comtroller
            SButton sbutton = (SButton) 1001;
            bool flag = false;
            if(this.Helper.Input.IsDown((SButton) 1001))
            {
                flag = true;
                sbutton = (SButton) 1001;
            }
            else if(this.Helper.Input.IsDown((SButton) 6096))
            {
                flag = true;
                sbutton = (SButton) 6096;
            }
            if(!flag)
                return;

            //Transform rings
            if(Utility.IsNormalObjectAtParentSheetIndex(Game1.player.ActiveObject, this.RingLegendaryAnglerDusty) && !Game1.player.mailReceived.Contains("legendaryAnglerRing"))
            {
                foreach(Building building in Game1.getFarm().buildings)
                {
                    if(building.buildingType == "Fish Pond" && building.occupiesTile(e.Cursor.GrabTile))
                    {
                        Game1.player.reduceActiveItemByOne();
                        this.getTier3Ring(this.RingLegendaryAngler, "legendaryAnglerRing");
                    }
                }
            }
            else if(Utility.IsNormalObjectAtParentSheetIndex(Game1.player.ActiveObject, this.RingNaturesOracleGrassy) && !Game1.player.mailReceived.Contains("naturesOracleRing"))
            {
                foreach(FarmAnimal allFarmAnimal in Game1.getFarm().getAllFarmAnimals())
                {
                    Vector2 grabTile = allFarmAnimal.GetGrabTile();
                    int[,] coords = new int[9, 2]
                    {
                        {
                            (int) grabTile.X - 1,
                            (int) grabTile.Y - 1
                        },
                        {
                            (int) grabTile.X,
                            (int) grabTile.Y - 1
                        },
                        {
                            (int) grabTile.X + 1,
                            (int) grabTile.Y - 1
                        },
                        {
                            (int) grabTile.X - 1,
                            (int) grabTile.Y
                        },
                        {
                            (int) grabTile.X,
                            (int) grabTile.Y
                        },
                        {
                            (int) grabTile.X + 1,
                            (int) grabTile.Y
                        },
                        {
                            (int) grabTile.X - 1,
                            (int) grabTile.Y + 1
                        },
                        {
                            (int) grabTile.X,
                            (int) grabTile.Y + 1
                        },
                        {
                            (int) grabTile.X + 1,
                            (int) grabTile.Y + 1
                        }
                    };
                    if(Game1.player.currentLocation == allFarmAnimal.currentLocation && this.checkLocations(coords, e.Cursor.GrabTile))
                    {
                        this.Helper.Input.Suppress(sbutton);
                        Game1.player.reduceActiveItemByOne();
                        this.getTier3Ring(this.RingNaturesOracle, "naturesOracleRing");
                    }
                }
            }
            else if(Utility.IsNormalObjectAtParentSheetIndex(Game1.player.ActiveObject, this.RingWarGodCursed) && !Game1.player.mailReceived.Contains("warGodRing"))
            {
                int[,] coords = new int[4, 2]
                {
                    {
                        29,
                        6
                    },
                    {
                        30,
                        6
                    },
                    {
                        29,
                        7
                    },
                    {
                        30,
                        7
                    }
                };
                if(!(Game1.currentLocation is MineShaft) || Game1.CurrentMineLevel != 77377 || !this.checkLocations(coords, e.Cursor.GrabTile))
                    return;
                this.Helper.Input.Suppress(sbutton);
                Game1.player.reduceActiveItemByOne();
                this.getTier3Ring(this.RingWarGod, "warGodRing");
            }
            else if(Utility.IsNormalObjectAtParentSheetIndex(Game1.player.ActiveObject, this.RingNaturalBountySticky) && !Game1.player.mailReceived.Contains("naturalBountyRing"))
            {
                int[,] coords = new int[11, 2]
                {
                    {
                        8,
                        6
                    },
                    {
                        9,
                        6
                    },
                    {
                        10,
                        6
                    },
                    {
                        7,
                        7
                    },
                    {
                        8,
                        7
                    },
                    {
                        9,
                        7
                    },
                    {
                        10,
                        7
                    },
                    {
                        7,
                        8
                    },
                    {
                        8,
                        8
                    },
                    {
                        9,
                        8
                    },
                    {
                        10,
                        8
                    }
                };
                if(!(Game1.currentLocation is Woods) || !this.checkLocations(coords, e.Cursor.GrabTile))
                    return;
                this.Helper.Input.Suppress(sbutton);
                Game1.player.reduceActiveItemByOne();
                this.getTier3Ring(this.RingNaturalBounty, "naturalBountyRing");
            }
            else if(Utility.IsNormalObjectAtParentSheetIndex(Game1.player.ActiveObject, this.RingDwarvenLuckStone) && !Game1.player.mailReceived.Contains("dwarvenLuckRing"))
            {
                int[,] coords;
                if(this.hasSVE)
                    coords = new int[12, 2]
                    {
                        {
                            8,
                            13
                        },
                        {
                            9,
                            13
                        },
                        {
                            10,
                            13
                        },
                        {
                            11,
                            13
                        },
                        {
                            8,
                            14
                        },
                        {
                            9,
                            14
                        },
                        {
                            10,
                            14
                        },
                        {
                            11,
                            14
                        },
                        {
                            8,
                            15
                        },
                        {
                            9,
                            15
                        },
                        {
                            10,
                            15
                        },
                        {
                            11,
                            15
                        }
                    };
                else
                    coords = new int[9, 2]
                    {
                        {
                            11,
                            12
                        },
                        {
                            12,
                            12
                        },
                        {
                            13,
                            12
                        },
                        {
                            11,
                            13
                        },
                        {
                            12,
                            13
                        },
                        {
                            13,
                            13
                        },
                        {
                            11,
                            14
                        },
                        {
                            12,
                            14
                        },
                        {
                            13,
                            14
                        }
                    };
                if(!(Game1.currentLocation?.Name == "Blacksmith") || !this.checkLocations(coords, e.Cursor.GrabTile))
                    return;
                this.Helper.Input.Suppress(sbutton);
                Game1.player.reduceActiveItemByOne();
                this.getTier3Ring(this.RingDwarvenLuck, "dwarvenLuckRing");
            }
            else
            {
                if(!Utility.IsNormalObjectAtParentSheetIndex(Game1.player.ActiveObject, this.RingIneffableKnowledgeElusive) || Game1.player.mailReceived.Contains("ineffableKnowledgeRing"))
                    return;
                int[,] coords;
                if(this.hasSVE)
                    coords = new int[12, 2]
                    {
                        {
                            22,
                            4
                        },
                        {
                            23,
                            4
                        },
                        {
                            24,
                            4
                        },
                        {
                            25,
                            4
                        },
                        {
                            22,
                            5
                        },
                        {
                            23,
                            5
                        },
                        {
                            24,
                            5
                        },
                        {
                            25,
                            5
                        },
                        {
                            22,
                            6
                        },
                        {
                            23,
                            6
                        },
                        {
                            24,
                            6
                        },
                        {
                            25,
                            6
                        }
                    };
                else
                    coords = new int[6, 2]
                    {
                        {
                            11,
                            4
                        },
                        {
                            12,
                            4
                        },
                        {
                            13,
                            4
                        },
                        {
                            11,
                            5
                        },
                        {
                            12,
                            5
                        },
                        {
                            13,
                            5
                        }
                    };
                if((Game1.currentLocation?.Name == "WizardHouseBasement" || Game1.currentLocation?.Name == "WizardBasement" || Game1.currentLocation?.Name == "Custom_WizardBasement") && this.checkLocations(coords, e.Cursor.GrabTile))
                {
                    this.Helper.Input.Suppress(sbutton);
                    Game1.player.reduceActiveItemByOne();
                    this.getTier3Ring(this.RingIneffableKnowledge, "ineffableKnowledgeRing");
                }
            }
        }

        private bool checkLocations(int[,] coords, Vector2 tile)
        {
            for(int index = 0; index < coords.GetLength(0); ++index)
            {
                int coord1 = coords[index, 0];
                int coord2 = coords[index, 1];
                if(tile == new Vector2(coord1, coord2))
                    return true;
            }
            return false;
        }

        //Recursively checks a combined ring for a specific ring
        private bool recursiveCombinedSearch(Ring ring, string name)
        {
            if(ring is CombinedRing combinedRing1)
            {
                foreach(Ring combinedRing in combinedRing1.combinedRings)
                {
                    if(this.recursiveCombinedSearch(combinedRing, name))
                        return true;
                }
            }
            else if(ring.Name == name)
                return true;
            return false;
        }

        private bool ringEquipped(int id, string name)
        {
            //If Wear More Rings is enabled
            if(this.hasWMR)
            {
                //Use the WMR Api to cycle through every ring
                foreach(Ring allRing in this.moreRings.GetAllRings(Game1.player))
                {
                    //If the ring is a combined ring
                    if(allRing is CombinedRing combinedRing4)
                    {
                        //If combine many rings in enabled then search recursively
                        if(this.hasCMR)
                        {
                            if(this.recursiveCombinedSearch(allRing, name))
                                return true;
                        }
                        else
                        {
                            //Otherwise just check the "left" and "right" rings
                            foreach(Item combinedRing in combinedRing4.combinedRings)
                            {
                                if(combinedRing.Name == name)
                                    return true;
                            }
                        }
                    }
                    else if(allRing.Name == name)
                        return true;
                }
                return false;
            }

            //If Combine Many Rings is enabled without WMR
            if(this.hasCMR)
            {
                //Check the left and right rings for them being a combined ring
                if(Game1.player.leftRing.Value is CombinedRing combinedRing8)
                {
                    if(this.recursiveCombinedSearch(combinedRing8, name))
                        return true;
                }
                else if(Game1.player.rightRing.Value is CombinedRing combinedRing9 && this.recursiveCombinedSearch(combinedRing9, name))
                    return true;
            }
            //Otherwise return the most disgusting "one line" conditional known to man
            return Game1.player.leftRing.Value != null &&                          //If a left ring is worn
                   Game1.player.leftRing.Value.ParentSheetIndex == id ||           //And if said left ring is the ring we're looking for return true
                   Game1.player.rightRing.Value != null &&                         //If a right ring is worn
                   Game1.player.rightRing.Value.ParentSheetIndex == id ||          //And if said right ring is the ring we're looking for return true
                   Game1.player.leftRing.Value is CombinedRing combinedRing10 &&   //If the left ring is a combined ring
                   (combinedRing10.combinedRings[0].Name == name ||                //If the internal "Left Ring" is the ring we're looking for or
                        combinedRing10.combinedRings[1].Name == name) ||           //If the internal "Right Ring" is the ring we're looking for return true
                   Game1.player.rightRing.Value is CombinedRing combinedRing11 &&  //If the right ring is a combined ring
                   (combinedRing11.combinedRings[0].Name == name ||                //If the internal "Left Ring" is the ring we're looking for or
                        combinedRing11.combinedRings[1].Name == name);             //If the internal "Right Ring" is the ring we're looking for return true
        }

        //Handle the giving and animation for recieving a Tier 3 ring
        //I could not say exactly what this does, I pretty much copy pasted from the code for recieving the Galaxy Sword
        private void getTier3Ring(int id, string mailName)
        {
            Game1.flashAlpha = 1.0F;
            Game1.player.holdUpItemThenMessage(new StardewValley.Object(id, 1, false, -1, 0), true);
            if(!Game1.player.addItemToInventoryBool(new StardewValley.Object(id, 1, false, -1, 0), false))
                Game1.createItemDebris(new StardewValley.Object(id, 1, false, -1, 0), Game1.player.getStandingPosition(), 1, null, -1);
            Game1.player.mailReceived.Add(mailName);
            Game1.player.jitterStrength = 0.0F;
            Game1.screenGlowHold = false;
        }
    }
}