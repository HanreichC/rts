using Godot;

namespace RTS.Models
{
    public partial class HexTileBase : Node3D
    {
	    public int Q { get; set; } = 0;
	    public int R { get; set; } = 0;
    }
}