﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
using Microsoft.OpenApi.Services;
using Xunit;

namespace Microsoft.OpenApi.Tests.Walkers
{
    public class WalkerLocationTests
    {
        [Fact]
        public void LocateTopLevelObjects()
        {
            var doc = new OpenApiDocument();

            var locator = new LocatorVisitor();
            var walker = new OpenApiWalker(locator);
            walker.Walk(doc);

            Assert.Equivalent(new List<string> {
                "#/info",
                "#/servers",
                "#/paths",
                "#/tags"
            }, locator.Locations);
        }

        [Fact]
        public void LocateTopLevelArrayItems()
        {
            var doc = new OpenApiDocument
            {
                Servers = new List<OpenApiServer>
                {
                    new(),
                    new()
                },
                Tags = new List<OpenApiTag>
                {
                    new()
                }
            };

            var locator = new LocatorVisitor();
            var walker = new OpenApiWalker(locator);
            walker.Walk(doc);

            Assert.Equivalent(new List<string> {
                "#/info",
                "#/servers",
                "#/servers/0",
                "#/servers/1",
                "#/paths",
                "#/tags",
                "#/tags/0"
            }, locator.Locations);
        }

        [Fact]
        public void LocatePathOperationContentSchema()
        {
            var doc = new OpenApiDocument();
            doc.Paths.Add("/test", new OpenApiPathItem()
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Get] = new()
                    {
                        Responses = new()
                        {
                            ["200"] = new OpenApiResponse()
                            {
                                Content = new Dictionary<string, OpenApiMediaType>
                                {
                                    ["application/json"] = new()
                                    {
                                        Schema = new OpenApiSchema
                                        {
                                            Type = JsonSchemaType.String
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            var locator = new LocatorVisitor();
            var walker = new OpenApiWalker(locator);
            walker.Walk(doc);

            Assert.Equivalent(new List<string> {
                "#/info",
                "#/servers",
                "#/paths",
                "#/paths/~1test",
                "#/paths/~1test/get",
                "#/paths/~1test/get/responses",
                "#/paths/~1test/get/responses/200",
                "#/paths/~1test/get/responses/200/content",
                "#/paths/~1test/get/responses/200/content/application~1json",
                "#/paths/~1test/get/responses/200/content/application~1json/schema",
                "#/paths/~1test/get/tags",
                "#/tags",

            }, locator.Locations);

            Assert.Equivalent(new List<string> { "/test", "Get", "200", "application/json" }, locator.Keys);
        }

        [Fact]
        public void WalkDOMWithCycles()
        {
            var loopySchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["name"] = new() { Type = JsonSchemaType.String }
                }
            };

            loopySchema.Properties.Add("parent", loopySchema);

            var doc = new OpenApiDocument
            {
                Components = new()
                {
                    Schemas = new Dictionary<string, OpenApiSchema>
                    {
                        ["loopy"] = loopySchema
                    }
                }
            };

            var locator = new LocatorVisitor();
            var walker = new OpenApiWalker(locator);
            walker.Walk(doc);

            Assert.Equivalent(new List<string> {
                "#/info",
                "#/servers",
                "#/paths",
                "#/components",
                "#/components/schemas/loopy",
                "#/components/schemas/loopy/properties/name",
                "#/tags"
            }, locator.Locations);
        }

        /// <summary>
        /// Walk document and discover all references to components, including those inside components
        /// </summary>
        [Fact]
        public void LocateReferences()
        {
            var baseSchema = new OpenApiSchemaReference("base", null);

            var derivedSchema = new OpenApiSchema
            {
                AnyOf = new List<OpenApiSchema> { baseSchema },
                Reference = new()
                {
                    Id = "derived",
                    Type = ReferenceType.Schema
                },
                UnresolvedReference = false
            };

            var testHeader = new OpenApiHeader()
            {
                Schema = derivedSchema,
            };
            var testHeaderReference = new OpenApiHeaderReference(testHeader, "test-header");

            var doc = new OpenApiDocument
            {
                Paths = new()
                {
                    ["/"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            [OperationType.Get] = new()
                            {
                                Responses = new()
                                {
                                    ["200"] = new OpenApiResponse()
                                    {
                                        Content = new Dictionary<string, OpenApiMediaType>
                                        {
                                            ["application/json"] = new()
                                            {
                                                Schema = derivedSchema
                                            }
                                        },
                                        Headers =
                                        {
                                            ["test-header"] = testHeaderReference
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                Components = new()
                {
                    Schemas = new Dictionary<string, OpenApiSchema>
                    {
                        ["derived"] = derivedSchema,
                        ["base"] = baseSchema,
                    },
                    Headers =
                    {
                        ["test-header"] = testHeader
                    },
                    SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
                    {
                        ["test-secScheme"] = new OpenApiSecuritySchemeReference("reference-to-scheme", null, null)
                    }
                }
            };

            var locator = new LocatorVisitor();
            var walker = new OpenApiWalker(locator);
            walker.Walk(doc);

            Assert.Equivalent(new List<string> {
                "referenceAt: #/paths/~1/get/responses/200/content/application~1json/schema",
                "referenceAt: #/paths/~1/get/responses/200/headers/test-header",
                "referenceAt: #/components/schemas/derived/anyOf/0",
                "referenceAt: #/components/securitySchemes/test-secScheme",
                "referenceAt: #/components/headers/test-header/schema"
            }, locator.Locations.Where(l => l.StartsWith("referenceAt:", StringComparison.OrdinalIgnoreCase)));
        }
    }

    internal class LocatorVisitor : OpenApiVisitorBase
    {
        public List<string> Locations = new();
        public List<string> Keys = new();

        public override void Visit(OpenApiInfo info)
        {
            Locations.Add(this.PathString);
        }

        public override void Visit(OpenApiComponents components)
        {
            Locations.Add(this.PathString);
        }

        public override void Visit(OpenApiExternalDocs externalDocs)
        {
            Locations.Add(this.PathString);
        }

        public override void Visit(OpenApiPaths paths)
        {
            Locations.Add(this.PathString);
        }

        public override void Visit(IOpenApiPathItem pathItem)
        {
            Keys.Add(CurrentKeys.Path);
            Locations.Add(this.PathString);
        }

        public override void Visit(OpenApiResponses response)
        {
            Locations.Add(this.PathString);
        }

        public override void Visit(OpenApiOperation operation)
        {
            Keys.Add(CurrentKeys.Operation.ToString());
            Locations.Add(this.PathString);
        }
        public override void Visit(IOpenApiResponse response)
        {
            Keys.Add(CurrentKeys.Response);
            Locations.Add(this.PathString);
        }

        public override void Visit(IOpenApiReferenceHolder referenceable)
        {
            Locations.Add("referenceAt: " + this.PathString);
        }
        public override void Visit(IDictionary<string, OpenApiMediaType> content)
        {
            Locations.Add(this.PathString);
        }

        public override void Visit(OpenApiMediaType mediaType)
        {
            Keys.Add(CurrentKeys.Content);
            Locations.Add(this.PathString);
        }

        public override void Visit(OpenApiSchema schema)
        {
            Locations.Add(this.PathString);
        }

        public override void Visit(IList<OpenApiTag> openApiTags)
        {
            Locations.Add(this.PathString);
        }

        public override void Visit(IList<OpenApiServer> servers)
        {
            Locations.Add(this.PathString);
        }

        public override void Visit(OpenApiServer server)
        {
            Locations.Add(this.PathString);
        }
        public override void Visit(IList<OpenApiTagReference> openApiTags)
        {
            Locations.Add(this.PathString);
        }
    }
}
