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
        public override bool RightClick(int type, int buffIndex)
        {
            return base.RightClick(type, buffIndex);
        }
        // Insert a tooltip on how to use the mod if the modkeys are not bound
        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            PermaBuffsPlayer modPlayer = Main.LocalPlayer.GetModPlayer<PermaBuffsPlayer>();

            List<string> permaBuffKey = PermaBuffsPlayer.alwaysPermanentKey.GetAssignedKeys();
            List<string> neverBuffKey = PermaBuffsPlayer.neverPermanentKey.GetAssignedKeys();

            modPlayer.permaBound = permaBuffKey.Count > 0;
            modPlayer.neverBound = neverBuffKey.Count > 0;

            if (!modPlayer.permaBound)
            {
                string permaKeybindAsString = Language.GetTextValue("Mods.PermaBuffs.ToggleBuffAlwaysPermanent.DisplayName") + Language.GetTextValue("Mods.PermaBuffs.NotBound");
                tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.ToggleBuffAlwaysPermanent.Tooltip", permaKeybindAsString);
                modPlayer.permaTooltipSeen = false;
            }
            else if (!(modPlayer.permaTooltipSeen || modPlayer.alwaysPermanent[type] || modPlayer.goldenQueue[type] || modPlayer.neverPermanent[type]))
            {
                tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.ToggleBuffAlwaysPermanent.Tooltip", permaBuffKey[0]);
            }

            if (!modPlayer.neverBound)
            {
                string neverKeybindAsString = Language.GetTextValue("Mods.PermaBuffs.ToggleBuffNeverPermanent.DisplayName") + Language.GetTextValue("Mods.PermaBuffs.NotBound");
                tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.ToggleBuffNeverPermanent.Tooltip", neverKeybindAsString);
                modPlayer.permaTooltipSeen = false;
            }
            else if (!(modPlayer.neverTooltipSeen || modPlayer.alwaysPermanent[type] || modPlayer.goldenQueue[type] || modPlayer.neverPermanent[type]))
            {
                tip += "\n" + Language.GetTextValue("Mods.PermaBuffs.ToggleBuffNeverPermanent.Tooltip", neverBuffKey[0]);
            }

            /*

            if (type == BuffID.MonsterBanner && modPlayer.alwaysPermanent[type])
            {
                tip = "Increased damage and defense from the following:";

                for (int i = 0; i < NPCLoader.NPCCount; i++)
                {
                    int npcID = Item.BannerToNPC(i);

                    if (modPlayer.activeBanners[i] && npcID != 0)
                    {
                        string npcName = Lang.GetNPCNameValue(npcID);
                        tip += "\n" + npcName;
                    }
                }
            }

            */
        }
    }
}

