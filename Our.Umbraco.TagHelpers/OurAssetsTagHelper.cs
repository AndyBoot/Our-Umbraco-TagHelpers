using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.WebAssets;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    [HtmlTargetElement("our-assets", TagStructure = TagStructure.WithoutEndTag)]
    public class OurAssetsTagHelper : TagHelper
    {
        [HtmlAttributeName("add-css")]
        public string AddCss { get; set; } = string.Empty;
        [HtmlAttributeName("add-criticalcss")]
        public string AddCriticalCss { get; set; } = string.Empty;
        [HtmlAttributeName("add-js")]
        public string AddJs { get; set; } = string.Empty;
        [HtmlAttributeName("render-css")]
        public bool RenderCss { get; set; } = false;
        [HtmlAttributeName("render-criticalcss")]
        public bool RenderCriticalCss { get; set; } = false;
        [HtmlAttributeName("render-js")]
        public bool RenderJs { get; set; } = false;
        [HtmlAttributeName("render-preconnect")]
        public bool RenderPreConnect { get; set; } = false;

        private readonly IHttpContextAccessor _contextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;


        public OurAssetsTagHelper(IHttpContextAccessor contextAccessor, IWebHostEnvironment webHostEnvironment)
        {
            _contextAccessor = contextAccessor;
            _webHostEnvironment = webHostEnvironment;

            if (_webHostEnvironment == null) return;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var render = true;
            output.SuppressOutput();

            if (!string.IsNullOrWhiteSpace(AddCss))
            {
                AddFile(AssetType.Css, AddCss);
                render = false;
            }
            if (!string.IsNullOrWhiteSpace(AddCriticalCss))
            {
                AddFile(AssetType.CriticalCss, AddCriticalCss);
                render = false;
            }
            if (!string.IsNullOrWhiteSpace(AddJs))
            {
                AddFile(AssetType.Js, AddJs);
                render = false;
            }

            if (render)
            {
                var htmlOutput = string.Empty;
                var preConnectUris = new List<Uri>();
                if (RenderCss)
                {
                    var files = GetFiles(AssetType.Css);
                    foreach (string file in files)
                    {
                        htmlOutput += $"<link rel=\"preload\" href=\"{file}\" as=\"style\" onload=\"this.onload=null;this.rel='stylesheet'\">";// how to add asp-append-version="true"?
                        htmlOutput += $"<noscript><link rel=\"stylesheet\" href=\"{file}\"></noscript>";
                    }
                }
                if (RenderCriticalCss)
                {
                    var files = GetFiles(AssetType.CriticalCss);
                    foreach (string file in files)
                    {
                        htmlOutput += $"<style>{GetFileContents(file)}</style>";
                    }
                }
                if (RenderJs)
                {
                    var files = GetFiles(AssetType.Js);
                    foreach (string file in files)
                    {
                        htmlOutput += $"<script src=\"{file}\" type=\"module\"></script>";
                    }
                }
                if (RenderPreConnect)
                {
                    foreach (string url in GetPreConnectUrls())
                    {
                        htmlOutput += $"<link rel=\"preconnect\" href=\"{url}\">";
                    }
                }

                output.Content.SetHtmlContent(htmlOutput);
            }
        }


        private void AddFile(AssetType assetType, string path)
        {
            var list = new List<string>();
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext == null) return;

            if (httpContext.Items.ContainsKey(assetType.ToString()))
            {
                list = httpContext.Items[assetType.ToString()] as List<string>;
            }

            list ??= new List<string>();

            if (!list.Contains(path))
            {
                list.Add(path);
            }

            httpContext.Items[assetType.ToString()] = list;
        }

        private List<string> GetFiles(AssetType assetType)
        {
            var list = new List<string>();
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext == null) return list;

            if (httpContext.Items.ContainsKey(assetType.ToString()))
            {
                list = httpContext.Items[assetType.ToString()] as List<string>;
            }

            list ??= new List<string>();

            return list;
        }

        private List<string> GetFiles()
        {
            var list = new List<string>();

            list.AddRange(GetFiles(AssetType.Css));
            list.AddRange(GetFiles(AssetType.Js));

            return list;
        }

        public string GetFileContents(string path)
        {
            var physicalPath = Path.Combine(_webHostEnvironment!.WebRootPath, path.TrimStart("/").Replace("/", "\\"));
            return !System.IO.File.Exists(physicalPath) ? string.Empty : System.IO.File.ReadAllText(physicalPath);
        }

        private List<string> GetPreConnectUrls()
        {
            var files = GetFiles();
            var preConnectUrls = new List<string>();
            foreach (string file in files)
            {
                if (!file.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                preConnectUrls.Add((new Uri(file)).GetLeftPart(UriPartial.Authority));
            }

            return preConnectUrls;
        }

        private enum AssetType
        {
            Css,
            CriticalCss,
            Js
        }
    }
}
