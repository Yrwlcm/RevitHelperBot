using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Scenario;

public interface IScenarioService
{
    DialogueNode? GetNode(string id);
    DialogueNode? FindByKeyword(string searchText);
    Task ReloadData(CancellationToken cancellationToken);
}
