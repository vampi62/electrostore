using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore;

public class AddTotalCountHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameters = context.ApiDescription.ParameterDescriptions;
        AddDescriptionsToQueryParameters(operation, parameters);
    }
    
    private static void AddDescriptionsToQueryParameters(OpenApiOperation operation, IList<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription> parameters)
    {
        foreach (var parameter in operation.Parameters)
        {
            if (parameter.In == ParameterLocation.Query)
            {
                var queryParameter = parameters.FirstOrDefault(p => p.Name == parameter.Name && p.Source.Id == "Query");
                if (queryParameter != null)
                {
                    var description = queryParameter.CustomAttributes().OfType<SwaggerParameterAttribute>().FirstOrDefault()?.Description;
                    if (!string.IsNullOrEmpty(description))
                    {
                        parameter.Description = description;
                    }
                }
            }
        }
    }
}
