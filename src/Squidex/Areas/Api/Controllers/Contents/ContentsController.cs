﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Squidex.Areas.Api.Controllers.Contents.Models;
using Squidex.Domain.Apps.Core.Contents;
using Squidex.Domain.Apps.Entities;
using Squidex.Domain.Apps.Entities.Contents;
using Squidex.Domain.Apps.Entities.Contents.Commands;
using Squidex.Domain.Apps.Entities.Contents.GraphQL;
using Squidex.Infrastructure.Commands;
using Squidex.Shared;
using Squidex.Web;

namespace Squidex.Areas.Api.Controllers.Contents
{
    public sealed class ContentsController : ApiController
    {
        private readonly IOptions<MyContentsControllerOptions> controllerOptions;
        private readonly IContentQueryService contentQuery;
        private readonly IContentWorkflow contentWorkflow;
        private readonly IGraphQLService graphQl;

        public ContentsController(ICommandBus commandBus,
            IContentQueryService contentQuery,
            IContentWorkflow contentWorkflow,
            IGraphQLService graphQl,
            IOptions<MyContentsControllerOptions> controllerOptions)
            : base(commandBus)
        {
            this.contentQuery = contentQuery;
            this.contentWorkflow = contentWorkflow;
            this.controllerOptions = controllerOptions;

            this.graphQl = graphQl;
        }

        /// <summary>
        /// GraphQL endpoint.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="query">The graphql query.</param>
        /// <returns>
        /// 200 => Contents retrieved or mutated.
        /// 404 => Schema or app not found.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpGet]
        [HttpPost]
        [Route("content/{app}/graphql/")]
        [ApiPermission]
        [ApiCosts(2)]
        public async Task<IActionResult> PostGraphQL(string app, [FromBody] GraphQLQuery query)
        {
            var result = await graphQl.QueryAsync(Context(), query);

            if (result.HasError)
            {
                return BadRequest(result.Response);
            }
            else
            {
                return Ok(result.Response);
            }
        }

        /// <summary>
        /// GraphQL endpoint (Batch).
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="batch">The graphql queries.</param>
        /// <returns>
        /// 200 => Contents retrieved or mutated.
        /// 404 => Schema or app not found.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpGet]
        [HttpPost]
        [Route("content/{app}/graphql/batch")]
        [ApiPermission]
        [ApiCosts(2)]
        public async Task<IActionResult> PostGraphQLBatch(string app, [FromBody] GraphQLQuery[] batch)
        {
            var result = await graphQl.QueryAsync(Context(), batch);

            if (result.HasError)
            {
                return BadRequest(result.Response);
            }
            else
            {
                return Ok(result.Response);
            }
        }

        /// <summary>
        /// Queries contents.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="ids">The optional ids of the content to fetch.</param>
        /// <returns>
        /// 200 => Contents retrieved.
        /// 404 => App not found.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpGet]
        [Route("content/{app}/")]
        [ProducesResponseType(typeof(ContentsDto), 200)]
        [ApiPermission]
        [ApiCosts(1)]
        public async Task<IActionResult> GetAllContents(string app, [FromQuery] string ids)
        {
            var context = Context();
            var contents = await contentQuery.QueryAsync(context, Q.Empty.WithIds(ids).Ids);

            var response = await ContentsDto.FromContentsAsync(contents, context, this, null, contentWorkflow);

            if (controllerOptions.Value.EnableSurrogateKeys && response.Items.Length <= controllerOptions.Value.MaxItemsForSurrogateKeys)
            {
                Response.Headers["Surrogate-Key"] = response.ToSurrogateKeys();
            }

            Response.Headers[HeaderNames.ETag] = response.ToEtag();

            return Ok(response);
        }

        /// <summary>
        /// Queries contents.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="ids">The optional ids of the content to fetch.</param>
        /// <returns>
        /// 200 => Contents retrieved.
        /// 404 => Schema or app not found.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpGet]
        [Route("content/{app}/{name}/")]
        [ProducesResponseType(typeof(ContentsDto), 200)]
        [ApiPermission]
        [ApiCosts(1)]
        public async Task<IActionResult> GetContents(string app, string name, [FromQuery] string ids = null)
        {
            var context = Context();
            var contents = await contentQuery.QueryAsync(context, name, Q.Empty.WithIds(ids).WithODataQuery(Request.QueryString.ToString()));

            var schema = await contentQuery.GetSchemaOrThrowAsync(context, name);

            var response = await ContentsDto.FromContentsAsync(contents, context, this, schema, contentWorkflow);

            if (ShouldProvideSurrogateKeys(response))
            {
                Response.Headers["Surrogate-Key"] = response.ToSurrogateKeys();
            }

            Response.Headers[HeaderNames.ETag] = response.ToEtag();

            return Ok(response);
        }

        /// <summary>
        /// Get a content item.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="id">The id of the content to fetch.</param>
        /// <returns>
        /// 200 => Content found.
        /// 404 => Content, schema or app not found.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpGet]
        [Route("content/{app}/{name}/{id}/")]
        [ProducesResponseType(typeof(ContentsDto), 200)]
        [ApiPermission]
        [ApiCosts(1)]
        public async Task<IActionResult> GetContent(string app, string name, Guid id)
        {
            var context = Context();
            var content = await contentQuery.FindContentAsync(context, name, id);

            var response = ContentDto.FromContent(context, content, this);

            if (controllerOptions.Value.EnableSurrogateKeys)
            {
                Response.Headers["Surrogate-Key"] = content.Id.ToString();
            }

            Response.Headers[HeaderNames.ETag] = content.Version.ToString();

            return Ok(response);
        }

        /// <summary>
        /// Get a content by version.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="id">The id of the content to fetch.</param>
        /// <param name="version">The version fo the content to fetch.</param>
        /// <returns>
        /// 200 => Content found.
        /// 404 => Content, schema or app not found.
        /// 400 => Content data is not valid.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpGet]
        [Route("content/{app}/{name}/{id}/{version}/")]
        [ApiPermission(Permissions.AppContentsRead)]
        [ApiCosts(1)]
        public async Task<IActionResult> GetContentVersion(string app, string name, Guid id, int version)
        {
            var context = Context();
            var content = await contentQuery.FindContentAsync(context, name, id, version);

            var response = ContentDto.FromContent(context, content, this);

            if (controllerOptions.Value.EnableSurrogateKeys)
            {
                Response.Headers["Surrogate-Key"] = content.Id.ToString();
            }

            Response.Headers[HeaderNames.ETag] = content.Version.ToString();

            return Ok(response.Data);
        }

        /// <summary>
        /// Create a content item.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="request">The full data for the content item.</param>
        /// <param name="publish">Indicates whether the content should be published immediately.</param>
        /// <returns>
        /// 201 => Content created.
        /// 404 => Content, schema or app not found.
        /// 400 => Content data is not valid.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpPost]
        [Route("content/{app}/{name}/")]
        [ProducesResponseType(typeof(ContentsDto), 200)]
        [ApiPermission(Permissions.AppContentsCreate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PostContent(string app, string name, [FromBody] NamedContentData request, [FromQuery] bool publish = false)
        {
            await contentQuery.GetSchemaOrThrowAsync(Context(), name);

            if (publish && !this.HasPermission(Helper.StatusPermission(app, name, Status.Published)))
            {
                return new ForbidResult();
            }

            var command = new CreateContent { ContentId = Guid.NewGuid(), Data = request.ToCleaned(), Publish = publish };

            var response = await InvokeCommandAsync(app, name, command);

            return CreatedAtAction(nameof(GetContent), new { id = command.ContentId }, response);
        }

        /// <summary>
        /// Update a content item.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="id">The id of the content item to update.</param>
        /// <param name="request">The full data for the content item.</param>
        /// <param name="asDraft">Indicates whether the update is a proposal.</param>
        /// <returns>
        /// 200 => Content updated.
        /// 404 => Content, schema or app not found.
        /// 400 => Content data is not valid.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpPut]
        [Route("content/{app}/{name}/{id}/")]
        [ProducesResponseType(typeof(ContentsDto), 200)]
        [ApiPermission(Permissions.AppContentsUpdate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PutContent(string app, string name, Guid id, [FromBody] NamedContentData request, [FromQuery] bool asDraft = false)
        {
            await contentQuery.GetSchemaOrThrowAsync(Context(), name);

            var command = new UpdateContent { ContentId = id, Data = request.ToCleaned(), AsDraft = asDraft };

            var response = await InvokeCommandAsync(app, name, command);

            return Ok(response);
        }

        /// <summary>
        /// Patchs a content item.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="id">The id of the content item to patch.</param>
        /// <param name="request">The patch for the content item.</param>
        /// <param name="asDraft">Indicates whether the patch is a proposal.</param>
        /// <returns>
        /// 200 => Content patched.
        /// 404 => Content, schema or app not found.
        /// 400 => Content patch is not valid.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpPatch]
        [Route("content/{app}/{name}/{id}/")]
        [ProducesResponseType(typeof(ContentsDto), 200)]
        [ApiPermission(Permissions.AppContentsUpdate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PatchContent(string app, string name, Guid id, [FromBody] NamedContentData request, [FromQuery] bool asDraft = false)
        {
            await contentQuery.GetSchemaOrThrowAsync(Context(), name);

            var command = new PatchContent { ContentId = id, Data = request.ToCleaned(), AsDraft = asDraft };

            var response = await InvokeCommandAsync(app, name, command);

            return Ok(response);
        }

        /// <summary>
        /// Publish a content item.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="id">The id of the content item to publish.</param>
        /// <param name="request">The status request.</param>
        /// <returns>
        /// 200 => Content published.
        /// 404 => Content, schema or app not found.
        /// 400 => Request is not valid.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpPut]
        [Route("content/{app}/{name}/{id}/status/")]
        [ProducesResponseType(typeof(ContentsDto), 200)]
        [ApiPermission]
        [ApiCosts(1)]
        public async Task<IActionResult> PutContentStatus(string app, string name, Guid id, ChangeStatusDto request)
        {
            await contentQuery.GetSchemaOrThrowAsync(Context(), name);

            if (!this.HasPermission(Helper.StatusPermission(app, name, Status.Published)))
            {
                return new ForbidResult();
            }

            var command = request.ToCommand(id);

            var response = await InvokeCommandAsync(app, name, command);

            return Ok(response);
        }

        /// <summary>
        /// Discard changes.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="id">The id of the content item to discard changes.</param>
        /// <returns>
        /// 200 => Content restored.
        /// 404 => Content, schema or app not found.
        /// 400 => Content was not archived.
        /// </returns>
        /// <remarks>
        /// You can read the generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpPut]
        [Route("content/{app}/{name}/{id}/discard/")]
        [ProducesResponseType(typeof(ContentsDto), 200)]
        [ApiPermission(Permissions.AppContentsDiscard)]
        [ApiCosts(1)]
        public async Task<IActionResult> DiscardDraft(string app, string name, Guid id)
        {
            await contentQuery.GetSchemaOrThrowAsync(Context(), name);

            var command = new DiscardChanges { ContentId = id };

            var response = await InvokeCommandAsync(app, name, command);

            return Ok(response);
        }

        /// <summary>
        /// Delete a content item.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="id">The id of the content item to delete.</param>
        /// <returns>
        /// 204 => Content deleted.
        /// 404 => Content, schema or app not found.
        /// </returns>
        /// <remarks>
        /// You can create an generated documentation for your app at /api/content/{appName}/docs.
        /// </remarks>
        [HttpDelete]
        [Route("content/{app}/{name}/{id}/")]
        [ApiPermission(Permissions.AppContentsDelete)]
        [ApiCosts(1)]
        public async Task<IActionResult> DeleteContent(string app, string name, Guid id)
        {
            await contentQuery.GetSchemaOrThrowAsync(Context(), name);

            var command = new DeleteContent { ContentId = id };

            await CommandBus.PublishAsync(command);

            return NoContent();
        }

        private async Task<ContentDto> InvokeCommandAsync(string app, string schema, ICommand command)
        {
            var context = await CommandBus.PublishAsync(command);

            var result = context.Result<IEnrichedContentEntity>();
            var response = ContentDto.FromContent(null, result, this);

            return response;
        }

        private QueryContext Context()
        {
            return QueryContext.Create(App, User)
                .WithAssetUrlsToResolve(Request.Headers["X-Resolve-Urls"])
                .WithFlatten(Request.Headers.ContainsKey("X-Flatten"))
                .WithLanguages(Request.Headers["X-Languages"])
                .WithUnpublished(Request.Headers.ContainsKey("X-Unpublished"));
        }

        private bool ShouldProvideSurrogateKeys(ContentsDto response)
        {
            return controllerOptions.Value.EnableSurrogateKeys && response.Items.Length <= controllerOptions.Value.MaxItemsForSurrogateKeys;
        }
    }
}
