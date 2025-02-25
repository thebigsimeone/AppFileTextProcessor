using AppFileTextProcessor.Models;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AppFileTextProcessor.Helpers
{
    public class SwaggerFileSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(PdfUploadModel))
            {
                schema.Type = "object";
                schema.Properties["file"] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
            }
        }
    }
}
