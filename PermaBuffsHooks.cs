using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq.Expressions;
using System.Collections.Generic;
using PermaBuffs;

namespace PermaBuffs
{
    /// <summary>
    /// A function delegate class that gets called during Post/PreUpdateBuffs on the player. For running custom code that correctly implements certain perma/neverbuffs
    /// </summary>
    /// <param name="player">The player instance who has the buff. This will be called with a null reference the first time the hook is run.</param>
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
            buffType = PermaBuffsConfig.instance.experimentalChanges ? CalamityHelper.rageBuffType : 0;
            if (player == null)
                return;

            // NeverBuff is already properly applied -> permabuff logic needed
            if (!isPermaBuffed)
                return;

            // This is needed to properly modify the instance values. Otherwise null reference exeption thrown.
            if (!CalamityHelper.SetPlayerInstance(player))
                return; // There was an error setting up reflection

            // Sets your rage to max rage every frame - this theoretically makes rage mode last forever
            CalamityHelper.rage = CalamityHelper.maxRage;
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
        public static void PotionSickness(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
            buffType = BuffID.PotionSickness;
            if (player == null)
                return;

            // If potion sickness is neverbuffed get rid of any heal cooldown
            if (!isPermaBuffed)
                player.potionDelay = 0;
            else // Player can never heal unless the permabuff is disabled
                player.potionDelay = Math.Max(player.potionDelay, 2);
        }
        
        // This function is now present in the tsorc revamp code. But this is how I'd do it if I wasn't a dev for the mod.
        // This will be commented out in the next release of tsorcRevamp
        public static void NeverBuffCurse(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
            // Use the helper class to set the buffType
            buffType = PermaBuffsConfig.instance.experimentalChanges ? TsorcRevampHelper.curseBuffType : 0;

            if (player == null)
                return;

            // Custom logic only applies during neverbuff
            if (isPermaBuffed)
                return;

            // This is needed to properly modify the instance values. Otherwise null reference exeption thrown.
            if (!TsorcRevampHelper.SetupPlayerInstance(player))
                return; // There was an error getting the values using reflection

            // These booleans control whether any curse value is applied - set them to false if neverBuffed.
            TsorcRevampHelper.curseActive = false;
            TsorcRevampHelper.powerfulCurseActive = false;
        }
    }

    // I'm a mod for this one so technically this class isn't needed - leaving it here as an example instead for possible future helper class patches for other mods
    // This kind of hacky functionality is only suitable if you don't have access to the source code of another mod.
    // Its important that these helper classes are static so they can be used freely in the Post/PreBuffUpdateLoops
    public class TsorcRevampHelper
    {
        static Dictionary<string, FieldAccessor> vars;

        // These let you acess the variables within the other mod. They require an instance to access mod data by string. Must call SetOwner(Player) before use
        public static bool curseActive { get { return (bool)vars["modPlayer.curseActive"].Get(myPlayer); } set { vars["modPlayer.curseActive"].Set(myPlayer, value); } }
        public static bool powerfulCurseActive { get { return (bool)vars["modPlayer.powerfulCurseActive"].Get(myPlayer); } set { vars["modPlayer.powerfulCurseActive"].Set(myPlayer, value); } }
        static Player myPlayer;
        // Cache for all buff type since you would otherwise be getting it with a slow tryFind every function call
        static int cachedCurseBuffType = -1;
        // The buff type of the buff named Curse
        public static int curseBuffType
        {
            get
            {
                if (cachedCurseBuffType == -1)
                    CacheInternals();

                return cachedCurseBuffType;
            }
            private set { cachedCurseBuffType = value; }
        }
        // Caches types. Just use a series of Modcontent try find if statements if you're lazy
        private static void CacheInternals()
        {
            if (!ModContent.TryFind("tsorcRevamp", "Curse", out ModBuff curseBuff))
                cachedCurseBuffType = 0;
            else
                cachedCurseBuffType = curseBuff.Type;
        }
        // This function sets up the player accessors properly and also sets the instance.
        // Must be called before using any accessors. Returns whether or not it was sucessful.
        public static bool SetupPlayerInstance(Player player)
        {
            // Only run this code once. The field acessors need to compile the first time.
            if (vars == null)
            {
                string playerPath = "tsorcRevamp.Player.tsorcRevampPlayerUpdateLoops.tsorcRevampPlayer";
                Type tsorcRevampPlayerType = ModLoader.GetMod("tsorcRevamp").Code.GetType(playerPath);

                // The story of red cloud isnt loaded
                if (tsorcRevampPlayerType == null)
                    return false;

                vars = new Dictionary<string, FieldAccessor>();
                List<string> errors = new List<string>();

                try { vars.Add("modPlayer.curseActive", new FieldAccessor(tsorcRevampPlayerType, "CurseActive")); }
                catch { errors.Add("Could not find CurseActive within the class " + playerPath); }
                try { vars.Add("modPlayer.powerfulCurseActive", new FieldAccessor(tsorcRevampPlayerType, "powerfulCurseActive")); }
                catch { errors.Add("Could not find powerfulCurseActive within the class " + playerPath); }
                
                if (errors.Count > 0) 
                {
                    Mod mod = ModLoader.GetMod("PermaBuffs");
                    vars = null;

                    foreach (string error in errors)
                    {
                        mod.Logger.Error(error);
                    }

                    return false;
                }
            }

            // Dynamically set the referenced instance of the player.
            myPlayer = player;

            return true;
        }
    }

    // This class is only necessary because I am not a developer for Calamity. Reflection in my case is necessary to modify the required variables.
    // If you are a calamity mod developer and wish to implement permabuffs compatibility, look at either partial class with the signature 'PermaBuffs Pre/Post BuffUpdateHooks' for a simpler method.
    public class CalamityHelper
    {
        static Dictionary<string, FieldAccessor> vars;
        public static float rage { get { return (float)vars["modPlayer.rage"].Get(myPlayer); } set { vars["modPlayer.rage"].Set(myPlayer, value); } }
        public static float maxRage { get { return (float)vars["modPlayer.rageMax"].Get(myPlayer); } set { vars["modPlayer.rageMax"].Set(myPlayer, value); } }
        static Player myPlayer;

        // Cache for all buff type since you would otherwise be getting it with a slow tryFind every function call
        static int rageBuffTypeCache = -1;
        public static int rageBuffType
        {
            get
            {
                if (rageBuffTypeCache == -1)
                    CacheInternalTypes();
                return rageBuffTypeCache;
            }
            private set { rageBuffTypeCache = value; }
        }
        // Caches the rage buff type once and then returns the cached value
        private static void CacheInternalTypes()
        {
            if (!ModContent.TryFind("CalamityMod", "RageMode", out ModBuff rageBuff))
                rageBuffTypeCache = 0;
            else
                rageBuffTypeCache = rageBuff.Type;
        }
        // Must be called with player instance before acessing any calamity player instance variables
        public static bool SetPlayerInstance(Player player)
        {
            if (vars == null)
            {
                string playerPath = "CalamityMod.CalPlayer.CalamityPlayer";
                Type calamityPlayerType = ModLoader.GetMod("CalamityMod").Code.GetType("CalamityMod.CalPlayer.CalamityPlayer");
                
                // Calamity mod isnt loaded
                if (calamityPlayerType == null)
                    return false;

                vars = new Dictionary<string, FieldAccessor>();
                List<string> errors = new List<string>();

                try { vars.Add("modPlayer.rage", new FieldAccessor(calamityPlayerType, "rage")); }
                catch { errors.Add("Could not find rage within the class " + playerPath); }
                try { vars.Add("modPlayer.rageMax", new FieldAccessor(calamityPlayerType, "rageMax")); }
                catch { errors.Add("Could not find rageMax within the class " + playerPath); }

                if (errors.Count > 0)
                {
                    Mod mod = ModLoader.GetMod("PermaBuffs");
                    vars = null;

                    foreach (string error in errors)
                    {
                        mod.Logger.Error(error);
                    }

                    return false;
                }
            }

            myPlayer = player;
            return true;
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
