# Auto Act
Automatically perform the same type of action, triggered by holding down the Shift key and taking the action.

| Supported Actions |
|:------------------:|
|Mine               |
|Collect            |
|Harvest           |
|Shear             |
|Brush             |
|Plow               |
|Plant Seed        |
|Reap Seed         |
|Fertilize         |
|Draw Water         |
|Pour Water         |
|Clean             |
|Read              |
|Pick              |
|Build              |
|Dig               |
|Steal               |
|Pick Lock          |
|Water Crops(Auto refill)|
|Talk               |

## Action filter

Open Elin's **mod viewer**, find **AutoAct**, and click its **settings** button. The native settings page contains the action filter in five groups, an enabled count, and Enable All / Disable All buttons. The existing general configuration entries appear below the filter. The middle-click settings menu is unchanged; no separate window or filter shortcut is needed.

All 25 switches are enabled by default and saved immediately in `BepInEx/config/redgeioz.plugin.AutoAct.cfg`, under `[ActionFilter]`. Existing selections are preserved. Collect now controls both gathering and harvesting mature crops. When migrating older separate Collect/Harvest entries, either disabled entry keeps the merged Collect switch disabled; the obsolete Harvest entry is removed, so the merged switch can subsequently be changed normally. Its former `OpenFilterShortcut` entry is no longer used.

| Group | Independent switches |
|---|---|
| Gathering and construction | Mine, Collect, ReapSeed, Dig, Pick, Build, Disassemble, Smash |
| Farming | Plow, PlantSeed, Fertilize, WaterCrops, DrawWater, PourWater |
| Animal work | Shear, Brush, Slaughter |
| Other interactions | Clean, Read, Chat, Steal, Unlock, FeedBaby |
| Gacha | FeedCoin, OpenGacha |

Disabled actions still work manually, including while holding the AutoAct trigger. Range selections obey the same switches and skip disabled gathering/mining operations. WaterCrops includes watering-can refills; DrawWater controls the separate water-pot action. Disassemble uses the harvesting task's disassembly mode, while Smash attacks objects that can be smashed. Trap handling has no filter switch.

Switches are checked before constructing AutoAct instances, both in action factories and in range-selection entry points. A disabled action returns to normal manual handling without creating an AutoAct or changing the original task's status. Changing a switch does not stop an existing job; it applies the next time automation is started. Mixed gathering/mining jobs capture their allowed action scope when created, so later setting changes do not alter the running job. The page uses Elin's built-in `IModConfig` interface and native scrolling/input handling, and is also available from the title-screen mod viewer. Action names use the same language keys or element names as the original game, following its current language and translation updates, except for the requested Build (建造), Feed Coin (Gacha) (扭蛋投币), Open Gacha Ball (开扭蛋), and Smash Objects names. Smash uses: 砸碎物件 (Simplified/Traditional Chinese), Smash Objects (English), and 物を砕く (Japanese). The filter's own headings/help support Simplified Chinese, Traditional Chinese, English, and Japanese, with English as fallback.

The filter uses the `AutomationAction` enum, for example `ActionFilter.IsEnabled(AutomationAction.Read)` or `ActionFilter.IsEnabled(AutomationAction.PlantSeed)`. Each switch represents one independent action, including actions sharing an AutoAct class. Action classification and native settings use the same enum; its names preserve the existing configuration keys.

Original action types and modes are cross-checked with [Elin-Decompiled](https://github.com/Elin-Modding-Resources/Elin-Decompiled); compatibility is built against the locally installed game assemblies.
