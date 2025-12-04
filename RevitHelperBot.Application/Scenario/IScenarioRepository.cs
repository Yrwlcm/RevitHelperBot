using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Scenario;

public interface IScenarioRepository
{
    Dictionary<string, DialogueNode> LoadScenario();
}
