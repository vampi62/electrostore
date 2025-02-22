using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Annotations;

public class AddTotalCountHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameters = context.ApiDescription.ParameterDescriptions;
        var hasLimit = parameters.Any(p => p.Name == "limit" && p.Source.Id == "Query");
        var hasOffset = parameters.Any(p => p.Name == "offset" && p.Source.Id == "Query");

        if (hasLimit && hasOffset)
        {
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                if (response.Headers is null)
                {
                    response.Headers = new Dictionary<string, OpenApiHeader>();
                }

                response.Headers.Add("X-Total-Count", new OpenApiHeader
                {
                    Description = "Total number of items",
                    Schema = new OpenApiSchema
                    {
                        Type = "integer"
                    }
                });
            }
        }
        // description for query parameters
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
