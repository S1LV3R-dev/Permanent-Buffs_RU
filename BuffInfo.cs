using System.Media;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Net.Mime.MediaTypeNames;

namespace PermaBuffs
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
        /// <summary>
        /// Parses a string of two numbers delimited by a space into a buff, the first number = buff.type and the second = buff.timeLeft
        /// </summary>
        /// <param name="data"></param>
        public BuffInfo(string data)
        {
            string[] values = data.Split(" ");

            type = 0;
            timeLeft = 0;

            if (values.Length == 2)
            {
                try { type = int.Parse(values[0]); } catch { }
                try { timeLeft = int.Parse(values[1]); } catch { }
            }
        }

        public readonly bool isVanilla { get { return IsVanilla(type); } }
        public static bool IsVanilla(int type) { return type < BuffID.Count; }
        /// <summary>
        /// Checks whether or not the buff is one of the 5 vanilla station buffs
        /// </summary>
        public static bool IsStationBuff(int type)
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
        /// <summary>
        /// Checks whether or not the buff is one of the 5 vanilla station buffs
        /// </summary>
        public readonly bool isStationBuff
        {
            get { return IsStationBuff(type); }
        }
        /// <summary>
        /// Returns whether or not the buff is junk data
        /// </summary>
        public readonly bool isActive { get { return type > 0 && timeLeft > 0; } }
        /// <summary>
        /// Returns if the buff spawns a pet
        /// </summary>
        public static bool IsPet(int type) { return Main.vanityPet[type] || Main.lightPet[type]; }
        /// <summary>
        /// Returns if the buff spawns a pet
        /// </summary>
        public readonly bool isPet { get { return IsPet(type); } }
        /// <summary>
        /// Returns true if the buff applies a negative effect. This does not necessarily coincide with Main.debuff -> certain entries in there are simply set to true so they can't be removed by the player.
        /// </summary>
        public readonly bool isActuallyADebuff { get { return IsActuallyADebuff(type); } }
        /// <summary>
        /// Returns true if the buff applies a negative effect. This does not necessarily coincide with Main.debuff -> certain entries in there are simply set to true so they can't be removed by the player.
        /// </summary>
        public static bool IsActuallyADebuff(int type) 
        {
            bool debuff = Main.debuff[type];

            // Override certain vanilla positive Main.debuff entries to return true instead of false
            switch (type)
            {
                // Undoubtedly buffs, these have positive effects only
                case BuffID.StarInBottle:
                case BuffID.Campfire:
                case BuffID.HeartLamp:
                case BuffID.Sunflower:
                case BuffID.MonsterBanner:
                case BuffID.CatBast:
                case BuffID.Werewolf:
                case BuffID.Merfolk:

                // Debatable section - could go either way depending on what you want to do.
                case BuffID.PeaceCandle:
                case BuffID.WaterCandle: // Debatable, marking as buff because it can help player farm but could be either way tbh
                case BuffID.ShadowCandle: // Also debatable, but player crafted so...
                // This is both a buff and nerf. It increases your crit chance by 10% but makes you unable to dodge again from the brain.
                // In the context of my mod however the player could permabuff this and then remove the brain, so ill mark it as a buff.
                case BuffID.BrainOfConfusionBuff: 

                    debuff = false; break;
            }

            return debuff;
        }
        /// <summary>
        /// Returns true if the buff has a pet, summon, mount, sentry, or projectile attached.
        /// </summary>
        /// <param name="modPlayer"></param>
        /// <returns></returns>
        public readonly bool BuffSpawnsEntity(PermaBuffsPlayer modPlayer, out int id) { id = modPlayer.buffItemIDs[type]; return id > 0; }

        /// <summary>
        /// Determines whether or not the buff should persist through death depending on the current config options.
        /// </summary>
        public bool shouldPersistThroughDeath(PermaBuffsPlayer modPlayer, PermaBuffsConfig config)
        {
            if (!isActive)
                return false;

            bool shouldPersist = false;
            shouldPersist = shouldPersist || (config.keepStationBuffs && isStationBuff);
            shouldPersist = shouldPersist || (config.keepBannerBuffs && type == BuffID.MonsterBanner);
            shouldPersist = shouldPersist || modPlayer.alwaysPermanent[type];
            shouldPersist = shouldPersist && !modPlayer.neverPermanent[type];
            shouldPersist = shouldPersist && !config.doNotApplyBuffsAfterDeathOrLoad;

            return shouldPersist;
        }
        /// <summary>
        /// Whether or not the buff should be added to the Draw Queue. This flags for a frame to be drawn within the draw buff icon hook.
        /// </summary>
        /// <param name="modPlayer"></param>
        /// <returns></returns>
        public bool shouldAddToDrawQueue(PermaBuffsPlayer modPlayer)
        {
            if (!isActive)
                return false;

            bool shouldDraw = modPlayer.alwaysPermanent[type];
            shouldDraw = shouldDraw || modPlayer.goldenQueue[type];
            shouldDraw = shouldDraw || modPlayer.neverPermanent[type];

            return shouldDraw;

        }
        /// <summary>
        /// Returns the string form of the type and time left variables delimited by a space
        /// </summary>
        /// <returns></returns>
        public override string? ToString()
        {
            return (string)this;
        }
        /// <summary>
        /// Returns the string form of the type and time left variables delimited by a space
        /// </summary>
        /// <returns></returns>
        public static explicit operator string(BuffInfo buff)
        {
            string str = "";
            str += buff.type.ToString() + " ";
            str += buff.timeLeft.ToString();

            return str;
        }
        /// <summary>
        /// Properly adds a permabuff to the player, including summoning any associated summon with the permabuff
        /// </summary>
        /// <param name="modPlayer"></param>
        /// <param name="player"></param>
        public void AddBuffToPlayer(PermaBuffsPlayer modPlayer, Player player)
        {
            // Client only code
            if (Main.netMode == NetmodeID.Server)
                return;

            player.AddBuff(type, timeLeft);
            modPlayer.goldenQueue[type] = true;

            // Summon only code ahead
            if (!BuffSpawnsEntity(modPlayer, out int itemNetID))
                return;

            float minionSlots = player.slotsMinions;
            ref float minionIncrease = ref modPlayer.minionIncrease;

            int itemIndex = player.FindItem(itemNetID);

            // The player does not have the stored item in their inventory to get spawn stats from.
            if (itemIndex == -1)
                return;

            // Get the actual item reference
            Item item = player.inventory[itemIndex];

            while (minionSlots + minionIncrease <= modPlayer.maxMinions)
            {
                #region TmodLoader Code Variable Dependencies

                Vector2 pointPosition = player.RotatedRelativePoint(player.MountedCenter);
                var projSource = player.GetSource_ItemUse_WithPotentialAmmo(item, 0);
                int playerIndex = player.whoAmI;
                int damage = player.GetWeaponDamage(item);
                int baseDamage = item.damage;
                int summonID = item.shoot;
                float knockBack = player.GetWeaponKnockback(item, item.knockBack);
                int projIndex = -1;
                float num2 = (float)Main.mouseX + Main.screenPosition.X - pointPosition.X;
                float num3 = (float)Main.mouseY + Main.screenPosition.Y - pointPosition.Y;
                if (player.gravDir == -1f)
                {
                    num3 = Main.screenPosition.Y + (float)Main.screenHeight - (float)Main.mouseY - pointPosition.Y;
                }
                Vector2 velocity = new Vector2(num2, num3);

                #endregion

                // These take care of mod summons.
                CombinedHooks.ModifyShootStats(player, item, ref pointPosition, ref velocity, ref summonID, ref damage, ref knockBack);

                if (!CombinedHooks.Shoot(player, item, (EntitySource_ItemUse_WithAmmo)projSource, pointPosition, velocity, summonID, damage, knockBack))
                    return;

                minionIncrease = 0f;
                SoundEngine.PlaySound(item.UseSound);

                #region Tmodloader Summon Spawn Code Stitched Together + My Minion Trackers

                if (item.type == ItemID.PygmyStaff)
                {
                    summonID = Main.rand.Next(191, 195);
                    projIndex = player.SpawnMinionOnCursor(projSource, playerIndex, summonID, damage, knockBack);
                    Main.projectile[projIndex].localAI[0] = 30f;

                    minionIncrease = 1;
                }
                else if (item.type == ItemID.OpticStaff)
                {
                    float x = 0f;
                    float y = 0f;
                    Vector2 spinningpoint = new Vector2(x, y);
                    spinningpoint = spinningpoint.RotatedBy(1.5707963705062866);
                    projIndex = player.SpawnMinionOnCursor(projSource, playerIndex, summonID, damage, knockBack, spinningpoint, spinningpoint);
                    spinningpoint = spinningpoint.RotatedBy(-3.1415927410125732);
                    projIndex = player.SpawnMinionOnCursor(projSource, playerIndex, summonID + 1, damage, knockBack, spinningpoint, spinningpoint);

                    minionIncrease = 1;
                }
                else if (item.type == ItemID.SpiderStaff)
                {
                    projIndex = player.SpawnMinionOnCursor(projSource, playerIndex, summonID + player.nextCycledSpiderMinionType, damage, knockBack);
                    player.nextCycledSpiderMinionType++;
                    player.nextCycledSpiderMinionType %= 3;

                    minionIncrease = 1;
                }
                else if (item.type == ItemID.StardustDragonStaff)
                {
                    int num142 = -1;
                    int num143 = -1;
                    for (int num144 = 0; num144 < 1000; num144++)
                    {
                        if (Main.projectile[num144].active && Main.projectile[num144].owner == Main.myPlayer)
                        {
                            if (num142 == -1 && Main.projectile[num144].type == 625)
                            {
                                num142 = num144;
                            }
                            if (num143 == -1 && Main.projectile[num144].type == 628)
                            {
                                num143 = num144;
                            }
                            if (num142 != -1 && num143 != -1)
                            {
                                break;
                            }
                        }
                    }
                    if (num142 == -1 && num143 == -1)
                    {
                        num2 = 0f;
                        num3 = 0f;
                        pointPosition.X = (float)Main.mouseX + Main.screenPosition.X;
                        pointPosition.Y = (float)Main.mouseY + Main.screenPosition.Y;
                        int num145 = Projectile.NewProjectile(projSource, pointPosition.X, pointPosition.Y, num2, num3, summonID, damage, knockBack, playerIndex);
                        int num146 = Projectile.NewProjectile(projSource, pointPosition.X, pointPosition.Y, num2, num3, summonID + 1, damage, knockBack, playerIndex, num145);
                        int num147 = Projectile.NewProjectile(projSource, pointPosition.X, pointPosition.Y, num2, num3, summonID + 2, damage, knockBack, playerIndex, num146);
                        int num148 = Projectile.NewProjectile(projSource, pointPosition.X, pointPosition.Y, num2, num3, summonID + 3, damage, knockBack, playerIndex, num147);
                        Main.projectile[num146].localAI[1] = num147;
                        Main.projectile[num147].localAI[1] = num148;
                        Main.projectile[num145].originalDamage = baseDamage;
                        Main.projectile[num146].originalDamage = baseDamage;
                        Main.projectile[num147].originalDamage = baseDamage;
                        Main.projectile[num148].originalDamage = baseDamage;
                    }
                    else if (num142 != -1 && num143 != -1)
                    {
                        int num149 = (int)Main.projectile[num143].ai[0];
                        int num150 = Projectile.NewProjectile(projSource, pointPosition.X, pointPosition.Y, num2, num3, summonID + 1, damage, knockBack, playerIndex, num149);
                        int num151 = Projectile.NewProjectile(projSource, pointPosition.X, pointPosition.Y, num2, num3, summonID + 2, damage, knockBack, playerIndex, num150);
                        Main.projectile[num150].localAI[1] = num151;
                        Main.projectile[num150].netUpdate = true;
                        Main.projectile[num150].ai[1] = 1f;
                        Main.projectile[num151].localAI[1] = num143;
                        Main.projectile[num151].netUpdate = true;
                        Main.projectile[num151].ai[1] = 1f;
                        Main.projectile[num143].ai[0] = num151;
                        Main.projectile[num143].netUpdate = true;
                        Main.projectile[num143].ai[1] = 1f;
                        Main.projectile[num150].originalDamage = baseDamage;
                        Main.projectile[num151].originalDamage = baseDamage;
                        Main.projectile[num143].originalDamage = baseDamage;
                    }

                    minionIncrease = 1;
                }
                else
                {
                    projIndex = player.SpawnMinionOnCursor(projSource, playerIndex, summonID, damage, knockBack);
                    minionIncrease = Main.projectile[projIndex].minionSlots;
                }

                #endregion

                minionSlots += minionIncrease;
            }
        }
    }
}