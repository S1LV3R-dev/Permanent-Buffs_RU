using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using tModPorter;

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
                    PermaBuffsConfig config = PermaBuffsConfig.instance;

                    bool shouldPersist = isActive;
                    shouldPersist = shouldPersist && (config.includeDebuffs || !isDebuff);
                    shouldPersist = shouldPersist && (config.keepAllBuffs || (config.keepStationBuffs && isStationBuff));

                    return shouldPersist;
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
        }

        public List<BuffInfo> pendingBuffs = new List<BuffInfo>();

        /// <summary>
        /// Make all buffs permanent
        /// </summary>
        public override void PostUpdateBuffs()
        {
            PermaBuffsConfig config = PermaBuffsConfig.instance;
            Player player = Main.LocalPlayer;

            if (!config.permanentBuffs)
                return;

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                BuffInfo buff = new BuffInfo(player.buffType[i], player.buffTime[i]);

                // Don't increase debuff time if debuffs aren't included
                if (!buff.isActive && !config.includeDebuffs && buff.isDebuff)
                    continue;

                // Increase the time remaining on the buff.
                if (!buff.isStationBuff && player.active && !player.dead)
                    Player.buffTime[i] += 1;
            }
        }

        /// <summary>
        /// Re-applies buffs that the player had before they saved and quit
        /// </summary>
        public override void OnEnterWorld()
        {
            PermaBuffsConfig config = PermaBuffsConfig.instance;
            Player player = Main.LocalPlayer;

            foreach (BuffInfo buff in pendingBuffs)
            {
                // Re-apply buffs between sessions if set to persist through death
                if (buff.shouldPersistThroughDeath)
                    player.AddBuff(buff.type, buff.timeLeft, false);
            }

            pendingBuffs.Clear();
        }
        /// <summary>
        /// Adds the buffs previously saved to a queue that is applied when the player first spawns.
        /// </summary>
        /// <param name="tag"></param>
        public override void LoadData(TagCompound tag)
        {
            IList<string> buffs = tag.GetList<string>("Buffs");

            for (int i = 0; i < buffs.Count; i++)
            {
                BuffInfo buff = new BuffInfo(buffs[i]);
                pendingBuffs.Add(buff);
            }
        }
        /// <summary>
        /// Saves the buffs the player had before saving and quitting
        /// </summary>
        /// <param name="tag"></param>
        public override void SaveData(TagCompound tag)
        {
            List<string> buffs = new List<string>();
            Player player = Main.LocalPlayer;

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                BuffInfo buff = new BuffInfo(player.buffType[i], player.buffTime[i]);

                if (!buff.isActive)
                    continue;

                buffs.Add((string)buff);
            }

            tag.Add("Buffs", buffs);
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
                player.AddBuff(buff.type, buff.timeLeft, false);
            }

            pendingBuffs.Clear();
        }
    }
}
