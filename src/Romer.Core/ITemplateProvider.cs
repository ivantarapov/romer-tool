namespace Romer.Core;

public interface ITemplateProvider
{
    IReadOnlyList<RomerTemplate> GetTemplates();

    RomerTemplate GetById(string id);
}
