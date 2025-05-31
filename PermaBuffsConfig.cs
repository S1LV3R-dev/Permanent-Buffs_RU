using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PermaBuffs
{
    public class PermaBuffsConfig : ModConfig
    {
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) => true;
        public override ConfigScope Mode => ConfigScope.ClientSide;
        public static PermaBuffsConfig instance { get { return ModContent.GetInstance<PermaBuffsConfig>(); } } 

        /// <summary>
        /// Set to true if all buffs have a permanent duration
        /// </summary>
        [DefaultValue(true)]
        public bool permanentBuffs;

        /// <summary>
        /// 60 ticks is equivalent to one second. Setting this value to 1 means most if not all buffs will persist.
        /// Setting this value to a high number can be useful to automatically filter out certain area buffs from being permanent, as their default duration is innately small.
        /// Ex - Happy, Campfire, Heart Lantern, etc. This setting does nothing if "Permanent Buffs" is not enabled.
        /// </summary>
        [DefaultValue(100)]
        [Range(1, 10000)]
        public int minimumDuration;

        /// <summary>
        /// Set to true if station buffs should persist through death
        /// </summary>
        [DefaultValue(true)]
        public bool keepStationBuffs;

        /// <summary>
        /// Set to true if all buffs should persist through death.
        /// </summary>
        [DefaultValue(false)]
        public bool keepAllBuffs;

        /// <summary>
        /// Set to true if debuffs should be affected by the mod as well.
        /// </summary>
        [DefaultValue(false)]
        public bool includeDebuffs;

        /// <summary>
        /// When set to true, buff icons affected by the mod will gain a golden frame.
        /// </summary>
        [DefaultValue(true)]
        public bool drawGoldenBorders;
    }
}