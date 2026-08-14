using Godot;

namespace rts.scripts.hexTiles;

public partial class WaterHexTile : HexTileBase
{
    // Segments per hex edge. The wall's top edge is only linear between hex corners; subdividing
    // it lets the shader's per-vertex wave displacement follow the water surface's ripple shape
    // along the edge instead of just at the 6 corners, closing the gap that appears mid-edge.
    [Export] public int SegmentsPerEdge { get; set; } = 12;

    // How far the walls hang below the water's top surface. The top mesh itself must stay at
    // local y = 0 (the water shader only applies wave displacement to vertices with
    // y >= -0.05, so moving the top mesh would silently kill the wave animation). The whole
    // tile is then lifted by exactly this same amount, so the wall's bottom ring always lands
    // back at world y = 0 - flush with every other hex tile's bottom - no matter what value
    // this is set to. There is deliberately only this single value: surface height and wall
    // depth are the same measurement, not two numbers that must be kept in sync by hand.
    [Export] public float WallDepth { get; set; } = 0.8f;

    private MeshInstance3D _topMesh;
    private float _hexRadius;

    protected override void BuildGeometry()
    {
        _topMesh = FindMeshInstance(this);
        _hexRadius = GetHexRadius(_topMesh);

        BuildTopMesh();
        BuildWalls();
    }

    // Locates the tile's mesh without requiring a manually configured, easy-to-break NodePath.
    private static MeshInstance3D FindMeshInstance(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is MeshInstance3D meshInstance)
                return meshInstance;

            var found = FindMeshInstance(child);
            if (found != null)
                return found;
        }

        return null;
    }

    // Reads the hex radius straight from the shader material instead of keeping a second,
    // separately maintained copy of the same number in C#.
    private static float GetHexRadius(MeshInstance3D topMesh)
    {
        const float defaultRadius = 3.0f;

        var material = topMesh?.GetSurfaceOverrideMaterial(0) ?? topMesh?.Mesh?.SurfaceGetMaterial(0);
        if (material is ShaderMaterial shaderMaterial)
        {
            var value = shaderMaterial.GetShaderParameter("hex_radius");
            if (value.VariantType == Variant.Type.Float)
                return value.AsSingle();
        }

        return defaultRadius;
    }

    private void BuildTopMesh()
    {
        if (_topMesh == null) return;

        var material = _topMesh.GetSurfaceOverrideMaterial(0) ?? _topMesh.Mesh?.SurfaceGetMaterial(0);

        // Triangulated hex disk: 6 triangular sectors (center -> two adjacent hex corners), each
        // subdivided into a regular barycentric triangle grid. This avoids concentric-ring bands
        // (which read visually as a spiral/whirlpool) while still guaranteeing the outer edge
        // vertices land exactly on the hex boundary at the same positions the wall uses.
        var n = Mathf.Max(1, SegmentsPerEdge);
        var center = Vector2.Zero;

        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        for (var i = 0; i < 6; i++)
        {
            var angleA = Mathf.DegToRad(30f + 60f * i);
            var angleB = Mathf.DegToRad(30f + 60f * (i + 1));
            var cornerA = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * _hexRadius;
            var cornerB = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * _hexRadius;

            for (var row = 0; row < n; row++)
            {
                for (var col = 0; col < n - row; col++)
                {
                    var p0 = PointAt(row, col);
                    var p1 = PointAt(row + 1, col);
                    var p2 = PointAt(row, col + 1);

                    var v0 = new Vector3(p0.X, 0f, p0.Y);
                    var v1 = new Vector3(p1.X, 0f, p1.Y);
                    var v2 = new Vector3(p2.X, 0f, p2.Y);

                    surfaceTool.SetNormal(Vector3.Up);
                    surfaceTool.AddVertex(v0);
                    surfaceTool.SetNormal(Vector3.Up);
                    surfaceTool.AddVertex(v1);
                    surfaceTool.SetNormal(Vector3.Up);
                    surfaceTool.AddVertex(v2);

                    // Second triangle of the quad cell, only valid while there is room below-right.
                    if (col >= n - row - 1)
                        continue;

                    var p3 = PointAt(row + 1, col + 1);
                    var v3 = new Vector3(p3.X, 0f, p3.Y);

                    surfaceTool.SetNormal(Vector3.Up);
                    surfaceTool.AddVertex(v1);
                    surfaceTool.SetNormal(Vector3.Up);
                    surfaceTool.AddVertex(v3);
                    surfaceTool.SetNormal(Vector3.Up);
                    surfaceTool.AddVertex(v2);
                }
            }

            continue;

            // Barycentric grid points: point(row, col) = center + row/n*(cornerA-center) + col/n*(cornerB-center),
            // with row+col <= n. This yields a uniform triangular tessellation of the sector.
            Vector2 PointAt(int row, int col)
            {
                var u = (float)row / n;
                var v = (float)col / n;
                return center + u * (cornerA - center) + v * (cornerB - center);
            }
        }

        var arrayMesh = surfaceTool.Commit();
        if (material != null)
            arrayMesh.SurfaceSetMaterial(0, material);
        _topMesh.Mesh = arrayMesh;
    }

    private void BuildWalls()
    {
        var material = _topMesh?.GetSurfaceOverrideMaterial(0) ?? _topMesh?.Mesh?.SurfaceGetMaterial(0);

        // Flat-top-in-X hexagon corners: bisector angles between the 0/60/120 degree edge
        // normals used by the shader's clip_to_hex test, at 30 + 60*k degrees.
        var corners = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.DegToRad(30f + 60f * i);
            corners[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _hexRadius;
        }

        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        for (var i = 0; i < 6; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % 6];
            var normal = new Vector3(b.Y - a.Y, 0f, -(b.X - a.X)).Normalized();

            var segments = Mathf.Max(1, SegmentsPerEdge);
            for (var s = 0; s < segments; s++)
            {
                var t0 = (float)s / segments;
                var t1 = (float)(s + 1) / segments;
                var p0 = a.Lerp(b, t0);
                var p1 = a.Lerp(b, t1);

                var top0 = new Vector3(p0.X, 0f, p0.Y);
                var top1 = new Vector3(p1.X, 0f, p1.Y);
                var bottom0 = new Vector3(p0.X, -WallDepth, p0.Y);
                var bottom1 = new Vector3(p1.X, -WallDepth, p1.Y);

                surfaceTool.SetNormal(normal);
                surfaceTool.AddVertex(top0);
                surfaceTool.SetNormal(normal);
                surfaceTool.AddVertex(bottom0);
                surfaceTool.SetNormal(normal);
                surfaceTool.AddVertex(bottom1);

                surfaceTool.SetNormal(normal);
                surfaceTool.AddVertex(top0);
                surfaceTool.SetNormal(normal);
                surfaceTool.AddVertex(bottom1);
                surfaceTool.SetNormal(normal);
                surfaceTool.AddVertex(top1);
            }
        }

        var arrayMesh = surfaceTool.Commit();
        if (material != null)
            arrayMesh.SurfaceSetMaterial(0, material);

        var wallsInstance = new MeshInstance3D
        {
            Name = "Walls",
            Mesh = arrayMesh
        };
        _topMesh?.GetParent().AddChild(wallsInstance);
    }
}
