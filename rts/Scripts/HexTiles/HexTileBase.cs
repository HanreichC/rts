using System;
using Godot;
using rts.scripts.constructionSites;

namespace rts.scripts.hexTiles;

public partial class HexTileBase : Node3DBase
{
    [Export] public PackedScene ConstructionSiteScene { get; set; }

    private float? _surfaceHeight;
    protected float SurfaceHeight => _surfaceHeight ??= GetSurfaceHeight();

    public int Q { get; set; }
    public int R { get; set; }

    public ConstructionSiteBase ConstructionSite { get; private set; }

    public Node3D Building { get; private set; }

    public bool AllowedToAddBuilding => Building == null;

    public override void _Ready()
    {
        // Let subclasses build/rebuild their own mesh geometry first (e.g. WaterHexTile's
        // procedural top + walls), then align every tile type the same way: whatever the
        // lowest point of the tile's own geometry ends up being, shift the tile so that point
        // sits exactly at world y = 0. This keeps every tile's bottom flush with every other
        // tile's bottom, regardless of each tile's mesh height/pivot, without assuming any
        // tile type already sits at y = 0 by convention.
        BuildGeometry();
        Position += new Vector3(0, -GetLocalBottomY(this), 0);

        if (ConstructionSiteScene == null)
            return;

        ConstructionSite = ConstructionSiteScene.Instantiate<ConstructionSiteBase>();
        AddChild(ConstructionSite);
        ConstructionSite.Position = new Vector3(0, SurfaceHeight - GetLocalBottomY(ConstructionSite), 0);

        SetConstructionSiteVisible(false);
    }

    public void SetConstructionSiteVisible(bool visible)
    {
        if (ConstructionSite == null)
            return;

        if (!AllowedToAddBuilding)
            visible = false;

        ConstructionSite.Visible = visible;
    }

    public void AddBuilding(Node3D building)
    {
        if (building == null
            || !AllowedToAddBuilding)
            return;

        Building = building;
        AddChild(Building);
        Building.Position = new Vector3(0, SurfaceHeight - GetLocalBottomY(Building), 0);
        Building.RotationDegrees =
            new Vector3(0, new Random().Next(6) * 60f, 0); // random rotation 0,60,120,180,240,300
        SetConstructionSiteVisible(false);
    }
}