﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Mvc;
using System;

namespace Squidex.Web
{
    public static class UrlHelperExtensions
    {
        private static class NameOf<T>
        {
            public static readonly string Controller;

            static NameOf()
            {
                const string suffix = "Controller";

                var name = typeof(T).Name;

                if (name.EndsWith(suffix))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                }

                Controller = name;
            }
        }

        public static string Url<T>(this IUrlHelper urlHelper, Func<T, string> action, object values = null) where T : Controller
        {
            return urlHelper.Action(action(null), NameOf<T>.Controller, values);
        }

        public static string Url<T>(this Controller controller, Func<T, string> action, object values = null) where T : Controller
        {
            return controller.Url.Url(action, values);
        }
    }
}
