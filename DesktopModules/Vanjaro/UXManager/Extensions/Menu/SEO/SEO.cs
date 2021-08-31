﻿using DotNetNuke.Entities.Users;
using System;
using System.Collections.Generic;
using Vanjaro.Common.Engines.UIEngine.AngularBootstrap;
using Vanjaro.Common.Entities.Apps;
using Vanjaro.Common.Utilities;
using Vanjaro.Core.Entities.Menu;
using Vanjaro.UXManager.Extensions.Menu.SEO.Components;
using Vanjaro.UXManager.Extensions.Menu.SEO.Factories;
using Vanjaro.UXManager.Library;
using Vanjaro.UXManager.Library.Entities.Interface;
using Vanjaro.UXManager.Library.Entities.Menu;
using static Vanjaro.Common.FrameworkManager;

namespace Vanjaro.UXManager.Extensions.Menu.SEO
{
    public class SEO : IMenuItem
    {
        public List<MenuItem> Items
        {
            get
            {
                List<MenuItem> ListItems = new List<MenuItem>();
                ListItems.Add(new MenuItem
                {
                    Text = Localization.Get(ExtensionInfo.Name, "Text", Constants.ResourcesFile, Extension.ShowMissingKeysStatic, Localization.SharedMissingPrefix),

                    ItemGuid = Guid.Parse(ExtensionInfo.GUID),
                    Icon = "fas fa-search",
                    ViewOrder = 820,
                    BelowBreakLine = true,
                    Hierarchy = new MenuItem
                    {
                        Text = Localization.Get("Settings", "Text", Constants.ResourcesFile, Extension.ShowMissingKeysStatic, Localization.SharedMissingPrefix),
                        Icon = "fa fa-file",
                    }
                });
                return ListItems;
            }
        }


        public Guid SettingGuid => Guid.Parse(ExtensionInfo.GUID);

        public int? Width => 900;

        public string UIPath => "~/DesktopModules/Vanjaro/UXManager/Extensions/Menu/" + ExtensionInfo.Name + "/Views/";

        public string AppCssPath => "~/DesktopModules/Vanjaro/UXManager/Extensions/Menu/" + ExtensionInfo.Name + "/Resources/Stylesheets/app.css";

        public string AppJsPath => "~/DesktopModules/Vanjaro/UXManager/Extensions/Menu/" + ExtensionInfo.Name + "/Resources/Scripts/app.js";

        public string SearchKeywords => Localization.Get("SearchKeywords", "Text", Constants.ResourcesFile, Extension.ShowMissingKeysStatic, Localization.SharedMissingPrefix);

        public string UIEngineAngularBootstrapPath => "~/DesktopModules/Vanjaro/UXManager/Extensions/Menu/" + ExtensionInfo.Name + "/Resources/UIEngine/AngularBootstrap";

        public string[] Dependencies => new string[] {
                    Frameworks.jQueryUI.ToString(),
                    AngularPlugins.Tags.ToString(),
                    AngularPlugins.AutoComplete.ToString(),
                    JavaScriptPlugins.ValidationJS.ToString(),
                    "Bootstrap"
                   };

        public string Icon => "fa fa-cog";

        public bool Visibility => Factories.AppFactory.GetAccessRoles(UserController.Instance.GetCurrentUserInfo()).Contains("admin");

        public MenuAction Event => MenuAction.RightOverlay;

        public AppInformation App => AppFactory.GetAppInformation();

        public List<AngularView> AngularViews => AppFactory.GetViews();

        public string AccessRoles(UserInfo userInfo)
        {
            return Factories.AppFactory.GetAccessRoles(userInfo);
        }
    }
}