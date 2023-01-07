
Skin Mod Helper Guide
======================
This guide will walk you through making your skin mod compatible with Skin Mod Helper.

A brief introduction to config files
------------------------------------
Config files can help the Skin Mod Helper find information about your skin mod.
If your mod provides a skin, then you need to create a file named "SkinModHelperConfig.yaml" in your mod root (next to your everest.yaml).

Here is a skeleton of a SkinModHelperConfig.yaml file.
Each of the fields will be explained below.

```yaml
- Options: [an option that is required]
  Player_List: [true/false]
  Silhouette_List: [true/false]
  OtherSprite_ExPath: [non-player Skin's required]
  
  Character_ID: [PlayerSkin ID]
  hashSeed: [Options]
  
  BadelineMode: [true/false]    
  SilhouetteMode: [true/false]
  JungleLanternMode: [true/false]
  
  SpecificPlayerSprite_Path: [a path]
  OtherSprite_Path: [a path]
  colorGrade_Path: [a path]
  
  HairColors:
  - < HairColors >
```


Options
-----------------------------------
In the config file, we first need to write this information:
```
- Options: [Set a base option for your skin]     # required
```

`Character_ID`
---------------------------
If your skin type is "Player Skin",
Then, you need to set a PlayerSkin ID for the Option information you wrote, use this to do it:
```
  Character_ID: [your PlayerSkin ID]     # this also needs you to Create a "Sprites.Xml", will be detailed description later
```

Specific Player Sprite
-----------------------------------
A player skin has some skin textures that cannot be easily replaced in vanilla, 
such as the "bangs" and "startStarFlyWhite" textures.

Skin Mod Helper will make those textures no longer use "characters/player" root path, 
Then try to use "characters/[Character_ID]" as the new root path.

If you don't want to do that, and if you want to manually set a more unique new root path, then you can use this:
```
  SpecificPlayerSprite_Path: [path to the root directory of some specific texture]     
    # Path's starting point is "Graphics/Atlases/Gameplay/"
```

HairColors
-----------------------------------
If you want your player skin to have a new hair color, other than the default maddy's color, 
Then you can use this:
```
  HairColors:     # The following content can be used multiple times, but do not repeat
  - Dashes: [use 0 to 5]
    Color: [use six digit RGB hex code]
```

Character Orientation
-----------------------------------
If you want to set their character-orientation for your player skin, 
Then you can choose to add the orientation you want (you can add multiple):
```
  BadelineMode: true     # Let the default hair color of PlayerSkin be baddy
  SilhouetteMode: true     # Let PlayerSkin's all-body get its itself HairColor, be like a Silhouette
  JungleLanternMode: true     # This involves some gameplay mechanisms of JungleHelper, probably don't add this unless you know what you're doing
```

If you want to know more about config file, you may need to know a little about XMLs first

A brief introduction to Sprites.xml
-----------------------------------
Sprites.Xml has two types, one is "Normal type" and the other is "non-Normal type"

Normal type: 
* If a Sprites.xml's root path is "Celeste/Mods/[mod_name]/Graphics/". Then that Sprites.xml is "Normal type".
* a Sprites.xml as "Normal type" means: That any ID in this Sprites.xml can be reskin/cover by skins compatible with SkinModHelper.
   * (Vanilla's Sprites.xml also is "Normal type")

If, the skin you make is a player skin. Then: 
1. you need to create a Sprites.xml of "Normal Type"
2. inside it create a new ID called "[Character_ID]"
   * Use the "player_badeline" ID of vanilla as a guide -- that new ID should have all animations
   * Note: If the new ID does not match "[Character_ID]". Then it will directly crash the game


OtherSprite
-----------------------------------
Regarding the Player IDs in Sprites.xml, there are many IDs that are not classified as player IDs, 
but maddy appears in the animation texture of those IDs.

Such as "lookout", "payphone" and other IDs, or the "HonlyHelper_Petter" ID from HonlyHelper.
Below we will introduce a method to let SkinModHelper reskin them with the same ID:
```
  OtherSprite_Path: [Root directory path of Sprites.xml of non-Normal type]   # Path's starting point is "Graphics/"
```

If your skin type is "non-Player Skin", you just want to simply reskin some IDs. Then use this:
```
  OtherSprite_ExPath: [same as OtherSprite_Path]
```

You can also do something like this for any ID in Portraits.xml


ColorGrades
-----------------------------------
You can add color grades, let your playerSkin self are rendered differently at different dash counts by placing them
```
  colorGrade_Path: [custom colorGrade's root directory Path]    # Path's starting point is "Graphics/ColorGrading/"
```
in "Graphics/ColorGrading/[colorGrade_Path]/" and name the images "dashX.png", where X is the number
of dashes the color grade should apply to. For example, if I had a 0-dash color grade, I would name
the file "Graphics/ColorGrading/Bigkahuna/MySkin/dash0.png".
   * You can grab the base color grade from "Celeste/Content/Graphics/ColorGrading/none.png"
   * Pick the color you want to replace on the sprite, find that color on the color grade, and then
   replace it with the color you want for that dash count.


`let your skin appear in Mod-Options`
-----------------------------------
If your skin type is "Player Skin", Then We need to use some more content to let it get there:
```
  Player_List: true    # Affects the "Player Skin" option
  Silhouette_List: true    # Affects the "Silhouette Skin" option
```
If your skin type is "non-Player Skin", Then when you set [OtherSprite_ExPath] after, them will appear in "Extra Settings" list


hashSeed
-----------------------------------
We should have mentioned that the SkinModHelper will make your player skin compatible with CelesteNet.
SkinModHelper use a "hashSeed" to do it, that "hashSeed" defaults is "[Options]"

If your skin happens to conflict with other skins when compatible with CelesteNet, 
Then you can use this to change and fix it.
```
  hashSeed: [any]
```

You can write multiple skin info to your config file, 
this just need repeats everything about "ConfigInfo"


Special Jump of config files
-----------------------------------
If: exist "[Options] + _NB" in your config file, 
So: When the player is no_backpack state, 
     your custom skin will auto-jump to custom skins that from "{Options} + _NB"

There are other similar things, they as follows:
* "[Options] + _NB"
* "[Options] + _lantern"
   * If: you want to reskin JungleHelper some unique content that About Player ID, 
   * then: you need use this to jump
* "[Options] + _lantern_NB"




Standard example of config file
-----------------------------------
The following content can be copied directly into your config file for test: 
```
- Options: "SkinTest_TestA"
  OtherSprite_ExPath: "SkinTest/TestA"

- Options: "SkinTest_TestB"
  OtherSprite_ExPath: "SkinTest/TestB"


- Options: "vanilla_player"
  Player_List: true
  Character_ID: "player"
  OtherSprite_Path: "SkinTest/TestA"

- Options: "vanilla_player_NB"
  Character_ID: "player_no_backpack"


- Options: "vanilla_Silhouette"
  Player_List: true

  SilhouetteMode: true
  Character_ID: "player_playback"
```
(Regarding the files and sprites required for the above configurations, SkinModHelper's own files already contain them, you can refer to those files)


More Miscellaneous
---------------------
1. You can add a custom death particle (the circles that appear around Madeline when she dies) by
creating a small image named death_particle.png and place it in your [SpecificPlayerSprite_Path] folder. Use white
as the only color -- it will be filled in by your current hair color on death.
   * For reference, the vanilla death particle is an 8x8 white circle (hair00.png).

2. A few extra things that can be reskinned:
   * The particles for feathers: "../Gameplay/[OtherSprite_Path]/particles/feather.png"
   * The particles for dream blocks: "../Gameplay/[OtherSprite_Path]/objects/dreamblock/particles.png"
      * Use the vanilla image as a guide -- you need to space out the three particle sizes in a specific way for them to be used correctly.
   * The bangs for NPC badeline: "../Gameplay/[OtherSprite_Path]/badeline_bangs[number].png"
   * The hair for NPC badeline: "../Gameplay/[OtherSprite_Path]/badeline_hair00.png"

Note: some specific sprites's reskin path, can also use [OtherSprite_ExPath] to complete the reskin for them



Troubleshooting
-----------------------
If your skin is not appearing in the menu:
* Make sure your configuration file is named correctly and in the right place
* Make sure the ID is present, unique, and correct

If your sprites/portraits are not appearing in-game:
* Make sure your XML is valid. You can compare to the vanilla files or use an [online syntax checker](https://www.xmlvalidation.com/)
* Make sure the "path" fields to your sprites/portraits are correct and the files are in the right place
* Make sure the "start" field references an animation you have reskinned.

If you get missing textures or unexpected vanilla textures:
* Check your log to see what textures are missing -- these messages can point you in the right direction
* Make sure the number of images matches the number of animation "frames"

If you get crashes:
* Check your log to see if it's a missing texture
* Make sure you don't have any "Metadata" sections for missing animations in Sprites.xml
* Contact me!

This process can be pretty involved, especially if you are porting over an existing skin mod,
so feel free to [contact me](../../README.md#contact) if you need help, find an issue, or would
like a new feature supported! You can also use a currently supported skin mod as a reference.

