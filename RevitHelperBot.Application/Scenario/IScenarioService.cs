using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Scenario;

public interface IScenarioService
{
    DialogueNode? GetNode(string id);
    Task ReloadData(CancellationToken cancellationToken);
}
