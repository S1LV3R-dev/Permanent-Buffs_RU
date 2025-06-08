using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq.Expressions;

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
        public static void CalamityRageMode(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
            buffType = CalamityHelper.rageBuffType;
            if (player == null)
                return;

            if (isPermaBuffed)
                CalamityHelper.RageMode(player);
        }
    }

    public class CalamityHelper
    {
        static FieldAccessor rageAcessor;
        static FieldAccessor maxRageAcessor;
        static int rageBuffTypeCache = -1;
        public static int rageBuffType
        {
            get
            {
                if (rageBuffTypeCache == -1)
                    CacheInternalTypes();
                return rageBuffTypeCache;
            }
        }
        // Caches the rage buff type once and then returns the cached value
        private static void CacheInternalTypes()
        {
            if (!ModContent.TryFind("CalamityMod", "RageMode", out ModBuff rageBuff))
                rageBuffTypeCache = 0;
            else
                rageBuffTypeCache = rageBuff.Type;
        }
        public static void RageMode(Player player)
        {
            if (rageAcessor == null)
            {
                Type calamityPlayerType = ModLoader.GetMod("CalamityMod").Code.GetType("CalamityMod.CalPlayer.CalamityPlayer");
                rageAcessor = new FieldAccessor(calamityPlayerType, "rage");
                maxRageAcessor = maxRageAcessor = new FieldAccessor(calamityPlayerType, "rageMax");
            }

            // Keeps the current rage at max so rage mode lasts forever
            rageAcessor.Set(player, maxRageAcessor.Get(player));
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
        // Example Function:
        /*
        public static void NeverBuffCurse(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
        // Use the helper class to set the buffType
            buffType = TsorcRevampHelper.curseBuffType;
            if (player == null)
                return;

            if (!isPermaBuffed)
                TsorcRevampHelper.SetCurse(player);
        }
        */
    }

    // I'm a mod for this one so technically this class isn't needed - leaving it here as an example instead for possible future helper class patches for other mods
    // This kind of hacky functionality is only suitable if you don't have access to the source code of another mod.
    // Its important that these helper classes are static so they can be used freely in the Post/PreBuffUpdateLoops
    public class TsorcRevampHelper
    {
        // These let you acess the variables within the other mod. They require an instance to access mod data by string.
        static FieldAccessor curseAcessor;
        static FieldAccessor powerfulCurseAcessor;
        // Cache for all buff type since you would otherwise be getting it with a slow tryFind every function call
        public static bool cachedTypes = false;
        // The buff type of the buff named Curse
        public static int curseBuffType
        {
            get
            {
                if (!cachedTypes)
                    CacheInternals();

                return curseBuffType;
            }
            set { curseBuffType = value; }
        }
        // Caches types. Just use a series of Modcontent try find if statements if you're lazy
        private static void CacheInternals()
        {
            if (!ModContent.TryFind("tsorcRevamp", "Curse", out ModBuff curseBuff))
                curseBuffType = 0;
            else
                curseBuffType = curseBuff.Type;

            cachedTypes = true;
        }
        // This function caches the two curse player member variables and set them to false so the buff isnt applied.
        // This function is called if the buff is neverBuffed.
        public static void SetCurse(Player player)
        {
            if (curseAcessor == null)
            {
                Type tsorcRevampPlayerType = ModLoader.GetMod("tsorcRevamp").Code.GetType("tsorcRevamp.Player.tsorcRevampPlayerUpdateLoops");
                curseAcessor = new FieldAccessor(tsorcRevampPlayerType, "CurseActive");
                powerfulCurseAcessor = powerfulCurseAcessor = new FieldAccessor(tsorcRevampPlayerType, "powerfulCurseActive");
            }

            curseAcessor.Set(player, false);
            powerfulCurseAcessor.Set(player, false);
        }
    }

    /// <summary>
    /// Courtesy of Darrel Lee on stack overflow - this class lets you access another mod's variables effeciently using Expression.compile() with reflection.
    /// </summary>
    class FieldAccessor
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
