module SkinModHelperSkinModSwapTrigger
using ..Ahorn, Maple

@mapdef Trigger "SkinModHelper/SkinModSwapTrigger" SkinModSwapTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16,
	skinId::String="default")

const placements = Ahorn.PlacementDict(
	"Skin Mod Swap Trigger (Skin Mod Helper)" => Ahorn.EntityPlacement(
		SkinModSwapTrigger,
		"rectangle"
	)
)

end