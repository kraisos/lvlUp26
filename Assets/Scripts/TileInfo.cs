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

public class TileTypeInfo
{
	public readonly TileType tileType;
	public readonly bool wallNorth;
	public readonly bool wallEast;
	public readonly bool wallSouth;
	public readonly bool wallWest;
	public readonly bool isWall;
	public readonly bool isDoor;

	public TileTypeInfo(TileType type, bool north, bool east, bool south, bool west, bool wall, bool door)
	{
		tileType = type;
		wallNorth = north;
		wallEast = east;
		wallSouth = south;
		wallWest = west;
		isWall = wall;
		isDoor = door;
	}

	public static readonly TileTypeInfo[] wallTypes = new TileTypeInfo[] {
		new TileTypeInfo(TileType.WallNW, false, true, true, false, true, false),
		new TileTypeInfo(TileType.WallNE, false, false, true, true, true, false),
		new TileTypeInfo(TileType.WallSE, true, false, false, true, true, false),
		new TileTypeInfo(TileType.WallSW, true, true, false, false, true, false),
		new TileTypeInfo(TileType.WallVertical, true, false, true, false, true, false),
		new TileTypeInfo(TileType.WallHorizontal, false, true, false, true, true, false),
		new TileTypeInfo(TileType.WallTN, false, true, true, true, true, false),
		new TileTypeInfo(TileType.WallTE, true, false, true, true, true, false),
		new TileTypeInfo(TileType.WallTS, true, true, false, true, true, false),
		new TileTypeInfo(TileType.WallTW, true, true, true, false, true, false),
		new TileTypeInfo(TileType.WallCross, true, true, true, true, true, false),
		new TileTypeInfo(TileType.DoorVertical, true, false, true, false, true, true),
		new TileTypeInfo(TileType.DoorHorizontal, false, true, false, true, true, true)
	};
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

	public static TileInfo VOID = new(TileType.Void);

	public bool IsOpenWallNorth {
		get { return Array.Exists(OpenNorth, t => t == tileType); }
	}
	public bool IsOpenWallEast {
		get { return Array.Exists(OpenEast, t => t == tileType); }
	}
	public bool IsOpenWallSouth {
		get { return Array.Exists(OpenSouth, t => t == tileType); }
	}
	public bool IsOpenWallWest {
		get { return Array.Exists(OpenWest, t => t == tileType); }
	}

	public bool IsVoid {
		get { return tileType == TileType.Void; }
	}

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
