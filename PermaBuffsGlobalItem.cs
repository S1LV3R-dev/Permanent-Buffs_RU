using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PermaBuffs
{
    public class PermaBuffsGlobalItem : GlobalItem
    {
        public static void SaveItemType(Item item, Player player)
        {
            if (item.buffType != 0 && (item.shoot != ProjectileID.None || item.sentry || item.CountsAsClass(DamageClass.Summon)) || item.mountType != -1)
            {
                PermaBuffsPlayer modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
                modPlayer.buffItemIDs[item.buffType] = item.netID;
            }
        }
        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            SaveItemType(item, player);
            return true;
        }

        public override bool? UseItem(Item item, Player player)
        {
            SaveItemType(item, player);
            return null;
        }
    }
}
