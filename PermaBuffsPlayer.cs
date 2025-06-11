using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Steamworks;
using Terraria.Localization;
using Terraria.UI.Chat;
using tModPorter;
using Terraria.ModLoader.Config;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Terraria.Enums;
using Terraria.GameContent.UI.Minimap;


namespace PermaBuffs
{
    public class PermaBuffsPlayer : ModPlayer
    {
        /// <summary>
        /// The list of buffs to be added to the player as soon as possible
        /// </summary>
        public List<BuffInfo> pendingBuffs = new List<BuffInfo>();
        /// <summary>
        /// The texture of the golden and red border to be drawn on top of the standard buff icon
        /// </summary>
        public static Asset<Texture2D> goldenBorder = null;
        public static Asset<Texture2D> purpleBorder = null;
        /// <summary>
        /// Sets the time a buff icon keeps the golden border
        /// </summary>
        public const int TimeForGolden = 5 * 60;
        /// <summary>
        /// The timer in ticks, caps out at TimeForGolden
        /// </summary>
        public int goldenTicks = TimeForGolden;

        /// <summary>
        /// The buffs modified by the mod - these get a special border
        /// </summary>
        public bool[] drawQueue;
        /// <summary>
        /// The queue of buff icons recently modified by a persist after death function temporarily get a golden border
        /// </summary>
        public bool[] goldenQueue;
        public bool[] alwaysPermanent;
        public bool[] neverPermanent;
        public bool[] activeBanners;
        public int[] buffItemIDs;

        public bool alwaysPermanentKeyPressed = false;
        public bool neverPermanentKeyPressed = false;
        public bool permaTooltipSeen = false;
        public bool neverTooltipSeen = false;

        public int viewingPermaTooltip = 0;
        public int viewingNeverTooltip = 0;
        public float minionSlots;
        public float minionIncrease;
        public float maxMinions = 1f;

        public bool permaBound = false;
        public bool neverBound = false;
        public bool buffCountDifferent = false;
        public bool tryAddModBuff;
        public bool npcCountDifferent = false;
        public bool tryAddModBanner;
        public bool bannersNeedRefresh = false;
        public bool initialized = false;
        public bool setCrystalLeafFlag;
        public bool setSolarCounterFlag;
        public bool stardustGuardianFlag;
        public bool hadPermaBeetleBuff = false;
        public bool hideWerewolf = false;
        public bool hideMerman = false;

        public static ModKeybind alwaysPermanentKey;
        public static ModKeybind neverPermanentKey;

        public override void Initialize()
        {
            goldenQueue = new bool[BuffLoader.BuffCount];
            alwaysPermanent = new bool[BuffLoader.BuffCount];
            neverPermanent = new bool[BuffLoader.BuffCount];
            buffItemIDs = new int[BuffLoader.BuffCount];
            activeBanners = new bool[NPCLoader.NPCCount];
            initialized = true;
            tryAddModBanner = false;
            tryAddModBuff = false;
        }

        public void CheckToggleKeys(BuffInfo buff, int buffSlot)
        {
            if (!(alwaysPermanentKeyPressed || neverPermanentKeyPressed))
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.MenuTick);

            if (alwaysPermanentKeyPressed)
            {
                alwaysPermanent[buff.type] = !alwaysPermanent[buff.type];
                neverPermanent[buff.type] = false;

                if (!alwaysPermanent[buff.type])
                {
                    Player player = Main.LocalPlayer;
                    player.buffTime[buffSlot] = Math.Max(buff.timeLeft, TimeForGolden);
                }
            }
            else if (neverPermanentKeyPressed)
            {
                neverPermanent[buff.type] = !neverPermanent[buff.type];
                alwaysPermanent[buff.type] = false;

                if (buffItemIDs[buff.type] != 0 && neverPermanent[buff.type])
                    Main.NewText(Language.GetTextValue("Mods.PermaBuffs.Errors.NeverBuffSummon"));
            }

            alwaysPermanentKeyPressed = false;
            neverPermanentKeyPressed = false;
            goldenQueue[buff.type] = false;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (alwaysPermanentKey.JustPressed)
            {
                alwaysPermanentKeyPressed = true;
            }
            else if (alwaysPermanentKey.JustReleased)
            {
                alwaysPermanentKeyPressed = false;
            }

            if (neverPermanentKey.JustPressed)
            {
                neverPermanentKeyPressed = true;
            }
            else if (neverPermanentKey.JustReleased)
            {
                neverPermanentKeyPressed = false;
            }
        }

        public override void SetStaticDefaults()
        {
            // Loads the draw texture in async
            goldenBorder = ModContent.Request<Texture2D>("Permabuffs/buffFrame");
            purpleBorder = ModContent.Request<Texture2D>("Permabuffs/neverBuffFrame");
        }

        public override void PreUpdateBuffs()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            minionSlots = 0f;

            // Golden queue manager. 
            if (goldenTicks < TimeForGolden)
            {
                goldenTicks++;
            }
            else if (goldenTicks == TimeForGolden)
            {
                goldenQueue = new bool[BuffLoader.BuffCount];
                goldenTicks++;
            }

            Player player = Main.LocalPlayer;
            if (!player.dead) pendingBuffs.Clear();

            // Re apply permanent buffs if they're somehow missing
            for (int buffType = 0; buffType < BuffLoader.BuffCount; buffType++)
            {
                if (!alwaysPermanent[buffType])
                    continue;

                // i in this case is the buffType, since alwaysPermanent is indexed by buffType
                int buffSlotOnPlayer = player.FindBuffIndex(buffType);

                // if the buff was not found and we can re-add the buff
                if (buffSlotOnPlayer == -1)
                {
                    BuffInfo buff = new BuffInfo(buffType, TimeForGolden);
                    // Re-add it
                    buff.AddBuffToPlayer(this, player);
                }
                else if (player.slotsMinions + minionIncrease <= maxMinions && buffItemIDs[buffType] != 0)
                {
                    BuffInfo buff = new BuffInfo(buffType, player.buffTime[buffSlotOnPlayer]);
                    // more minions
                    buff.AddBuffToPlayer(this, player);
                }
            }

            // Apply hooks 
            for (int buffSlot = 0; buffSlot < Player.MaxBuffs; buffSlot++)
            {
                BuffInfo buff = new BuffInfo(player.buffType[buffSlot], player.buffTime[buffSlot]);

                if (PermaBuffs.preBuffUpdateHooks[buff.type] != null)
                {
                    BuffStatus status = BuffStatus.NotModified;

                    if (alwaysPermanent[buff.type])
                        status = BuffStatus.IsPermaBuffed;
                    else if (neverPermanent[buff.type])
                        status = BuffStatus.IsNeverBuffed;

                    foreach (BuffHook hook in PermaBuffs.preBuffUpdateHooks[buff.type])
                        hook(player, ref buffSlot, (int)status, out int type);
                }
            }
        }

        /// <summary>
        /// Make all buffs permanent
        /// </summary>
        public override void PostUpdateBuffs()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            PermaBuffsConfig config = PermaBuffsConfig.instance;
            Player player = Main.LocalPlayer;
            bool bannersSet = false;

            for (int buffSlot = 0; buffSlot < Player.MaxBuffs; buffSlot++)
            {
                BuffInfo buff = new BuffInfo(player.buffType[buffSlot], player.buffTime[buffSlot]);

                if (!buff.isActive)
                {
                    if (!alwaysPermanent[buff.type])
                        continue;
                    else
                        player.buffTime[buffSlot] = 2;
                }

                // Populate saved banners with the banners on screen if banners are active
                if (buff.type == BuffID.MonsterBanner && (config.keepBannerBuffs || alwaysPermanent[buff.type]))
                {
                    for (int j = 0; j < NPCLoader.NPCCount; j++)
                    {
                        if (Main.SceneMetrics.NPCBannerBuff[j])
                        {
                            activeBanners[j] = true;
                        }
                    }

                    bannersNeedRefresh = true;
                    bannersSet = true;
                }

                // Call hooks
                if (PermaBuffs.postBuffUpdateHooks[buff.type] != null)
                {
                    BuffStatus status = BuffStatus.NotModified;

                    if (alwaysPermanent[buff.type])
                        status = BuffStatus.IsPermaBuffed;
                    else if (neverPermanent[buff.type])
                        status = BuffStatus.IsNeverBuffed;

                    foreach (BuffHook hook in PermaBuffs.postBuffUpdateHooks[buff.type])
                        hook(player, ref buffSlot, (int)status, out int type);
                }
            }

            // Banner buff is no longer there, set all collected types to false
            if (!bannersSet && bannersNeedRefresh)
            {
                bannersNeedRefresh = false;
                activeBanners = new bool[NPCLoader.NPCCount];
            }
        }

        /// <summary>
        /// Saves the buffs the player had before death
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="hitDirection"></param>
        /// <param name="pvp"></param>
        /// <param name="playSound"></param>
        /// <param name="genDust"></param>
        /// <param name="damageSource"></param>
        /// <returns></returns>
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            if (Main.netMode == NetmodeID.Server)
                return true;

            PermaBuffsConfig config = PermaBuffsConfig.instance;
            Player player = Main.LocalPlayer;

            if (config.clearPermaBuffsOnDeathOrLoad)
                alwaysPermanent = new bool[BuffLoader.BuffCount];
            if (config.clearNeverBuffsOnDeathOrLoad)
                neverPermanent = new bool[BuffLoader.BuffCount];

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                BuffInfo buff = new BuffInfo(player.buffType[i], player.buffTime[i]);

                // Queue the buff to be reapplied if the config settings allow for it
                if (buff.shouldPersistThroughDeath(this, config))
                    pendingBuffs.Add(buff);
            }

            return true;
        }

        /// <summary>
        /// Applies the stored buffs from after death on respawn
        /// </summary>
        public override void OnRespawn()
        {
            Player player = Main.LocalPlayer;
            minionSlots = 0f;
            minionIncrease = 0f;

            foreach (BuffInfo buff in pendingBuffs)
            {
                if (!player.HasBuff(buff.type))
                    buff.AddBuffToPlayer(this, player);
            }

            goldenTicks = 0;
            pendingBuffs.Clear();
        }

        /// <summary>
        /// Re-applies buffs that the player had before they saved and quit. This is called on local client only
        /// </summary>
        public override void OnEnterWorld()
        {
            Player player = Main.LocalPlayer;
            PermaBuffsConfig config = PermaBuffsConfig.instance;
            minionSlots = 0f;
            minionIncrease = 0f;

            if (config.clearPermaBuffsOnDeathOrLoad)
                alwaysPermanent = new bool[BuffLoader.BuffCount];
            if (config.clearNeverBuffsOnDeathOrLoad)
                neverPermanent = new bool[BuffLoader.BuffCount];

            for (int i = 0; i < pendingBuffs.Count && i < Player.MaxBuffs; i++)
            {
                BuffInfo buff = pendingBuffs[i];
                // Re-apply buffs between sessions if set to persist through death
                if (buff.shouldPersistThroughDeath(this, config))
                {
                    buff.AddBuffToPlayer(this, player);
                }
            }

            goldenTicks = 0;
            pendingBuffs.Clear();

            // Error handlers.
            if (buffCountDifferent && tryAddModBuff)
            {
                Main.NewText(Language.GetTextValue("Mods.PermaBuffs.Errors.BuffCountDifferent"));
            }

            if (npcCountDifferent && tryAddModBanner)
            {
                Main.NewText(Language.GetTextValue("Mods.PermaBuffs.Errors.NpcCountDifferent"));
            }
        }


        public override void PostUpdate()
        {
            if (Main.netMode != NetmodeID.Server)
                maxMinions = Main.LocalPlayer.maxMinions;
        }

        public static void ParseTypeList(IList<string> list, ref bool[] array, bool countDifferent, int limit, ref bool errorTracker)
        {
            int type;

            for (int i = 0; i < list.Count; i++)
            {
                if (!int.TryParse(list[i], out type))
                    continue;
                if (countDifferent && type >= limit)
                {
                    errorTracker = true;
                    continue;
                }

                array[type] = true;
            }
        }
        /// <summary>
        /// Adds the buffs previously saved to a queue that is applied when the player first spawns.
        /// </summary>
        /// <param name="tag"></param>
        public override void LoadData(TagCompound tag)
        {
            IList<string> buffs = tag.GetList<string>("Buffs");
            IList<string> permaList = tag.GetList<string>("PermaList");
            IList<string> neverList = tag.GetList<string>("NeverList");
            IList<string> bannerList = tag.GetList<string>("BannerList");
            IList<string> itemList = tag.GetList<string>("ItemList");
            PermaBuffsConfig config = PermaBuffsConfig.instance;

            try
            {
                int oldCount = int.Parse(tag.GetString("CurrentBuffTotal"));
                buffCountDifferent = BuffLoader.BuffCount != oldCount && oldCount != BuffID.Count;
            }
            catch
            {
                buffCountDifferent = true;
            }

            for (int i = 0; i < buffs.Count; i++)
            {
                BuffInfo buff = new BuffInfo(buffs[i]);

                // Don't add the buff if it is invalid
                if (!buffCountDifferent || buff.isVanilla)
                    pendingBuffs.Add(buff);
            }

            ParseTypeList(permaList, ref alwaysPermanent, buffCountDifferent, BuffID.Count, ref tryAddModBuff);
            ParseTypeList(neverList, ref neverPermanent, buffCountDifferent, BuffID.Count, ref tryAddModBuff);
            
            try
            {
                int oldCount = int.Parse(tag.GetString("CurrentNPCTotal"));
                npcCountDifferent = NPCLoader.NPCCount != oldCount && oldCount != NPCID.Count;
            }
            catch
            {
                npcCountDifferent = true;
            }

            ParseTypeList(bannerList, ref activeBanners, npcCountDifferent, NPCID.Count, ref tryAddModBanner);
            
            for (int i = 0; i < itemList.Count; i++)
            {
                string[] list = itemList[i].ToString().Split(',');
                int buffType;
                int itemType;

                if (list.Length != 2)
                    continue;

                try
                {
                    buffType = int.Parse(list[0]);
                    itemType = int.Parse(list[1]);
                }
                catch
                {
                    continue;
                }

                if (!buffCountDifferent || BuffInfo.IsVanilla(buffType))
                    buffItemIDs[buffType] = itemType;
            }
        }
        /// <summary>
        /// Saves the buffs the player had before saving and quitting
        /// </summary>
        /// <param name="tag"></param>
        public override void SaveData(TagCompound tag)
        {
            List<string> buffs = new List<string>();
            List<string> permaList = new List<string>();
            List<string> neverList = new List<string>();
            List<string> bannerList = new List<string>();
            List<string> itemList = new List<string>();

            Player player = Main.LocalPlayer;

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                BuffInfo buff = new BuffInfo(player.buffType[i], player.buffTime[i]);

                if (!buff.isActive)
                    continue;

                buffs.Add(buff.ToString());
            }

            for (int i = 0; i < BuffLoader.BuffCount; i++)
            {
                if (alwaysPermanent[i])
                {
                    permaList.Add(i.ToString());
                }
                else if (neverPermanent[i])
                {
                    neverList.Add(i.ToString());
                }

                if (buffItemIDs[i] != 0)
                {
                    itemList.Add(i.ToString() + "," + buffItemIDs[i].ToString());
                }
            }

            for (int i = 0; i < NPCLoader.NPCCount; i++)
            {
                if (activeBanners[i])
                {
                    bannerList.Add(i.ToString());
                }
            }

            tag.Add("Buffs", buffs);
            tag.Add("PermaList", permaList);
            tag.Add("NeverList", neverList);
            tag.Add("BannerList", bannerList);
            tag.Add("ItemList", itemList);
            tag.Add("CurrentBuffTotal", BuffLoader.BuffCount.ToString());
            tag.Add("CurrentNPCTotal", NPCLoader.NPCCount.ToString());
        }

        public override void PostUpdateEquips()
        {
            Player player = Player;

            if (setCrystalLeafFlag)
                player.crystalLeaf = true;

            if (setSolarCounterFlag)
                player.solarCounter = 180;

            if (stardustGuardianFlag)
                player.AddBuff(BuffID.StardustGuardianMinion, 3600);

            if (alwaysPermanent[BuffID.Werewolf])
            {
                if (player.wolfAcc)
                {
                    hideWerewolf = player.hideWolf;
                }
                else
                {
                    player.wolfAcc = true;
                    player.hideWolf = hideWerewolf;
                }
            }

            if (alwaysPermanent[BuffID.Merfolk])
            {
                if (player.accMerman)
                {
                    hideMerman = player.hideMerman;
                }
                else
                {
                    player.accMerman = true;
                    player.hideMerman = hideMerman;
                }
            }
        }
    }
}
