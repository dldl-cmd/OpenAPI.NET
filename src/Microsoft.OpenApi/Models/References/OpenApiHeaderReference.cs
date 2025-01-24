﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Microsoft.OpenApi.Models.References
{
    /// <summary>
    /// Header Object Reference.
    /// </summary>
    public class OpenApiHeaderReference : IOpenApiHeader, IOpenApiReferenceHolder<OpenApiHeader, IOpenApiHeader>
    {
        /// <inheritdoc/>
        public OpenApiReference Reference { get; set; }

        /// <inheritdoc/>
        public bool UnresolvedReference { get; set; }
        internal OpenApiHeader _target;
        /// <summary>
        /// Gets the target header.
        /// </summary>
        /// <remarks>
        /// If the reference is not resolved, this will return null.
        /// </remarks>
        public OpenApiHeader Target
        {
            get
            {
                _target ??= Reference.HostDocument.ResolveReferenceTo<OpenApiHeader>(Reference);
                return _target;
            }
        }

        /// <summary>
        /// Constructor initializing the reference object.
        /// </summary>
        /// <param name="referenceId">The reference Id.</param>
        /// <param name="hostDocument">The host OpenAPI document.</param>
        /// <param name="externalResource">Optional: External resource in the reference.
        /// It may be:
        /// 1. a absolute/relative file path, for example:  ../commons/pet.json
        /// 2. a Url, for example: http://localhost/pet.json
        /// </param>
        public OpenApiHeaderReference(string referenceId, OpenApiDocument hostDocument, string externalResource = null)
        {
            Utils.CheckArgumentNullOrEmpty(referenceId);

            Reference = new OpenApiReference()
            {
                Id = referenceId,
                HostDocument = hostDocument,
                Type = ReferenceType.Header,
                ExternalResource = externalResource
            };
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="header">The <see cref="OpenApiHeaderReference"/> object to copy</param>
        public OpenApiHeaderReference(OpenApiHeaderReference header)
        {
            Utils.CheckArgumentNull(header);
            Reference = header.Reference != null ? new(header.Reference) : null;
            UnresolvedReference = header.UnresolvedReference;
            //no need to copy description as if they are not overridden, they will be fetched from the target
            //if they are, the reference copy will handle it
        }

        internal OpenApiHeaderReference(OpenApiHeader target, string referenceId)
        {
            _target = target;

            Reference = new OpenApiReference()
            {
                Id = referenceId,
                Type = ReferenceType.Header,
            };
        }

        /// <inheritdoc/>
        public string Description
        {
            get => string.IsNullOrEmpty(Reference?.Description) ? Target?.Description : Reference.Description;
            set 
            {
                if (Reference is not null)
                {
                    Reference.Description = value;
                }
            }
        }

        /// <inheritdoc/>
        public bool Required { get => Target.Required; }

        /// <inheritdoc/>
        public bool Deprecated { get => Target.Deprecated; }

        /// <inheritdoc/>
        public bool AllowEmptyValue { get => Target.AllowEmptyValue; }

        /// <inheritdoc/>
        public OpenApiSchema Schema { get => Target.Schema; }

        /// <inheritdoc/>
        public ParameterStyle? Style { get => Target.Style; }

        /// <inheritdoc/>
        public bool Explode { get => Target.Explode; }

        /// <inheritdoc/>
        public bool AllowReserved { get => Target.AllowReserved; }

        /// <inheritdoc/>
        public JsonNode Example { get => Target.Example; }

        /// <inheritdoc/>
        public IDictionary<string, IOpenApiExample> Examples { get => Target.Examples; }

        /// <inheritdoc/>
        public IDictionary<string, OpenApiMediaType> Content { get => Target.Content; }

        /// <inheritdoc/>
        public IDictionary<string, IOpenApiExtension> Extensions { get => Target.Extensions; }

        /// <inheritdoc/>
        public void SerializeAsV31(IOpenApiWriter writer)
        {
            if (!writer.GetSettings().ShouldInlineReference(Reference))
            {
                Reference.SerializeAsV31(writer);
            }
            else
            {
                SerializeInternal(writer, (writer, element) => CopyReferenceAsTargetElementWithOverrides(element).SerializeAsV31(writer));
            }
        }

        /// <inheritdoc/>
        public void SerializeAsV3(IOpenApiWriter writer)
        {
            if (!writer.GetSettings().ShouldInlineReference(Reference))
            {
                Reference.SerializeAsV3(writer);
            }
            else
            {
                SerializeInternal(writer, (writer, element) => CopyReferenceAsTargetElementWithOverrides(element).SerializeAsV3(writer));
            }
        }

        /// <inheritdoc/>
        public void SerializeAsV2(IOpenApiWriter writer)
        {
            if (!writer.GetSettings().ShouldInlineReference(Reference))
            {
                Reference.SerializeAsV2(writer);
            }
            else
            {
                SerializeInternal(writer, (writer, element) => CopyReferenceAsTargetElementWithOverrides(element).SerializeAsV2(writer));
            }
        }
        /// <inheritdoc/>
        public IOpenApiHeader CopyReferenceAsTargetElementWithOverrides(IOpenApiHeader source)
        {
            return source is OpenApiHeader ? new OpenApiHeader(this) : source;
        }

        /// <inheritdoc/>
        private void SerializeInternal(IOpenApiWriter writer,
            Action<IOpenApiWriter, IOpenApiHeader> action)
        {
            Utils.CheckArgumentNull(writer);
            action(writer, Target);
        }
    }
}
