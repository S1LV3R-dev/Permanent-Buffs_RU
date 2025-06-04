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
        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (item.buffType != 0 && (item.shoot != ProjectileID.None || item.sentry || item.CountsAsClass(DamageClass.Summon)) || item.mountType != -1)
            {
                PermaBuffsPlayer modPlayer = player.GetModPlayer<PermaBuffsPlayer>();
                modPlayer.buffItemIDs[item.buffType] = item.type;
            }

            return true;
        }
    }
}
