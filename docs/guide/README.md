

Skin Mod Helper Guide
======================

This guide will walk you through making your skin mod compatible with Skin Mod Helper.




Some features of Skin Mod Helper
-----------------------------------
Skin Mod Helper can specify the hair color for a player skin, and configure each player skin with a unique "bangs" texture, or more

Skin Mod Helper also makes your player skin compatible with CelesteNet, allowing everyone to get a player skin of their own

Skin Mod Helper can also switch any of your skins at any time, switch at any time, no longer need to restart the game to do it




A brief introduction to ConfigFiles
-----------------------------------
ConfigFiles can help our Skin Mod Helper to get information about your Skin Mod

And this Skin Mod Helper ConfigFile is "SkinModHelper.yaml"

If your mod needs to use some functions of Skin Mod Helper, then first, you need to do this:
Create a file named "SkinModHelperConfig.yaml" in your mod root (next to your everest.yaml).

In the ConfigFile, you can set two skin types: "PlayerSkin", or "non-PlayerSkin"




Write your Skin's information to the ConfigFile   -- A --
-----------------------------------
In the ConfigFile, we first need to write this information:
```
- Options: ["Set a base option for your skin"]     # required
```

If your skin type is "Player Skin",
Then, you need to set a PlayerSkin ID for the Option information you wrote, use this to do it:
```
   Character_ID: ["your PlayerSkin ID"]     # this also needs you to Create a "Sprites.Xml", will be detailed description later
```

A player skin has some skin textures that cannot be easily replaced in vanilla, such as the "bangs" and "startStarFlyWhite" textures
Skin Mod Helper will make those textures no longer use "characters/player" root path, but try to use "characters/[Character_ID]" as the new root path
If you don't want to do that, and if you want to manually set a more unique new root path, then you can use this:
```
  SpecificPlayerSprite_Path: ["path to the root directory of a specific texture"]
```

If you want your player skin to have a new hair color, other than the default maddy's color, then you can use this:
```
  HairColors:     # The following content can be used multiple times, but do not repeat
  - Dashes: 0 [use 0 to 5]
    Color: "ABCDEF" ["use six digit RGB hex code"]
```

If you want to set their character-orientation for your player skin, then you can choose to add the orientation you want (you can add multiple):
```
  BadelineMode: true     # Let the default hair color of PlayerSkin be baddy
  SilhouetteMode: true     # Let PlayerSkin's all-body get its itself HairColor, be like a Silhouette
  JungleLanternMode: true     # This involves some gameplay mechanisms of JungleHelper, please add carefully
```


You can write multiple skin info to your ConfigFile, this just need repeats everything about "Write your Skin's information to the ConfigFile"
If you want to know more about ConfigFile, you may need to know a little about XMLs first




A brief introduction to Xmls File
-----------------------------------
Sprites.Xml has two types, one is "Normal type" and the other is "non-Normal type"

Although it is an Xmls file, for the time being, only the Normal type of "Sprites.xml" will be introduced here

If the Sprites.xml is located on the path "Celeste/Mods/anymod/Graphics/", Then this Sprites.xml is "Normal type"
(Vanilla's "Celeste/ContentGraphics/Sprites.xml" is also of "Normal type")

If, the skin you make is a player skin.
Then, you need to create a Sprites.xml on a path that conforms to "Normal Type", and inside it create a new ID called "[Character_ID]" (it should be based on "player_badeline" in vanilla)
(Note: If the new ID does not match "[Character_ID]", then it will directly crash the game)


as Normal type means: that any ID in this Sprites.xml can be reskin/cover by skins compatible with SkinModHelper




Write your Skin's information to the ConfigFile   -- B --
-----------------------------------
After briefly talking about Xmls, let's come back to the remaining information available in the ConfigFile

Regarding the player IDs in Sprites.xml, there are many IDs that are not classified as player IDs, but maddy appears in the animation texture of those IDs
Such as "lookout", "payphone" and other IDs, or the "HonlyHelper_Petter" ID from HonlyHelper
Below we will introduce a method to let SkinModHelper reskin them with the same ID:
```
  OtherSprite_Path: ["Root directory path of Sprites.xml of non-Normal type"]   # Path's starting point is "Graphics/"
  OtherSprite_ExPath: ["same as above"] # You should only use this if your skin type is "non-player skin". Otherwise you should not set it
```


For the description of colorGrade, please jump to "https://github.com/bigkahuna443/SkinModHelper/blob/dev/docs/guide/README.md"
The only difference is that it sets the same content with a new name
```
  colorGrade_Path: [custom colorGrade's root directory Path]    # Path's starting point is "Graphics/ColorGrading/"
```


On the premise that the above content settings are basically correct, you will find that your player skin does not appear in "Mod Settings - SMH - Player Skin" when you open the game
We need to use some more content to let it get there:
```
  Player_List: true    # Affects the "SMH - Player Skin" option
  Silhouette_List: true    # Affects the "SMH - Silhouette Skin" option
```


We should have mentioned that the Skin Mod Helper will make your skin compatible with CelesteNet
Skin Mod Helper use a hashSeed to do it, that "hashSeed" defaults is "[Options]"
If your skin unfortunately conflicts with other skins when compatible with CelesteNet, then you can use this to change and fix it
```
  hashSeed: ["any"]
```




Special Jump of ConfigFiles
-----------------------------------
If: exist "{Options} + _NB" in your ConfigFile, 
So: When the player is no_backpack state, 
     your custom skin will auto-jump to custom skins that from "{Options} + _NB"

There are other similar things, they as follows:
  "{Options} + _NB"
  "{Options} + _lantern
            If: you want to reskin JungleHelper some unique content that About Player ID, 
            then: you need use this to jump
  "{Options} + _lantern_NB"




Standard example of ConfigFile
-----------------------------------
The following content can be copied directly into your ConfigFile for test:
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


















    
Miscellaneous
---------------------
1. You can add color grades that render over your sprite for different dash values by placing them
in "Graphics/ColorGrading/[your unique path]" and name the images "dashX.png", where X is the number
of dashes the color grade should apply to. For example, if I had a 0-dash color grade, I would name
the file "Graphics/ColorGrading/Bigkahuna/MySkin/dash0.png".
   * You can grab the base color grade from "Celeste/Content/Graphics/ColorGrading/none.png"
   * Pick the color you want to replace on the sprite, find that color on the color grade, and then
   replace it with the color you want for that dash count.

2. You can add a custom death particle (the circles that appear around Madeline when she dies) by
creating a small image named death_particle.png and place it in your player sprite folder. Use white
as the only color -- it will be filled in by your current hair color on death.
   * For reference, the vanilla death particle is an 8x8 white circle (hair00.png).

3. A few extra things that can be reskinned:
   * The particles for feathers: "../Gameplay/[OtherSprite_Path]/particles/feather.png"
   * The particles for dream blocks: "../Gameplay/[OtherSprite_Path]/objects/dreamblock/particles.png"
      * Use the vanilla image as a guide -- you need to space out the three particle sizes in a specific way for them to be used correctly.
   * The bangs for NPC badeline: "../Gameplay/[OtherSprite_Path]/badeline_bangs[number]"
   * The hair for NPC badeline: "../Gameplay/[OtherSprite_Path]/badeline_hair00"




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
