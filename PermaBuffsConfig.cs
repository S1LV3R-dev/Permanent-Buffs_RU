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
        /// Set to true if station buffs should persist through death
        /// </summary>
        [DefaultValue(true)]
        public bool keepStationBuffs;

        /// <summary>
        /// Set to true if banners should be applied permanently
        /// </summary>
        [DefaultValue(true)]
        public bool keepBannerBuffs;

        [DefaultValue(false)]
        public bool doNotApplyBuffsAfterDeathOrLoad;

        /// <summary>
        /// When set to true, buff icons affected by the mod will gain a frame.
        /// </summary>
        [DefaultValue(true)]
        public bool drawBorders;

        /// <summary>
        /// When set to true, keybinding tooltips are hidden after they're shown for the first time
        /// </summary>
        [DefaultValue(false)]
        public bool autoHideKeybindTooltips;
    }
}