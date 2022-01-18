module Liquid

open System
open System.IO
open System.Collections.Concurrent
open System.Net.Mime
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open AspFeat.Builder
open AspFeat.HttpContext
open Fluid
open Fluid.ViewEngine

let private viewLocationsCache = ConcurrentDictionary<string, string> ()

let private locatePageFromViewLocations viewName (options: FluidViewEngineOptions) =
    match viewLocationsCache.TryGetValue viewName with
    | true, cachedLocation when not (isNull cachedLocation) ->
        Some cachedLocation
    | _ ->
        options.ViewsLocationFormats
        |> Seq.tryPick (fun location ->
            let viewFilename = Path.Combine(String.Format(location, viewName))
            let fileInfo = options.ViewsFileProvider.GetFileInfo viewFilename
            if fileInfo.Exists then
                viewLocationsCache[viewName] <- viewFilename
                Some viewFilename
            else
                None)

let write viewName model (ctx: Http.HttpContext) : Task =
    task {
        let fluidViewRenderer = ctx.RequestServices.GetService<IFluidViewRenderer>()
        let options = ctx.RequestServices.GetService<IOptions<FluidViewEngineOptions>>().Value
        match locatePageFromViewLocations viewName options with
        | Some viewPath ->
            ctx.Response.ContentType <- MediaTypeNames.Text.Html
            let tmplContext = TemplateContext (box model)
            tmplContext.Options.FileProvider <- options.PartialsFileProvider
            use sw = new StreamWriter (ctx.Response.Body)
            do! fluidViewRenderer.RenderViewAsync (sw, viewPath, tmplContext)
        | None ->
            do! notFound ctx
    }

let feat () : Feat =
    fun builder ->
        builder.Services
            .AddFluid()
        |> ignore
    ,
    Feat.ignore
