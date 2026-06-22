namespace IndoorNav.Models;

public class NavNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;
    public int FloorNumber { get; set; }

    public float X { get; set; }
    public float Y { get; set; }

    public bool IsTransition { get; set; }
    public bool IsElevator { get; set; }
    public string TransitionGroupId { get; set; } = string.Empty;

    public bool IsQrAnchor { get; set; }
    public bool IsExit { get; set; }
    public bool IsFireExtinguisher { get; set; }
    public bool IsEvacuationExit { get; set; }
    public bool IsHidden { get; set; }
    public bool IsLabelHidden { get; set; }

    public float NodeRadiusScale { get; set; } = 1f;
    public float LabelScale { get; set; } = 1f;
    public string? NodeColorHex { get; set; }

    public bool IsRoom { get; set; }
    public bool IsWaypoint { get; set; }
    public string InnerLabel { get; set; } = string.Empty;
    public string SearchTags { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsCategoryHidden { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(SearchTags)
        ? Name
        : $"{Name} {SearchTags}";

    public List<List<float[]>>? Boundaries { get; set; }

    public override string ToString() => Name;
}
