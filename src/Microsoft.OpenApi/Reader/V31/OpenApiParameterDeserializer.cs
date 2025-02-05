﻿using System;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
using Microsoft.OpenApi.Reader.ParseNodes;

namespace Microsoft.OpenApi.Reader.V31
{
    /// <summary>
    /// Class containing logic to deserialize Open API V31 document into
    /// runtime Open API object model.
    /// </summary>
    internal static partial class OpenApiV31Deserializer
    {
        private static readonly FixedFieldMap<OpenApiParameter> _parameterFixedFields =
            new()
            {
                {
                    "name", (o, n, _) =>
                    {
                        o.Name = n.GetScalarValue();
                    }
                },
                {
                    "in", (o, n, _) =>
                    {
                        if (!n.GetScalarValue().TryGetEnumFromDisplayName<ParameterLocation>(n.Context, out var _in))
                        {
                            return;
                        }
                        o.In = _in;
                    }
                },
                {
                    "description", (o, n, _) =>
                    {
                        o.Description = n.GetScalarValue();
                    }
                },
                {
                    "required", (o, n, _) =>
                    {
                        o.Required = bool.Parse(n.GetScalarValue());
                    }
                },
                {
                    "deprecated", (o, n, _) =>
                    {
                        o.Deprecated = bool.Parse(n.GetScalarValue());
                    }
                },
                {
                    "allowEmptyValue", (o, n, _) =>
                    {
                        o.AllowEmptyValue = bool.Parse(n.GetScalarValue());
                    }
                },
                {
                    "allowReserved", (o, n, _) =>
                    {
                        o.AllowReserved = bool.Parse(n.GetScalarValue());
                    }
                },
                {
                    "style", (o, n, _) =>
                    {
                        if (!n.GetScalarValue().TryGetEnumFromDisplayName<ParameterStyle>(n.Context, out var style))
                        {
                            return;
                        }
                        o.Style = style;
                    }
                },
                {
                    "explode", (o, n, _) =>
                    {
                        o.Explode = bool.Parse(n.GetScalarValue());
                    }
                },
                {
                    "schema", (o, n, t) =>
                    {
                        o.Schema = LoadSchema(n, t);
                    }
                },
                {
                    "content", (o, n, t) =>
                    {
                        o.Content = n.CreateMap(LoadMediaType, t);
                    }
                },
                {
                    "examples", (o, n, t) =>
                    {
                        o.Examples = n.CreateMap(LoadExample, t);
                    }
                },
                {
                    "example", (o, n, _) =>
                    {
                        o.Example = n.CreateAny();
                    }
                },
            };

        private static readonly PatternFieldMap<OpenApiParameter> _parameterPatternFields =
            new()
            {
                {s => s.StartsWith(OpenApiConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, p, n, _) => o.AddExtension(p, LoadExtension(p,n))}
            };

        private static readonly AnyFieldMap<OpenApiParameter> _parameterAnyFields = new AnyFieldMap<OpenApiParameter>
        {
            {
                OpenApiConstants.Example,
                new AnyFieldMapParameter<OpenApiParameter>(
                    s => s.Example,
                    (s, v) => s.Example = v,
                    s => s.Schema)
            }
        };

        private static readonly AnyMapFieldMap<OpenApiParameter, IOpenApiExample> _parameterAnyMapOpenApiExampleFields =
            new AnyMapFieldMap<OpenApiParameter, IOpenApiExample>
        {
            {
                OpenApiConstants.Examples,
                new AnyMapFieldMapParameter<OpenApiParameter, IOpenApiExample>(
                    m => m.Examples,
                    e => e.Value,
                    (e, v) => {if (e is OpenApiExample ex) {ex.Value = v;}},
                    m => m.Schema)
            }
        };

        public static IOpenApiParameter LoadParameter(ParseNode node, OpenApiDocument hostDocument)
        {
            var mapNode = node.CheckMapNode("parameter");

            var pointer = mapNode.GetReferencePointer();
            if (pointer != null)
            {
                var reference = GetReferenceIdAndExternalResource(pointer);
                return new OpenApiParameterReference(reference.Item1, hostDocument, reference.Item2);
            }

            var parameter = new OpenApiParameter();

            ParseMap(mapNode, parameter, _parameterFixedFields, _parameterPatternFields, hostDocument);
            ProcessAnyFields(mapNode, parameter, _parameterAnyFields);
            ProcessAnyMapFields(mapNode, parameter, _parameterAnyMapOpenApiExampleFields);

            return parameter;
        }
    }
}
