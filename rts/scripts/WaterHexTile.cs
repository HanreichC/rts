using Godot;
using RTS.Implementations;

public partial class WaterHexTile : HexTileBase
{
	// Must match shader_parameter/hex_radius on the top mesh's material (the shader's
	// clip_to_hex test uses this as the hexagon's circumradius).
	[Export] public float HexRadius { get; set; } = 3.0f;
	[Export] public float WallHeight { get; set; } = 1f;
	[Export] public NodePath TopMeshPath { get; set; } = "Body/Mesh";
	// Segments per hex edge. The wall's top edge is only linear between hex corners; subdividing
	// it lets the shader's per-vertex wave displacement follow the water surface's ripple shape
	// along the edge instead of just at the 6 corners, closing the gap that appears mid-edge.
	[Export] public int SegmentsPerEdge { get; set; } = 12;

	public override void _Ready()
	{
		BuildTopMesh();
		BuildWalls();
	}

	private void BuildTopMesh()
	{
		var topMesh = GetNodeOrNull<MeshInstance3D>(TopMeshPath);
		if (topMesh == null) return;

		var material = topMesh.GetSurfaceOverrideMaterial(0) ?? topMesh.Mesh?.SurfaceGetMaterial(0);

		// Triangulated hex disk: 6 triangular sectors (center -> two adjacent hex corners), each
		// subdivided into a regular barycentric triangle grid. This avoids concentric-ring bands
		// (which read visually as a spiral/whirlpool) while still guaranteeing the outer edge
		// vertices land exactly on the hex boundary at the same positions the wall uses.
		int n = Mathf.Max(1, SegmentsPerEdge);
		var center = Vector2.Zero;

		var surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

		for (int i = 0; i < 6; i++)
		{
			float angleA = Mathf.DegToRad(30f + 60f * i);
			float angleB = Mathf.DegToRad(30f + 60f * (i + 1));
			Vector2 cornerA = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * HexRadius;
			Vector2 cornerB = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * HexRadius;

			// Barycentric grid points: point(row, col) = center + row/n*(cornerA-center) + col/n*(cornerB-center),
			// with row+col <= n. This yields a uniform triangular tessellation of the sector.
			Vector2 PointAt(int row, int col)
			{
				float u = (float)row / n;
				float v = (float)col / n;
				return center + u * (cornerA - center) + v * (cornerB - center);
			}

			for (int row = 0; row < n; row++)
			{
				for (int col = 0; col < n - row; col++)
				{
					Vector2 p0 = PointAt(row, col);
					Vector2 p1 = PointAt(row + 1, col);
					Vector2 p2 = PointAt(row, col + 1);

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
					if (col < n - row - 1)
					{
						Vector2 p3 = PointAt(row + 1, col + 1);
						var v3 = new Vector3(p3.X, 0f, p3.Y);

						surfaceTool.SetNormal(Vector3.Up);
						surfaceTool.AddVertex(v1);
						surfaceTool.SetNormal(Vector3.Up);
						surfaceTool.AddVertex(v3);
						surfaceTool.SetNormal(Vector3.Up);
						surfaceTool.AddVertex(v2);
					}
				}
			}
		}

		var arrayMesh = surfaceTool.Commit();
		if (material != null)
			arrayMesh.SurfaceSetMaterial(0, material);
		topMesh.Mesh = arrayMesh;
	}

	private void BuildWalls()
	{
		var topMesh = GetNodeOrNull<MeshInstance3D>(TopMeshPath);
		var material = topMesh?.GetSurfaceOverrideMaterial(0) ?? topMesh?.Mesh?.SurfaceGetMaterial(0);

		// Flat-top-in-X hexagon corners: bisector angles between the 0/60/120 degree edge
		// normals used by the shader's clip_to_hex test, at 30 + 60*k degrees.
		var corners = new Vector2[6];
		for (int i = 0; i < 6; i++)
		{
			float angle = Mathf.DegToRad(30f + 60f * i);
			corners[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * HexRadius;
		}

		var surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

		for (int i = 0; i < 6; i++)
		{
			Vector2 a = corners[i];
			Vector2 b = corners[(i + 1) % 6];
			Vector3 normal = new Vector3(b.Y - a.Y, 0f, -(b.X - a.X)).Normalized();

			int segments = Mathf.Max(1, SegmentsPerEdge);
			for (int s = 0; s < segments; s++)
			{
				float t0 = (float)s / segments;
				float t1 = (float)(s + 1) / segments;
				Vector2 p0 = a.Lerp(b, t0);
				Vector2 p1 = a.Lerp(b, t1);

				var top0 = new Vector3(p0.X, 0f, p0.Y);
				var top1 = new Vector3(p1.X, 0f, p1.Y);
				var bottom0 = new Vector3(p0.X, -WallHeight, p0.Y);
				var bottom1 = new Vector3(p1.X, -WallHeight, p1.Y);

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
		topMesh?.GetParent().AddChild(wallsInstance);
	}
}
