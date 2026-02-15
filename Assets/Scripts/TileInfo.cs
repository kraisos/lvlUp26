using System;
using System.Collections.Generic;

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

[Flags]
public enum TileFlags
{
	None       = 0,
	LinkNorth  = 1 << 0,
	LinkEast   = 1 << 1,
	LinkSouth  = 1 << 2,
	LinkWest   = 1 << 3,
	IsWall     = 1 << 4,
	IsDoor     = 1 << 5,
}

public class TileTypeInfo : IWeighted
{
	public readonly TileType tileType;
	public readonly TileFlags flags;

	public float Weight { get; }

	public bool LinkNorth => (flags & TileFlags.LinkNorth) != 0;
	public bool LinkEast  => (flags & TileFlags.LinkEast)  != 0;
	public bool LinkSouth => (flags & TileFlags.LinkSouth) != 0;
	public bool LinkWest  => (flags & TileFlags.LinkWest)  != 0;
	public bool IsWall    => (flags & TileFlags.IsWall)    != 0;
	public bool IsDoor    => (flags & TileFlags.IsDoor)    != 0;

	public TileTypeInfo(TileType type, TileFlags flags, float weight = 1f)
	{
		tileType = type;
		this.flags = flags;
		this.Weight = weight;
	}

	public static readonly Dictionary<TileType, TileTypeInfo> WallTypes = new Dictionary<TileType, TileTypeInfo> {
		{ TileType.WallNW,         new TileTypeInfo(TileType.WallNW,         TileFlags.LinkEast  | TileFlags.LinkSouth | TileFlags.IsWall) },
		{ TileType.WallNE,         new TileTypeInfo(TileType.WallNE,         TileFlags.LinkSouth | TileFlags.LinkWest  | TileFlags.IsWall) },
		{ TileType.WallSE,         new TileTypeInfo(TileType.WallSE,         TileFlags.LinkNorth | TileFlags.LinkWest  | TileFlags.IsWall) },
		{ TileType.WallSW,         new TileTypeInfo(TileType.WallSW,         TileFlags.LinkNorth | TileFlags.LinkEast  | TileFlags.IsWall) },
		{ TileType.WallVertical,   new TileTypeInfo(TileType.WallVertical,   TileFlags.LinkNorth | TileFlags.LinkSouth | TileFlags.IsWall, .8f) },
		{ TileType.WallHorizontal, new TileTypeInfo(TileType.WallHorizontal, TileFlags.LinkEast  | TileFlags.LinkWest  | TileFlags.IsWall, .8f) },
		{ TileType.WallTN,         new TileTypeInfo(TileType.WallTN,         TileFlags.LinkEast  | TileFlags.LinkSouth | TileFlags.LinkWest | TileFlags.IsWall, .5f) },
		{ TileType.WallTE,         new TileTypeInfo(TileType.WallTE,         TileFlags.LinkNorth | TileFlags.LinkSouth | TileFlags.LinkWest | TileFlags.IsWall, .5f) },
		{ TileType.WallTS,         new TileTypeInfo(TileType.WallTS,         TileFlags.LinkNorth | TileFlags.LinkEast  | TileFlags.LinkWest | TileFlags.IsWall, .5f) },
		{ TileType.WallTW,         new TileTypeInfo(TileType.WallTW,         TileFlags.LinkNorth | TileFlags.LinkEast  | TileFlags.LinkSouth | TileFlags.IsWall, .5f) },
		{ TileType.WallCross,      new TileTypeInfo(TileType.WallCross,      TileFlags.LinkNorth | TileFlags.LinkEast  | TileFlags.LinkSouth | TileFlags.LinkWest | TileFlags.IsWall, .5f) },
		{ TileType.DoorVertical,   new TileTypeInfo(TileType.DoorVertical,   TileFlags.LinkNorth | TileFlags.LinkSouth | TileFlags.IsWall | TileFlags.IsDoor) },
		{ TileType.DoorHorizontal, new TileTypeInfo(TileType.DoorHorizontal, TileFlags.LinkEast  | TileFlags.LinkWest  | TileFlags.IsWall | TileFlags.IsDoor) },
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

	public static readonly TileInfo Void = new(TileType.Void);

	public bool IsLinkNorth => GetFlags().HasFlag(TileFlags.LinkNorth);
	public bool IsLinkEast  => GetFlags().HasFlag(TileFlags.LinkEast);
	public bool IsLinkSouth => GetFlags().HasFlag(TileFlags.LinkSouth);
	public bool IsLinkWest  => GetFlags().HasFlag(TileFlags.LinkWest);

	public bool IsVoid {
		get { return tileType == TileType.Void; }
	}

	private TileFlags GetFlags()
	{
		if (TileTypeInfo.WallTypes.TryGetValue(tileType, out TileTypeInfo info))
		{
			return info.flags;
		}
		return TileFlags.None;
	}
}
