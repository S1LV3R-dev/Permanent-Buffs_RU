using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static PermaBuffs.PermaBuffsPlayer;
using Terraria.UI.Chat;
using Microsoft.Xna.Framework;
using ReLogic.Graphics;

namespace PermaBuffs
{
    public class PermaBuffs : Mod
    {
        public static BuffHook[] postBuffUpdateHooks { get; internal set; }
        public static BuffHook[] preBuffUpdateHooks { get; internal set; }
        public override void PostSetupContent()
        {
            preBuffUpdateHooks = new BuffHook[BuffLoader.BuffCount];
            postBuffUpdateHooks = new BuffHook[BuffLoader.BuffCount];
            
            Type hookClassType = typeof(PreBuffUpdateHooks);

            foreach (var method in hookClassType.GetMethods())
            {
                try
                {
                    BuffHook hook = method.CreateDelegate<BuffHook>();
                    hook(null, 0, false, out int buffType);

                    if (buffType > 0 && buffType < BuffLoader.BuffCount)
                        preBuffUpdateHooks[buffType] = hook;
                }
                catch
                {
                    // Only valid Delegates are added, thrown errors are to be expected from this code
                }
            }

            hookClassType = typeof(PostBuffUpdateHooks);

            foreach (var method in hookClassType.GetMethods())
            {
                try
                {
                    BuffHook hook = method.CreateDelegate<BuffHook>();
                    hook(null, 0, false, out int buffType);

                    if (buffType > 0 && buffType < BuffLoader.BuffCount) 
                        postBuffUpdateHooks[buffType] = hook;
                }
                catch
                {
                    // Only valid Delegates are added, thrown errors are to be expected from this code
                }
            }
        }
        public override void Load()
        {
            // Sets my custom function to be used instead of the default
            Terraria.On_Main.DrawBuffIcon += DrawBuffIcons;

            // Make never buffs unable to apply their effects
            Terraria.On_Player.UpdateBuffs += UpdateBuffs;

            // Banner compatibility hooks.
            // The has banner hook is bugged out to only occasionally activate when the player is hit and nowhere else
            // Meaning I had to instead hook everything that applied the hasBannerBuff hook to my code...
            // Terraria.On_Player.HasNPCBannerBuff += HasNPCBannerBuff;
            Terraria.On_Player.ApplyBannerOffenseBuff_int_refHitModifiers += ApplyBannerOffenseBuff;
            Terraria.On_Player.ApplyBannerDefenseBuff_int_refHurtModifiers += ApplyBannerDefenseBuff;
            Terraria.On_Main.MouseText_DrawBuffTooltip += MouseTextDrawBuffTooltip;

            PermaBuffsPlayer.alwaysPermanentKey = KeybindLoader.RegisterKeybind(this, "Toggle Buff Always Permanent", Microsoft.Xna.Framework.Input.Keys.P);
            PermaBuffsPlayer.neverPermanentKey = KeybindLoader.RegisterKeybind(this, "Toggle Buff Never Permanent", Microsoft.Xna.Framework.Input.Keys.N);
        }

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

            Player player = Main.LocalPlayer;
            PermaBuffsPlayer modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
            PermaBuffsConfig config = PermaBuffsConfig.instance;


            Texture2D texture2;
            // Load textures
            if (modPlayer.neverPermanent[buffType])
            {
                /*if (purpleBorder == null)
                {
                    purpleBorder = ModContent.Request<Texture2D>("Permabuffs/neverBuffFrame", ReLogic.Content.AssetRequestMode.ImmediateLoad);
                }
                */
                texture2 = purpleBorder.Value;
            }
            else
            {
                /*
                if (goldenBorder == null)
                {
                    goldaenBorder = ModContent.Request<Texture2D>("Permabuffs/buffFrame", ReLogic.Content.AssetRequestMode.ImmediateLoad);
                }
                */
                texture2 = goldenBorder.Value;
            }

            // Set the necessary parameters
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

            bool shouldDrawBorder = false;

            // Disable drawing if the config disables it
            if (config.drawBorders)
            {
                // Look at the queues and determines whether or not to draw the border

                shouldDrawBorder = modPlayer.goldenQueue[buffType];
                shouldDrawBorder = shouldDrawBorder || modPlayer.alwaysPermanent[buffType];
                shouldDrawBorder = shouldDrawBorder || modPlayer.neverPermanent[buffType];
            }

            // If the preDraw for this buff was not overridden to cancel drawing the buff icon
            if (!preventDraw)
            {
                // Draw the buff icon
                Main.spriteBatch.Draw(texture, drawPosition, sourceRectangle, drawColor, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);

                // If this bufftype is in the draw queue
                if (config.drawBorders && shouldDrawBorder)
                {
                    // Draw the border on top of the buff icon
                    Main.spriteBatch.Draw(texture2, drawPosition, sourceRectangle2, drawColor, 0f, default(Vector2), 1f, SpriteEffects.None, 0.1f);
                }
            }

            BuffLoader.PostDraw(Main.spriteBatch, buffType, buffSlotOnPlayer, drawParams);

            // Only show the time if no golden border was drawn
            if (Main.TryGetBuffTime(buffSlotOnPlayer, out var buffTimeValue) && buffTimeValue > 2 && (!shouldDrawBorder || modPlayer.neverPermanent[buffType]) && player.buffType[buffSlotOnPlayer] != BuffID.MonsterBanner)
            {
                string text = Lang.LocalizedDuration(new TimeSpan(0, 0, buffTimeValue / 60), abbreviated: true, showAllAvailableUnits: false);
                Main.spriteBatch.DrawString(FontAssets.ItemStack.Value, text, textPosition, color, 0f, default(Vector2), 0.8f, SpriteEffects.None, 0f);
            }

            if (mouseRectangle.Contains(new Point(Main.mouseX, Main.mouseY)))
            {
                BuffInfo buff = new BuffInfo(player.buffType[buffSlotOnPlayer], player.buffTime[buffSlotOnPlayer]);
                modPlayer.CheckToggleKeys(buff, buffSlotOnPlayer);

                // If the buff is not modified
                if (!(modPlayer.alwaysPermanent[buffType] || modPlayer.goldenQueue[buffType] || modPlayer.neverPermanent[buffType]))
                {
                    // Sets the flag for the tooltip not to be shown after the player binds the key
                    if (!modPlayer.permaTooltipSeen)
                    {
                        modPlayer.viewingPermaTooltip = modPlayer.permaBound ? buffType : 0;
                    }
                    if (!modPlayer.neverTooltipSeen)
                    {
                        modPlayer.viewingNeverTooltip = modPlayer.neverBound ? buffType : 0;
                    }
                }

                #region TmodloaderSourceCode2

                drawBuffText = buffSlotOnPlayer;
                Main.buffAlpha[buffSlotOnPlayer] += 0.1f;
                bool tryRemoveBuff = Main.mouseRight && Main.mouseRightRelease;

                if (PlayerInput.UsingGamepad)
                {
                    tryRemoveBuff = Main.mouseLeft && Main.mouseLeftRelease && Main.playerInventory;
                    if (Main.playerInventory)
                    {
                        Main.player[Main.myPlayer].mouseInterface = true;
                    }
                }
                else
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                }

                #endregion

                // The user has right clicked the buff to remove it.
                if (tryRemoveBuff)
                {
                    tryRemoveBuff &= BuffLoader.RightClick(buffType, buffSlotOnPlayer);

                    // Modified buffs can always be deleted regardless of what another mod returns
                    tryRemoveBuff = tryRemoveBuff || modPlayer.neverPermanent[buffType] || modPlayer.alwaysPermanent[buffType];
                }

                // The buff has passed all other mod checks to be removed or is a neverBuff
                if (tryRemoveBuff)
                {
                    Main.TryRemovingBuff(buffSlotOnPlayer, buffType);

                    // The buff was not deleted, force delete it
                    if ((modPlayer.neverPermanent[buffType] || modPlayer.alwaysPermanent[buffType]) && buffType == player.buffType[buffSlotOnPlayer])
                    {
                        player.DelBuff(buffSlotOnPlayer);
                    }

                    // Manually removed buffs should also be removed from the list of always permanent buffs. 
                    // This is for the sake of convienience.
                    modPlayer.alwaysPermanent[buffType] = false; 
                }
            }
            // The player is not moused over the buff
            else
            {
                // Sets the flag for the tooltip not to be shown after the player binds the key
                bool autoHide = config.autoHideKeybindTooltips;

                if (modPlayer.viewingPermaTooltip == buffType)
                {
                    modPlayer.viewingPermaTooltip = 0;
                    modPlayer.permaTooltipSeen = autoHide;
                }
                if (modPlayer.viewingNeverTooltip == buffType)
                {
                    modPlayer.viewingNeverTooltip = 0;
                    modPlayer.neverTooltipSeen = autoHide;
                }

                #region TmodloaderSourceCode3

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

        // Unfortunately this function is bugged as a hook. It is not always called when banner code is run - meaning its useless to override the short way. 
        // This means I had to override all the associated hooks that called this function with my Function call to the hook... SMH
        internal static bool HasNPCBannerBuff(Player player, int bannerType)
        {
            PermaBuffsPlayer modPlayer = player.GetModPlayer<PermaBuffsPlayer>();

            if (modPlayer.alwaysPermanent[BuffID.MonsterBanner] || PermaBuffsConfig.instance.keepBannerBuffs)
            {
                return modPlayer.activeBanners[bannerType];
            }
            else
            {
                return player.HasNPCBannerBuff(bannerType);
            }
        }

        internal static void ApplyBannerOffenseBuff(Terraria.On_Player.orig_ApplyBannerOffenseBuff_int_refHitModifiers orig, Player player, int bannerID, ref NPC.HitModifiers modifiers)
        {
            if (HasNPCBannerBuff(player, bannerID))
            {
                // Main.NewText("OffenseHook called");
                ItemID.BannerEffect effect = ItemID.Sets.BannerStrength[Item.BannerToItem(bannerID)];
                modifiers.TargetDamageMultiplier *= Main.expertMode ? effect.ExpertDamageDealt : effect.NormalDamageDealt;
            }
            else
            {
                // Main.NewText("OffenseOrig called");
                orig(player, bannerID, ref modifiers);
            }
        }

        internal static void ApplyBannerDefenseBuff(Terraria.On_Player.orig_ApplyBannerDefenseBuff_int_refHurtModifiers orig, Player player, int bannerID, ref Player.HurtModifiers modifiers)
        {
            if (HasNPCBannerBuff(player, bannerID))
            {
                // Main.NewText("DefenseHook called");
                ItemID.BannerEffect effect = ItemID.Sets.BannerStrength[Item.BannerToItem(bannerID)];
                modifiers.IncomingDamageMultiplier *= Main.expertMode ? effect.ExpertDamageDealt : effect.NormalDamageDealt;
            }
            else
            {
                // Main.NewText("DefenseOrig called");
                orig(player, bannerID, ref modifiers);
            }
        }

        internal static void MouseTextDrawBuffTooltip(Terraria.On_Main.orig_MouseText_DrawBuffTooltip orig, Main self, string buffString, ref int X, ref int Y, int buffNameHeight)
        {
            Player player = Main.LocalPlayer;

            #region TmodloaderSourceCode

            Point p = new Point(X, Y);
            int num = 220;
            int num2 = 72;
            int num3 = -1;
            float num4 = 1f;
            List<Vector2> list = new List<Vector2>();
            Vector2 vector = ChatManager.GetStringSize(FontAssets.MouseText.Value, buffString, Vector2.One);
            list.Add(vector);
            int num5 = (int)((float)(Main.screenHeight - Y - 24 - num2) * num4) / 20;
            if (num5 < 1)
            {
                num5 = 1;
            }

            if (Main.bannerMouseOver)
            {
                int num6 = 0;

                #endregion

                for (int i = 0; i < NPCLoader.NPCCount; i++)
                {
                    if (Item.BannerToNPC(i) != 0 && HasNPCBannerBuff(player, i))

                    #region TmodloaderSourceCode

                    {
                        num6++;
                        string nPCNameValue = Lang.GetNPCNameValue(Item.BannerToNPC(i));
                        Vector2 vector2 = FontAssets.MouseText.Value.MeasureString(nPCNameValue);
                        int num7 = X;
                        int num8 = Y + (int)vector2.Y + num6 * 20 + 10;
                        int num9 = 0;
                        int num10 = num6 / num5;
                        for (int j = 0; j < num10; j++)
                        {
                            num9++;
                            num7 += num;
                            num8 -= num5 * 20;
                        }
                        if ((float)(num7 - 24 - num) > (float)Main.screenWidth * num4)
                        {
                            num3 = num6;
                            break;
                        }
                        list.Add(new Vector2(num7, num8) + vector2 - p.ToVector2());
                    }
                }
            }
            BuffLoader.CustomBuffTipSize(buffString, list);
            Vector2 zero = Vector2.Zero;
            foreach (Vector2 item in list)
            {
                if (zero.X < item.X)
                {
                    zero.X = item.X;
                }
                if (zero.Y < item.Y)
                {
                    zero.Y = item.Y;
                }
            }
            if ((float)X + zero.X + 24f > (float)Main.screenWidth * num4)
            {
                X = (int)((float)Main.screenWidth * num4 - zero.X - 24f);
            }
            if ((float)Y + zero.Y + 4f > (float)Main.screenHeight * num4)
            {
                Y = (int)((float)Main.screenHeight * num4 - zero.Y - 4f);
            }
            ChatManager.DrawColorCodedStringWithShadow(baseColor: new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, 255), spriteBatch: Main.spriteBatch, font: FontAssets.MouseText.Value, text: buffString, position: new Vector2(X, Y + buffNameHeight), rotation: 0f, origin: Vector2.Zero, baseScale: Vector2.One);
            if (!Main.bannerMouseOver)
            {
                return;
            }
            int num13 = 0;
            for (int l = 0; l < NPCLoader.NPCCount; l++)
            {
                #endregion
                if (Item.BannerToNPC(l) == 0 || !HasNPCBannerBuff(player, l))
                {
                    continue;
                }
                #region TmodloaderSourceCode
                num13++;
                bool flag = false;
                for (int m = 0; m < 5; m++)
                {
                    int num14 = X;
                    int num15 = Y + (int)vector.Y + num13 * 20 + 10;
                    int num16 = (num13 - 1) / num5;
                    num14 += num * num16;
                    num15 -= num5 * 20 * num16;
                    string text = Lang.GetNPCNameValue(Item.BannerToNPC(l));
                    if (num3 == num13)
                    {
                        text = Language.GetTextValue("UI.Ellipsis");
                        flag = true;
                    }
                    Color color2 = Color.Black;
                    switch (m)
                    {
                        case 0:
                            num14 -= 2;
                            break;
                        case 1:
                            num14 += 2;
                            break;
                        case 2:
                            num15 -= 2;
                            break;
                        case 3:
                            num15 += 2;
                            break;
                        default:
                            {
                                float num17 = (float)(int)Main.mouseTextColor / 255f;
                                color2 = new Color((byte)(80f * num17), (byte)(255f * num17), (byte)(120f * num17), Main.mouseTextColor);
                                break;
                            }
                    }
                    Main.spriteBatch.DrawString(FontAssets.MouseText.Value, text, new Vector2(num14, num15), color2, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
                }
                if (flag)
                {
                    break;
                }
            }
            BuffLoader.DrawCustomBuffTip(buffString, Main.spriteBatch, X, Y + (int)FontAssets.MouseText.Value.MeasureString(buffString).Y);
            #endregion
        }

        #region TmodloaderUpdateBuffsDependencies

        internal static string? ToContextString(int ID)
        {
            return ID switch
            {
                1 => "SetBonus_SolarExplosion_WhenTakingDamage",
                2 => "SetBonus_SolarExplosion_WhenDashing",
                3 => "SetBonus_ForbiddenStorm",
                4 => "SetBonus_Titanium",
                5 => "SetBonus_Orichalcum",
                6 => "SetBonus_Chlorophyte",
                7 => "SetBonus_Stardust",
                8 => "WeaponEnchantment_Confetti",
                9 => "PlayerDeath_TombStone",
                11 => "FallingStar",
                12 => "PlayerHurt_DropFootball",
                13 => "StormTigerTierSwap",
                14 => "AbigailTierSwap",
                15 => "SetBonus_GhostHeal",
                16 => "SetBonus_GhostHurt",
                18 => "VampireKnives",
                10 => "TorchGod",
                _ => null,
            };
        }

        #endregion

        internal static void UpdateBuffs(On_Player.orig_UpdateBuffs orig, Player player, int i)
        {
            if (player.soulDrain > 0 && player.whoAmI == Main.myPlayer)
                player.AddBuff(151, 2);

            if (Main.dontStarveWorld)
                player.UpdateStarvingState(withEmote: true);

            for (int j = 0; j < Player.MaxBuffs; j++)
            {
                if (player.buffType[j] <= 0 || player.buffTime[j] <= 0)
                    continue;

                PermaBuffsPlayer modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
                int buffType = player.buffType[j];

                // Don't decrement the time if it is a permabuff
                if (player.whoAmI == Main.myPlayer && !BuffID.Sets.TimeLeftDoesNotDecrease[player.buffType[j]] && !modPlayer.alwaysPermanent[buffType] && !(buffType == BuffID.MonsterBanner && PermaBuffsConfig.instance.keepBannerBuffs))
                    player.buffTime[j]--;

                //TML: player will be used at the very end of player scope.
                int originalIndex = j;

                // Skip applying the buff if it is a neverbuff
                if (modPlayer.neverPermanent[buffType])
                {
                    continue;
                }

                #region TmodloaderCode

                if (player.buffType[j] == 1)
                {
                    player.lavaImmune = true;
                    player.fireWalk = true;
                    player.buffImmune[24] = true;
                }
                else if (BuffID.Sets.BasicMountData[player.buffType[j]] != null)
                {
                    BuffID.Sets.BuffMountData buffMountData = BuffID.Sets.BasicMountData[player.buffType[j]];
                    player.mount.SetMount(buffMountData.mountID, player, buffMountData.faceLeft);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 158)
                {
                    player.manaRegenDelayBonus += 0.5f;
                    player.manaRegenBonus += 10;
                }
                // TML:
                // The changes to player block exist to fix the Sharpening Station's armor penetration bonus
                // being able to be applied to non-melee weapons (most notably summons) by holding a melee weapon.
                // - Thomas
                /*
                else if (player.buffType[j] == 159 && inventory[selectedItem].melee) {
                */
                else if (player.buffType[j] == 159)
                {
                    /*
                    armorPenetration += 12;
                    */
                    player.GetArmorPenetration(DamageClass.Melee) += 12;
                }
                else if (player.buffType[j] == 192)
                {
                    player.pickSpeed -= 0.2f;
                    player.moveSpeed += 0.2f;
                }
                else if (player.buffType[j] == 321)
                {
                    int num = 10;
                    player.GetCritChance(DamageClass.Generic) += num;
                    /*
                    meleeCrit += num;
                    rangedCrit += num;
                    magicCrit += num;
                    */
                    player.GetDamage(DamageClass.Summon) += (float)num / 100f;
                }
                else if (player.buffType[j] == 2)
                {
                    player.lifeRegen += 4;
                }
                else if (player.buffType[j] == 3)
                {
                    player.moveSpeed += 0.25f;
                }
                else if (player.buffType[j] == 4)
                {
                    player.gills = true;
                }
                else if (player.buffType[j] == 5)
                {
                    player.statDefense += 8;
                }
                else if (player.buffType[j] == 6)
                {
                    player.manaRegenBuff = true;
                }
                else if (player.buffType[j] == 7)
                {
                    player.GetDamage(DamageClass.Magic) += 0.2f;
                }
                else if (player.buffType[j] == 8)
                {
                    player.slowFall = true;
                }
                else if (player.buffType[j] == 9)
                {
                    player.findTreasure = true;
                }
                else if (player.buffType[j] == 343)
                {
                    player.biomeSight = true;
                }
                else if (player.buffType[j] == 10)
                {
                    player.invis = true;
                }
                else if (player.buffType[j] == 11)
                {
                    Lighting.AddLight((int)(player.position.X + (float)(player.width / 2)) / 16, (int)(player.position.Y + (float)(player.height / 2)) / 16, 0.8f, 0.95f, 1f);
                }
                else if (player.buffType[j] == 12)
                {
                    player.nightVision = true;
                }
                else if (player.buffType[j] == 13)
                {
                    player.enemySpawns = true;
                }
                else if (player.buffType[j] == 14)
                {
                    if (player.thorns < 1f)
                        player.thorns = 1f;
                }
                else if (player.buffType[j] == 15)
                {
                    player.waterWalk = true;
                }
                else if (player.buffType[j] == 16)
                {
                    player.archery = true;

                    //TML: Moved from PickAmmo, as StatModifier allows multiplicative buffs to be 'registered' alongside additive ones.
                    player.arrowDamage *= 1.1f;
                }
                else if (player.buffType[j] == 17)
                {
                    player.detectCreature = true;
                }
                else if (player.buffType[j] == 18)
                {
                    player.gravControl = true;
                }
                else if (player.buffType[j] == 30)
                {
                    player.bleed = true;
                }
                else if (player.buffType[j] == 31)
                {
                    player.confused = true;
                }
                else if (player.buffType[j] == 32)
                {
                    player.slow = true;
                }
                else if (player.buffType[j] == 35)
                {
                    player.silence = true;
                }
                else if (player.buffType[j] == 160)
                {
                    player.dazed = true;
                }
                else if (player.buffType[j] == 46)
                {
                    player.chilled = true;
                }
                else if (player.buffType[j] == 47)
                {
                    player.frozen = true;
                }
                else if (player.buffType[j] == 156)
                {
                    player.stoned = true;
                }
                else if (player.buffType[j] == 69)
                {
                    player.ichor = true;
                    player.statDefense -= 15;
                }
                else if (player.buffType[j] == 36)
                {
                    player.brokenArmor = true;
                }
                else if (player.buffType[j] == 48)
                {
                    player.honey = true;
                }
                else if (player.buffType[j] == 59)
                {
                    player.shadowDodge = true;
                }
                else if (player.buffType[j] == 93)
                {
                    player.ammoBox = true;
                }
                else if (player.buffType[j] == 58)
                {
                    player.palladiumRegen = true;
                }
                else if (player.buffType[j] == 306)
                {
                    player.hasTitaniumStormBuff = true;
                }
                else if (player.buffType[j] == 88)
                {
                    player.chaosState = true;
                }
                else if (player.buffType[j] == 215)
                {
                    player.statDefense += 5;
                }
                else if (player.buffType[j] == 311)
                {
                    player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.35f;
                }
                else if (player.buffType[j] == 308)
                {
                    player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.25f;
                }
                else if (player.buffType[j] == 314)
                {
                    player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.12f;
                }
                else if (player.buffType[j] == 312)
                {
                    player.coolWhipBuff = true;
                }
                else if (player.buffType[j] == 63)
                {
                    player.moveSpeed += 1f;
                }
                else if (player.buffType[j] == 104)
                {
                    player.pickSpeed -= 0.25f;
                }
                else if (player.buffType[j] == 105)
                {
                    player.lifeMagnet = true;
                }
                else if (player.buffType[j] == 106)
                {
                    player.calmed = true;
                }
                else if (player.buffType[j] == 121)
                {
                    player.fishingSkill += 15;
                }
                else if (player.buffType[j] == 122)
                {
                    player.sonarPotion = true;
                }
                else if (player.buffType[j] == 123)
                {
                    player.cratePotion = true;
                }
                else if (player.buffType[j] == 107)
                {
                    player.tileSpeed += 0.25f;
                    player.wallSpeed += 0.25f;
                    player.blockRange++;
                }
                else if (player.buffType[j] == 108)
                {
                    player.kbBuff = true;
                }
                else if (player.buffType[j] == 109)
                {
                    player.ignoreWater = true;
                    player.accFlipper = true;
                }
                else if (player.buffType[j] == 110)
                {
                    player.maxMinions++;
                }
                else if (player.buffType[j] == 150)
                {
                    player.maxMinions++;
                }
                else if (player.buffType[j] == 348)
                {
                    player.maxTurrets++;
                }
                else if (player.buffType[j] == 111)
                {
                    player.dangerSense = true;
                }
                else if (player.buffType[j] == 112)
                {
                    player.ammoPotion = true;
                }
                else if (player.buffType[j] == 113)
                {
                    player.lifeForce = true;
                    player.statLifeMax2 += player.statLifeMax / 5 / 20 * 20;
                }
                else if (player.buffType[j] == 114)
                {
                    player.endurance += 0.1f;
                }
                else if (player.buffType[j] == 115)
                {
                    player.GetCritChance(DamageClass.Generic) += 10;
                    /*
                    meleeCrit += 10;
                    rangedCrit += 10;
                    magicCrit += 10;
                    */
                }
                else if (player.buffType[j] == 116)
                {
                    player.inferno = true;
                    Lighting.AddLight((int)(((Entity)player).Center.X / 16f), (int)(((Entity)player).Center.Y / 16f), 0.65f, 0.4f, 0.1f);
                    int num2 = 323;
                    float num3 = 200f;
                    bool flag = player.infernoCounter % 60 == 0;
                    int damage = 20;
                    if (player.whoAmI != Main.myPlayer)
                        goto UpdateBuffsLoopEnd; //continue;

                    for (int k = 0; k < 200; k++)
                    {
                        NPC nPC = Main.npc[k];
                        if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[num2] && player.CanNPCBeHitByPlayerOrPlayerProjectile(nPC) && Vector2.Distance(((Entity)player).Center, nPC.Center) <= num3)
                        {
                            if (nPC.FindBuffIndex(num2) == -1)
                                nPC.AddBuff(num2, 120);

                            if (flag)
                                player.ApplyDamageToNPC(nPC, damage, 0f, 0, crit: false);
                        }
                    }

                    if (!player.hostile)
                        goto UpdateBuffsLoopEnd; //continue;

                    for (int l = 0; l < 255; l++)
                    {
                        Player player2 = Main.player[l];
                        if (player2 == player || !player2.active || player2.dead || !player2.hostile || player2.buffImmune[num2] || (player2.team == player.team && player2.team != 0) || !(Vector2.Distance(((Entity)player).Center, player2.Center) <= num3))
                            continue;

                        if (player2.FindBuffIndex(num2) == -1)
                            player2.AddBuff(num2, 120);

                        if (flag)
                        {
                            PlayerDeathReason reason = PlayerDeathReason.ByOther(16, player.whoAmI);
                            player2.Hurt(reason, damage, 0, pvp: true, quiet: false);
                        }
                    }
                }
                else if (player.buffType[j] == 117)
                {
                    player.GetDamage(DamageClass.Generic) += 0.1f;
                    /*
                    meleeDamage += 0.1f;
                    rangedDamage += 0.1f;
                    magicDamage += 0.1f;
                    minionDamage += 0.1f;
                    */
                }
                else if (player.buffType[j] == 119)
                {
                    player.loveStruck = true;
                }
                else if (player.buffType[j] == 120)
                {
                    player.stinky = true;
                }
                else if (player.buffType[j] == 124)
                {
                    player.resistCold = true;
                }
                else if (player.buffType[j] == 257)
                {
                    if (Main.myPlayer == player.whoAmI)
                    {
                        if (player.buffTime[j] > 36000)
                            player.luckPotion = 3;
                        else if (player.buffTime[j] > 18000)
                            player.luckPotion = 2;
                        else
                            player.luckPotion = 1;
                    }
                }
                else if (player.buffType[j] == 165)
                {
                    player.lifeRegen += 6;
                    player.statDefense += 8;
                    player.dryadWard = true;
                    if (player.thorns < 1f)
                        player.thorns += 0.5f;
                }
                else if (player.buffType[j] == 144)
                {
                    player.electrified = true;
                    Lighting.AddLight((int)((Entity)player).Center.X / 16, (int)((Entity)player).Center.Y / 16, 0.3f, 0.8f, 1.1f);
                }
                else if (player.buffType[j] == 94)
                {
                    player.manaSick = true;
                    player.manaSickReduction = Player.manaSickLessDmg * ((float)player.buffTime[j] / (float)Player.manaSickTime);
                }
                else if (player.buffType[j] >= 95 && player.buffType[j] <= 97)
                {
                    player.buffTime[j] = 5;
                    int num4 = (byte)(1 + player.buffType[j] - 95);
                    if (player.beetleOrbs > 0 && player.beetleOrbs != num4)
                    {
                        if (player.beetleOrbs > num4)
                        {
                            player.DelBuff(j);
                            j--;
                        }
                        else
                        {
                            for (int m = 0; m < Player.MaxBuffs; m++)
                            {
                                if (player.buffType[m] >= 95 && player.buffType[m] <= 95 + num4 - 1)
                                {
                                    player.DelBuff(m);
                                    m--;
                                }
                            }
                        }
                    }

                    player.beetleOrbs = num4;
                    if (!player.beetleDefense)
                    {
                        player.beetleOrbs = 0;
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.beetleBuff = true;
                    }
                }
                else if (player.buffType[j] >= 170 && player.buffType[j] <= 172)
                {
                    player.buffTime[j] = 5;
                    int num5 = (byte)(1 + player.buffType[j] - 170);
                    if (player.solarShields > 0 && player.solarShields != num5)
                    {
                        if (player.solarShields > num5)
                        {
                            player.DelBuff(j);
                            j--;
                        }
                        else
                        {
                            for (int n = 0; n < Player.MaxBuffs; n++)
                            {
                                if (player.buffType[n] >= 170 && player.buffType[n] <= 170 + num5 - 1)
                                {
                                    player.DelBuff(n);
                                    n--;
                                }
                            }
                        }
                    }

                    player.solarShields = num5;
                    if (!player.setSolar)
                    {
                        player.solarShields = 0;
                        player.DelBuff(j);
                        j--;
                    }
                }
                else if (player.buffType[j] >= 98 && player.buffType[j] <= 100)
                {
                    int num6 = (byte)(1 + player.buffType[j] - 98);
                    if (player.beetleOrbs > 0 && player.beetleOrbs != num6)
                    {
                        if (player.beetleOrbs > num6)
                        {
                            player.DelBuff(j);
                            j--;
                        }
                        else
                        {
                            for (int num7 = 0; num7 < Player.MaxBuffs; num7++)
                            {
                                if (player.buffType[num7] >= 98 && player.buffType[num7] <= 98 + num6 - 1)
                                {
                                    player.DelBuff(num7);
                                    num7--;
                                }
                            }
                        }
                    }

                    player.beetleOrbs = num6;
                    player.GetDamage(DamageClass.Melee) += 0.1f * (float)player.beetleOrbs;
                    player.GetAttackSpeed(DamageClass.Melee) += 0.1f * (float)player.beetleOrbs;
                    if (!player.beetleOffense)
                    {
                        player.beetleOrbs = 0;
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.beetleBuff = true;
                    }
                }
                else if (player.buffType[j] >= 176 && player.buffType[j] <= 178)
                {
                    int num8 = player.nebulaLevelMana;
                    int num9 = (byte)(1 + player.buffType[j] - 176);
                    if (num8 > 0 && num8 != num9)
                    {
                        if (num8 > num9)
                        {
                            player.DelBuff(j);
                            j--;
                        }
                        else
                        {
                            for (int num10 = 0; num10 < Player.MaxBuffs; num10++)
                            {
                                if (player.buffType[num10] >= 176 && player.buffType[num10] <= 178 + num9 - 1)
                                {
                                    player.DelBuff(num10);
                                    num10--;
                                }
                            }
                        }
                    }

                    player.nebulaLevelMana = num9;
                    if (player.buffTime[j] == 2 && player.nebulaLevelMana > 1)
                    {
                        player.nebulaLevelMana--;
                        player.buffType[j]--;
                        player.buffTime[j] = 480;
                    }
                }
                else if (player.buffType[j] >= 173 && player.buffType[j] <= 175)
                {
                    int num11 = player.nebulaLevelLife;
                    int num12 = (byte)(1 + player.buffType[j] - 173);
                    if (num11 > 0 && num11 != num12)
                    {
                        if (num11 > num12)
                        {
                            player.DelBuff(j);
                            j--;
                        }
                        else
                        {
                            for (int num13 = 0; num13 < Player.MaxBuffs; num13++)
                            {
                                if (player.buffType[num13] >= 173 && player.buffType[num13] <= 175 + num12 - 1)
                                {
                                    player.DelBuff(num13);
                                    num13--;
                                }
                            }
                        }
                    }

                    player.nebulaLevelLife = num12;
                    if (player.buffTime[j] == 2 && player.nebulaLevelLife > 1)
                    {
                        player.nebulaLevelLife--;
                        player.buffType[j]--;
                        player.buffTime[j] = 480;
                    }

                    player.lifeRegen += 6 * player.nebulaLevelLife;
                }
                else if (player.buffType[j] >= 179 && player.buffType[j] <= 181)
                {
                    int num14 = player.nebulaLevelDamage;
                    int num15 = (byte)(1 + player.buffType[j] - 179);
                    if (num14 > 0 && num14 != num15)
                    {
                        if (num14 > num15)
                        {
                            player.DelBuff(j);
                            j--;
                        }
                        else
                        {
                            for (int num16 = 0; num16 < Player.MaxBuffs; num16++)
                            {
                                if (player.buffType[num16] >= 179 && player.buffType[num16] <= 181 + num15 - 1)
                                {
                                    player.DelBuff(num16);
                                    num16--;
                                }
                            }
                        }
                    }

                    player.nebulaLevelDamage = num15;
                    if (player.buffTime[j] == 2 && player.nebulaLevelDamage > 1)
                    {
                        player.nebulaLevelDamage--;
                        player.buffType[j]--;
                        player.buffTime[j] = 480;
                    }

                    float num17 = 0.15f * (float)player.nebulaLevelDamage;
                    player.GetDamage(DamageClass.Generic) += num17;
                    /*
                    meleeDamage += num17;
                    rangedDamage += num17;
                    magicDamage += num17;
                    minionDamage += num17;
                    */
                }
                else if (player.buffType[j] == 62)
                {
                    if ((double)player.statLife <= (double)player.statLifeMax2 * 0.5)
                    {
                        Lighting.AddLight((int)(((Entity)player).Center.X / 16f), (int)(((Entity)player).Center.Y / 16f), 0.1f, 0.2f, 0.45f);
                        player.iceBarrier = true;
                        player.endurance += 0.25f;
                        player.iceBarrierFrameCounter++;
                        if (player.iceBarrierFrameCounter > 2)
                        {
                            player.iceBarrierFrameCounter = 0;
                            player.iceBarrierFrame++;
                            if (player.iceBarrierFrame >= 12)
                                player.iceBarrierFrame = 0;
                        }
                    }
                    else
                    {
                        player.DelBuff(j);
                        j--;
                    }
                }
                else if (player.buffType[j] == 49)
                {
                    for (int num18 = 191; num18 <= 194; num18++)
                    {
                        if (player.ownedProjectileCounts[num18] > 0)
                            player.pygmy = true;
                    }

                    if (!player.pygmy)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 83)
                {
                    if (player.ownedProjectileCounts[317] > 0)
                        player.raven = true;

                    if (!player.raven)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 64)
                {
                    if (player.ownedProjectileCounts[266] > 0)
                        player.slime = true;

                    if (!player.slime)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 125)
                {
                    if (player.ownedProjectileCounts[373] > 0)
                        player.hornetMinion = true;

                    if (!player.hornetMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 126)
                {
                    if (player.ownedProjectileCounts[375] > 0)
                        player.impMinion = true;

                    if (!player.impMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 133)
                {
                    if (player.ownedProjectileCounts[390] > 0 || player.ownedProjectileCounts[391] > 0 || player.ownedProjectileCounts[392] > 0)
                        player.spiderMinion = true;

                    if (!player.spiderMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 134)
                {
                    if (player.ownedProjectileCounts[387] > 0 || player.ownedProjectileCounts[388] > 0)
                        player.twinsMinion = true;

                    if (!player.twinsMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 135)
                {
                    if (player.ownedProjectileCounts[393] > 0 || player.ownedProjectileCounts[394] > 0 || player.ownedProjectileCounts[395] > 0)
                        player.pirateMinion = true;

                    if (!player.pirateMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 214)
                {
                    if (player.ownedProjectileCounts[758] > 0)
                        player.vampireFrog = true;

                    if (!player.vampireFrog)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 139)
                {
                    if (player.ownedProjectileCounts[407] > 0)
                        player.sharknadoMinion = true;

                    if (!player.sharknadoMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 140)
                {
                    if (player.ownedProjectileCounts[423] > 0)
                        player.UFOMinion = true;

                    if (!player.UFOMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 182)
                {
                    if (player.ownedProjectileCounts[613] > 0)
                        player.stardustMinion = true;

                    if (!player.stardustMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 213)
                {
                    if (player.ownedProjectileCounts[755] > 0)
                        player.batsOfLight = true;

                    if (!player.batsOfLight)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 216)
                {
                    bool flag2 = true;
                    if (player.ownedProjectileCounts[759] > 0)
                    {
                        player.babyBird = true;
                    }
                    else if (player.whoAmI == Main.myPlayer)
                    {
                        if (player.numMinions < player.maxMinions)
                        {
                            int num19 = player.FindItem(4281);
                            if (num19 != -1)
                            {
                                Item item = player.inventory[num19];
                                int num20 = Projectile.NewProjectile(player.GetSource_ItemUse(item), ((Entity)player).Top, Vector2.Zero, item.shoot, item.damage, item.knockBack, player.whoAmI);
                                Main.projectile[num20].originalDamage = item.damage;
                                player.babyBird = true;
                            }
                        }

                        if (!player.babyBird)
                        {
                            player.DelBuff(j);
                            j--;
                            flag2 = false;
                        }
                    }

                    if (flag2)
                        player.buffTime[j] = 18000;
                }
                else if (player.buffType[j] == 325)
                {
                    if (player.ownedProjectileCounts[951] > 0)
                        player.flinxMinion = true;

                    if (!player.flinxMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 335)
                {
                    if (player.ownedProjectileCounts[970] > 0)
                        player.abigailMinion = true;

                    if (!player.abigailMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }

                    if (player.whoAmI == Main.myPlayer)
                    {
                        int num = 963;
                        if (player.ownedProjectileCounts[970] < 1)
                        {
                            for (int a = 0; a < 1000; a++)
                            {
                                Projectile projectile = Main.projectile[a];
                                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == num)
                                {
                                    projectile.Kill();
                                }
                            }
                        }
                        else if (player.ownedProjectileCounts[num] < 1)
                        {
                            Projectile.NewProjectile(player.GetSource_Misc(ToContextString(14)), player.Center, Vector2.Zero, num, 0, 0f, player.whoAmI);
                        }
                    }
                }
                else if (player.buffType[j] == 263)
                {
                    if (player.ownedProjectileCounts[831] > 0)
                        player.stormTiger = true;

                    if (!player.stormTiger)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }

                    if (player.whoAmI == Main.myPlayer)
                    {
                        int result = 0;
                        {
                            int num41 = player.ownedProjectileCounts[831];
                            if (num41 > 0)
                            {
                                result = 1;
                            }
                            if (num41 > 3)
                            {
                                result = 2;
                            }
                            if (num41 > 6)
                            {
                                result = 3;
                            }
                        }

                        int num = result switch
                        {
                            1 => 833,
                            2 => 834,
                            3 => 835,
                            _ => -1,
                        };
                        bool flag = false;
                        if (num == -1)
                        {
                            flag = true;
                        }
                        for (int a = 0; a < ProjectileID.Sets.StormTigerIds.Length; a++)
                        {
                            int num2 = ProjectileID.Sets.StormTigerIds[a];
                            if (num2 != num && player.ownedProjectileCounts[num2] >= 1)
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                        {
                            for (int a = 0; a < 1000; a++)
                            {
                                Projectile projectile = Main.projectile[a];
                                if (projectile.active && projectile.owner == ((Entity)player).whoAmI && projectile.type != num && ProjectileID.Sets.StormTiger[projectile.type])
                                {
                                    projectile.Kill();
                                }
                            }
                        }
                        else if (player.ownedProjectileCounts[num] < 1)
                        {
                            int num3 = Projectile.NewProjectile(player.GetSource_Misc(ToContextString(13)), ((Entity)player).Center, Vector2.Zero, num, 0, 0f, ((Entity)player).whoAmI, 0f, 1f);
                            Main.projectile[num3].localAI[0] = 60f;
                        }
                    }
                }
                else if (player.buffType[j] == 271)
                {
                    if (player.ownedProjectileCounts[864] > 0)
                        player.smolstar = true;

                    if (!player.smolstar)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 322)
                {
                    if (player.ownedProjectileCounts[946] > 0)
                        player.empressBlade = true;

                    if (!player.empressBlade)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 187)
                {
                    if (player.ownedProjectileCounts[623] > 0)
                        player.stardustGuardian = true;

                    if (!player.stardustGuardian)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 188)
                {
                    if (player.ownedProjectileCounts[625] > 0)
                        player.stardustDragon = true;

                    if (!player.stardustDragon)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 161)
                {
                    if (player.ownedProjectileCounts[533] > 0)
                        player.DeadlySphereMinion = true;

                    if (!player.DeadlySphereMinion)
                    {
                        player.DelBuff(j);
                        j--;
                    }
                    else
                    {
                        player.buffTime[j] = 18000;
                    }
                }
                else if (player.buffType[j] == 90)
                {
                    player.mount.SetMount(0, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 128)
                {
                    player.mount.SetMount(1, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 129)
                {
                    player.mount.SetMount(2, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 130)
                {
                    player.mount.SetMount(3, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 131)
                {
                    player.ignoreWater = true;
                    player.accFlipper = true;
                    player.mount.SetMount(4, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 132)
                {
                    player.mount.SetMount(5, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 168)
                {
                    player.ignoreWater = true;
                    player.accFlipper = true;
                    player.mount.SetMount(12, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 141)
                {
                    player.mount.SetMount(7, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 142)
                {
                    player.mount.SetMount(8, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 143)
                {
                    player.mount.SetMount(9, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 162)
                {
                    player.mount.SetMount(10, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 193)
                {
                    player.mount.SetMount(14, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 212)
                {
                    player.mount.SetMount(17, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 230)
                {
                    player.mount.SetMount(23, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 265)
                {
                    player.canFloatInWater = true;
                    player.accFlipper = true;
                    player.mount.SetMount(37, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 275)
                {
                    player.mount.SetMount(40, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 276)
                {
                    player.mount.SetMount(41, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 277)
                {
                    player.mount.SetMount(42, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 278)
                {
                    player.mount.SetMount(43, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 279)
                {
                    player.ignoreWater = true;
                    player.accFlipper = true;
                    player.mount.SetMount(44, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 280)
                {
                    player.mount.SetMount(45, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 281)
                {
                    player.mount.SetMount(46, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 282)
                {
                    player.mount.SetMount(47, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 283)
                {
                    player.mount.SetMount(48, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 305)
                {
                    player.ignoreWater = true;
                    player.accFlipper = true;
                    player.lavaImmune = true;
                    player.mount.SetMount(49, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 318)
                {
                    player.mount.SetMount(50, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 342)
                {
                    player.mount.SetMount(52, player);
                    player.buffTime[j] = 10;
                }
                else if (player.buffType[j] == 37)
                {
                    if (Main.wofNPCIndex >= 0 && Main.npc[Main.wofNPCIndex].type == 113)
                    {
                        player.gross = true;
                        player.buffTime[j] = 10;
                    }
                    else
                    {
                        player.DelBuff(j);
                        j--;
                    }
                }
                else if (player.buffType[j] == 38)
                {
                    player.buffTime[j] = 10;
                    player.tongued = true;
                }
                else if (player.buffType[j] == 146)
                {
                    player.moveSpeed += 0.1f;
                    player.moveSpeed *= 1.1f;
                    player.sunflower = true;
                }
                else if (player.buffType[j] == 19)
                {
                    player.buffTime[j] = 18000;
                    player.lightOrb = true;
                    bool flag3 = true;
                    if (player.ownedProjectileCounts[18] > 0)
                        flag3 = false;

                    if (flag3 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 18, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 155)
                {
                    player.buffTime[j] = 18000;
                    player.crimsonHeart = true;
                    bool flag4 = true;
                    if (player.ownedProjectileCounts[500] > 0)
                        flag4 = false;

                    if (flag4 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 500, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 191)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.companionCube, 653);
                }
                else if (player.buffType[j] == 202)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDD2Dragon, 701);
                }
                else if (player.buffType[j] == 217)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagUpbeatStar, 764);
                }
                else if (player.buffType[j] == 219)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagBabyShark, 774);
                }
                else if (player.buffType[j] == 258)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagLilHarpy, 815);
                }
                else if (player.buffType[j] == 259)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagFennecFox, 816);
                }
                else if (player.buffType[j] == 260)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagGlitteryButterfly, 817);
                }
                else if (player.buffType[j] == 261)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagBabyImp, 821);
                }
                else if (player.buffType[j] == 262)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagBabyRedPanda, 825);
                }
                else if (player.buffType[j] == 264)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagPlantero, 854);
                }
                else if (player.buffType[j] == 266)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDynamiteKitten, 858);
                }
                else if (player.buffType[j] == 267)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagBabyWerewolf, 859);
                }
                else if (player.buffType[j] == 268)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagShadowMimic, 860);
                }
                else if (player.buffType[j] == 274)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagVoltBunny, 875);
                }
                else if (player.buffType[j] == 284)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagKingSlimePet, 881);
                }
                else if (player.buffType[j] == 285)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagEyeOfCthulhuPet, 882);
                }
                else if (player.buffType[j] == 286)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagEaterOfWorldsPet, 883);
                }
                else if (player.buffType[j] == 287)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagBrainOfCthulhuPet, 884);
                }
                else if (player.buffType[j] == 288)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagSkeletronPet, 885);
                }
                else if (player.buffType[j] == 289)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagQueenBeePet, 886);
                }
                else if (player.buffType[j] == 290)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDestroyerPet, 887);
                }
                else if (player.buffType[j] == 291)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagTwinsPet, 888);
                }
                else if (player.buffType[j] == 292)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagSkeletronPrimePet, 889);
                }
                else if (player.buffType[j] == 293)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagPlanteraPet, 890);
                }
                else if (player.buffType[j] == 294)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagGolemPet, 891);
                }
                else if (player.buffType[j] == 295)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDukeFishronPet, 892);
                }
                else if (player.buffType[j] == 296)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagLunaticCultistPet, 893);
                }
                else if (player.buffType[j] == 297)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagMoonLordPet, 894);
                }
                else if (player.buffType[j] == 298)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagFairyQueenPet, 895);
                }
                else if (player.buffType[j] == 299)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagPumpkingPet, 896);
                }
                else if (player.buffType[j] == 300)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagEverscreamPet, 897);
                }
                else if (player.buffType[j] == 301)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagIceQueenPet, 898);
                }
                else if (player.buffType[j] == 302)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagMartianPet, 899);
                }
                else if (player.buffType[j] == 303)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDD2OgrePet, 900);
                }
                else if (player.buffType[j] == 304)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDD2BetsyPet, 901);
                }
                else if (player.buffType[j] == 317)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagQueenSlimePet, 934);
                }
                else if (player.buffType[j] == 327)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagBerniePet, 956);
                }
                else if (player.buffType[j] == 328)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagGlommerPet, 957);
                }
                else if (player.buffType[j] == 329)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDeerclopsPet, 958);
                }
                else if (player.buffType[j] == 330)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagPigPet, 959);
                }
                else if (player.buffType[j] == 331)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagChesterPet, 960);
                }
                else if (player.buffType[j] == 341)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagKingSlimePet, 881);
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagQueenSlimePet, 934);
                }
                else if (player.buffType[j] == 345)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagJunimoPet, 994);
                }
                else if (player.buffType[j] == 349)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagBlueChickenPet, 998);
                }
                else if (player.buffType[j] == 351)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagSpiffo, 1003);
                }
                else if (player.buffType[j] == 352)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagCaveling, 1004);
                }
                else if (player.buffType[j] == 354)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDirtiestBlock, 1018);
                }
                else if (player.buffType[j] == 200)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDD2Gato, 703);
                }
                else if (player.buffType[j] == 201)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagDD2Ghost, 702);
                }
                else if (player.buffType[j] == 218)
                {
                    player.BuffHandle_SpawnPetIfNeededAndSetTime(j, ref player.petFlagSugarGlider, 765);
                }
                else if (player.buffType[j] == 190)
                {
                    player.buffTime[j] = 18000;
                    player.suspiciouslookingTentacle = true;
                    bool flag5 = true;
                    if (player.ownedProjectileCounts[650] > 0)
                        flag5 = false;

                    if (flag5 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 650, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 27 || player.buffType[j] == 101 || player.buffType[j] == 102)
                {
                    player.buffTime[j] = 18000;
                    bool flag6 = true;
                    int num21 = 72;
                    if (player.buffType[j] == 27)
                        player.blueFairy = true;

                    if (player.buffType[j] == 101)
                    {
                        num21 = 86;
                        player.redFairy = true;
                    }

                    if (player.buffType[j] == 102)
                    {
                        num21 = 87;
                        player.greenFairy = true;
                    }

                    if (player.head == 45 && player.body == 26 && player.legs == 25)
                        num21 = 72;

                    if (player.ownedProjectileCounts[num21] > 0)
                        flag6 = false;

                    if (flag6 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, num21, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 40)
                {
                    player.buffTime[j] = 18000;
                    player.bunny = true;
                    bool flag7 = true;
                    if (player.ownedProjectileCounts[111] > 0)
                        flag7 = false;

                    if (flag7 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 111, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 148)
                {
                    player.rabid = true;
                    if (Main.rand.NextBool(1200))
                    {
                        int num22 = Main.rand.Next(6);
                        float num23 = (float)Main.rand.Next(60, 100) * 0.01f;
                        switch (num22)
                        {
                            case 0:
                                player.AddBuff(22, (int)(60f * num23 * 3f));
                                break;
                            case 1:
                                player.AddBuff(23, (int)(60f * num23 * 0.75f));
                                break;
                            case 2:
                                player.AddBuff(31, (int)(60f * num23 * 1.5f));
                                break;
                            case 3:
                                player.AddBuff(32, (int)(60f * num23 * 3.5f));
                                break;
                            case 4:
                                player.AddBuff(33, (int)(60f * num23 * 5f));
                                break;
                            case 5:
                                player.AddBuff(35, (int)(60f * num23 * 1f));
                                break;
                        }
                    }

                    player.GetDamage(DamageClass.Generic) += 0.2f;
                    /*
                    meleeDamage += 0.2f;
                    magicDamage += 0.2f;
                    rangedDamage += 0.2f;
                    minionDamage += 0.2f;
                    */
                }
                else if (player.buffType[j] == 41)
                {
                    player.buffTime[j] = 18000;
                    player.penguin = true;
                    bool flag8 = true;
                    if (player.ownedProjectileCounts[112] > 0)
                        flag8 = false;

                    if (flag8 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 112, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 152)
                {
                    player.buffTime[j] = 18000;
                    player.magicLantern = true;
                    if (player.ownedProjectileCounts[492] == 0 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 492, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 91)
                {
                    player.buffTime[j] = 18000;
                    player.puppy = true;
                    bool flag9 = true;
                    if (player.ownedProjectileCounts[334] > 0)
                        flag9 = false;

                    if (flag9 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 334, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 92)
                {
                    player.buffTime[j] = 18000;
                    player.grinch = true;
                    bool flag10 = true;
                    if (player.ownedProjectileCounts[353] > 0)
                        flag10 = false;

                    if (flag10 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 353, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 84)
                {
                    player.buffTime[j] = 18000;
                    player.blackCat = true;
                    bool flag11 = true;
                    if (player.ownedProjectileCounts[319] > 0)
                        flag11 = false;

                    if (flag11 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 319, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 61)
                {
                    player.buffTime[j] = 18000;
                    player.dino = true;
                    bool flag12 = true;
                    if (player.ownedProjectileCounts[236] > 0)
                        flag12 = false;

                    if (flag12 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 236, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 154)
                {
                    player.buffTime[j] = 18000;
                    player.babyFaceMonster = true;
                    bool flag13 = true;
                    if (player.ownedProjectileCounts[499] > 0)
                        flag13 = false;

                    if (flag13 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 499, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 65)
                {
                    player.buffTime[j] = 18000;
                    player.eyeSpring = true;
                    bool flag14 = true;
                    if (player.ownedProjectileCounts[268] > 0)
                        flag14 = false;

                    if (flag14 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 268, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 66)
                {
                    player.buffTime[j] = 18000;
                    player.snowman = true;
                    bool flag15 = true;
                    if (player.ownedProjectileCounts[269] > 0)
                        flag15 = false;

                    if (flag15 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 269, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 42)
                {
                    player.buffTime[j] = 18000;
                    player.turtle = true;
                    bool flag16 = true;
                    if (player.ownedProjectileCounts[127] > 0)
                        flag16 = false;

                    if (flag16 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 127, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 45)
                {
                    player.buffTime[j] = 18000;
                    player.eater = true;
                    bool flag17 = true;
                    if (player.ownedProjectileCounts[175] > 0)
                        flag17 = false;

                    if (flag17 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 175, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 50)
                {
                    player.buffTime[j] = 18000;
                    player.skeletron = true;
                    bool flag18 = true;
                    if (player.ownedProjectileCounts[197] > 0)
                        flag18 = false;

                    if (flag18 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 197, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 51)
                {
                    player.buffTime[j] = 18000;
                    player.hornet = true;
                    bool flag19 = true;
                    if (player.ownedProjectileCounts[198] > 0)
                        flag19 = false;

                    if (flag19 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 198, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 52)
                {
                    player.buffTime[j] = 18000;
                    player.tiki = true;
                    bool flag20 = true;
                    if (player.ownedProjectileCounts[199] > 0)
                        flag20 = false;

                    if (flag20 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 199, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 53)
                {
                    player.buffTime[j] = 18000;
                    player.lizard = true;
                    bool flag21 = true;
                    if (player.ownedProjectileCounts[200] > 0)
                        flag21 = false;

                    if (flag21 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 200, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 54)
                {
                    player.buffTime[j] = 18000;
                    player.parrot = true;
                    bool flag22 = true;
                    if (player.ownedProjectileCounts[208] > 0)
                        flag22 = false;

                    if (flag22 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 208, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 55)
                {
                    player.buffTime[j] = 18000;
                    player.truffle = true;
                    bool flag23 = true;
                    if (player.ownedProjectileCounts[209] > 0)
                        flag23 = false;

                    if (flag23 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 209, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 56)
                {
                    player.buffTime[j] = 18000;
                    player.sapling = true;
                    bool flag24 = true;
                    if (player.ownedProjectileCounts[210] > 0)
                        flag24 = false;

                    if (flag24 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 210, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 85)
                {
                    player.buffTime[j] = 18000;
                    player.cSapling = true;
                    bool flag25 = true;
                    if (player.ownedProjectileCounts[324] > 0)
                        flag25 = false;

                    if (flag25 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 324, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 81)
                {
                    player.buffTime[j] = 18000;
                    player.spider = true;
                    bool flag26 = true;
                    if (player.ownedProjectileCounts[313] > 0)
                        flag26 = false;

                    if (flag26 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 313, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 82)
                {
                    player.buffTime[j] = 18000;
                    player.squashling = true;
                    bool flag27 = true;
                    if (player.ownedProjectileCounts[314] > 0)
                        flag27 = false;

                    if (flag27 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 314, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 57)
                {
                    player.buffTime[j] = 18000;
                    player.wisp = true;
                    bool flag28 = true;
                    if (player.ownedProjectileCounts[211] > 0)
                        flag28 = false;

                    if (flag28 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 211, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 60)
                {
                    player.buffTime[j] = 18000;
                    player.crystalLeaf = true;
                    bool flag29 = true;
                    for (int num24 = 0; num24 < 1000; num24++)
                    {
                        if (Main.projectile[num24].active && Main.projectile[num24].owner == player.whoAmI && Main.projectile[num24].type == 226)
                        {
                            if (!flag29)
                                Main.projectile[num24].Kill();

                            flag29 = false;
                        }
                    }

                    if (flag29 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 226, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 127)
                {
                    player.buffTime[j] = 18000;
                    player.zephyrfish = true;
                    bool flag30 = true;
                    if (player.ownedProjectileCounts[380] > 0)
                        flag30 = false;

                    if (flag30 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 380, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 136)
                {
                    player.buffTime[j] = 18000;
                    player.miniMinotaur = true;
                    bool flag31 = true;
                    if (player.ownedProjectileCounts[398] > 0)
                        flag31 = false;

                    if (flag31 && player.whoAmI == Main.myPlayer)
                        Projectile.NewProjectile(new EntitySource_Buff(player, player.buffType[j], j), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, 398, 0, 0f, player.whoAmI);
                }
                else if (player.buffType[j] == 70)
                {
                    player.venom = true;
                }
                else if (player.buffType[j] == 20)
                {
                    player.poisoned = true;
                }
                else if (player.buffType[j] == 21)
                {
                    player.potionDelay = player.buffTime[j];
                }
                else if (player.buffType[j] == 22)
                {
                    player.blind = true;
                }
                else if (player.buffType[j] == 80)
                {
                    player.blackout = true;
                }
                else if (player.buffType[j] == 23)
                {
                    player.noItems = true;
                    player.cursed = true;
                }
                else if (player.buffType[j] == 24)
                {
                    player.onFire = true;
                }
                else if (player.buffType[j] == 103)
                {
                    player.dripping = true;
                }
                else if (player.buffType[j] == 137)
                {
                    player.drippingSlime = true;
                }
                else if (player.buffType[j] == 320)
                {
                    player.drippingSparkleSlime = true;
                }
                else if (player.buffType[j] == 67)
                {
                    player.burned = true;
                }
                else if (player.buffType[j] == 68)
                {
                    player.suffocating = true;
                }
                else if (player.buffType[j] == 39)
                {
                    player.onFire2 = true;
                }
                else if (player.buffType[j] == 323)
                {
                    player.onFire3 = true;
                }
                else if (player.buffType[j] == 44)
                {
                    player.onFrostBurn = true;
                }
                else if (player.buffType[j] == 324)
                {
                    player.onFrostBurn2 = true;
                }
                else if (player.buffType[j] == 353)
                {
                    player.shimmering = true;
                    player.frozen = true;
                    player.fallStart = (int)(player.position.Y / 16f);
                    if (Main.myPlayer != player.whoAmI)
                        goto UpdateBuffsLoopEnd; //continue;

                    if (player.position.Y / 16f > (float)Main.UnderworldLayer)
                    {
                        if (Main.myPlayer == player.whoAmI)
                        {
                            player.DelBuff(j);
                            j--;
                        }
                        goto UpdateBuffsLoopEnd; //continue;
                    }

                    if (player.shimmerWet)
                    {
                        player.buffTime[j] = 60;
                        goto UpdateBuffsLoopEnd; //continue;
                    }

                    bool flag32 = false;
                    for (int num25 = (int)(player.position.X / 16f); (float)num25 <= (player.position.X + (float)player.width) / 16f; num25++)
                    {
                        for (int num26 = (int)(player.position.Y / 16f); (float)num26 <= (player.position.Y + (float)player.height) / 16f; num26++)
                        {
                            if (WorldGen.SolidTile3(num25, num26))
                                flag32 = true;
                        }
                    }

                    if (flag32)
                        player.buffTime[j] = 6;
                    else
                    {
                        player.DelBuff(j);
                        j--;
                    }
                }
                else if (player.buffType[j] == 163)
                {
                    player.headcovered = true;
                    player.bleed = true;
                }
                else if (player.buffType[j] == 164)
                {
                    player.vortexDebuff = true;
                }
                else if (player.buffType[j] == 194)
                {
                    player.windPushed = true;
                }
                else if (player.buffType[j] == 195)
                {
                    player.witheredArmor = true;
                }
                else if (player.buffType[j] == 205)
                {
                    player.ballistaPanic = true;
                }
                else if (player.buffType[j] == 196)
                {
                    player.witheredWeapon = true;
                }
                else if (player.buffType[j] == 197)
                {
                    player.slowOgreSpit = true;
                }
                else if (player.buffType[j] == 198)
                {
                    player.parryDamageBuff = true;
                }
                else if (player.buffType[j] == 145)
                {
                    player.moonLeech = true;
                }
                else if (player.buffType[j] == 149)
                {
                    player.webbed = true;
                    if (player.velocity.Y != 0f)
                        player.velocity = new Vector2(0f, 1E-06f);
                    else
                        player.velocity = Vector2.Zero;

                    Player.jumpHeight = 0;
                    player.gravity = 0f;
                    player.moveSpeed = 0f;
                    player.dash = 0;
                    player.dashType = 0;
                    player.noKnockback = true;
                    player.RemoveAllGrapplingHooks();
                }
                else if (player.buffType[j] == 43)
                {
                    player.defendedByPaladin = true;
                }
                else if (player.buffType[j] == 29)
                {
                    player.GetCritChance(DamageClass.Magic) += 2;
                    player.GetDamage(DamageClass.Magic) += 0.05f;
                    player.statManaMax2 += 20;
                    player.manaCost -= 0.02f;
                }
                else if (player.buffType[j] == 28)
                {
                    if (!Main.dayTime && player.wolfAcc && !player.merman)
                    {
                        player.lifeRegen++;
                        player.wereWolf = true;
                        player.GetCritChance(DamageClass.Melee) += 2;
                        player.GetDamage(DamageClass.Melee) += 0.051f;
                        player.GetAttackSpeed(DamageClass.Melee) += 0.051f;
                        player.statDefense += 3;
                        player.moveSpeed += 0.05f;
                    }
                    else
                    {
                        player.DelBuff(j);
                        j--;
                    }
                }
                else if (player.buffType[j] == 33)
                {
                    player.GetDamage(DamageClass.Melee) -= 0.051f;
                    player.GetAttackSpeed(DamageClass.Melee) -= 0.051f;
                    player.statDefense -= 4;
                    player.moveSpeed -= 0.1f;
                }
                else if (player.buffType[j] == 25)
                {
                    player.tipsy = true;
                    player.statDefense -= 4;
                    player.GetCritChance(DamageClass.Melee) += 2;
                    player.GetDamage(DamageClass.Melee) += 0.1f;
                    player.GetAttackSpeed(DamageClass.Melee) += 0.1f;
                }
                else if (player.buffType[j] == 26)
                {
                    player.wellFed = true;
                    player.statDefense += 2;
                    player.GetCritChance(DamageClass.Generic) += 2;
                    player.GetDamage(DamageClass.Generic) += 0.05f;
                    /*
                    meleeCrit += 2;
                    meleeDamage += 0.05f;
                    */
                    player.GetAttackSpeed(DamageClass.Melee) += 0.05f;
                    /*
                    magicCrit += 2;
                    magicDamage += 0.05f;
                    rangedCrit += 2;
                    rangedDamage += 0.05f;
                    minionDamage += 0.05f;
                    */
                    player.GetKnockback(DamageClass.Summon) += 0.5f;
                    player.moveSpeed += 0.2f;
                    player.pickSpeed -= 0.05f;
                }
                else if (player.buffType[j] == 206)
                {
                    player.wellFed = true;
                    player.statDefense += 3;
                    player.GetCritChance(DamageClass.Generic) += 3;
                    player.GetDamage(DamageClass.Generic) += 0.075f;
                    /*
                    meleeCrit += 3;
                    meleeDamage += 0.075f;
                    */
                    player.GetAttackSpeed(DamageClass.Melee) += 0.075f;
                    /*
                    magicCrit += 3;
                    magicDamage += 0.075f;
                    rangedCrit += 3;
                    rangedDamage += 0.075f;
                    minionDamage += 0.075f;
                    */
                    player.GetKnockback(DamageClass.Summon) += 0.75f;
                    player.moveSpeed += 0.3f;
                    player.pickSpeed -= 0.1f;
                }
                else if (player.buffType[j] == 207)
                {
                    player.wellFed = true;
                    player.statDefense += 4;
                    player.GetCritChance(DamageClass.Generic) += 4;
                    player.GetDamage(DamageClass.Generic) += 0.1f;
                    /*
                    meleeCrit += 4;
                    meleeDamage += 0.1f;
                    */
                    player.GetAttackSpeed(DamageClass.Melee) += 0.1f;
                    /*
                    magicCrit += 4;
                    magicDamage += 0.1f;
                    rangedCrit += 4;
                    rangedDamage += 0.1f;
                    minionDamage += 0.1f;
                    */
                    player.GetKnockback(DamageClass.Summon) += 1f;
                    player.moveSpeed += 0.4f;
                    player.pickSpeed -= 0.15f;
                }
                else if (player.buffType[j] == 333)
                {
                    player.hungry = true;
                    player.statDefense -= 2;
                    player.GetCritChance(DamageClass.Generic) -= 2;
                    player.GetDamage(DamageClass.Generic) -= 0.05f;
                    /*
                    meleeCrit -= 2;
                    meleeDamage -= 0.05f;
                    */
                    player.GetAttackSpeed(DamageClass.Melee) -= 0.05f;
                    /*
                    magicCrit -= 2;
                    magicDamage -= 0.05f;
                    rangedCrit -= 2;
                    rangedDamage -= 0.05f;
                    minionDamage -= 0.05f;
                    */
                    player.GetKnockback(DamageClass.Summon) -= 0.5f;
                    player.pickSpeed += 0.05f;
                }
                else if (player.buffType[j] == 334)
                {
                    player.starving = true;
                    player.statDefense -= 4;
                    player.GetCritChance(DamageClass.Generic) -= 4;
                    player.GetDamage(DamageClass.Generic) -= 0.1f;
                    /*
                    meleeCrit -= 4;
                    meleeDamage -= 0.1f;
                    */
                    player.GetAttackSpeed(DamageClass.Melee) -= 0.1f;
                    /*
                    magicCrit -= 4;
                    magicDamage -= 0.1f;
                    rangedCrit -= 4;
                    rangedDamage -= 0.1f;
                    minionDamage -= 0.1f;
                    */
                    player.GetKnockback(DamageClass.Summon) -= 1f;
                    player.pickSpeed += 0.15f;
                }
                else if (player.buffType[j] == 336)
                {
                    player.heartyMeal = true;
                }
                else if (player.buffType[j] == 71)
                {
                    player.meleeEnchant = 1;
                }
                else if (player.buffType[j] == 73)
                {
                    player.meleeEnchant = 2;
                }
                else if (player.buffType[j] == 74)
                {
                    player.meleeEnchant = 3;
                }
                else if (player.buffType[j] == 75)
                {
                    player.meleeEnchant = 4;
                }
                else if (player.buffType[j] == 76)
                {
                    player.meleeEnchant = 5;
                }
                else if (player.buffType[j] == 77)
                {
                    player.meleeEnchant = 6;
                }
                else if (player.buffType[j] == 78)
                {
                    player.meleeEnchant = 7;
                }
                else if (player.buffType[j] == 79)
                {
                    player.meleeEnchant = 8;
                }

            UpdateBuffsLoopEnd:
                if (j == originalIndex)
                    BuffLoader.Update(player.buffType[j], player, ref j);
            }

            player.UpdateHungerBuffs();
            if (player.whoAmI == Main.myPlayer && player.luckPotion != player.oldLuckPotion)
            {
                player.luckNeedsSync = true;
                player.oldLuckPotion = player.luckPotion;
            }

            #endregion
        }
    }
}
