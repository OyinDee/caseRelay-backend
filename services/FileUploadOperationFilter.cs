using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using System;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        const string fileUploadMime = "multipart/form-data";

        // Check if the endpoint supports file upload
        if (operation.RequestBody == null || !operation.RequestBody.Content.Any(x => x.Key.Equals(fileUploadMime, StringComparison.InvariantCultureIgnoreCase)))
        {
            var formFileParameters = context.ApiDescription.ParameterDescriptions
                .Where(p => 
                    p.Type == typeof(IFormFile) || 
                    (p.Type.IsGenericType && p.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                        && p.Type.GetGenericArguments()[0] == typeof(IFormFile)))
                .ToList();

            if (!formFileParameters.Any())
            {
                return;
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [fileUploadMime] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = formFileParameters.ToDictionary(
                                param => param.Name,
                                param => new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "Upload a file"
                                }),
                            Required = new HashSet<string>(formFileParameters.Select(p => p.Name))
                        }
                    }
                }
            };
        }
    }
}
