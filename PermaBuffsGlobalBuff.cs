using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace PermaBuffs
{
    class PermaBuffsGlobalBuff : GlobalBuff
    {
        // Insert a tooltip on how to use the mod if the modkeys are not bound
        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            PermaBuffsPlayer modPlayer = Main.LocalPlayer.GetModPlayer<PermaBuffsPlayer>();
            PermaBuffsConfig config = PermaBuffsConfig.instance;
            bool modifiedIcon = true;

            // Modify buff name if the icon is modified by the mod

            if (modPlayer.alwaysPermanent[type])
            {
                buffName = "PermaBuff: " + buffName;
                rare = ItemRarityID.Yellow;
            }
            else if (modPlayer.neverPermanent[type])
            {
                buffName = "NeverBuff: " + buffName;
                rare = ItemRarityID.Purple;
            }
            else if (type == BuffID.MonsterBanner)
            {
                if (config.keepBannerBuffs)
                {
                    buffName = "PermaBuff: " + buffName;
                    rare = ItemRarityID.Yellow;
                }
            }
            else if (BuffInfo.IsStationBuff(type) && config.keepStationBuffs)
            {
                buffName = "PermaBuff: " + buffName;
                rare = ItemRarityID.Yellow;
            }
            else if (!modPlayer.goldenQueue[type])
            {
                modifiedIcon = false;
            }

            // Dont modify tooltip for buffs that are already modified or banners.
            if (modifiedIcon)
                return;

            // Modify tooltips

            List<string> permaBuffKey = PermaBuffsPlayer.alwaysPermanentKey.GetAssignedKeys();
            List<string> neverBuffKey = PermaBuffsPlayer.neverPermanentKey.GetAssignedKeys();

            modPlayer.permaBound = permaBuffKey.Count > 0;
            modPlayer.neverBound = neverBuffKey.Count > 0;

            if (!BuffInfo.IsActuallyADebuff(type)) // Show permabuff tooltips for Buffs
            {
                if (!modPlayer.permaBound)
                {
                    string permaKeybindAsString = Language.GetTextValue("Mods.PermaBuffs.ToggleBuffAlwaysPermanent.DisplayName") + Language.GetTextValue("Mods.PermaBuffs.NotBound");
                    tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.ToggleBuffAlwaysPermanent.Tooltip", permaKeybindAsString);
                    modPlayer.permaTooltipSeen = false;
                }
                else if (!(modPlayer.permaTooltipSeen || config.autoHideKeybindTooltips))
                {
                    tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.ToggleBuffAlwaysPermanent.Tooltip", permaBuffKey[0]);
                }
            }
            else // Show neverbuff tooltips for Debuffs
            {
                if (!modPlayer.neverBound)
                {
                    string neverKeybindAsString = Language.GetTextValue("Mods.PermaBuffs.ToggleBuffNeverPermanent.DisplayName") + Language.GetTextValue("Mods.PermaBuffs.NotBound");
                    tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.ToggleBuffNeverPermanent.Tooltip", neverKeybindAsString);
                    modPlayer.neverTooltipSeen = false;
                }
                else if (!(modPlayer.neverTooltipSeen || config.autoHideKeybindTooltips))
                {
                    tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.ToggleBuffNeverPermanent.Tooltip", neverBuffKey[0]);
                }
            }
        }
    }
}

