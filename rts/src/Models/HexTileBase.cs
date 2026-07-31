using Godot;

namespace RTS.Models
{
    public partial class HexTileBase : Node3D
    {
        [Export] public PackedScene ConstructionSiteScene { get; set; }
        [Export] public float ConstructionSiteHeight { get; set; } = 0.5f;

        public int Q { get; set; } = 0;
        public int R { get; set; } = 0;

        private Node3D _constructionSite;

        public void SetConstructionSiteVisible(bool visible)
        {
            if (visible && _constructionSite == null && ConstructionSiteScene != null)
            {
                _constructionSite = ConstructionSiteScene.Instantiate<Node3D>();
                AddChild(_constructionSite);
                _constructionSite.Position = new Vector3(0, ConstructionSiteHeight, 0);
            }

            if (_constructionSite != null)
                _constructionSite.Visible = visible;
        }
    }
}