using System;
using Godot;
using rts.Exceptions;
using rts.Helpers;
using rts.scripts.Buildings;
using rts.scripts.constructionSites;
using rts.scripts.Player;

namespace rts.scripts.hexTiles;

public partial class HexTileBase : Node3DBase
{
    [Export] public PackedScene ConstructionSiteScene { get; set; }

    private float? _surfaceHeight;
    protected float SurfaceHeight => _surfaceHeight ??= GetSurfaceHeight();

    public int Q { get; set; }
    public int R { get; set; }

    public ConstructionSiteBase ConstructionSite { get; private set; }

    private BuildingBase _building;

    public BuildingBase Building
    {
        get => _building;
        private set
        {
            if (_building != null)
                RemoveChild(_building);

            if (value != null)
                AddChild(value);

            _building = value;
        }
    }

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

    public virtual void TryPlace(
        Vector2I key,
        float hexSize)
    {
        EnsureCanBePlaced();
        SpendPlacementRequirements();

        Position = WorldHelper.AxialToWorld(key, hexSize);
        Q = key.X;
        R = key.Y;
    }

    private static void SpendPlacementRequirements()
        => PlayerResources.Instance[PlayerResource.PlayerResourceType.HexTile].TryIncrement();

    private static void EnsureCanBePlaced()
    {
        var resource =
            PlayerResources.Instance[PlayerResource.PlayerResourceType.HexTile];

        if (resource is null
            || !resource.CanIncrement())
            throw new BuildRequirementsAreNotMetException();
    }


    public void SetConstructionSiteVisible(bool visible)
    {
        if (ConstructionSite == null)
            return;

        if (!AllowedToAddBuilding)
            visible = false;

        ConstructionSite.Visible = visible;
    }

    public void OpenConstructionSitePicker()
    {
        if (ConstructionSite == null
            || !AllowedToAddBuilding)
            return;

        ConstructionSite.BuildingSelected += OnBuildingPickerSelected;
        ConstructionSite.OpenPicker();
    }

    private void OnBuildingPickerSelected(PackedScene scene)
    {
        if (scene.Instantiate() is not BuildingBase building)
        {
            GD.PrintErr("Building scene root not Node3D");
            return;
        }

        try
        {
            AddBuilding(building);
        }
        catch (BuildRequirementsAreNotMetException)
        {
            // ToDo: Show error message
        }

        CloseConstructionSitePicker();
    }

    public void CloseConstructionSitePicker()
    {
        if (ConstructionSite == null)
            return;

        ConstructionSite.BuildingSelected -= OnBuildingPickerSelected;
        ConstructionSite.ClosePicker();
    }

    public void AddBuilding(BuildingBase building)
    {
        if (building == null
            || !AllowedToAddBuilding)
            return;

        building.TryBuild(
            new Vector3(0, SurfaceHeight - GetLocalBottomY(building), 0),
            new Vector3(0, new Random().Next(6) * 60f, 0));

        Building = building;

        SetConstructionSiteVisible(false);
    }
}