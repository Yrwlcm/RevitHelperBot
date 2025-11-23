using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Scenario;

public class ScenarioService : IScenarioService
{
    private readonly IScenarioRepository repository;
    private readonly SemaphoreSlim reloadLock = new(1, 1);
    private IReadOnlyDictionary<string, DialogueNode> cache;

    public ScenarioService(IScenarioRepository repository)
    {
        this.repository = repository;
        cache = new Dictionary<string, DialogueNode>(repository.LoadScenario(), StringComparer.OrdinalIgnoreCase);
    }

    public DialogueNode? GetNode(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        cache.TryGetValue(id, out var node);
        return node;
    }

    public async Task ReloadData(CancellationToken cancellationToken)
    {
        await reloadLock.WaitAsync(cancellationToken);
        try
        {
            var data = repository.LoadScenario();
            cache = new Dictionary<string, DialogueNode>(data, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            reloadLock.Release();
        }
    }
}
