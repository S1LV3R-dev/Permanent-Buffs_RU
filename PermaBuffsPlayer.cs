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
        /// Stores a buff's type and time left
        /// </summary>
        public struct BuffInfo
        {
            /// <summary>
            /// The buff type of the buff. This can be obtained by either finding its vanilla BuffID or by getting it through Tmodloader.
            /// </summary>
            public int type;
            /// <summary>
            /// The amount of time remaining in ticks before the buff expires.
            /// </summary>
            public int timeLeft;

            public BuffInfo(int type = 0, int timeLeft = 0)
            {
                this.type = type;
                this.timeLeft = timeLeft;
            }
            public BuffInfo(string data)
            {
                string[] values = data.Split(" ");

                try
                {
                    type = int.Parse(values[0]);
                    timeLeft = int.Parse(values[1]);
                }
                catch 
                {
                    type = 0;
                    timeLeft = 0;
                }
            }

            public bool isVanilla { get { return type < BuffID.Count; } }
            /// <summary>
            /// Checks whether or not the buff is one of the 5 vanilla station buffs
            /// </summary>
            public bool isStationBuff
            {
                get
                {
                    switch (type)
                    {
                        case BuffID.Bewitched:
                        case BuffID.Sharpened:
                        case BuffID.AmmoBox:
                        case BuffID.Clairvoyance:
                        case BuffID.WarTable:
                            return true;
                        default:
                            return false;
                    }
                }
            }
            /// <summary>
            /// Returns whether or not the buff is junk data
            /// </summary>
            public bool isActive
            {
                get
                {
                    return type > 0 && timeLeft > 0;
                }
            }
            public bool isVanillaMount
            {
                get
                {
                    bool mount = false;

                    switch (type)
                    {
                        case BuffID.BunnyMount:
                        case BuffID.PigronMount:
                        case BuffID.SlimeMount:
                        case BuffID.TurtleMount:
                        case BuffID.BeeMount:
                        case BuffID.UFOMount:
                        case BuffID.DrillMount:
                        case BuffID.ScutlixMount:
                        case BuffID.UnicornMount:
                        case BuffID.CuteFishronMount:
                        case BuffID.BasiliskMount:
                        case BuffID.GolfCartMount:
                        case BuffID.PaintedHorseMount:
                        case BuffID.MajesticHorseMount:
                        case BuffID.DarkHorseMount:
                        case BuffID.PogoStickMount:
                        case BuffID.PirateShipMount:
                        case BuffID.SpookyWoodMount:
                        case BuffID.SantankMount:
                        case BuffID.WallOfFleshGoatMount:
                        case BuffID.DarkMageBookMount:
                        case BuffID.LavaSharkMount:
                        case BuffID.QueenSlimeMount:
                        case BuffID.WolfMount:
                        case BuffID.WitchBroom:
                        case BuffID.Flamingo:
                        case BuffID.Rudolph:
                            mount = true;
                            break;
                    }

                    return mount;
                }
            }
            /// <summary>
            /// Returns if the buff spawns a pet
            /// </summary>
            public bool isPet
            {
                get
                {
                    return Main.vanityPet[type] || Main.lightPet[type];
                }
            }

            public bool isVanillaSummon
            {
                get
                {
                    bool summon = false;

                    switch (type)
                    {
                        case BuffID.AbigailMinion:
                        case BuffID.BabySlime:
                        case BuffID.BabyBird:
                        case BuffID.HornetMinion:
                        case BuffID.ImpMinion:
                        case BuffID.SpiderMinion:
                        case BuffID.TwinEyesMinion:
                        case BuffID.PirateMinion:
                        case BuffID.Pygmies:
                        case BuffID.UFOMinion:
                        case BuffID.Ravens:
                        case BuffID.SharknadoMinion:
                        case BuffID.EmpressBlade:
                        case BuffID.DeadlySphere:
                        case BuffID.StardustDragonMinion:
                        case BuffID.StardustGuardianMinion:
                        case BuffID.StardustMinion:
                        case BuffID.Smolstar:
                        case BuffID.FlinxMinion:
                        case BuffID.BatOfLight:
                        case BuffID.VampireFrog:
                            summon = true;
                            break;
                    }

                    return summon;
                }
            }

            /*
            public bool canAddBuffToPlayer
            {
                get
                {
                    return isActive;
                    // return isActive && !isPet && !isMount && !isSummon;
                }
            }
            */
            /// <summary>
            /// Returns if the buff is a debuff
            /// </summary>
            public bool isDebuff { 
                get 
                {
                     return Main.debuff[type];
                }
            }
            /// <summary>
            /// Determines whether or not the buff should persist through death depending on the current config options.
            /// </summary>
            public bool shouldPersistThroughDeath
            {
                get
                {
                    if (!isActive)
                    {
                        return false;
                    }

                    PermaBuffsConfig config = PermaBuffsConfig.instance;
                    PermaBuffsPlayer modPlayer = Main.player[Main.myPlayer].GetModPlayer<PermaBuffsPlayer>();

                    bool shouldPersist = false;
                    shouldPersist = shouldPersist || (config.keepStationBuffs && isStationBuff);
                    shouldPersist = shouldPersist || (config.keepBannerBuffs && type == BuffID.MonsterBanner);
                    shouldPersist = shouldPersist || modPlayer.alwaysPermanent[type];
                    shouldPersist = shouldPersist && !modPlayer.neverPermanent[type];
                    shouldPersist = shouldPersist && !config.doNotApplyBuffsAfterDeathOrLoad;

                    return shouldPersist;
                }
            }
            public bool shouldAddToDrawQueue
            {
                get
                {
                    if (!isActive)
                    {
                        return false;
                    }

                    PermaBuffsPlayer modPlayer = Main.player[Main.myPlayer].GetModPlayer<PermaBuffsPlayer>();

                    bool shouldDraw = modPlayer.alwaysPermanent[type];
                    shouldDraw = shouldDraw || modPlayer.goldenQueue[type];
                    shouldDraw = shouldDraw || modPlayer.neverPermanent[type];

                    return shouldDraw;
                }
            }
            /// <summary>
            /// Returns the string form of the type and time left variables delimited by a space.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return (string)this;
            }
            /// <summary>
            /// Returns the string form of the type and time left variables delimited by a space.
            /// </summary>
            /// <returns></returns>
            public static explicit operator string(BuffInfo buff)
            {
                string str = "";
                str += buff.type.ToString() + " ";
                str += buff.timeLeft.ToString();

                return str;
            }

            public void AddBuffToPlayer(PermaBuffsPlayer modPlayer, Player player)
            {
                if (modPlayer.buffItemIDs[type] == 0) // Buff does not spawn a summon, projectile, or mount of some kind
                {
                    player.AddBuff(type, timeLeft);
                    modPlayer.goldenQueue[type] = true;
                }
            }
        }

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

        public bool permaBound = false;
        public bool neverBound = false;
        public bool buffCountDifferent = false;
        public bool npcCountDifferent = false;
        public bool bannersNeedRefresh = false;
        public bool initialized = false;

        public static ModKeybind alwaysPermanentKey;
        public static ModKeybind neverPermanentKey;

        public override void Initialize()
        {
            drawQueue = new bool[BuffLoader.BuffCount];
            goldenQueue = new bool[BuffLoader.BuffCount];
            alwaysPermanent = new bool[BuffLoader.BuffCount];
            neverPermanent = new bool[BuffLoader.BuffCount];
            buffItemIDs = new int[BuffLoader.BuffCount];
            activeBanners = new bool[NPCLoader.NPCCount];
            initialized = true;
        }

        public void CheckToggleKeys(BuffInfo buff, int buffSlot)
        {
            if (!(alwaysPermanentKeyPressed || neverPermanentKeyPressed))
            {
                return;
            }

            if (alwaysPermanentKeyPressed) 
            {
                alwaysPermanent[buff.type] = !alwaysPermanent[buff.type];
                neverPermanent[buff.type] = false;

                if (!alwaysPermanent[buff.type])
                {
                    Player player = Main.LocalPlayer;

                    player.buffTime[buffSlot] = Math.Max(buff.timeLeft, TimeForGolden);
                }

                SoundEngine.PlaySound(SoundID.MenuTick);
            }
            else if (neverPermanentKeyPressed)
            {
                neverPermanent[buff.type] = !neverPermanent[buff.type];
                alwaysPermanent[buff.type] = false;

                SoundEngine.PlaySound(SoundID.MenuTick);
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

            drawQueue = new bool[BuffLoader.BuffCount];

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                BuffInfo buff = new BuffInfo(player.buffType[i], player.buffTime[i]);

                if (!buff.isActive)
                    continue;

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

                drawQueue[buff.type] = buff.shouldAddToDrawQueue;
            }

            // Banner buff is no longer there, set all collected types to false
            if (!bannersSet && bannersNeedRefresh)
            {
                bannersNeedRefresh = false;
                activeBanners = new bool[NPCLoader.NPCCount];
            }
        }

        /// <summary>
        /// Re-applies buffs that the player had before they saved and quit. This is called on local client only
        /// </summary>
        public override void OnEnterWorld()
        {
            Player player = Main.LocalPlayer;
            bool hasBanner = false;

            for (int i = 0; i < pendingBuffs.Count && i < Player.MaxBuffs; i++)
            {
                BuffInfo buff = pendingBuffs[i];
                // Re-apply buffs between sessions if set to persist through death
                if (buff.shouldPersistThroughDeath)
                {
                    buff.AddBuffToPlayer(this, player);

                    if (buff.type == BuffID.MonsterBanner)
                    {
                        hasBanner = true;
                    }
                }
            }

            goldenTicks = 0;
            pendingBuffs.Clear();

            // Error handlers.

            if (buffCountDifferent)
            {
                Main.NewText("The number of ModBuffs currently loaded are different from when they were previously saved. This means the non-vanilla buffID's previously saved are no longer valid.\n" +
                    "Therefore only Vanilla buffs are loaded. This issue is caused by adding or removing mods that contain a ModBuff between saves.", Color.Red);
            }

            if (npcCountDifferent && hasBanner)
            {
                Main.NewText("The number of ModNPCs currently loaded are different from when they were previously saved. This means the non-vanilla npcID's previously saved are no longer valid.\n" +
                   "Therefore only Vanilla NPCs are loaded. This issue is caused by adding or removing mods that contain a ModNPC between saves.", Color.Red);
            }
        }

        
        public static void ParseTypeList(IList<string> list, ref bool[] array, bool countDifferent, int limit)
        {
            int type;

            for (int i = 0; i < list.Count; i++)
            {
                if (!int.TryParse(list[i], out type))
                    continue;
                if (countDifferent && type >= limit)
                    continue;

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

            if (PermaBuffsConfig.instance.doNotApplyBuffsAfterDeathOrLoad)
                return;

            try
            {
                int oldCount = int.Parse(tag.GetString("CurrentBuffTotal"));
                buffCountDifferent = BuffLoader.BuffCount != oldCount && oldCount != BuffID.Count;
            }
            catch
            {
                buffCountDifferent = true;
            }

            try
            {
                int oldCount = int.Parse(tag.GetString("CurrentNPCTotal"));
                npcCountDifferent = NPCLoader.NPCCount != oldCount && oldCount != NPCID.Count;
            }
            catch
            {
                npcCountDifferent = true; 
            }

            for (int i = 0; i < buffs.Count; i++)
            {
                BuffInfo buff = new BuffInfo(buffs[i]);

                // Don't add the buff if it is invalid
                if (!buffCountDifferent || buff.type < BuffID.Count)
                    pendingBuffs.Add(buff);
            }

            ParseTypeList(permaList, ref alwaysPermanent, buffCountDifferent, BuffID.Count);
            ParseTypeList(neverList, ref neverPermanent, buffCountDifferent, BuffID.Count);
            ParseTypeList(bannerList, ref activeBanners, npcCountDifferent, NPCID.Count);

            for (int i = 0; i < itemList.Count; i++)
            {
                string[] list = itemList[i].ToString().Split(',');
                int buffType;
                int itemType;

                try
                {
                    buffType = int.Parse(list[0]);
                    itemType = int.Parse(list[1]);
                }
                catch
                {
                    continue;
                }

                if (buffCountDifferent && buffType >= BuffID.Count)
                    continue;

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

                buffs.Add((string)buff);
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

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                BuffInfo buff = new BuffInfo(player.buffType[i], player.buffTime[i]);

                // Queue the buff to be reapplied if the config settings allow for it
                if (buff.shouldPersistThroughDeath)
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

            foreach (BuffInfo buff in pendingBuffs)
            {
                if (player.HasBuff(buff.type))
                {
                    continue;
                }

                buff.AddBuffToPlayer(this, player);
            }

            goldenTicks = 0;
            pendingBuffs.Clear();
        }
    }
}
