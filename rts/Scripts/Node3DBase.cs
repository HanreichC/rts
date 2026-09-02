using Godot;

namespace rts.scripts;

public partial class Node3DBase : Node3D
{
    [Export] public MeshInstance3D Body { get; set; }
    
    /// <summary>
    /// Override to build/rebuild this tile's own mesh geometry (e.g. procedural meshes).
    /// Runs before the tile is aligned so its bottom flush with other tiles, so any geometry
    /// built here is already accounted for in that alignment.
    /// </summary>
    protected virtual void BuildGeometry()
    {
    }
    
    /// <summary>
    /// Computes the tile's actual walkable surface height (local Y) by inspecting the
    /// combined bounding box of all mesh geometry under this tile. This works for any tile
    /// type regardless of how tall or where its mesh sits, without any hardcoded values.
    /// </summary>
    protected float GetSurfaceHeight() => GetLocalAabb(Body).End.Y;

    /// <summary>
    /// Computes how far the lowest point of a node's combined mesh geometry lies below (or
    /// above) the node's own origin, in the node's local Y axis. Used to compensate for
    /// building models whose pivot isn't at the mesh's bottom, so placement always rests
    /// exactly on the target surface regardless of pivot position.
    /// </summary>
    public float GetLocalBottomY() => GetLocalAabb(this).Position.Y;

    /// <summary>
    /// Computes how far the node's combined mesh geometry reaches out from its own origin on
    /// the XZ plane. Used to place things beside a node instead of inside it, without
    /// hardcoding per-model footprints.
    /// </summary>
    public float GetLocalRadius()
    {
        var aabb = GetLocalAabb(this);

        var x = Mathf.Max(Mathf.Abs(aabb.Position.X), Mathf.Abs(aabb.End.X));
        var z = Mathf.Max(Mathf.Abs(aabb.Position.Z), Mathf.Abs(aabb.End.Z));

        return Mathf.Sqrt(x * x + z * z);
    }

    /// <summary>
    /// Merges the bounding boxes of every mesh below <paramref name="from"/> into a single
    /// box expressed in this node's local space. Returns an empty box when there is no mesh.
    /// </summary>
    private Aabb GetLocalAabb(Node from)
    {
        var aabb = new Aabb();
        var hasAabb = false;

        CollectAabb(this, from, ref aabb, ref hasAabb);

        return aabb;
    }

    private static void CollectAabb(Node3D root, Node current, ref Aabb aabb, ref bool hasAabb)
    {
        if (current == null)
            return;

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
}
