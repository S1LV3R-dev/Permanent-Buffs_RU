using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq.Expressions;

namespace PermaBuffs
{
    /// <summary>
    /// Courtesy of Darrel Lee on stack overflow - this class lets you acess variables effeciently using reflection.
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

    /// <summary>
    /// A function delegate class that gets called during PostUpdateBuffs on the player. For running custom code that correctly implements certain perma/neverbuffs
    /// </summary>
    /// <param name="player">The player instance who has the buff. This will be called with a null reference the first time the hook is run.</param>
    /// <param name="buffSlotOnPlayer"> The index of the buff in player.buffType and player.buffTime</param>
    /// <param name="isPermaBuffed">Called with true if the buff is permabuffed, false if it's neverbuffed</param>
    /// <param name="buffType">The buffType this hook is for. This needs to be set in the function before the player null reference check.</param>
    public delegate void BuffHook(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType);
    /// <summary>
    /// These methods will be called prior to Buff.Update. For proper integration please follow the instructions below.
    /// Every function should have the return type of void, be static, and return after buffType is set if "player" == null.
    /// Do not write any functions that don't directly follow the function signature of void(Player, int, bool, out int, bool) in the class. 
    /// This class is reserved for functions that are set to become delegates of the above function type. Nothing else should be present.
    /// </summary> 
    public partial class PreBuffUpdateHooks
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
                    CacheInternals();
                return rageBuffTypeCache;
            }
        }
        // Caches the rage buff type once and then returns the cached value
        private static void CacheInternals()
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


            rageAcessor.Set(player, maxRageAcessor.Get(player));
        }
    }

    /// <summary>
    /// These methods will be called post Buff.Update. For proper integration please follow the instruction below.
    /// Every function should have the return type of void, be static, and return after buffType is set if "player" == null.
    /// If the function is meant for a modbuff, return a bufftype of 0 if the mod is not loaded
    /// Do not write any functions that don't directly follow the function signature of void(Player, int, bool, out int, bool) in the class. 
    /// This class is reserved for functions that are set to become delegates of the above function type. Nothing else should be present.
    /// </summary>
    public partial class PostBuffUpdateHooks
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
        /*
        public static void Curse(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
            buffType = TsorcRevampHelper.curseBuffType;
            if (player == null)
                return;

            if (!isPermaBuffed)
                TsorcRevampHelper.SetCurse(player);
        }
        */
    }

    public class TsorcRevampHelper
    {
        static FieldAccessor curseAcessor;
        static FieldAccessor powerfulCurseAcessor;
        private static int curseBuffCache = -1;
        public static int curseBuffType
        {
            get
            {
                if (curseBuffCache == -1)
                    CacheInternals();

                return curseBuffCache;
            }
        }
        // Caches the rage buff type once and then returns the cached value
        private static void CacheInternals()
        {
            if (!ModContent.TryFind("tsorcRevamp", "Curse", out ModBuff curseBuff))
                curseBuffCache = 0;
            else
                curseBuffCache = curseBuff.Type;
        }
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
}
