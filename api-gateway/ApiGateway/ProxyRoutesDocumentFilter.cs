using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiGateway;

public class ProxyRoutesDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Adicionar endpoints do AuthService
        AddProxyRoute(swaggerDoc, "auth", "Auth Service", new[]
        {
            new { Path = "/api/auth/register", Method = "POST", Description = "Register a new user" },
            new { Path = "/api/auth/login", Method = "POST", Description = "User login" },
            new { Path = "/api/auth/health", Method = "GET", Description = "Health check" },
            new { Path = "/api/auth/users", Method = "GET", Description = "Get all users (Admin only)" },
            new { Path = "/api/auth/users/{id}", Method = "GET", Description = "Get user by ID" }
        });

        // Adicionar endpoints do StockService
        AddProxyRoute(swaggerDoc, "stock", "Stock Service", new[]
        {
            new { Path = "/api/stock/products", Method = "GET", Description = "Get all products" },
            new { Path = "/api/stock/products/{id}", Method = "GET", Description = "Get product by ID" },
            new { Path = "/api/stock/products", Method = "POST", Description = "Create new product (Admin only)" },
            new { Path = "/api/stock/products/{id}", Method = "PUT", Description = "Update product (Admin only)" },
            new { Path = "/api/stock/products/{id}", Method = "DELETE", Description = "Delete product (Admin only)" },
            new { Path = "/api/stock/categories", Method = "GET", Description = "Get all categories" },
            new { Path = "/api/stock/health", Method = "GET", Description = "Health check" }
        });

        // Adicionar endpoints do SalesService
        AddProxyRoute(swaggerDoc, "sales", "Sales Service", new[]
        {
            new { Path = "/api/sales/orders", Method = "GET", Description = "Get user orders" },
            new { Path = "/api/sales/orders/{id}", Method = "GET", Description = "Get order by ID" },
            new { Path = "/api/sales/orders", Method = "POST", Description = "Create new order" },
            new { Path = "/api/sales/orders/{id}/cancel", Method = "PUT", Description = "Cancel order" },
            new { Path = "/api/sales/health", Method = "GET", Description = "Health check" }
        });
    }

    private void AddProxyRoute(OpenApiDocument swaggerDoc, string serviceName, string serviceDescription, dynamic[] endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            var operation = new OpenApiOperation
            {
                Summary = endpoint.Description,
                Description = $"Proxy to {serviceDescription}",
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = serviceName } }
            };

            // Adicionar parâmetros de path se existirem
            if (endpoint.Path.Contains("{id}"))
            {
                operation.Parameters = new List<OpenApiParameter>
                {
                    new OpenApiParameter
                    {
                        Name = "id",
                        In = ParameterLocation.Path,
                        Required = true,
                        Schema = new OpenApiSchema { Type = "string" }
                    }
                };
            }

            // Adicionar responses
            operation.Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse { Description = "Success" },
                ["400"] = new OpenApiResponse { Description = "Bad Request" },
                ["401"] = new OpenApiResponse { Description = "Unauthorized" },
                ["404"] = new OpenApiResponse { Description = "Not Found" },
                ["500"] = new OpenApiResponse { Description = "Internal Server Error" }
            };

            // Adicionar ao documento
            if (!swaggerDoc.Paths.ContainsKey(endpoint.Path))
            {
                swaggerDoc.Paths.Add(endpoint.Path, new OpenApiPathItem());
            }

            var pathItem = swaggerDoc.Paths[endpoint.Path];
            switch (endpoint.Method.ToUpper())
            {
                case "GET":
                    pathItem.Operations[OperationType.Get] = operation;
                    break;
                case "POST":
                    pathItem.Operations[OperationType.Post] = operation;
                    break;
                case "PUT":
                    pathItem.Operations[OperationType.Put] = operation;
                    break;
                case "DELETE":
                    pathItem.Operations[OperationType.Delete] = operation;
                    break;
            }
        }
    }
}
