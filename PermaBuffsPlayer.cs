using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using ReLogic.Content.Sources;
using ReLogic.Graphics;
using ReLogic.Localization.IME;
using ReLogic.OS;
using ReLogic.Peripherals.RGB;
using ReLogic.Utilities;
using SDL2;
using Terraria.Achievements;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.Cinematics;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Achievements;
using Terraria.GameContent.Ambience;
using Terraria.GameContent.Animations;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Events;
using Terraria.GameContent.Golf;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Liquid;
using Terraria.GameContent.NetModules;
using Terraria.GameContent.Skies;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.GameContent.UI.Chat;
using Terraria.GameContent.UI.Minimap;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.GameContent.UI.States;
using Terraria.GameInput;
using Terraria.Graphics;
using Terraria.Graphics.CameraModifiers;
using Terraria.Graphics.Capture;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Light;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Initializers;
using Terraria.IO;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.Engine;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.UI;
using Terraria.Net;
using Terraria.ObjectData;
using Terraria.Social;
using Terraria.Social.Steam;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;
using Terraria.Utilities;
using Terraria.WorldBuilding;
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

        /// <summary>
        /// The list of buffs to be added to the player as soon as possible
        /// </summary>
        public List<BuffInfo> pendingBuffs = new List<BuffInfo>();
        /// <summary>
        /// The queue of buffs modified by the mod - so they get a golden border
        /// </summary>
        public List<BuffInfo> drawQueue = new List<BuffInfo>();
        /// <summary>
        /// The queue of buff icons recently modified by a persist after death function temporarily get a golden border
        /// </summary>
        public List<BuffInfo> goldenQueue = new List<BuffInfo>();
        /// <summary>
        /// The texture of the golden border to be drawn on top of the standard buff icon
        /// </summary>
        public static Asset<Texture2D> goldenBorder = null;
        /// <summary>
        /// Sets the time a buff icon keeps the golden border
        /// </summary>
        public const int TimeForGolden = 3 * 60;
        /// <summary>
        /// The timer in ticks, caps out at TimeForGolden
        /// </summary>
        public int goldenCount = TimeForGolden;

        // This is a modified version of the original DrawBuffIcons function.
        // It inserts a golden frame around the buff icon to visually show it's modified to be permanent.
        // It was necessary to insert the golden frame within specific points of this function, simply calling orig does not work.
        internal static int DrawBuffIcons(Terraria.On_Main.orig_DrawBuffIcon orig, int drawBuffText, int buffSlotOnPlayer, int x, int y)
        {
            #region TmodloaderSourceCode1
            // Original Tmodloader code
            int buffType = Main.player[Main.myPlayer].buffType[buffSlotOnPlayer];
            if (buffType == 0)
            {
                return drawBuffText;
            }
            Color color = new Color(Main.buffAlpha[buffSlotOnPlayer], Main.buffAlpha[buffSlotOnPlayer], Main.buffAlpha[buffSlotOnPlayer], Main.buffAlpha[buffSlotOnPlayer]);
            Asset<Texture2D> obj = TextureAssets.Buff[buffType];
            Texture2D texture = obj.Value;
            Vector2 drawPosition = new Vector2(x, y);
            int width = obj.Width();
            int height = obj.Height();
            Vector2 textPosition = new Vector2(x, y + height);
            Rectangle sourceRectangle = new Rectangle(0, 0, width, height);
            Rectangle mouseRectangle = new Rectangle(x, y, width, height);
            Color drawColor = color;
            BuffDrawParams drawParams = new BuffDrawParams(texture, drawPosition, textPosition, sourceRectangle, mouseRectangle, drawColor);
            bool preventDraw = !BuffLoader.PreDraw(Main.spriteBatch, buffType, buffSlotOnPlayer, ref drawParams);
            ref BuffDrawParams buffDrawParams = ref drawParams;
            (texture, drawPosition, textPosition, sourceRectangle, mouseRectangle, drawColor) = (BuffDrawParams)(buffDrawParams);

            #endregion

            // Make sure the texture is properly loaded.
            if (goldenBorder == null)
            {
                goldenBorder = ModContent.Request<Texture2D>("Permabuffs/buffFrame", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }

            // Set the necessary parameters
            Texture2D texture2 = goldenBorder.Value;
            Vector2 drawPosition2 = new Vector2(x, y);
            int width2 = goldenBorder.Width();
            int height2 = goldenBorder.Height();
            Vector2 textPosition2 = new Vector2(x, y + height2);
            Rectangle sourceRectangle2 = new Rectangle(0, 0, width2, height2);
            Rectangle mouseRectangle2 = new Rectangle(x, y, width2, height2);
            Color drawColor2 = color;

            // Combine all the parameters into one object
            BuffDrawParams drawParams2 = new BuffDrawParams(texture2, drawPosition2, textPosition2, sourceRectangle2, mouseRectangle2, drawColor2);

            // I'm not entirely sure what this part does, but it's crucial to get the texture drawn...
            // I guess it's converting all the variables into reference parameters of drawParams2?
            ref BuffDrawParams buffDrawParams2 = ref drawParams2;
            (texture2, drawPosition2, textPosition2, sourceRectangle2, mouseRectangle2, drawColor2) = (BuffDrawParams)(buffDrawParams2);

            int shouldDrawBorder = 0;
            int removePos;

            PermaBuffsPlayer modPlayer = Main.player[Main.myPlayer].GetModPlayer<PermaBuffsPlayer>();
            PermaBuffsConfig config = PermaBuffsConfig.instance;

            // Looks at the draw queue and determines whether or not to draw the border
            for (removePos = 0; removePos < modPlayer.drawQueue.Count; removePos++)
            {
                if (modPlayer.drawQueue[removePos].type == buffType)
                {
                    shouldDrawBorder = 1;
                    break;
                }
            }
            // Fallback to golden queue if it isn't in the main drawqueue
            if (shouldDrawBorder == 0)
            {
                // For golden queue buffs
                for (int i = 0; i < modPlayer.goldenQueue.Count; i++)
                {
                    if (modPlayer.goldenQueue[i].type == buffType)
                    {
                        shouldDrawBorder = 2;
                        break;
                    }
                }
            }
            
            // If the preDraw for this buff was not overridden to cancel drawing the buff icon
            if (!preventDraw)
            {
                // Draw the buff icon
                Main.spriteBatch.Draw(texture, drawPosition, sourceRectangle, drawColor, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);

                // If this bufftype is in the draw queue
                if (config.drawGoldenBorders && shouldDrawBorder > 0)
                {
                    // Draw the golden border on top of the buff icon
                    Main.spriteBatch.Draw(texture2, drawPosition, sourceRectangle2, drawColor, 0f, default(Vector2), 1f, SpriteEffects.None, 0.1f);

                    // If this border was drawn as a result of the draw queue
                    if (shouldDrawBorder == 1)
                    {
                        // Remove this buff from the drawQueue
                        modPlayer.drawQueue.RemoveAt(removePos);
                    }
                }
            }
            #region TmodloaderSourceCode2
            // All code after this is Tmodloader code to finish the function
            BuffLoader.PostDraw(Main.spriteBatch, buffType, buffSlotOnPlayer, drawParams);
            if (Main.TryGetBuffTime(buffSlotOnPlayer, out var buffTimeValue) && buffTimeValue > 2)
            {
                string text = Lang.LocalizedDuration(new TimeSpan(0, 0, buffTimeValue / 60), abbreviated: true, showAllAvailableUnits: false);
                Main.spriteBatch.DrawString(FontAssets.ItemStack.Value, text, textPosition, color, 0f, default(Vector2), 0.8f, SpriteEffects.None, 0f);
            }
            if (mouseRectangle.Contains(new Point(Main.mouseX, Main.mouseY)))
            {
                drawBuffText = buffSlotOnPlayer;
                Main.buffAlpha[buffSlotOnPlayer] += 0.1f;
                bool flag = Main.mouseRight && Main.mouseRightRelease;
                if (PlayerInput.UsingGamepad)
                {
                    flag = Main.mouseLeft && Main.mouseLeftRelease && Main.playerInventory;
                    if (Main.playerInventory)
                    {
                        Main.player[Main.myPlayer].mouseInterface = true;
                    }
                }
                else
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                }
                if (flag)
                {
                    flag &= BuffLoader.RightClick(buffType, buffSlotOnPlayer);
                }
                if (flag)
                {
                    Main.TryRemovingBuff(buffSlotOnPlayer, buffType);
                }
            }
            else
            {
                Main.buffAlpha[buffSlotOnPlayer] -= 0.05f;
            }
            if (Main.buffAlpha[buffSlotOnPlayer] > 1f)
            {
                Main.buffAlpha[buffSlotOnPlayer] = 1f;
            }
            else if ((double)Main.buffAlpha[buffSlotOnPlayer] < 0.4)
            {
                Main.buffAlpha[buffSlotOnPlayer] = 0.4f;
            }
            if (PlayerInput.UsingGamepad && !Main.playerInventory)
            {
                drawBuffText = -1;
            }
            #endregion

            return drawBuffText;
        }

        public override void Load()
        {
            // Sets my custom function to be used instead of the default.
            Terraria.On_Main.DrawBuffIcon += DrawBuffIcons;
        }

        public override void SetStaticDefaults()
        {
            goldenBorder = ModContent.Request<Texture2D>("Permabuffs/buffFrame");
        }

        /// <summary>
        /// Make all buffs permanent
        /// </summary>
        public override void PostUpdateBuffs()
        {
            PermaBuffsConfig config = PermaBuffsConfig.instance;
            Player player = Main.LocalPlayer;

            // Golden queue manager. 
            goldenCount = Math.Min(TimeForGolden, goldenCount + 1);
            if (goldenCount == TimeForGolden)
            {
                goldenQueue.Clear();
            }
            drawQueue.Clear();

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
                {
                    Player.buffTime[i] += 1;
                    drawQueue.Add(buff);
                }
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
                {
                    player.AddBuff(buff.type, buff.timeLeft, false);
                    goldenQueue.Add(buff);
                }
            }

            goldenCount = 0;
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
                goldenQueue.Add(buff);
            }

            goldenCount = 0;
            pendingBuffs.Clear();
        }
    }
}
