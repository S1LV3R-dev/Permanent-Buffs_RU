using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq.Expressions;
using System.Collections.Generic;
using PermaBuffs;
using Terraria.ModLoader.Core;
using tsorcRevamp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Terraria.Map;
using System.ComponentModel;

namespace PermaBuffs
{
    /// <summary>
    /// A function delegate class that gets called during Post/PreUpdateBuffs on the player. For running custom code that correctly implements certain perma/neverbuffs
    /// </summary>
    /// <param name="player">The Player instance who has the buff. This will be called with a null reference the first time the hook is run.</param>
    /// <param name="buffSlotOnPlayer"> The index of the buff in player.buffType and player.buffTime</param>
    /// <param name="isPermaBuffed">Called with true if the buff is permabuffed, false if it's neverbuffed</param>
    /// <param name="buffType">The buffType this hook is for. This needs to be set in the function before the player null reference check.</param>
    public delegate void BuffHook(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType);

    /// <summary>
    /// These methods will be called post Buff.Update. For proper integration please follow the instruction below.
    /// Every function should have the return type of void, be static, and return after buffType is set if "player" == null.
    /// If the function is meant for a modbuff, return a bufftype of 0 if the mod is not loaded
    /// Do not write any functions that don't directly follow the function signature of void(Player, int, bool, out int, bool) in the class. 
    /// This class is reserved for functions that are set to become delegates of the above function type. Nothing else should be present.
    /// If you're extending compatibility of your mod to this mod, the process is simple. Create a public partial class with the same type name 
    /// as the one below, enclose it within a 'PermaBuffs' namespace, and define functions with the above specifications. 
    /// My mod will auto recognize it and add the buffType to the list of hooks.
    /// </summary>
    public partial class PermaBuffsPreBuffUpdateHooks
    {
        // Commented out for next release because I'm not sure the code works or not.
        public static void CalamityRageMode(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.Calamity.rageBuffType : 0;
            if (player == null)
                return;

            // NeverBuff is already properly applied -> permabuff logic needed
            if (!isPermaBuffed)
                return;

            // This is needed to properly modify the instance values. Otherwise null reference exeption thrown.
            if (!PrivateAccess.TrySetupPlayerInstances(player, "CalamityMod"))
                return; // There was an error setting up reflection

            // Sets your rage to max rage every frame - this theoretically makes rage mode last forever
            PrivateAccess.Calamity.rage = PrivateAccess.Calamity.maxRage;
        }
    }

    /// <summary>
    /// These methods will be called post Buff.Update. For proper integration please follow the instruction below.
    /// Every function should have the return type of void, be static, and return after buffType is set if "player" == null.
    /// If the function is meant for a modbuff, return a bufftype of 0 if the mod is not loaded
    /// Do not write any functions that don't directly follow the function signature of void(Player, int, bool, out int, bool) in the class. 
    /// This class is reserved for functions that are set to become delegates of the above function type. Nothing else should be present.
    /// If you're extending compatibility of your mod to this mod, the process is simple. Create a public partial class with the same type name 
    /// as the one below, enclose it within a 'PermaBuffs' namespace, and define functions with the above specifications. 
    /// My mod will auto recognize it and add the buffType to the list of hooks.
    /// </summary>
    public partial class PermaBuffsPostBuffUpdateHooks
    {
        public static void PotionSickness(Player p, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
            buffType = BuffID.PotionSickness;
            if (p == null)
                return;

            // If potion sickness is neverbuffed get rid of any heal cooldown
            if (!isPermaBuffed)
                p.potionDelay = 0;
            else // Player can never heal unless the permabuff is disabled
                p.potionDelay = Math.Max(p.potionDelay, 2);
        }
       
        public static void NeverBuffCurse(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
            // Use the helper class to set the buffType
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.TsorcRevamp.curseBuffType : 0;

            if (player == null)
                return;

            // Custom logic only applies during neverbuff
            if (isPermaBuffed)
                return;

            // This is needed to properly modify the instance values. Otherwise null reference exeption thrown.
            if (!PrivateAccess.TrySetupPlayerInstances(player, "tsorcRevamp"))
                return; // There was an error getting the values using reflection

            PrivateAccess.TsorcRevamp.curseActive = false;
        }
        public static void NeverBuffPowerfulCurse(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
            // Use the helper class to set the buffType
            buffType = PermaBuffsConfig.instance.experimentalChanges ? PrivateAccess.TsorcRevamp.powerfulCurseBuffType : 0;

            if (player == null)
                return;

            // Custom logic only applies during neverbuff
            if (isPermaBuffed)
                return;

            // This is needed to properly modify the instance values. Otherwise null reference exeption thrown.
            if (!PrivateAccess.TrySetupPlayerInstances(player, "tsorcRevamp"))
                return; // There was an error getting the values using reflection

            PrivateAccess.TsorcRevamp.powerfulCurseActive = false; 
        }
    }

    // This class is where the magic happens. It uses reflection to get accessors to otherwise private variables and compiles them so it's fast.
    // This kind of hacky functionality is only suitable if you don't have access to the source code of another mod.
    // Its important that this helper class is static so it can be used freely in the Post/PreBuffUpdateLoops
    public class PrivateAccess
    {
        internal static Dictionary<string, FieldAccessor> vars;
        // I don't need to have this class since I'm a dev for tsorcRevamp, but the next mod update hasn't been pushed yet so...
        // I'll leave it here until the next update.
        public class TsorcRevamp
        {
            // These let you acess the variables within the other mod. They require an instance to access mod data by string. Must call SetupPlayerInstance(Player) before use
            public static bool curseActive { get { return (bool)vars["modPlayer.curseActive"].Get(myPlayer); } set { vars["modPlayer.curseActive"].Set(myPlayer, value); } }
            public static bool powerfulCurseActive { get { return (bool)vars["modPlayer.powerfulCurseActive"].Get(myPlayer); } set { vars["modPlayer.powerfulCurseActive"].Set(myPlayer, value); } }
            internal static ModPlayer myPlayer;
            internal static Type playerType;
            internal static int cachedCurseBuffType = -1;
            internal static int cachedPowerfulCurseBuffType = -1;
            public static int curseBuffType
            {
                get
                {
                    if (cachedCurseBuffType == -1)
                        CacheInternalBuffTypes();

                    return cachedCurseBuffType;
                }
                private set { cachedCurseBuffType = value; }
            }
            public static int powerfulCurseBuffType
            {
                get
                {
                    if (cachedPowerfulCurseBuffType == -1)
                        CacheInternalBuffTypes();

                    return cachedPowerfulCurseBuffType;
                }
                private set { cachedPowerfulCurseBuffType = value; }
            }
        }
        public class Calamity
        {
            public static float rage { get { return (float)vars["modPlayer.rage"].Get(myPlayer); } set { vars["modPlayer.rage"].Set(myPlayer, value); } }
            public static float maxRage { get { return (float)vars["modPlayer.rageMax"].Get(myPlayer); } set { vars["modPlayer.rageMax"].Set(myPlayer, value); } }
            internal static ModPlayer myPlayer;
            internal static Type playerType;
            internal static int rageBuffTypeCache = -1;
            public static int rageBuffType
            {
                get
                {
                    if (rageBuffTypeCache == -1)
                        CacheInternalBuffTypes();
                    return rageBuffTypeCache;
                }
                private set { rageBuffTypeCache = value; }
            }
        }

        // This function sets up the player accessors properly and also sets the instance.
        // Must be called before using any accessors. Returns whether or not it was sucessful.
        // Accessor code depends on this returning true.
        public static bool TrySetupPlayerInstances(Player player, string modName)
        {
            // Only run this code once. The field acessors need to compile the first time.
            if (vars == null)
            {
                vars = new Dictionary<string, FieldAccessor>();

                // This is needed to get any mod specific variables, if it fails just return.
                try { vars.Add("playerArr", new FieldAccessor(typeof(Player), "modPlayers")); }
                catch { vars = null; return false; }

                // Set up the field accessors of the supported mods. 
                // Set mod access keys returns null if the mod isn't loaded.
                TsorcRevamp.playerType = SetModAccessKeys("tsorcRevamp", "tsorcRevampPlayer",
                    ["modPlayer.curseActive", "modPlayer.powerfulCurseActive"],
                    ["CurseActive", "powerfulCurseActive"]
                    );

                Calamity.playerType = SetModAccessKeys("CalamityMod", "CalamityPlayer",
                    ["modPlayer.rage", "modPlayer.rageMax"],
                    ["rage", "rageMax"]
                    );
            }

            int modPlayerIndex;

            if (modName == "tsorcRevamp")
            {
                try
                {
                    modPlayerIndex = GetPlayerIndex(player, TsorcRevamp.playerType);
                    TsorcRevamp.myPlayer = ((ModPlayer[])vars["playerArr"].Get(player))[modPlayerIndex];
                    return true;
                }
                catch { return false; }
            }
            else if (modName == "CalamityMod")
            {
                try
                {
                    modPlayerIndex = GetPlayerIndex(player, Calamity.playerType);
                    Calamity.myPlayer = ((ModPlayer[])vars["playerArr"].Get(player))[modPlayerIndex];
                    return true;
                }
                catch { return false; }
            }

            return false;
        }

        // Sets up all the supported buff types
        // Called when any buffType is read for the first time
        private static void CacheInternalBuffTypes()
        {
            if (!ModContent.TryFind("tsorcRevamp", "Curse", out ModBuff curseBuff))
                TsorcRevamp.cachedCurseBuffType = 0;
            else
                TsorcRevamp.cachedCurseBuffType = curseBuff.Type;

            if (!ModContent.TryFind("tsorcRevamp", "PowerfulCurse", out ModBuff powerfulCurseBuff))
                TsorcRevamp.cachedPowerfulCurseBuffType = 0;
            else
                TsorcRevamp.cachedPowerfulCurseBuffType = powerfulCurseBuff.Type;

            if (!ModContent.TryFind("CalamityMod", "RageMode", out ModBuff rageBuff))
                Calamity.rageBuffTypeCache = 0;
            else
                Calamity.rageBuffTypeCache = rageBuff.Type;
        }

        // Called by SetupPlayer instance with different mods.
        // Sets up the accessors properly so they can be quickly used to get private variables.
        private static Type SetModAccessKeys(string modName, string classTypeStr, string[] variableNames, string[] accessorKeys)
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

                for (int i = 0; i < variableNames.Length && i < accessorKeys.Length; i++)
                {
                    // This is what it's called in my code
                    string name = variableNames[i];
                    // This is what it's called in the mod assembly
                    string accessKey = accessorKeys[i];

                    try { vars.Add(name, new FieldAccessor(pendingType, accessKey)); }
                    catch { errors.Add("Could not find " + name + " within the loaded " + classTypeStr + " type."); }
                }

                // There was an exception, log it and throw out the bad keys
                if (errors.Count > 0)
                {
                    var logger = ModLoader.GetMod("PermaBuffs").Logger;
                    foreach (string error in errors) { logger.Error(error); }
                    foreach (string name in variableNames) { vars.Remove(name); }
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
            foreach (ModPlayer p in (ModPlayer[])vars["playerArr"].Get(player))
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
