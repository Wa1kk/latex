using System.Text.Json;
using IndoorNav.Models;

namespace IndoorNav.Services;

public class EmergencyChangedArgs : EventArgs
{
    public string? BuildingId { get; init; }
    public bool IsActive { get; init; }
}

public class EmergencyService
{
    public event EventHandler<EmergencyChangedArgs>? EmergencyChanged;

    private readonly HashSet<string> _activeBuildings = new();

    private static string GetProjectRootPath()
    {
        var basePath = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(basePath);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IndoorNav.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? basePath;
    }

    private static string StatePath =>
        Path.Combine(GetProjectRootPath(), "Resources", "Raw", "emergency_state.json");

    public bool IsEmergencyActive => _activeBuildings.Count > 0;

    public bool IsActiveForBuilding(string? buildingId) =>
        !string.IsNullOrEmpty(buildingId) && _activeBuildings.Contains(buildingId);

    public string EmergencyMessage => IsEmergencyActive
        ? "РЕЖИМ ЧРЕЗВЫЧАЙНОЙ СИТУАЦИИ - следуйте по маршруту до выхода!"
        : string.Empty;

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(StatePath)) return;
            var json = await File.ReadAllTextAsync(StatePath);
            var ids  = JsonSerializer.Deserialize<List<string>>(json);
            if (ids == null || ids.Count == 0) return;
            foreach (var id in ids)
                _activeBuildings.Add(id);
            EmergencyChanged?.Invoke(this, new EmergencyChangedArgs { BuildingId = null, IsActive = true });
        }
        catch { }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_activeBuildings.ToList());
            File.WriteAllText(StatePath, json);
        }
        catch { }
    }

    public void Activate(string buildingId)
    {
        if (_activeBuildings.Add(buildingId))
        {
            Save();
            EmergencyChanged?.Invoke(this, new EmergencyChangedArgs { BuildingId = buildingId, IsActive = true });
        }
    }

    public void Deactivate(string buildingId)
    {
        if (_activeBuildings.Remove(buildingId))
        {
            Save();
            EmergencyChanged?.Invoke(this, new EmergencyChangedArgs { BuildingId = buildingId, IsActive = false });
        }
    }

    public List<NavNode> FindNearestExitRoute(NavNode start, NavGraph graph, ISet<string>? excludeIds)
    {
        var exits = graph.Nodes
            .Where(n => (n.IsExit || n.IsEvacuationExit)
                        && n.BuildingId == start.BuildingId
                        && excludeIds?.Contains(n.Id) != true)
            .ToList();
        if (!exits.Any())
            exits = graph.Nodes
                .Where(n => (n.IsExit || n.IsEvacuationExit) && excludeIds?.Contains(n.Id) != true)
                .ToList();
        if (!exits.Any()) return new();

        var dist  = new Dictionary<string, double>();
        var prev  = new Dictionary<string, string?>();
        var queue = new SortedSet<(double d, string id)>(Comparer<(double d, string id)>.Create(
            (a, b) => a.d != b.d ? a.d.CompareTo(b.d) : string.Compare(a.id, b.id, StringComparison.Ordinal)));

        foreach (var n in graph.Nodes)
        {
            dist[n.Id] = double.MaxValue;
            prev[n.Id] = null;
        }
        dist[start.Id] = 0;
        queue.Add((0, start.Id));

        var nodeMap = graph.Nodes.ToDictionary(n => n.Id);
        var adj     = BuildAdjacency(graph);

        while (queue.Count > 0)
        {
            var (d, uid) = queue.Min;
            queue.Remove(queue.Min);

            if (!adj.TryGetValue(uid, out var neighbours)) continue;
            foreach (var (nid, w) in neighbours)
            {
                if (nid != start.Id && excludeIds?.Contains(nid) == true) continue;

                var alt = d + w;
                if (alt < dist[nid])
                {
                    queue.Remove((dist[nid], nid));
                    dist[nid] = alt;
                    prev[nid]  = uid;
                    queue.Add((alt, nid));
                }
            }
        }

        var bestExit = exits.OrderBy(e => dist.GetValueOrDefault(e.Id, double.MaxValue)).FirstOrDefault();
        if (bestExit == null || dist[bestExit.Id] == double.MaxValue) return new();

        var path = new List<NavNode>();
        string? cur = bestExit.Id;
        while (cur != null)
        {
            if (nodeMap.TryGetValue(cur, out var node)) path.Add(node);
            prev.TryGetValue(cur, out cur);
        }
        path.Reverse();
        return path;
    }

    private static Dictionary<string, List<(string id, double w)>> BuildAdjacency(NavGraph graph)
    {
        var adj = new Dictionary<string, List<(string, double)>>();
        foreach (var n in graph.Nodes)
            adj[n.Id] = new();

        var nodeMap = graph.Nodes.ToDictionary(n => n.Id);
        foreach (var e in graph.Edges)
        {
            if (!nodeMap.TryGetValue(e.FromId, out var a)) continue;
            if (!nodeMap.TryGetValue(e.ToId,   out var b)) continue;
            double w = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            adj[e.FromId].Add((e.ToId, w));
            adj[e.ToId].Add((e.FromId, w));
        }
        return adj;
    }
}
