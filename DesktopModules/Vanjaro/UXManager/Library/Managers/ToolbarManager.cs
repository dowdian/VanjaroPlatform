﻿using DotNetNuke.Abstractions;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vanjaro.Common.Utilities;
using Vanjaro.Core.Entities.Menu;
using Vanjaro.UXManager.Library.Entities.Interface;
using Vanjaro.UXManager.Library.Entities.Menu;
using static Vanjaro.UXManager.Library.Factories;

namespace Vanjaro.UXManager.Library
{
    public static partial class Managers
    {
        public class ToolbarManager
        {
            #region Sync Menu Extensions
            public static List<IToolbarItem> GetExtentions()
            {
                return ToolbarFactory.Extentions.ToList();
            }

            public static string RenderMenu()
            {
                StringBuilder sb = new StringBuilder();
                if (ToolbarFactory.Extentions.Where(x => x.Visibility).ToList().Count > 0)
                {
                    sb.Append("<ul class=\"ToolbarItem\">");

                    foreach (IToolbarItem mItem in ToolbarFactory.Extentions.Where(x => x.Visibility).OrderBy(o => o.SortOrder).ToList())
                    {
                        string url = ServiceProvider.NavigationManager.NavigateURL().ToLower().Replace(PortalSettings.Current.DefaultLanguage.ToLower(), PortalSettings.Current.CultureCode.ToLower()).TrimEnd('/') + MenuManager.GetURL() + "mid=0&icp=true&guid=" + mItem.SettingGuid.ToString();
                        string name = mItem.Item.Text;
                        string icon = string.Empty;

                        if (name == "Language" && !Core.Entities.Editor.Options.Language)
                            continue;

                        if (!string.IsNullOrEmpty(mItem.Icon))
                        {
                            icon = "<em class=\"" + mItem.Icon + "\" data-bs-toggle='tooltip' title='" + name + "' data-bs-placement='top'></em>";
                        }

                        if (name == "Revisions" && string.IsNullOrEmpty(Core.Entities.Editor.Options.RevisionGUID))
                        {
                            sb.Append("<li guid=" + mItem.SettingGuid.ToString().ToLower() + "><span onclick=\"ShowNotification('Warning', '', 'error');\">" + icon + "</span>");
                        }
                        else if (mItem.SettingGuid != Guid.Empty && mItem.ToolbarAction != null && mItem.ToolbarAction.ContainsKey(MenuAction.onClick))
                        {
                            if (!mItem.ToolbarAction[MenuAction.onClick].StartsWith("http"))
                                sb.Append(string.Format(@"<li class='{4}' onclick='{0}' guid='{3}'>" + icon + "", mItem.ToolbarAction[MenuAction.onClick], mItem.Width, name, mItem.SettingGuid.ToString().ToLower(), name.Replace(" ", "")));
                            else
                            {
                                sb.Append(string.Format(@"<li guid='{3}'><a href='{0}' data-url='{0}' data-width=''{1}''>" + icon + "</a>", mItem.ToolbarAction[MenuAction.onClick], mItem.Width, name, mItem.SettingGuid.ToString().ToLower()));
                            }
                        }
                        else if (mItem.SettingGuid != Guid.Empty && mItem.ToolbarAction != null && mItem.ToolbarAction.ContainsKey(MenuAction.OpenInNewWindow))
                        {
                            sb.Append(string.Format(@"<li guid='{3}'><a href='{0}' target=" + mItem.ToolbarAction[MenuAction.OpenInNewWindow] + ">" + icon + " </a>", url ?? "#", mItem.Icon, name, mItem.SettingGuid.ToString().ToLower()));
                        }
                        else
                        {
                            sb.Append(string.Format(@"<li guid='{3}'><a href='{0}' data-url='{0}' data-width=''{1}''>" + icon + "</a>", url ?? "#", mItem.Width, name, mItem.SettingGuid.ToString().ToLower()));
                        }
                    }
                    sb.Append("</ul>");
                }
                return sb.ToString();

            }

            #endregion
        }
    }
}

