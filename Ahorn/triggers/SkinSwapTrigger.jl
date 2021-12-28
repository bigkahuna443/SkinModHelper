module SkinModHelperSkinSwapTrigger
using ..Ahorn, Maple

@mapdef Trigger "SkinModHelper/SkinSwapTrigger" SkinSwapTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16,
	skinId::String="Default", revertOnLeave::Bool=false)

const placements = Ahorn.PlacementDict(
	"Skin Swap Trigger (Skin Mod Helper)" => Ahorn.EntityPlacement(
		SkinSwapTrigger,
		"rectangle"
	)
)

end