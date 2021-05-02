Skin Mod Helper Guide
======================

This guide will walk you through making your skin mod compatible with Skin Mod Helper.


Part 1: Create a Configuration File
-----------------------------------
Create a file named "SkinModHelperConfig.yaml" in your mod root (next to your everest.yaml). 
This is the file that will let our helper find all of your assets.

Here are the different fields you can use:
```
SkinId: [unique ID of your skin, required]
SkinDialogKey: [dialog ID of your skin's name, required]
```

Your skin ID should be in the format "[Name]_[Skin]", e.g. "Bigkahuna_MySkin". This will double
as the file asset path -- "Bigkahuna_MySkin" turns into "Bigkahuna/MySkin". You will use this path
for all custom assets. I'll refer to this as **[your unique path]**.


Part 2: Create your XMLs
-------------------------
This helper uses XMLs to reskin most objects. XMLs should be placed in "Graphics/[your unique path]/",
e.g. "Graphics/Bigkahuna/MySkin/Sprites.xml".

You should follow these steps to set up your XMLs:
1. Copy the vanilla version of the XML.
   * You can find them in Celeste/Contest/Graphics/.
2. Take out the sprites and animations you aren't reskinning.
   * These will automatically be replaced vanilla sprites/animations.
   * For Sprites.xml, make sure to remove the "Metadata" section for any animations you remove.
3. Change the "path" fields to the correct path for your skin and put the files there.
   * For Sprites.xml, the base folder is "Graphics/Atlases/Gameplay". So for example, for the player
    sprite I would use "path=Bigkahuna/MySkin/characters/player/" and put the images in
    "Graphics/Atlases/Gameplay/Bigkahuna/MySkin/characters/player/".
   * For Portraits.xml, the base folder is "Graphics/Atlases/Portraits". If you have textbox
    reskins, you should use "Graphics/Atlases/Portraits/textbox" as the base folder.
4. Tweak any animation values that you want.
   * You can edit the "delay", "frames", and "goto" attributes of animations to change how they work.
   * For Sprites.xml, you can change the bangs for a sprite, by editing the "hair" frames for
    each animation under "Metadata". You can remove hair for a certain frame by putting "|x|" for
    that frame, or remove hair entirely for that animation by removing the whole line.


Part 3: Troubleshooting
-----------------------
If your skin is not appearing in the menu:
* Make sure your configuration file is named correctly and in the right place
* Make sure the ID and dialog keys are present, unique, and correct

If your sprites/portraits are not appearing in-game:
* Make sure your XML is valid. You can compare to the vanilla files or use an [online syntax checker](https://www.xmlvalidation.com/)
* Make sure the "path" fields to your sprites/portraits are correct and the files are in the right place

If you get missing textures or unexpected vanilla textures:
* Check your log to see what textures are missing -- these messages can point you in the right direction
* Make sure the number of images matches the number of animation "frames"

If you get crashes:
* Check your log to see if it's a missing texture
* Make sure you don't have any "Metadata" sections for missing animations in Sprites.xml
* Contact me!

This process can be pretty involved, especially if you are porting over an existing skin mod,
so feel free to [contact me](../../README.md#contact) if you need help, find an issue, or would
like a new feature supported! You can also use a [currently supported skin mod](../../README.md#installation-guide) as a reference.