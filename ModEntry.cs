using Microsoft.Xna.Framework;
using Netcode;
using SpaceShared.APIs;
using ContentPatcher;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using SpaceCore;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using StardewValley.Buffs;

namespace SkillRings
{
    internal sealed class ModEntry : Mod
    {
        private ModConfig cfg;

        private bool hasSpaceLuckSkill = false;
        private bool hasSVE = false;

        private float expMultiplier = 0f;
        private int[] oldExperience = { 0, 0, 0, 0, 0, 0 };

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += new EventHandler<GameLaunchedEventArgs>(this.onGameLaunched);
            helper.Events.GameLoop.UpdateTicked += new EventHandler<UpdateTickedEventArgs>(this.onUpdateTicked);
            helper.Events.GameLoop.DayStarted += new EventHandler<DayStartedEventArgs>(this.onDayStarted);
            helper.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(this.onButtonPressed);

            //Adding console commands to the game
            //Fixes the health of the player if it was messed up by the mod
            helper.ConsoleCommands.Add("fixhealth", "Changes max health to what it should be, take off combat rings and don't have combat buffs on\n\nUsage: fixhealth", new Action<string, string[]>(this.fixHealth));
            //Converts the held broken ring into its fixed tier 3 equivalent
            helper.ConsoleCommands.Add("fixring", "Fixes the currently held broken ring, converting it to its tier 3 equivalent", new Action<string, string[]>(this.fixRing));
            //Load the config file
            this.cfg = helper.ReadConfig<ModConfig>();
        }

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

        private void fixRing(string command, string[] args)
        {
            if(Game1.player.ActiveItem == null) return;
            if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_FishingRingB")
            {
                Game1.player.reduceActiveItemByOne();
                this.getTier3Ring("AlphaMeece.SkillRings_FishingRing3");
                this.Monitor.Log("Got the Ring of the Legendary Angler.", (LogLevel) 1);
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_FarmingRingB")
            {
                Game1.player.reduceActiveItemByOne();
                this.getTier3Ring("AlphaMeece.SkillRings_FarmingRing3");
                this.Monitor.Log("Got the Ring of Nature's Oracle.", (LogLevel) 1);
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.AlphaMeece.SkillRings_ForagingRingB")
            {
                Game1.player.reduceActiveItemByOne();
                this.getTier3Ring("AlphaMeece.SkillRings_ForagingRing3");
                this.Monitor.Log("Got the Ring of Natural Bounty.", (LogLevel) 1);
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_MiningRingB")
            {
                Game1.player.reduceActiveItemByOne();
                this.getTier3Ring("AlphaMeece.SkillRings_MiningRing#");
                this.Monitor.Log("Got the Ring of Dwarven Luck.", (LogLevel) 1);
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_CombatRingB")
            {
                Game1.player.reduceActiveItemByOne();
                this.getTier3Ring("AlphaMeece.SkillRings_CombatRing3");
                this.Monitor.Log("Got the Ring of the War God.", (LogLevel) 1);
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_ExperienceRingB")
            {
                Game1.player.reduceActiveItemByOne();
                this.getTier3Ring("AlphaMeece.SkillRings_ExperienceRing3");
                this.Monitor.Log("Got the Ring of Ineffable Knowledge.", (LogLevel) 1);
            }
            else
            {
                this.Monitor.Log("Player not holding a broken ring.", (LogLevel) 1);
            }
        }

        private void getTier3Ring(string id)
        {
            Game1.flashAlpha = 1.0F;
            Game1.player.holdUpItemThenMessage(new StardewValley.Object(id, 1, false, -1, 0), true);
            if(!Game1.player.addItemToInventoryBool(ItemRegistry.Create(id), false))
                Game1.createItemDebris(new StardewValley.Object(id, 1, false, -1, 0), Game1.player.getStandingPosition(), 1, null, -1);
            Game1.player.jitterStrength = 0.0F;
            Game1.screenGlowHold = false;
        }

        private void onDayStarted(object sender, DayStartedEventArgs e)
        {
            this.oldExperience = Game1.player.experiencePoints.ToArray();
            handleMail();
        }

        private void onGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            this.hasSpaceLuckSkill = this.Helper.ModRegistry.IsLoaded("spacechase0.LuckSkill");
            this.hasSVE = this.Helper.ModRegistry.IsLoaded("FlashShifter.SVECode");
            //this.hasWMR = this.Helper.ModRegistry.IsLoaded("bcmpinc.WearMoreRings");
            //this.hasCMR = this.Helper.ModRegistry.IsLoaded("Stari.CombineManyRings") || this.Helper.ModRegistry.IsLoaded("Arruda.BalancedCombineManyRings");

            var contentPatcherAPI = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            contentPatcherAPI.RegisterToken(this.ModManifest, "TierOneRingPrice", () => new[] { this.cfg.tier1SkillRingPrice.ToString() });
            contentPatcherAPI.RegisterToken(this.ModManifest, "TierTwoRingPrice", () => new[] { this.cfg.tier2SkillRingPrice.ToString() });
            contentPatcherAPI.RegisterToken(this.ModManifest, "TierThreeRingPrice", () => new[] { this.cfg.tier3SkillRingPrice.ToString() });
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

        private void onButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if(!Context.IsWorldReady)
                return;
            //If F9(debug key) is pressed
            if(this.Helper.Input.IsDown((SButton) 120))
            {
                this.Monitor.Log(string.Format("Cursor At X:{0} Y:{1} \n Player at {2}", e.Cursor.GrabTile.X, e.Cursor.GrabTile.Y, Game1.currentLocation?.Name), (LogLevel) 1);
                //if(this.hasWMR)
                //{
                //    foreach(Item allRing in this.moreRings.GetAllRings(Game1.player))
                //        this.Monitor.Log("Ring: " + allRing.Name, (LogLevel) 1);
                //}
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

            if(Game1.player.ActiveItem == null) return;

            //Transform rings
            if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_FishingRingB")
            {
                foreach(Building building in Game1.getFarm().buildings)
                {
                    if(building.buildingType.Value == "Fish Pond" && building.occupiesTile(e.Cursor.GrabTile))
                    {
                        Game1.player.reduceActiveItemByOne();
                        this.getTier3Ring("AlphaMeece.SkillRings_FishingRing3");
                    }
                }
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_FarmingRingB")
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
                        this.getTier3Ring("AlphaMeece.SkillRings_FarmingRing3");
                    }
                }
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_CombatRingB")
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
                this.getTier3Ring("AlphaMeece.SkillRings_CombatRing3");
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_ForagingRingB")
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
                this.getTier3Ring("AlphaMeece.SkillRings_ForagingRing3");
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_MiningRingB")
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
                this.getTier3Ring("AlphaMeece.SkillRings_MiningRing3");
            }
            else if(Game1.player.ActiveItem.QualifiedItemId == "(O)AlphaMeece.SkillRings_ExperienceRingB")
            { 
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
                    this.getTier3Ring("AlphaMeece.SkillRings_ExperienceRing3");
                }
            }
        }

        private void onUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if(!Context.IsPlayerFree || !e.IsOneSecond)
                return;

            List<Buff> buffs = new List<Buff>();

            //Farming
            Buff farmingBuff = new Buff(id: "AlphaMeece.SkillRings_FarmingBuff", duration: Buff.ENDLESS);
            int farmingLevel = -1;

            if(this.hasRing("AlphaMeece.SkillRings_FarmingRing3"))
                farmingLevel = cfg.tier3SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_FarmingRing2"))
                farmingLevel = cfg.tier2SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_FarmingRing1"))
                farmingLevel = cfg.tier1SkillRingBoost;
            else if(Game1.player.hasBuff("AlphaMeece.SkillRings_FarmingBuff"))
                Game1.player.buffs.Remove("AlphaMeece.SkillRings_FarmingBuff");

            if(farmingLevel != -1) 
            {
                farmingBuff.effects.Add(new BuffEffects()
                {
                    FarmingLevel = { farmingLevel }
                });
                buffs.Add(farmingBuff);
            }

            //Fishing
            Buff fishingBuff = new Buff(id: "AlphaMeece.SkillRings_FishingBuff", duration: Buff.ENDLESS);
            int fishingLevel = -1;

            if(this.hasRing("AlphaMeece.SkillRings_FishingRing3"))
                fishingLevel = cfg.tier3SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_FishingRing2"))
                fishingLevel = cfg.tier2SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_FishingRing1"))
                fishingLevel = cfg.tier1SkillRingBoost; 
            else if(Game1.player.hasBuff("AlphaMeece.SkillRings_FishingBuff"))
                Game1.player.buffs.Remove("AlphaMeece.SkillRings_FishingBuff");

            if(fishingLevel != -1)
            {
                fishingBuff.effects.Add(new BuffEffects()
                {
                    FishingLevel = { fishingLevel }
                });
                buffs.Add(fishingBuff);
            }

            //Mining
            Buff miningBuff = new Buff(id: "AlphaMeece.SkillRings_MiningBuff", duration: Buff.ENDLESS);
            int miningLevel = -1;

            if(this.hasRing("AlphaMeece.SkillRings_MiningRing3"))
                miningLevel = cfg.tier3SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_MiningRing2"))
                miningLevel = cfg.tier2SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_MiningRing1"))
                miningLevel = cfg.tier1SkillRingBoost;
            else if(Game1.player.hasBuff("AlphaMeece.SkillRings_MiningBuff"))
                Game1.player.buffs.Remove("AlphaMeece.SkillRings_MiningBuff");

            if(miningLevel != -1)
            {
                miningBuff.effects.Add(new BuffEffects()
                {
                    MiningLevel = { miningLevel }
                });
                buffs.Add(miningBuff);
            }

            //Foraging
            Buff foragingBuff = new Buff(id: "AlphaMeece.SkillRings_ForagingBuff", duration: Buff.ENDLESS);
            int foragingLevel = -1;

            if(this.hasRing("AlphaMeece.SkillRings_ForagingRing3"))
                foragingLevel = cfg.tier3SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_ForagingRing2"))
                foragingLevel = cfg.tier2SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_ForagingRing1"))
                foragingLevel = cfg.tier1SkillRingBoost;
            else if(Game1.player.hasBuff("AlphaMeece.SkillRings_ForagingBuff"))
                Game1.player.buffs.Remove("AlphaMeece.SkillRings_ForagingBuff");

            if(foragingLevel != -1)
            {
                foragingBuff.effects.Add(new BuffEffects()
                {
                    ForagingLevel = { foragingLevel }
                });
                buffs.Add(foragingBuff);
            }

            //Combat
            Buff combatBuff = new Buff(id: "AlphaMeece.SkillRings_CombatBuff", duration: Buff.ENDLESS);
            int combatLevel = -1;

            if(this.hasRing("AlphaMeece.SkillRings_CombatRing3"))
                combatLevel = cfg.tier3SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_CombatRing2"))
                combatLevel = cfg.tier2SkillRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_CombatRing1"))
                combatLevel = cfg.tier1SkillRingBoost;
            else if(Game1.player.hasBuff("AlphaMeece.SkillRings_CombatBuff"))
                Game1.player.buffs.Remove("AlphaMeece.SkillRings_CombatBuff");

            if(combatLevel != -1)
            {
                combatBuff.effects.Add(new BuffEffects()
                {
                    CombatLevel = { combatLevel },
                    Attack = { combatLevel * 2 },
                    Defense = { combatLevel * 2 },
                    Immunity = { combatLevel * 2 }
                });
                buffs.Add(combatBuff);
            }

            //Experience
            Buff experienceBuff = new Buff(id: "AlphaMeece.SkillRings_ExperienceBuff", duration: Buff.ENDLESS);
            float expLevel = -1f;

            if(this.hasRing("AlphaMeece.SkillRings_ExperienceRing3"))
                expLevel = cfg.tier3ExperienceRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_ExperienceRing2"))
                expLevel = cfg.tier2ExperienceRingBoost;
            else if(this.hasRing("AlphaMeece.SkillRings_ExperienceRing1"))
                expLevel = cfg.tier1ExperienceRingBoost;
            else if(Game1.player.hasBuff("AlphaMeece.SkillRings_ExperienceBuff"))
            {
                Game1.player.buffs.Remove("AlphaMeece.SkillRings_ExperienceBuff");
                this.expMultiplier = 0f;
            }
                

            if(expLevel != -1f)
            {
                this.expMultiplier = expLevel;
                buffs.Add(experienceBuff);
            }

            //Modded skills
            //Luck
            if(this.hasSpaceLuckSkill)
            {
                Buff luckBuff = new Buff(id: "AlphaMeece.SkillRings_LuckBuff", duration: Buff.ENDLESS);
                int luckLevel = -1;

                if(this.hasRing("AlphaMeece.SkillRings_LuckRing3"))
                    luckLevel = cfg.tier3SkillRingBoost;
                else if(this.hasRing("AlphaMeece.SkillRings_LuckRing2"))
                    luckLevel = cfg.tier2SkillRingBoost;
                else if(this.hasRing("AlphaMeece.SkillRings_LuckRing1"))
                    luckLevel = cfg.tier1SkillRingBoost;
                else if(Game1.player.hasBuff("AlphaMeece.SkillRings_LuckBuff"))
                    Game1.player.buffs.Remove("AlphaMeece.SkillRings_LuckBuff");

                if(luckLevel != -1)
                {
                    luckBuff.effects.Add(new BuffEffects()
                    {
                        LuckLevel = { luckLevel }
                    });
                    buffs.Add(luckBuff);
                }
            }

            foreach (var item in buffs)
            {
                item.visible = false;
                Game1.player.applyBuff(item);
            }

            if(Game1.player.hasBuff("AlphaMeece.SkillRings_ExperienceBuff"))
            {
                if(oldExperience != Game1.player.experiencePoints.ToArray())
                {
                    for(int skill = 0; skill < 6; skill++)
                    {
                        int currentExp = Game1.player.experiencePoints.ElementAt(skill);
                        if(currentExp > oldExperience[skill])
                        {
                            Game1.player.gainExperience(skill, (int) Math.Ceiling((currentExp - oldExperience[skill]) * this.expMultiplier));
                            this.Monitor.Log($"Gained experience from experience ring\nCurrent Multiplier:{1 + this.expMultiplier}\nExp Change:{currentExp} - {oldExperience[skill]} = {currentExp - oldExperience[skill]}\nGained Experience: {Math.Ceiling((currentExp - oldExperience[skill]) * this.expMultiplier)}\nNew Total: {Game1.player.experiencePoints.ElementAt(skill)}", LogLevel.Debug);
                        }
                    }
                }
            }
            oldExperience = Game1.player.experiencePoints.ToArray();
        }

        private bool hasRing(string Id)
        {
            return Game1.player.isWearingRing(Id);
        }

        private void handleMail()
        {
            if(!Game1.player.mailReceived.Contains("AlphaMeece.SkillRings_DustyRing"))
            {
                if(Game1.player.getFriendshipHeartLevelForNPC("Willy") >= 4) Game1.player.mailbox.Add("AlphaMeece.SkillRings_DustyRing");
            }

            if(!Game1.player.mailReceived.Contains("AlphaMeece.SkillRings_StoneRing"))
            {
                if(Game1.player.getFriendshipHeartLevelForNPC("Dwarf") >= 4) Game1.player.mailbox.Add("AlphaMeece.SkillRings_StoneRing");
            }

            if(!Game1.player.mailReceived.Contains("AlphaMeece.SkillRings_StickyRing"))
            {
                if(Game1.player.getFriendshipHeartLevelForNPC("Linus") >= 4) Game1.player.mailbox.Add("AlphaMeece.SkillRings_StickyRing");
            }

            if(!Game1.player.mailReceived.Contains("AlphaMeece.SkillRings_GrassyRing"))
            {
                if(Game1.player.getFriendshipHeartLevelForNPC("Marnie") >= 4) Game1.player.mailbox.Add("AlphaMeece.SkillRings_GrassyRing");
            }

            if(!Game1.player.mailReceived.Contains("AlphaMeece.SkillRings_CursedRing"))
            {
                if(Game1.player.getFriendshipHeartLevelForNPC("Abigail") >= 4) Game1.player.mailbox.Add("AlphaMeece.SkillRings_CursedRing");
            }

            //Recipes
            if(!Game1.player.mailReceived.Contains("AlphaMeece.SkillRings_Foraging1Recipe"))
            {
                if(Game1.player.getFriendshipHeartLevelForNPC("Linus") >= 1) Game1.player.mailbox.Add("AlphaMeece.SkillRings_Foraging1Recipe");
            }

            if(!Game1.player.mailReceived.Contains("AlphaMeece.SkillRings_Foraging2Recipe"))
            {
                if(Game1.player.getFriendshipHeartLevelForNPC("Linus") >= 2) Game1.player.mailbox.Add("AlphaMeece.SkillRings_Foraging2Recipe");
            }
        }
    }
}