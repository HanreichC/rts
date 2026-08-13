using System;
using Godot;
using rts.scripts.constructionSites;

namespace rts.scripts.hexTiles;

public partial class HexTileBase : Node3D
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

    /// <summary>
    /// Override to build/rebuild this tile's own mesh geometry (e.g. procedural meshes).
    /// Runs before the tile is aligned so its bottom flush with other tiles, so any geometry
    /// built here is already accounted for in that alignment.
    /// </summary>
    protected virtual void BuildGeometry()
    {
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

    /// <summary>
    /// Computes the tile's actual walkable surface height (local Y) by inspecting the
    /// combined bounding box of all mesh geometry under this tile. This works for any tile
    /// type regardless of how tall or where its mesh sits, without any hardcoded values.
    /// </summary>
    private float GetSurfaceHeight()
    {
        var aabb = new Aabb();
        var hasAabb = false;

        CollectAabb(this, this, ref aabb, ref hasAabb);

        return hasAabb ? aabb.Position.Y + aabb.Size.Y : 0f;
    }

    private static void CollectAabb(Node3D root, Node current, ref Aabb aabb, ref bool hasAabb)
    {
        if (current is MeshInstance3D meshInstance
            && meshInstance.Mesh != null)
        {
            // Transform the mesh's AABB from its own local space into root's local space.
            var relativeTransform = root.GlobalTransform.AffineInverse() * meshInstance.GlobalTransform;
            var meshAabb = relativeTransform * meshInstance.GetAabb();

            aabb = hasAabb ? aabb.Merge(meshAabb) : meshAabb;
            hasAabb = true;
        }

        foreach (var child in current.GetChildren())
            CollectAabb(root, child, ref aabb, ref hasAabb);
    }

    /// <summary>
    /// Computes how far the lowest point of a node's combined mesh geometry lies below (or
    /// above) the node's own origin, in the node's local Y axis. Used to compensate for
    /// building models whose pivot isn't at the mesh's bottom, so placement always rests
    /// exactly on the target surface regardless of pivot position.
    /// </summary>
    protected static float GetLocalBottomY(Node3D node)
    {
        var aabb = new Aabb();
        var hasAabb = false;

        CollectAabb(node, node, ref aabb, ref hasAabb);

        return hasAabb ? aabb.Position.Y : 0f;
    }
}