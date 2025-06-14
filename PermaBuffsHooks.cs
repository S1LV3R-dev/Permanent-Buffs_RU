using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq.Expressions;
using System.Collections.Generic;
using Terraria.ModLoader.Core;

namespace PermaBuffs
{
    public class BuffStatus
    {
        public const int NotModified = 0;
        public const int IsPermaBuffed = 1;
        public const int IsNeverBuffed = 2;
    }
    /// <summary>
    /// A function delegate class that gets called during Post/PreUpdateBuffs on the player's active buffs. For running custom code that correctly implements certain perma/neverbuffs
    /// </summary>
    /// <param name="player">The Player instance who has the buff. This will be called with a null reference the first time the hook is run.</param>
    /// <param name="buffSlotOnPlayer"> The index of the buff in player.buffType and player.buffTime</param>
    /// <param name="buffStatus">Called with 0 if the buff is not modified, 1 if it's permabuffed, and 2 if it's neverbuffed. </param>
    /// <param name="buffType">The buffType this hook is for. This needs to be set in the function before the player null reference check.</param>
    public delegate void BuffHook(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType);

    /// <summary>
    /// These methods will be called post Buff.Update. For proper integration please follow the instruction below.
    /// Every function should have the return type of void, be static, and return after buffType is set if "player" == null.
    /// If the function is meant for a modbuff, return a bufftype of 0 if the mod is not loaded
    /// Do not write any functions that don't directly follow the function signature of void(Player, ref int, int, out int) in the class. 
    /// This class is reserved for functions that are set to become delegates of the above function type. Nothing else should be present.
    /// If you're extending compatibility of your mod to this mod, the process is simple. Create a public partial class with the same type name 
    /// as the one below, enclose it within a 'PermaBuffs' namespace, and define functions with the above specifications. 
    /// My mod will auto recognize it and add the buffType to the list of hooks.
    /// </summary>
    public partial class PermaBuffsPreBuffUpdateHooks
    {

        #region Vanilla

        // If the player permabuffs one of the tier buffs, auto upgrade it to level 3
        public static void BeetleEnduranceBuff1(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = BuffID.BeetleEndurance1;

            if (player == null) return;
            if (buffStatus != BuffStatus.IsPermaBuffed) return;

            var modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
            modPlayer.alwaysPermanent[BuffID.BeetleEndurance1] = false;
            modPlayer.alwaysPermanent[BuffID.BeetleEndurance3] = true;
        }
        public static void BeetleEnduranceBuff2(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = BuffID.BeetleEndurance2;

            if (player == null) return;
            if (buffStatus != BuffStatus.IsPermaBuffed) return;

            var modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
            modPlayer.alwaysPermanent[BuffID.BeetleEndurance2] = false;
            modPlayer.alwaysPermanent[BuffID.BeetleEndurance3] = true;
        }
        public static void SolarShieldBuff1(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = BuffID.SolarShield1;

            if (player == null) return;
            if (buffStatus != BuffStatus.IsPermaBuffed) return;

            var modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
            modPlayer.alwaysPermanent[BuffID.SolarShield1] = false;
            modPlayer.alwaysPermanent[BuffID.SolarShield3] = true;
        }
        public static void SolarShieldBuff2(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = BuffID.SolarShield2;

            if (player == null) return;
            if (buffStatus != BuffStatus.IsPermaBuffed) return;

            var modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
            modPlayer.alwaysPermanent[BuffID.SolarShield2] = false;
            modPlayer.alwaysPermanent[BuffID.SolarShield3] = true;
        }
        public static void BeetleDamageBuff1(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = BuffID.BeetleMight1;
            
            if (player == null) return;
            if (buffStatus != BuffStatus.IsPermaBuffed) return;

            var modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
            modPlayer.alwaysPermanent[BuffID.BeetleMight1] = false;
            modPlayer.alwaysPermanent[BuffID.BeetleMight3] = true;
        }
        public static void BeetleDamageBuff2(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = BuffID.BeetleMight2;

            if (player == null) return;
            if (buffStatus != BuffStatus.IsPermaBuffed) return;

            var modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
            modPlayer.alwaysPermanent[BuffID.BeetleMight2] = false;
            modPlayer.alwaysPermanent[BuffID.BeetleMight3] = true;
        }
#endregion

        #region CalamityMod
        public static void CalamityPermaRageMode(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.Calamity.rageBuffType : 0;
            if (player == null)
                return;

            // NeverBuff is already properly applied -> permabuff logic needed
            if (buffStatus != BuffStatus.IsPermaBuffed)
                return;

            // This is needed to properly modify the instance values. Otherwise null reference exeption thrown.
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.Calamity.ModName))
                return; // There was an error setting up reflection

            // Sets your rage to max rage every frame - this theoretically makes rage mode last forever
            // I'm not familiar enough with calamity to know whether or not there are other considerations to take into account however.
            PrivateAccess.Calamity.rage = PrivateAccess.Calamity.maxRage;
        }
        public static void CalamityPermaAquaticHeartBuff(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.Calamity.aquaticHeartBuffType : 0;
            if (player == null) 
                return;

            if (buffStatus != BuffStatus.IsPermaBuffed) return;
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.Calamity.ModName)) return;

            // This was originally controlled by the accessory, now if it's permabuffed it will remain true
            PrivateAccess.Calamity.aquaticHeart = true;
        }
        public static void CalamityPermaHasteBuff(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.Calamity.aquaticHeartBuffType : 0;
            if (player == null) 
                return;

            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.Calamity.ModName))
                return;

            if (buffStatus == BuffStatus.IsPermaBuffed)
            {
                // The code has a max haste level of 3
                PrivateAccess.Calamity.hasteLevel = 3;
            }
            else if (buffStatus == BuffStatus.IsNeverBuffed)
            {
                // Immediately set haste level to 0 if never buffed.
                PrivateAccess.Calamity.hasteLevel = 0;
            }
        }
        public static void CalamityPermaProfanedCrystalBuff(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.Calamity.aquaticHeartBuffType : 0;
            if (player == null)
                return;

            if (buffStatus != BuffStatus.IsPermaBuffed)
                return;

            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.Calamity.ModName))
                return;

            PrivateAccess.Calamity.profanedCrystal = true;
        }
        public static void CalamityAbyssalDivingSuitBuff(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.Calamity.abyssalDivingSuitBuffType : 0;
            if (player == null)
                return;

            if (buffStatus != BuffStatus.IsPermaBuffed)
                return;

            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.Calamity.ModName))
                return;

            PrivateAccess.Calamity.abyssalDivingSuit = true;
        }
        public static void CalamityPopoBuff(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.Calamity.popoBuffType : 0;

            if (player == null) return;
            if (buffStatus != BuffStatus.IsPermaBuffed) return;
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.Calamity.ModName)) return;

            PrivateAccess.Calamity.snowman = true;
        }
        public static void CalamityPopoNoselessBuff(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.Calamity.popoNoselessBuffType : 0;

            if (player == null) return;
            if (buffStatus != BuffStatus.IsPermaBuffed) return;
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.Calamity.ModName)) return;

            PrivateAccess.Calamity.snowman = true;
            PrivateAccess.Calamity.snowmanNoseless = true;
        }
        #endregion

    }

    /// <summary>
    /// These methods will be called prior to Buff.Update. For proper integration please follow the instruction below.
    /// Every function should have the return type of void, be static, and return after buffType is set if "player" == null.
    /// If the function is meant for a modbuff, return a bufftype of 0 if the mod is not loaded
    /// Do not write any functions that don't directly follow the function signature of void(Player, ref int, int, out int) in the class. 
    /// This class is reserved for functions that are set to become delegates of the above function type. Nothing else should be present.
    /// If you're extending compatibility of your mod to this mod, the process is simple. Create a public partial class with the same type name 
    /// as the one below, enclose it within a 'PermaBuffs' namespace, and define functions with the above specifications. 
    /// My mod will auto recognize it and add the buffType to the list of hooks.
    /// </summary>
    public partial class PermaBuffsPostBuffUpdateHooks
    {
        #region Vanilla
        public static void PotionSickness(Player p, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            buffType = BuffID.PotionSickness;
            if (p == null)
                return;

            // If potion sickness is neverbuffed get rid of any heal cooldown
            if (buffStatus == BuffStatus.IsNeverBuffed)
                p.potionDelay = 0;
            else if (buffStatus == BuffStatus.IsPermaBuffed) // Player can never heal unless the permabuff is disabled
                p.potionDelay = Math.Max(p.potionDelay, 2);
        }

        public static void PermaTitaniumStorm(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = BuffID.TitaniumStorm;

            if (player == null) return;
            if (buffStatus != BuffStatus.IsPermaBuffed) return;

            player.onHitTitaniumStorm = true;
        }
        #endregion

        #region tsorcRevamp

        public static void NeverBuffCurse(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            // Use the helper class to set the buffType
            buffType = PrivateAccess.TsorcRevamp.curseBuffType;

            if (player == null)
                return;

            // Custom logic only applies during neverbuff
            if (buffStatus != BuffStatus.IsNeverBuffed)
                return;

            // This is needed to properly modify the instance values. Otherwise null reference exeption thrown.
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.TsorcRevamp.ModName))
                return; // There was an error getting the values using reflection

            PrivateAccess.TsorcRevamp.curseActive = false;
            PrivateAccess.TsorcRevamp.curseLevel = 0;
        }
        public static void NeverBuffPowerfulCurse(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            // Use the helper class to set the buffType
            buffType = PrivateAccess.TsorcRevamp.powerfulCurseBuffType;

            if (player == null)
                return;

            // Custom logic only applies during neverbuff
            if (buffStatus != BuffStatus.IsNeverBuffed)
                return;

            // This is needed to properly modify the instance values. Otherwise null reference exeption thrown.
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.TsorcRevamp.ModName))
                return; // There was an error getting the values using reflection

            PrivateAccess.TsorcRevamp.powerfulCurseActive = false;
            PrivateAccess.TsorcRevamp.powerfulCurseLevel = 0;
        }
        // Curse buildup shouldn't exist if the curse is neverbuffed
        public static void RemoveCurseBuildup(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            buffType = PrivateAccess.TsorcRevamp.curseBuildupBuffType;

            if (player == null) return;
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.TsorcRevamp.ModName)) return;

            PermaBuffsPlayer modPlayer = player.GetModPlayer<PermaBuffsPlayer>();

            if (modPlayer.neverPermanent[PrivateAccess.TsorcRevamp.curseBuffType])
            {
                player.DelBuff(buffSlotOnPlayer);
                buffSlotOnPlayer--;
                PrivateAccess.TsorcRevamp.curseLevel = 0;
            }
        }
        // PowerfulCurse buildup shouldn't exist if the powerfulCurse is neverbuffed
        public static void RemovePowerfulCurseBuildup(Player player, ref int buffSlotOnPlayer, int buffStatus, out int buffType)
        {
            buffType = PrivateAccess.TsorcRevamp.powerfulCurseBuildupBuffType;

            if (player == null) return;
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.TsorcRevamp.ModName)) return;

            PermaBuffsPlayer modPlayer = player.GetModPlayer<PermaBuffsPlayer>();

            if (modPlayer.neverPermanent[PrivateAccess.TsorcRevamp.powerfulCurseBuffType])
            {
                player.DelBuff(buffSlotOnPlayer);
                buffSlotOnPlayer--;
                PrivateAccess.TsorcRevamp.powerfulCurseLevel = 0;
            }
        }
        // Neverbuff already works functionally, this really just updates the display lol. It looking less buggy is what counts
        public static void NeverBuffFracturingArmor(Player player, ref int buffSlot, int status, out int buffType)
        {
            buffType = PrivateAccess.TsorcRevamp.fracturingArmorBuffType;

            if (player == null) return;
            if (status != BuffStatus.IsNeverBuffed) return;
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.TsorcRevamp.ModName)) return;

            PrivateAccess.TsorcRevamp.fracturingArmor = 0;
        }

        public static void PermaPhoenixRebirthBuff(Player player, ref int buffSlot, int status, out int buffType)
        {
            buffType = PrivateAccess.TsorcRevamp.phoenixRebirthBuffType;

            if (player == null) return;
            if (status != BuffStatus.IsPermaBuffed) return;
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.TsorcRevamp.ModName)) return;

            PrivateAccess.TsorcRevamp.phoenixSkull = true;
        }

        public static void DelPhoenixRebirthCooldown(Player player, ref int buffSlot, int buffStatus, out int buffType)
        {
            buffType = PrivateAccess.TsorcRevamp.phoenixRebirthCooldownBuffType;

            if (player == null) return;

            var modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
            if (!modPlayer.alwaysPermanent[PrivateAccess.TsorcRevamp.phoenixRebirthBuffType]) return;

            player.DelBuff(buffSlot);
            buffSlot--;
        }

        public static void PermaTitaniumStormShunpoBuff(Player player, ref int buffSlot, int status, out int buffType)
        {
            buffType = PrivateAccess.TsorcRevamp.titaniumStormShunpoBuffType;

            if (player == null) return;
            if (status != BuffStatus.IsPermaBuffed) return;
            if (!PrivateAccess.TrySetupPlayerInstance(player, PrivateAccess.TsorcRevamp.ModName)) return;

            // Shunpo is a part of the buff in tsorcRevamp, the player can never buff shunpo cooldown manually if they want no cooldowns on the blink
            PrivateAccess.TsorcRevamp.shunpo = true;
        }

        #endregion
    }

    public partial class PermaBuffsPostPlayerUpdateHooks
    {
        #region Vanilla

        #endregion
    }

    // This class is where the magic happens. It uses reflection to get accessors to otherwise private variables and compiles them so it's fast.
    // This kind of hacky functionality is only suitable if you don't have access to the source code of another mod.
    // Its important that this helper class is static so it can be used freely in the Post/PreBuffUpdateLoops
    public class PrivateAccess
    {
        internal const string keyModPlayerArray = "modPlayers";
        internal static Dictionary<string, FieldAccessor> vars;
        // I don't need to have this class since I'm a dev for tsorcRevamp, but the next mod update hasn't been pushed yet so...
        // I'll leave it here until the next update.
        public class TsorcRevamp
        {
            public const string ModName = "tsorcRevamp";
            internal const string playerClass = "tsorcRevampPlayer";
            internal const string keyCurseActive = "CurseActive";
            internal const string keyPowerfulCurseActive = "powerfulCurseActive";
            internal const string keyCurseLevel = "CurseLevel";
            internal const string keyPowerfulCurseLevel = "PowerfulCurseLevel";
            internal const string keyFracturingArmor = "FracturingArmor";
            internal const string keyPhoenixSkull = "PhoenixSkull";
            internal const string keyShunpo = "Shunpo";

            // These let you access the variables within the other mod. They require an instance to access mod data by string. Must call SetupPlayerInstance(Player) before use
            // Otherwise a null reference will be thrown. These properties are unsafe to access before setting up the player with the current instance.
            public static bool curseActive { get { return (bool)vars[keyCurseActive].Get(myPlayer); } set { vars[keyCurseActive].Set(myPlayer, value); } }
            public static bool powerfulCurseActive { get { return (bool)vars[keyPowerfulCurseActive].Get(myPlayer); } set { vars[keyPowerfulCurseActive].Set(myPlayer, value); } }
            public static int curseLevel { get { return (int)vars[keyCurseLevel].Get(myPlayer); } set { vars[keyCurseLevel].Set(myPlayer, value); } }
            public static int powerfulCurseLevel { get { return (int)vars[keyPowerfulCurseLevel].Get(myPlayer); } set { vars[keyPowerfulCurseLevel].Set(myPlayer, value); } }
            public static int fracturingArmor { get { return (int)vars[keyFracturingArmor].Get(myPlayer); } set { vars[keyFracturingArmor].Set(myPlayer, value); } }
            public static bool phoenixSkull { get { return (bool)vars[keyPhoenixSkull].Get(myPlayer); } set { vars[keyPhoenixSkull].Set(myPlayer, value); } }
            public static bool shunpo { get { return (bool)vars[keyShunpo].Get(myPlayer); } set { vars[keyShunpo].Set(myPlayer, value); } }

            internal static ModPlayer myPlayer;
            internal static Type playerType;

            internal static int curseBuffTypeCached = -1;
            internal static int powerfulCurseBuffTypeCached = -1;
            internal static int curseBuildupBuffTypeCached = -1;
            internal static int powerfulCurseBuildupBuffTypeCached = -1;
            internal static int fracturingArmorBuffTypeCached = -1;
            internal static int phoenixRebirthBuffTypeCached = -1;
            internal static int phoenixRebirthCooldownBuffTypeCached = -1;
            internal static int titaniumStormShunpoBuffTypeCached = -1;

            // The buff type properties are safe to acess before setting up the player instance - they will return 0 if the mod isn't loaded.
            public static int curseBuffType
            {
                get
                {
                    if (curseBuffTypeCached == -1)
                        CacheLoadedModBuffTypes();

                    return curseBuffTypeCached;
                }
            }
            public static int powerfulCurseBuffType
            {
                get
                {
                    if (powerfulCurseBuffTypeCached == -1)
                        CacheLoadedModBuffTypes();

                    return powerfulCurseBuffTypeCached;
                }
            }
            public static int curseBuildupBuffType
            {
                get
                {
                    if (curseBuildupBuffTypeCached == -1)
                        CacheLoadedModBuffTypes();

                    return curseBuildupBuffTypeCached;
                }
            }
            public static int powerfulCurseBuildupBuffType
            {
                get
                {
                    if (powerfulCurseBuildupBuffTypeCached == -1)
                        CacheLoadedModBuffTypes();

                    return powerfulCurseBuildupBuffTypeCached;
                }
            }
            public static int fracturingArmorBuffType
            {
                get
                {
                    if (fracturingArmorBuffTypeCached == -1)
                        CacheLoadedModBuffTypes();
                    return fracturingArmorBuffTypeCached;
                }
            }
            public static int phoenixRebirthBuffType
            {
                get
                {
                    if (phoenixRebirthBuffTypeCached == -1)
                        CacheLoadedModBuffTypes();
                    return phoenixRebirthBuffTypeCached;
                }
            }
            public static int phoenixRebirthCooldownBuffType
            {
                get
                {
                    if (phoenixRebirthCooldownBuffTypeCached == -1)
                        CacheLoadedModBuffTypes();
                    return phoenixRebirthCooldownBuffTypeCached;
                }
            }
            public static int titaniumStormShunpoBuffType
            {
                get
                {
                    if (titaniumStormShunpoBuffTypeCached == -1)
                        CacheLoadedModBuffTypes();
                    return titaniumStormShunpoBuffTypeCached;
                }
            }
        }
        public class Calamity
        {
            public const string ModName = "CalamityMod";
            internal const string playerClass = "CalamityPlayer";

            internal const string keyRage = "rage";
            internal const string keyRageMax = "rageMax";
            internal const string keyAquaticHeart = "aquaticHeart";
            internal const string keyHasteLevel = "hasteLevel";
            internal const string keyProfanedCrystal = "profanedCrystal";
            internal const string keyAbyssalDivingSuit = "abyssalDivingSuit";
            internal const string keySnowman = "snowman";
            internal const string keySnowmanNoseless = "snowmanNoseless";
            public static float rage { get { return (float)vars[keyRage].Get(myPlayer); } set { vars[keyRage].Set(myPlayer, value); } }
            public static float maxRage { get { return (float)vars[keyRageMax].Get(myPlayer); } set { vars[keyRageMax].Set(myPlayer, value); } }
            public static bool aquaticHeart { get { return (bool)vars[keyAquaticHeart].Get(myPlayer); } set { vars[keyAquaticHeart].Set(myPlayer, value); } }
            public static int hasteLevel { get { return (int)vars[keyHasteLevel].Get(myPlayer); } set { vars[keyHasteLevel].Set(myPlayer, value); } }
            public static bool profanedCrystal { get { return (bool)vars[keyProfanedCrystal].Get(myPlayer); } set { vars[keyProfanedCrystal].Set(myPlayer, value); } }
            public static bool abyssalDivingSuit { get { return (bool)vars[keyAbyssalDivingSuit].Get(myPlayer); } set { vars[keyAbyssalDivingSuit].Set(myPlayer, value); } }
            public static bool snowman { get { return (bool)vars[keySnowman].Get(myPlayer); } set { vars[keySnowman].Set(myPlayer, value); } }
            public static bool snowmanNoseless { get { return (bool)vars[keySnowmanNoseless].Get(myPlayer); } set { vars[keySnowmanNoseless].Set(myPlayer, value); } }

            internal static ModPlayer myPlayer;
            internal static Type playerType;

            internal static int rageBuffTypeCache = -1;
            internal static int aquaticHeartBuffTypeCache = -1;
            internal static int hasteBuffTypeCache = -1;
            internal static int profanedCrystalBuffTypeCache = -1;
            internal static int abyssalDivingSuitBuffTypeCache = -1;
            internal static int popoBuffTypeCache = -1;
            internal static int popoNoselessBuffTypeCache = -1;
            public static int rageBuffType
            {
                get
                {
                    if (rageBuffTypeCache == -1)
                        CacheLoadedModBuffTypes();
                    return rageBuffTypeCache;
                }
            }
            public static int aquaticHeartBuffType
            {
                get
                {
                    if (aquaticHeartBuffTypeCache == -1)
                        CacheLoadedModBuffTypes();
                    return aquaticHeartBuffTypeCache;
                }
            }
            public static int hasteBuffType
            {
                get
                {
                    if (hasteBuffTypeCache == -1)
                        CacheLoadedModBuffTypes();
                    return hasteBuffTypeCache;
                }
            }
            public static int profanedCrystalBuffType
            {
                get
                {
                    if (profanedCrystalBuffTypeCache == -1)
                        CacheLoadedModBuffTypes();
                    return profanedCrystalBuffTypeCache;
                }
            }
            public static int abyssalDivingSuitBuffType
            {
                get
                {
                    if (abyssalDivingSuitBuffTypeCache == -1)
                        CacheLoadedModBuffTypes();
                    return abyssalDivingSuitBuffTypeCache;
                }
            }
            public static int popoBuffType
            {
                get
                {
                    if (popoBuffTypeCache == -1)
                        CacheLoadedModBuffTypes();
                    return popoBuffTypeCache;
                }
            }
            public static int popoNoselessBuffType
            {
                get
                {
                    if (popoNoselessBuffTypeCache == -1)
                        CacheLoadedModBuffTypes();
                    return popoNoselessBuffTypeCache;
                }
            }

        }

        // This function sets up the player accessors properly and also sets the instance.
        // Must be called before using any accessors. Returns whether or not it was sucessful.
        // Accessor code depends on this returning true.
        public static bool TrySetupPlayerInstance(Player player, string modName)
        {
            // Only run this code once. The field acessors need to compile the first time.
            if (vars == null)
            {
                vars = new Dictionary<string, FieldAccessor>();

                // This is needed to get any mod specific variables, if it fails just return.
                try { vars.Add(keyModPlayerArray, new FieldAccessor(typeof(Player), keyModPlayerArray)); }
                catch { vars = null; return false; }

                // Set up the field accessors of the supported mods. 
                // Set mod access keys returns null if the mod isn't loaded or any of the accessors fail to compile
                TsorcRevamp.playerType = SetModAccessKeys(TsorcRevamp.ModName, TsorcRevamp.playerClass,
                [
                    TsorcRevamp.keyCurseActive, TsorcRevamp.keyPowerfulCurseActive, TsorcRevamp.keyCurseLevel,
                    TsorcRevamp.keyPowerfulCurseLevel, TsorcRevamp.keyFracturingArmor, TsorcRevamp.keyPhoenixSkull,
                    TsorcRevamp.keyShunpo
                ]);

                Calamity.playerType = SetModAccessKeys(Calamity.ModName, Calamity.playerClass,
                [
                    Calamity.keyRage, Calamity.keyRageMax, Calamity.keyAquaticHeart, Calamity.keyHasteLevel, 
                    Calamity.keyProfanedCrystal, Calamity.keyAbyssalDivingSuit, Calamity.keySnowman, Calamity.keySnowmanNoseless
                ]);
            }

            int modPlayerIndex;

            if (modName == TsorcRevamp.ModName)
            {
                try
                {
                    modPlayerIndex = GetPlayerIndex(player, TsorcRevamp.playerType);
                    TsorcRevamp.myPlayer = ((ModPlayer[])vars[keyModPlayerArray].Get(player))[modPlayerIndex];
                    return true;
                }
                catch { return false; }
            }
            else if (modName == Calamity.ModName)
            {
                try
                {
                    modPlayerIndex = GetPlayerIndex(player, Calamity.playerType);
                    Calamity.myPlayer = ((ModPlayer[])vars[keyModPlayerArray].Get(player))[modPlayerIndex];
                    return true;
                }
                catch { return false; }
            }

            return false;
        }

        // Sets up all the supported buff types
        // Called when any buffType is read for the first time
        private static void CacheLoadedModBuffTypes()
        {
            // The story of red cloud
            if (!ModContent.TryFind(TsorcRevamp.ModName, "Curse", out ModBuff curseBuff))
                TsorcRevamp.curseBuffTypeCached = 0;
            else
                TsorcRevamp.curseBuffTypeCached = curseBuff.Type;

            if (!ModContent.TryFind(TsorcRevamp.ModName, "PowerfulCurse", out ModBuff powerfulCurseBuff))
                TsorcRevamp.powerfulCurseBuffTypeCached = 0;
            else
                TsorcRevamp.powerfulCurseBuffTypeCached = powerfulCurseBuff.Type;

            if (!ModContent.TryFind(TsorcRevamp.ModName, "CurseBuildup", out ModBuff curseBuildupBuff))
                TsorcRevamp.curseBuildupBuffTypeCached = 0;
            else
                TsorcRevamp.curseBuildupBuffTypeCached = curseBuildupBuff.Type;

            if (!ModContent.TryFind(TsorcRevamp.ModName, "PowerfulCurseBuildup", out ModBuff powerfulCurseBuildupBuff))
                TsorcRevamp.powerfulCurseBuildupBuffTypeCached = 0;
            else
                TsorcRevamp.powerfulCurseBuildupBuffTypeCached = powerfulCurseBuildupBuff.Type;

            if (!ModContent.TryFind(TsorcRevamp.ModName, "FracturingArmor", out ModBuff fracturingArmorBuff))
                TsorcRevamp.fracturingArmorBuffTypeCached = 0;
            else
                TsorcRevamp.fracturingArmorBuffTypeCached = fracturingArmorBuff.Type;

            if (!ModContent.TryFind(TsorcRevamp.ModName, "PhoenixRebirthBuff", out ModBuff phoenixRebirthBuff))
                TsorcRevamp.phoenixRebirthBuffTypeCached = 0;
            else
                TsorcRevamp.phoenixRebirthBuffTypeCached = phoenixRebirthBuff.Type;

            if (!ModContent.TryFind(TsorcRevamp.ModName, "PhoenixRebirthCooldown", out ModBuff phoenixRebirthCooldown))
            {
                TsorcRevamp.titaniumStormShunpoBuffTypeCached = 0; // Only run shunpo code if mod is loaded
                TsorcRevamp.phoenixRebirthCooldownBuffTypeCached = 0;
            }
            else
            {
                TsorcRevamp.titaniumStormShunpoBuffTypeCached = BuffID.TitaniumStorm;
                TsorcRevamp.phoenixRebirthCooldownBuffTypeCached = phoenixRebirthCooldown.Type;
            }


            // Calamity mod
            if (!ModContent.TryFind(Calamity.ModName, "RageMode", out ModBuff rageBuff))
                Calamity.rageBuffTypeCache = 0;
            else
                Calamity.rageBuffTypeCache = rageBuff.Type;

            if (!ModContent.TryFind(Calamity.ModName, "AquaticHeartBuff", out ModBuff auqaticHeartBuff))
                Calamity.aquaticHeartBuffTypeCache = 0;
            else
                Calamity.aquaticHeartBuffTypeCache = auqaticHeartBuff.Type;

            if (!ModContent.TryFind(Calamity.ModName, "Haste", out ModBuff hasteBuff))
                Calamity.hasteBuffTypeCache = 0;
            else
                Calamity.hasteBuffTypeCache = hasteBuff.Type;

            if (!ModContent.TryFind(Calamity.ModName, "ProfanedCrystalBuff", out ModBuff profanedCrystalBuff))
                Calamity.profanedCrystalBuffTypeCache = 0;
            else
                Calamity.profanedCrystalBuffTypeCache = profanedCrystalBuff.Type;

            if (!ModContent.TryFind(Calamity.ModName, "AbyssalDivingSuitBuff", out ModBuff AbyssalDivingSuitBuff))
                Calamity.abyssalDivingSuitBuffTypeCache = 0;
            else
                Calamity.abyssalDivingSuitBuffTypeCache = AbyssalDivingSuitBuff.Type;

            if (!ModContent.TryFind(Calamity.ModName, "PopoBuff", out ModBuff popoBuff))
                Calamity.popoBuffTypeCache = 0;
            else
                Calamity.popoBuffTypeCache = popoBuff.Type;

            if (!ModContent.TryFind(Calamity.ModName, "PopoNoselessBuff", out ModBuff popoNoselessBuff))
                Calamity.popoNoselessBuffTypeCache = 0;
            else
                Calamity.popoNoselessBuffTypeCache = popoNoselessBuff.Type;
        }

        // Called by SetupPlayer instance with different mods.
        // Sets up the accessors properly so they can be quickly used to get private variables.
        private static Type SetModAccessKeys(string modName, string classTypeStr, string[] accessorKeys)
        {
            // If the mod didn't load return null
            if (!ModLoader.TryGetMod(modName, out Mod mod))
                return null;

            // Get all the types within the mod's loaded assembly
            foreach (Type pendingType in AssemblyManager.GetLoadableTypes(mod.Code))
            {
                if (pendingType.Name != classTypeStr)
                    continue;

                List<string> errors = new List<string>();

                for (int i = 0; i < accessorKeys.Length; i++)
                {
                    // This is what it's called in the mod assembly
                    string accessKey = accessorKeys[i];

                    try { vars.Add(accessKey, new FieldAccessor(pendingType, accessKey)); }
                    catch { errors.Add("Could not find " + accessKey + " within the loaded " + classTypeStr + " type."); }
                }

                // There was an exception, log it and throw out the bad keys
                if (errors.Count > 0)
                {
                    var logger = ModLoader.GetMod("PermaBuffs").Logger;
                    foreach (string error in errors) { logger.Error(error); }
                    foreach (string name in accessorKeys) { vars.Remove(name); }
                }
                else
                {
                    // The acessors were set up properly
                    return pendingType;
                }
            }

            // The given type was not found within the mod's assembly
            return null;
        }

        // Gets the correct player index present in Player.modPlayers[] for the modPlayer we're trying to access. 
        private static int GetPlayerIndex(Player player, Type modPlayerType)
        {
            bool found = false;
            int index = 0;

            if (modPlayerType == null)
                throw new ArgumentNullException();

            // Dynamically set the referenced instance of the player.
            // Done by acessing the private array attached to each player instance containing their ModPlayer variants
            foreach (ModPlayer p in (ModPlayer[])vars[keyModPlayerArray].Get(player))
            {
                Type pType = p.GetType();

                if (pType.Name == modPlayerType.Name)
                {
                    found = true;
                    break;
                }

                index++;
            }

            // For this to fail, something went horribly wrong. 
            if (!found)
                throw new KeyNotFoundException();

            return index;
        }

    }

    /// <summary>
    /// Courtesy of Darrel Lee on stack overflow - this class lets you access another mod's variables effeciently using Expression.compile() with reflection.
    /// </summary>
    public class FieldAccessor
    {
        private static readonly ParameterExpression fieldParameter = Expression.Parameter(typeof(object));
        private static readonly ParameterExpression ownerParameter = Expression.Parameter(typeof(object));

        public FieldAccessor(Type type, string fieldName)
        {
            var field = type.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) throw new ArgumentException();

            var fieldExpression = Expression.Field(
                Expression.Convert(ownerParameter, type), field);

            Get = Expression.Lambda<Func<object, object>>(
                Expression.Convert(fieldExpression, typeof(object)),
                ownerParameter).Compile();

            Set = Expression.Lambda<Action<object, object>>(
                Expression.Assign(fieldExpression,
                    Expression.Convert(fieldParameter, field.FieldType)),
                ownerParameter, fieldParameter).Compile();
        }

        public Func<object, object> Get { get; }
        public Action<object, object> Set { get; }
    }
}
