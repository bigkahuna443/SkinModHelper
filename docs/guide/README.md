Skin Mod Helper Guide
======================

This guide will walk you through making your skin mod compatible with Skin Mod Helper.

Before starting, choose a **unique path** for your assets so they don't conflict with other mods.
[Name]/[Skin] is usually a good format, e.g. Bigkahuna/MySkin. You'll use this path in several steps.

Part 1: Create a Configuration File
-----------------------------------
Create a file named "SkinModHelperConfig.yaml" in your mod root (next to your everest.yaml). 
This is the file that will let our helper find all of your assets.

Here are the different fields we can use:
```
SkinId: [unique ID of your skin, required]
SkinDialogKey: [dialog ID of your skin's name, required]
SpritesXmlPath: [path from mod root to your sprites XML, optional]
PortraitsXmlPath: [path from mod root to your portraits XML, optional]
```

Part 2: Create your XMLs
-------------------------
This helper requires XMLs for sprites and/or portraits to be reskinnable. However, instead of
using the full XMLs from vanilla, you only have to include the animations that you've actually
made. The helper will automatically use vanilla animations if there are any missing.

If you're starting from scratch, you can copy the outline of each sprite field and add each animation line one at a time as you make them.
If you're porting an existing helper, you'll need to remove animations that you haven't drawn yet (make sure to also remove any Metadata animation info from Sprites.xml).

**Notes for Sprites.xml**:
* You can grab the vanilla XML from Celeste/Content/Graphics/Sprites.xml.
Most things in there should be able to be reskinned, although not everything has been tested.
* For the "path" field in each sprite, use "Graphics/Atlases/Gameplay/[your unique path]/[normal sprite path]/", 
e.g. "Graphics/Atlases/Gameplay/Bigkahuna/MySkin/characters/player/"
* You can tweak the "delay", "frames", and "goto" attributes of animations to use different amounts of frames,
add new animations, etc.
* If you want to tweak the bangs for a sprite, edit the "hair" frames for each animation under Metadata. 
You can remove hair by putting "|x|" for the frames you want or just remove the entire line. 

**Notes for Portraits.xml**:
* You can grab the vanilla XML from Celeste/Content/Graphics/Portraits.xml.
* For the "path" field in each portrait, use "Graphics/Atlases/Portraits/[your unique path]/[normal portrait path]/", 
e.g. "Graphics/Atlases/Portraits/Bigkahuna/MySkin/madeline/"
* For the "textbox" field in each portrait, use "Graphics/Atlases/Portraits/textbox[your unique path]/[normal textbox path]/"
e.g. "Graphics/Atlases/Portraits/textbox/Bigkahuna/MySkin/madeline/" (if you have a custom textbox).
* You can tweak the "delay", "frames", and "goto" attributes of animations to use different amounts of frames,
add new animations, etc.


Part 3: Troubleshooting
-----------------------
If your skin is not appearing in the menu:
* Make sure your configuration file is named correctly and in the right place
* Make sure the ID and dialog keys are present, unique, and correct

If your sprites/portraits are not appearing in-game:
* Make sure the paths to your XMLs are correct in the configuration file and don't conflict with another mod
* Make sure your XML is valid. You can compare to the vanilla files or use an [online syntax checker](https://www.xmlvalidation.com/)
* Make sure the "path" fields to your sprites/portraits are correct

If you get missing textures or unexpected vanilla textures:
* Check your log to see what textures are missing -- these messages can point you in the right direction
* Make sure the number of images matches the number of animation "frames"

If you get crashes:
* Check your log to see if it's a missing texture crash

This process can be pretty involved, especially if you are porting over and existing skin mod,
so feel free to [contact me](../../README.md#contact) if you need help, find an issue, or would
like a new feature supported!