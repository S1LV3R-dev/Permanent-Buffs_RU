using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

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
        public readonly bool BuffSpawnsEntity(PermaBuffsPlayer modPlayer) { return modPlayer.buffItemIDs[type] > 0; }

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

        public void AddBuffToPlayer(PermaBuffsPlayer modPlayer, Player player)
        {
            if (!BuffSpawnsEntity(modPlayer)) 
            {
                player.AddBuff(type, timeLeft);
                modPlayer.goldenQueue[type] = true;
            }
        }
    }
}