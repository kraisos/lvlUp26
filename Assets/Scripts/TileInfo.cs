using System;

public enum TileType
{
	Void,
    Ground,
	Woods,
	Lake,
	WallNW,  // ┌
	WallNE,  // ┐
	WallSE,  // ┘
	WallSW,  // └
	WallVertical,  // │
	WallHorizontal, // ─
	WallTN,    // ┬
	WallTE,    // ┤
	WallTS,    // ┴
	WallTW,    // ├
	WallCross,   // ┼
	DoorVertical,  // ║
	DoorHorizontal  // ═
}

public enum TilePrefabType {
	Ground,
	Woods,
	Lake,
	WallStraight,
	Door,
	WallCorner,
	WallT,
	WallCross
}

[Serializable]
public class TileInfo
{
    public readonly TileType tileType;

    public TileInfo(TileType type)
    {
        tileType = type;
    }

	public static TileInfo VOID = new TileInfo(TileType.Void);

	// Tiles with an opening in each direction
	public static readonly TileType[] OpenNorth = {
		TileType.WallSE, TileType.WallSW, TileType.WallVertical,
		TileType.WallTE, TileType.WallTS, TileType.WallTW,
		TileType.WallCross, TileType.DoorVertical
	};

	public static readonly TileType[] OpenEast = {
		TileType.WallNW, TileType.WallSW, TileType.WallHorizontal,
		TileType.WallTN, TileType.WallTS, TileType.WallTW,
		TileType.WallCross, TileType.DoorHorizontal
	};

	public static readonly TileType[] OpenSouth = {
		TileType.WallNW, TileType.WallNE, TileType.WallVertical,
		TileType.WallTN, TileType.WallTE, TileType.WallTW,
		TileType.WallCross, TileType.DoorVertical
	};

	public static readonly TileType[] OpenWest = {
		TileType.WallNE, TileType.WallSE, TileType.WallHorizontal,
		TileType.WallTN, TileType.WallTE, TileType.WallTS,
		TileType.WallCross, TileType.DoorHorizontal
	};
}
