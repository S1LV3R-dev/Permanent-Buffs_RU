# Permanent Buffs

Permanent Buffs is a simple mod that allows the user better control over their buffs. This can be done in two ways:

- PermaBuff: By designating a buff as a 'PermaBuff', it becomes permanent. This means the buff has an infinite duration, cannot be deleted, and will be re-applied after a death/save&quit.
PermaBuffs are indicated by a Golden Border around the buff icon.
- NeverBuff: By designating a buff as a 'NeverBuff', it becomes inactive. This means the buff will be blocked from applying its effects in 'Buff.Update' and force enables deletion by right clicking the buff. 
Neverbuffs are indicated by a Purple Border around the buff icon.

Both Permabuffs and Neverbuffs can be toggled by hovering the mouse over the buff icon and pressing their respective keybinds.

This mod should be compatible with most other mods and does not change any behaviour unless the config options are enabled. Some additional features are:
- Option to selectively apply death/save&quit persistence to station buffs, so you don't have to click the stations every time you respawn.
- Option to selectively apply death/save&quit persistence to banners, so that they remain wherever you go once placed.
- Option to disable visuals on affected buff icons.
- Option to hide additional keybind tooltips once the keybinds are bound.

**Disclaimer**: Depending on the buff in question, blocking/force deleting the buff using NeverBuff might not work fully. This is because certain buffs have their behaviour coded somewhere that isn't in Buff.Update. Ex (potion sickness)
Still I don't have a better way of implementing this in mind because any true solution would have be made on a case by case basis.
Permabuffing summons (mod summons too) will make the duration infinite, but they won't persist after death or load. You'll have to manually summon them each time.

Steam Workshop Page: https://steamcommunity.com/sharedfiles/filedetails/?id=3490291206

Possible future changes: 
- Any other suggestions/bugs as commented in Github or the Steam community workshop page