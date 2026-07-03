using System.Text.Json;

namespace ElectrostoreNOTIF.Services.NotificationTemplateService;

public interface INotificationTemplateService
{
    /// <summary>
    /// Charge le template <paramref name="templateId"/> pour la langue demandée,
    /// remplace les placeholders et retourne le rendu.
    /// Supporte les scalaires ({{key}}) et les listes ({{#each key}}…{{/each}}).
    /// Retourne null si le template est introuvable.
    /// </summary>
    NotificationTemplateRender? RenderTemplate(
        string templateId,
        IDictionary<string, JsonElement>? values,
        string? language);
}
