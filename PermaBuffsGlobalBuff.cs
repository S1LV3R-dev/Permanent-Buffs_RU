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
            bool writeAutoDeleteTooltip = false;

            // Modify buff name if the icon is modified by the mod

            if (modPlayer.alwaysPermanent[type])
            {
                buffName = Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.PermaBuffPreBuffDisplayName") + buffName;
                rare = ItemRarityID.Yellow;
            }
            else if (modPlayer.neverPermanent[type])
            {
                buffName = Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.NeverBuffPreBuffDisplayName") + buffName;
                rare = ItemRarityID.Purple;

                writeAutoDeleteTooltip = true;
            }
            else if (type == BuffID.MonsterBanner)
            {
                if (config.keepBannerBuffs)
                {
                    buffName = Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.PermaBuffPreBuffDisplayName") + buffName;
                }
            }
            else if (BuffInfo.IsStationBuff(type) && config.keepStationBuffs)
            {
                buffName = Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.PermaBuffPreBuffDisplayName") + buffName;
            }
            else if (!(modPlayer.goldenQueue[type] || BuffInfo.IsPet(type)))
            {
                modifiedIcon = false;
            }

            // Dont modify tooltip for buffs that are already modified or banners.
            if (modifiedIcon && !writeAutoDeleteTooltip)
                return;

            // Modify tooltips

            List<string> permaBuffKey = PermaBuffsPlayer.alwaysPermanentKey.GetAssignedKeys();
            List<string> neverBuffKey = PermaBuffsPlayer.neverPermanentKey.GetAssignedKeys();
            List<string> autoDeleteKey = PermaBuffsPlayer.autoDeleteKey.GetAssignedKeys();

            modPlayer.permaBound = permaBuffKey.Count > 0;
            modPlayer.neverBound = neverBuffKey.Count > 0;
            modPlayer.autoBound = autoDeleteKey.Count > 0;

            if (writeAutoDeleteTooltip)
            {
                if (!modPlayer.autoBound)
                {
                    string autoKeybindAsString = Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.AutoDeleteBuff.DisplayName") + Language.GetTextValue("Mods.PermaBuffs.NotBound");
                    tip = Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.AutoDeleteBuff.Tooltip", autoKeybindAsString);
                    modPlayer.autoTooltipSeen = false;
                }
                else if (!(modPlayer.autoTooltipSeen || config.autoHideKeybindTooltips))
                {
                    tip = Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.AutoDeleteBuff.Tooltip", autoDeleteKey[0]);
                }
            }
            else if (!BuffInfo.IsActuallyADebuff(type)) // Show permabuff tooltips for Buffs
            {
                if (!modPlayer.permaBound)
                {
                    string permaKeybindAsString = Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.ToggleBuffAlwaysPermanent.DisplayName") + Language.GetTextValue("Mods.PermaBuffs.NotBound");
                    tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.GlobablBuff.ToggleBuffAlwaysPermanent.Tooltip", permaKeybindAsString);
                    modPlayer.permaTooltipSeen = false;
                }
                else if (!(modPlayer.permaTooltipSeen || config.autoHideKeybindTooltips))
                {
                    tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.ToggleBuffAlwaysPermanent.Tooltip", permaBuffKey[0]);
                }
            }
            else  // Show neverbuff tooltips for Debuffs
            {
                if (!modPlayer.neverBound)
                {
                    string neverKeybindAsString = Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.ToggleBuffNeverPermanent.DisplayName") + Language.GetTextValue("Mods.PermaBuffs.NotBound");
                    tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.ToggleBuffNeverPermanent.Tooltip", neverKeybindAsString);
                    modPlayer.neverTooltipSeen = false;
                }
                else if (!(modPlayer.neverTooltipSeen || config.autoHideKeybindTooltips))
                {
                    tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.GlobalBuff.ToggleBuffNeverPermanent.Tooltip", neverBuffKey[0]);
                }
            }
        }
    }
}

