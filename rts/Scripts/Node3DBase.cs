using Godot;

namespace rts.scripts;

public partial class Node3DBase : Node3D
{
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
    protected float GetSurfaceHeight()
    {
        var aabb = new Aabb();
        var hasAabb = false;

        CollectAabb(this, this, ref aabb, ref hasAabb);

        return hasAabb ? aabb.Position.Y + aabb.Size.Y : 0f;
    }

    protected static void CollectAabb(Node3D root, Node current, ref Aabb aabb, ref bool hasAabb)
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