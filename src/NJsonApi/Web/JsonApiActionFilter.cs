﻿using System;
using System.Linq;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc;
using NJsonApi.Web.BadActionResultTransformers;
using Microsoft.AspNet.Http;
using NJsonApi.Serialization;

namespace NJsonApi.Web
{
    internal class JsonApiActionFilter : IActionFilter
    {
        public bool AllowMultiple { get { return false; } }
        private readonly JsonApiTransformer jsonApiTransformer;
        private readonly Configuration configuration;

        public JsonApiActionFilter(JsonApiTransformer jsonApiTransformer, Configuration configuration)
        {
            this.jsonApiTransformer = jsonApiTransformer;
            this.configuration = configuration;
        }
    
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Request.ContentType != configuration.DefaultJsonApiMediaType.MediaType)
            {
                context.Result = new UnsupportedMediaTypeResult();
            }

            if (!ValidateAcceptHeader(context.HttpContext.Request.Headers))
            { 
                context.Result = new HttpStatusCodeResult(406);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result == null)
            {
                return;
            }

            if (BadActionResultTransformer.IsBadAction(context.Result))
            {
                var transformed = BadActionResultTransformer.Transform(context.Result);

                context.Result = new ObjectResult(transformed)
                {
                    StatusCode = transformed.Errors.First().Status
                };
                return;
            }

            var responseResult = (ObjectResult)context.Result;
            var relationshipPaths = FindRelationshipPathsToInclude(context.HttpContext.Request);

            if (!configuration.ValidateIncludedRelationshipPaths(relationshipPaths, responseResult.Value))
            {
                context.Result = new HttpStatusCodeResult(400);
                return;
            }

            var jsonApiContext = new Context(
                configuration, 
                new Uri(context.HttpContext.Request.GetDisplayUrl()),
                relationshipPaths);
            responseResult.Value = jsonApiTransformer.Transform(responseResult.Value, jsonApiContext);
        }

        private string[] FindRelationshipPathsToInclude(HttpRequest request)
        {
            var result = request.Query["include"].FirstOrDefault();

            return string.IsNullOrEmpty(result) ? new string[0] : result.Split(',');
        }

        private bool ValidateAcceptHeader(IHeaderDictionary headers)
        {
            var acceptsHeaders = headers["Accept"].FirstOrDefault();

            if (string.IsNullOrEmpty(acceptsHeaders))
            {
                return true;
            }

            return acceptsHeaders
                .Split(',')
                .Select(x => x.Trim())
                .Any(x => 
                    x == "*/*" || 
                    x == configuration.DefaultJsonApiMediaType.MediaType);
        }       
    }
}