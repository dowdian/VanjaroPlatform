using DotNetNuke.Abstractions;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Localization;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using Vanjaro.Common.ASPNET;
using Vanjaro.Common.Factories;
using Vanjaro.Common.Utilities;
using Vanjaro.Core.Components;
using Vanjaro.Core.Data.Entities;
using Vanjaro.Core.Data.Scripts;
using static Vanjaro.Core.Components.Enum;
using static Vanjaro.Core.Factories;

namespace Vanjaro.Core
{
    public static partial class Managers
    {
        public class PageManager
        {

            private const string PortalRootToken = "{{PortalRoot}}";
            public const string ExportTemplateRootToken = "{{TemplateRoot}}";
            public static void Init(Page Page, PortalSettings PortalSettings)
            {
                string Markup = GetReviewToastMarkup(PortalSettings);
                if (!string.IsNullOrEmpty(Markup) && string.IsNullOrEmpty(Page.Request.QueryString["icp"]) && string.IsNullOrEmpty(Page.Request.QueryString["guid"]))
                {
                    WebForms.RegisterStartupScript(Page, "WorkflowReview", "<script type=\"text/javascript\" vanjarocore=\"true\">" + Markup + "</script>", false);
                }
            }

            public static string GetReviewToastMarkup(PortalSettings PortalSettings)
            {
                string Markup = string.Empty;
                Pages page = GetLatestVersion(PortalSettings.ActiveTab.TabID, PortalSettings.UserInfo);
                WorkflowState State = null;
                bool ShowReview = false;

                if (page != null && page.StateID.HasValue)
                {
                    State = WorkflowManager.GetStateByID(page.StateID.Value);
                    if (State != null && WorkflowManager.HasReviewPermission(page.StateID.Value, PortalSettings.UserInfo))
                    {
                        ShowReview = WorkflowManager.GetWorkflowType(State.WorkflowID) == WorkflowTypes.ContentApproval && !WorkflowManager.IsFirstState(State.WorkflowID, State.StateID) && !WorkflowManager.IsLastState(State.WorkflowID, State.StateID);
                    }
                }

                if (State != null)
                {
                    string URL = ServiceProvider.NavigationManager.NavigateURL("", "mid=0", "icp=true", "guid=33d8efed-0f1d-471e-80a4-6a7f10e87a42");
                    string ReviewChangesBtn = ShowReview ? "ReviewChangeMarkup.append(ReviewChangesBtn);" : string.Empty;
                    string Subject = ShowReview ? State.Name : DotNetNuke.Services.Localization.Localization.GetString("PendingReview", Components.Constants.LocalResourcesFile);
                    string Message = !ShowReview ? "ReviewChangeMarkup.append('" + DotNetNuke.Services.Localization.Localization.GetString("ThisPageIsWaiting", Components.Constants.LocalResourcesFile) + "');" : string.Empty;
                    string UnlockChangesBtn = TabPermissionController.CanManagePage(PortalSettings.ActiveTab) ? "ReviewChangeMarkup.append(UnlockChangesBtn);" : string.Empty;
                    string LocalCanvasMarkup = string.Empty;
                    string FirstStateName = WorkflowManager.GetFirstStateID(State.WorkflowID).Name;
                    string ReviewChangeFunction = !ShowReview ? "ConfirmReviewChange('" + FirstStateName + "');" : "$('.gjs-cv-canvas__frames').removeClass('lockcanvas'); VJIsLocked='False'; $('.toast-close-button').click();";
                    if (State != null && ShowReview || (!string.IsNullOrEmpty(UnlockChangesBtn) && !WorkflowManager.IsFirstState(State.WorkflowID, State.StateID) && !WorkflowManager.IsLastState(State.WorkflowID, State.StateID)))
                    {
                        Markup = "var ReviewChangeMarkup=$('<div>'); var ReviewChangesBtn = $('<button>'); ReviewChangesBtn.text('" + DotNetNuke.Services.Localization.Localization.GetString("ReviewPage", Components.Constants.LocalResourcesFile) + "'); ReviewChangesBtn.addClass('btn btn-success btn-sm'); ReviewChangesBtn.click(function(){OpenPopUp(null,600,'right','','" + URL + "'); }); var UnlockChangesBtn = $('<button>'); UnlockChangesBtn.text('" + DotNetNuke.Services.Localization.Localization.GetString("MakeChanges", Components.Constants.LocalResourcesFile) + "'); UnlockChangesBtn.addClass('btn btn-danger btn-sm'); UnlockChangesBtn.click(function(){" + ReviewChangeFunction + " });" + Message + " " + ReviewChangesBtn + " " + UnlockChangesBtn + " window.parent.ShowNotification('" + Subject + "',ReviewChangeMarkup, 'info', '', false,false);" + LocalCanvasMarkup + "";
                    }
                }

                return Markup;
            }

            public static dynamic Update(PortalSettings PortalSettings, dynamic Data)
            {
                dynamic result = new ExpandoObject();
                try
                {
                    if (Data != null)
                    {
                        int TabId = PortalSettings.ActiveTab.TabID;
                        Pages page = new Pages();
                        Pages pageVersion = GetLatestVersion(PortalSettings.ActiveTab.TabID, PortalSettings.UserInfo);

                        page.TabID = TabId;
                        page.Style = Data["gjs-css"].ToString();
                        page.Content = ResetModuleMarkup(PortalSettings.PortalId, Data["gjs-html"].ToString(), PortalSettings.UserId);
                        page.ContentJSON = Data["gjs-components"].ToString();
                        page.StyleJSON = Data["gjs-styles"].ToString();

                        if (Data["IsPublished"] != null && Convert.ToBoolean(Data["IsPublished"].ToString()) && (pageVersion != null && pageVersion.IsPublished))
                        {
                            page.IsPublished = true;
                            page.Version = GetNextVersionByTabID(TabId);
                        }
                        else
                        {
                            page.IsPublished = Convert.ToBoolean(Data["IsPublished"].ToString());

                            if (pageVersion != null && pageVersion.IsPublished)
                            {
                                page.Version = GetNextVersionByTabID(TabId);
                            }
                            else if (pageVersion != null && !pageVersion.IsPublished)
                            {
                                page.Version = pageVersion.Version;
                            }
                            else
                            {
                                page.Version = GetNextVersionByTabID(TabId);
                            }
                        }

                        Pages SavedPage = GetByVersion(page.TabID, page.Version, GetCultureCode(PortalSettings));
                        if (SavedPage != null)
                        {
                            page.CreatedBy = SavedPage.CreatedBy;
                            page.CreatedOn = SavedPage.CreatedOn;
                            page.ID = SavedPage.ID;
                            page.StateID = SavedPage.StateID;
                        }
                        page.Locale = GetCultureCode(PortalSettings);
                        if (pageVersion != null && page.Version == pageVersion.Version)
                        {
                            page.StateID = pageVersion.StateID;
                        }

                        page.PortalID = PortalSettings.PortalId;
                        WorkflowManager.AddComment(PortalSettings, "publish", Data["Comment"].ToString(), page);

                        ReviewSettings ReviewSettings = GetPageReviewSettings(PortalSettings);
                        if (!string.IsNullOrEmpty(Data["Comment"].ToString()) || ReviewSettings.IsPageDraft)
                        {
                            result.ReviewToastMarkup = GetReviewToastMarkup(PortalSettings);
                            result.PageReviewSettings = ReviewSettings;
                        }
                        result.NotifyCount = NotificationManager.RenderNotificationsCount(PortalSettings.PortalId);

                        try
                        {
                            if (page.IsPublished && Data != null && Data["m2v"] != null && Data["m2v"].Value)
                            {
                                PageManager.MigrateToVanjaro(PortalSettings);
                                result.RedirectAfterm2v = Common.Utilities.ServiceProvider.NavigationManager.NavigateURL(PortalSettings.ActiveTab.TabID);
                            }
                            else
                            {
                                result.RedirectAfterm2v = null;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                return result;
            }

            public static void MigrateToVanjaro(PortalSettings PortalSettings)
            {
                PageFactory.MigrateToVanjaro(PortalSettings);
            }

            public static void Rollback(int TabId, int Version, string Locale, int UserID)
            {
                Pages page = GetByVersion(TabId, Version, Locale);
                page.Version = GetNextVersionByTabID(TabId);
                PageFactory.Update(page, UserID);
            }

            public static dynamic Get(int TabID)
            {
                Pages tab = PageFactory.Get(TabID);
                Dictionary<string, object> result = new Dictionary<string, object>();
                if (tab != null)
                {
                    result.Add("gjs-css", tab.Style);
                    result.Add("gjs-styles", tab.StyleJSON);
                    result.Add("gjs-html", tab.Content);
                    result.Add("gjs-component", tab.ContentJSON);
                }
                return result;
            }

            public static List<Pages> GetPages(int TabID)
            {
                return PageFactory.GetAllByTabID(TabID).ToList();
            }

            public static string ResetModuleMarkup(int PortalId, string Markup, int UserId)
            {
                if (!string.IsNullOrEmpty(Markup))
                {
                    HtmlDocument html = new HtmlDocument();
                    html.LoadHtml(Markup);
                    IEnumerable<HtmlNode> query = html.DocumentNode.Descendants("div");
                    foreach (HtmlNode item in query.ToList())
                    {
                        if (item.Attributes.Where(a => a.Name == "dmid").FirstOrDefault() != null && item.Attributes.Where(a => a.Name == "mid").FirstOrDefault() != null)
                        {
                            item.InnerHtml = "<div vjmod=\"true\"><app id=\"" + item.Attributes.Where(a => a.Name == "mid").FirstOrDefault().Value + "\"></app>";
                        }
                        else if (item.Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault() != null)
                        {
                            if (item.Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault().Value.ToLower() == "global")
                            {
                                item.InnerHtml = "";
                            }
                            else
                            {
                                item.InnerHtml = item.Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault().Value;
                            }
                        }
                    }
                    Markup = html.DocumentNode.OuterHtml;
                }
                return Markup;
            }

            public static int GetNextVersionByTabID(int TabID)
            {
                List<Pages> pages = PageFactory.GetAllByTabID(TabID);
                if (pages.Count > 0)
                {
                    return pages.Max(a => a.Version) + 1;
                }

                return 1;
            }

            public static void Delete(int TabID)
            {
                PageFactory.Delete(TabID);
            }
            public static void Delete(int TabID, int Version)
            {
                PageFactory.Delete(TabID, Version);
            }

            public static Pages GetLatestVersion(int TabID, string Locale)
            {
                return GetLatestVersion(TabID, Locale, true);
            }

            public static Pages GetLatestVersion(int TabID, string Locale, bool GetDefaultLocale)
            {
                return GetLatestVersion(TabID, false, Locale, GetDefaultLocale);
            }

            public static Pages GetLatestVersion(int TabID, bool IgnoreDraft, string Locale, bool GetDefaultLocale)
            {
                UserInfo UserInfo = (PortalController.Instance.GetCurrentSettings() as PortalSettings).UserInfo;
                List<Pages> pages = GetPages(TabID).Where(a => a.Locale == Locale).ToList();
                Pages page = new Pages();

                if (!IgnoreDraft && (TabPermissionController.HasTabPermission("EDIT") || WorkflowManager.HasReviewPermission(UserInfo)))
                {
                    page = pages.OrderByDescending(a => a.Version).FirstOrDefault();
                }
                else
                {
                    page = pages.Where(a => a.IsPublished == true).OrderByDescending(a => a.Version).FirstOrDefault();
                }

                if (page == null && !string.IsNullOrEmpty(Locale) && GetDefaultLocale)
                {
                    return GetLatestVersion(TabID, IgnoreDraft, null, false);
                }
                return page;
            }

            public static void ChangePageWorkflow(PortalSettings PortalSettings, int TabID, int Workflowid)
            {

                SettingManager.UpdateValue(PortalSettings.PortalId, TabID, "setting_workflow", "WorkflowID", Workflowid.ToString());
                WorkflowState fstate = WorkflowManager.GetFirstStateID(Workflowid);
                if (fstate != null)
                {
                    foreach (string Locale in GetCultureListItems())
                    {
                        string _Locale = Locale == PortalSettings.DefaultLanguage ? null : Locale;
                        Pages page = GetLatestVersion(TabID, _Locale);
                        if (page != null && page.StateID.HasValue)
                        {
                            WorkflowState state = WorkflowManager.GetStateByID(page.StateID.Value);
                            if (page.StateID.HasValue && WorkflowManager.IsFirstState(state.WorkflowID, state.StateID))
                            {
                                page.StateID = fstate.StateID;
                                UpdatePage(page, PortalSettings.UserId);
                            }
                        }
                    }
                }
            }

            public static Pages GetLatestVersion(int TabID, UserInfo UserInfo)
            {

                List<Pages> pages = GetPages(TabID).ToList();
                Pages page = new Pages();

                try
                {
                    if (TabPermissionController.HasTabPermission("EDIT") || WorkflowManager.HasReviewPermission(UserInfo))
                    {
                        page = pages.OrderByDescending(a => a.Version).FirstOrDefault();
                    }
                    else
                    {
                        page = pages.Where(a => a.IsPublished == true).OrderByDescending(a => a.Version).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                    page = new Pages();
                }
                return page;
            }


            public static Pages GetByVersion(int TabID, int Version, string Locale)
            {
                return PageFactory.GetByVersion(TabID, Version, Locale);
            }

            public static List<Pages> GetLatestLocaleVersion(int TabID)
            {
                return GetPages(TabID).Where(a => a.IsPublished == true).OrderByDescending(a => a.Version).GroupBy(g => g.Version).FirstOrDefault()?.ToList() ?? new List<Pages>();
            }

            internal static List<Pages> GetAllByState(int State)
            {
                return PageFactory.GetAllByState(State);
            }

            public static string GetCultureCode(PortalSettings PortalSettings)
            {
                return PortalSettings.DefaultLanguage != PortalSettings.CultureCode ? PortalSettings.CultureCode : null;
            }

            internal static void ModeratePage(string Action, Pages Page, PortalSettings PortalSettings)
            {

                UserInfo UserInfo = PortalSettings.UserInfo;
                WorkflowState wState = Page.StateID.HasValue ? WorkflowManager.GetStateByID(Page.StateID.Value) : null;
                bool IsHaveReviewPermission = false;

                if (wState != null)
                {
                    IsHaveReviewPermission = WorkflowManager.HasReviewPermission(wState.StateID, UserInfo);
                }

                if (string.IsNullOrEmpty(Action) && !IsHaveReviewPermission)
                {
                    Page.StateID = WorkflowManager.GetFirstStateID(WorkflowManager.GetDefaultWorkflow(Page.TabID)).StateID;
                    AddLocalPages(Page, PortalSettings);
                }
                else if (!string.IsNullOrEmpty(Action) && Page.StateID.HasValue)
                {
                    foreach (string Locale in GetCultureListItems())
                    {
                        string _Locale = PortalSettings.DefaultLanguage == Locale ? null : Locale;

                        Pages LocalPage = GetLatestVersion(Page.TabID, _Locale, false);
                        if (LocalPage != null)
                        {
                            if (_Locale == Page.Locale)
                            {
                                LocalPage = Page;
                            }

                            if (Action == "approve" || Action == "publish")
                            {
                                int StateID = WorkflowManager.GetNextStateID(wState.WorkflowID, wState.StateID);
                                if (WorkflowManager.IsLastState(wState.WorkflowID, StateID))
                                {
                                    LocalPage.IsPublished = true;
                                    LocalPage.PublishedBy = UserInfo.UserID;
                                    LocalPage.PublishedOn = DateTime.UtcNow;
                                }
                                else
                                {
                                    LocalPage.IsPublished = false;
                                    LocalPage.PublishedBy = null;
                                    LocalPage.PublishedOn = null;
                                }
                                LocalPage.StateID = StateID;

                            }
                            else if (Action == "reject")
                            {
                                LocalPage.StateID = WorkflowManager.GetPreviousStateID(wState.WorkflowID, wState.StateID);
                            }

                            PageFactory.Update(LocalPage, UserInfo.UserID);
                        }
                    }

                }

                if (string.IsNullOrEmpty(Action) && IsHaveReviewPermission && !WorkflowManager.IsFirstState(wState.WorkflowID, wState.StateID))
                {
                    string SystemLog = "System Log: Changes made by user";
                    WorkflowLog log = WorkflowManager.GetPagesWorkflowLogs(Page.TabID, Page.Version).LastOrDefault();
                    if (log == null || (log != null && (!log.Comment.Contains(SystemLog) || log.ReviewedBy != UserInfo.UserID)))
                    {
                        WorkflowFactory.AddWorkflowLog(PortalSettings.PortalId, 0, UserInfo.UserID, Page, "approve", "System Log: Changes made by user");

                    }
                }

                PageFactory.Update(Page, UserInfo.UserID);
            }

            private static void AddLocalPages(Pages Page, PortalSettings PortalSettings)
            {
                int UserID = PortalSettings.UserInfo.UserID;
                string DefaultLanguage = PortalSettings.DefaultLanguage;

                foreach (string local in GetCultureListItems())
                {
                    string _TempLocale = local == DefaultLanguage ? null : local;
                    Pages LocalPage = GetLatestVersion(Page.TabID, _TempLocale, false);
                    if (Page.Version > 1)
                    {
                        if (LocalPage != null && Page.Locale != _TempLocale)
                        {
                            if (Page.Version > LocalPage.Version)
                            {
                                LocalPage.ID = 0;
                            }

                            LocalPage.Version = Page.Version;
                            LocalPage.StateID = Page.StateID;
                            LocalPage.IsPublished = false;
                            LocalPage.PublishedBy = null;
                            LocalPage.PublishedOn = null;
                        }
                        else
                        {
                            LocalPage = Page;
                        }
                        PageFactory.Update(LocalPage, UserID);
                    }
                    else
                    {
                        if (_TempLocale == Page.Locale)
                        {
                            PageFactory.Update(Page, PortalSettings.UserInfo.UserID);
                        }
                        else if (LocalPage != null)
                        {
                            LocalPage.Version = Page.Version;
                            LocalPage.StateID = Page.StateID;
                            LocalPage.IsPublished = false;
                            LocalPage.PublishedBy = null;
                            LocalPage.PublishedOn = null;
                            PageFactory.Update(LocalPage, PortalSettings.UserInfo.UserID);
                        }
                    }
                }

            }

            public static void UpdatePage(Pages page, int UserID)
            {
                PageFactory.Update(page, UserID);
            }

            public static List<Pages> GetAllPublishedPages(int PortalID, string Locale)
            {
                return PageFactory.GetAllPublishedPages(PortalID, Locale);
            }

            public static List<string> GetCultureListItems()
            {

                List<string> Languages = new List<string>();
                try
                {
                    IEnumerable<System.Web.UI.WebControls.ListItem> cultureListItems = DotNetNuke.Services.Localization.Localization.LoadCultureInListItems(CultureDropDownTypes.NativeName, CultureInfo.CurrentCulture.ToString(), "", false);
                    PortalSettings ps = PortalController.Instance.GetCurrentSettings() as PortalSettings;
                    foreach (Locale loc in LocaleController.Instance.GetLocales(ps.PortalId).Values)
                    {
                        string defaultRoles = PortalController.GetPortalSetting(string.Format("DefaultTranslatorRoles-{0}", loc.Code), ps.PortalId, "Administrators");
                        if (!ps.ContentLocalizationEnabled || (LocaleIsAvailable(loc) && (PortalSecurity.IsInRoles(ps.AdministratorRoleName) || loc.IsPublished || PortalSecurity.IsInRoles(defaultRoles))))
                        {
                            foreach (System.Web.UI.WebControls.ListItem cultureItem in cultureListItems)
                            {
                                if (cultureItem.Value == loc.Code)
                                {
                                    Languages.Add(loc.Code);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }

                return Languages;
            }
            private static bool LocaleIsAvailable(Locale Locale)
            {
                TabInfo tab = (PortalController.Instance.GetCurrentSettings() as PortalSettings).ActiveTab;
                if (tab.DefaultLanguageTab != null)
                {
                    tab = tab.DefaultLanguageTab;
                }

                TabInfo localizedTab = TabController.Instance.GetTabByCulture(tab.TabID, tab.PortalID, Locale);

                return localizedTab != null && !localizedTab.IsDeleted && TabPermissionController.CanViewPage(localizedTab);
            }

            private static void BuildGlobalBlockMarkup(int PortalId, int UserId, HtmlNode item)
            {
                string blockguid = item.Attributes.Where(a => a.Name == "data-guid").FirstOrDefault().Value;
                if (!string.IsNullOrEmpty(blockguid))
                {
                    CustomBlock customBlock = BlockManager.GetByLocale(PortalId, blockguid, GetCultureCode(PortalController.Instance.GetCurrentSettings() as PortalSettings));
                    if (customBlock == null)
                    {
                        customBlock = new CustomBlock
                        {
                            Guid = blockguid.ToLower(),
                            PortalID = PortalId,
                            Name = item.Attributes.Where(a => a.Name == "data-name").FirstOrDefault().Value,
                            Category = item.Attributes.Where(a => a.Name == "data-category").FirstOrDefault().Value,
                            IsGlobal = true,
                            CreatedBy = UserId,
                            UpdatedBy = UserId,
                            CreatedOn = DateTime.UtcNow,
                            UpdatedOn = DateTime.UtcNow
                        };
                        string[] str = item.InnerHtml.Split(new string[] { "</style>" }, StringSplitOptions.None);
                        if (str.Length > 1)
                        {
                            customBlock.Html = str[1];
                            customBlock.Css = str[0];
                        }
                        else
                        {
                            customBlock.Html = str[0];
                        }

                        BlockFactory.AddUpdate(customBlock);
                    }
                    item.InnerHtml = item.Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault().Value;
                    item.Attributes.Remove("data-category");
                    item.Attributes.Remove("data-name");
                }
            }

            public static IEnumerable<TabInfo> GetPageList(PortalSettings settings, int parentId = -1, string searchKey = "", bool includeHidden = true, bool includeDeleted = false, bool includeSubpages = false)
            {
                PortalSettings portalSettings = settings ?? PortalController.Instance.GetCurrentSettings() as PortalSettings;
                int adminTabId = portalSettings.AdminTabId;

                List<TabInfo> tabs = TabController.GetPortalTabs(portalSettings.PortalId, adminTabId, false, includeHidden, includeDeleted, true);
                IEnumerable<TabInfo> pages = from t in tabs
                                             where (t.ParentId != adminTabId || t.ParentId == Null.NullInteger) &&
                                                     !t.IsSystem &&
                                                         ((string.IsNullOrEmpty(searchKey) && (includeSubpages || t.ParentId == parentId))
                                                             || (!string.IsNullOrEmpty(searchKey) &&
                                                                     (t.TabName.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase) > Null.NullInteger
                                                                         || t.LocalizedTabName.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase) > Null.NullInteger)))
                                             select t;

                return includeSubpages ? pages.OrderBy(x => x.ParentId > -1 ? x.ParentId : x.TabID).ThenBy(x => x.TabID) : pages;
            }

            public static IEnumerable<TabInfo> GetPageList(PortalSettings portalSettings, bool? deleted, string tabName, string tabTitle, string tabPath,
                string tabSkin, bool? visible, int parentId, out int total, string searchKey = "", int pageIndex = -1, int pageSize = 10, bool includeSubpages = false)
            {
                pageIndex = pageIndex <= 0 ? 0 : pageIndex;
                pageSize = pageSize > 0 && pageSize <= 100 ? pageSize : 10;
                IEnumerable<TabInfo> tabs = GetPageList(portalSettings, parentId, searchKey, true, deleted ?? false, includeSubpages);
                List<TabInfo> finalList = new List<TabInfo>();
                if (deleted.HasValue)
                {
                    tabs = tabs.Where(tab => tab.IsDeleted == deleted);
                }

                if (visible.HasValue)
                {
                    tabs = tabs.Where(tab => tab.IsVisible == visible);
                }

                if (!string.IsNullOrEmpty(tabTitle) || !string.IsNullOrEmpty(tabName) || !string.IsNullOrEmpty(tabPath) ||
                    !string.IsNullOrEmpty(tabSkin))
                {
                    foreach (TabInfo tab in tabs)
                    {
                        bool bIsMatch = true;
                        if (!string.IsNullOrEmpty(tabTitle))
                        {
                            bIsMatch &= Regex.IsMatch(tab.Title, tabTitle.Replace("*", ".*"), RegexOptions.IgnoreCase);
                        }

                        if (!string.IsNullOrEmpty(tabName))
                        {
                            bIsMatch &= Regex.IsMatch(tab.TabName, tabName.Replace("*", ".*"), RegexOptions.IgnoreCase);
                        }

                        if (!string.IsNullOrEmpty(tabPath))
                        {
                            bIsMatch &= Regex.IsMatch(tab.TabPath, tabPath.Replace("*", ".*"), RegexOptions.IgnoreCase);
                        }

                        if (!string.IsNullOrEmpty(tabSkin))
                        {
                            string escapedString = Regex.Replace(tabSkin, "([^\\w^\\*\\s]+)+", @"\$1", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline);
                            bIsMatch &= Regex.IsMatch(tab.SkinSrc, escapedString.Replace("*", ".*"), RegexOptions.IgnoreCase);
                        }

                        if (bIsMatch)
                        {
                            finalList.Add(tab);
                        }
                    }
                }
                else
                {
                    finalList.AddRange(tabs);
                }
                total = finalList.Count;
                return finalList.Skip(pageIndex * pageSize).Take(pageSize);
            }

            public static ReviewSettings GetPageReviewSettings(PortalSettings PortalSettings)
            {
                int TabID = PortalSettings.ActiveTab.TabID;
                UserInfo UserInfo = PortalSettings.UserInfo;
                Pages page = GetLatestVersion(TabID, PortalSettings.UserInfo);

                bool IsPageDraft = false;
                bool IsContentApproval = false;
                bool IsLocked = false;
                string NextStateName = string.Empty;
                bool IsModeratorEditPermission = false;
                int CurruntWorkflowID = WorkflowManager.GetDefaultWorkflow(TabID);
                if (page != null && page.StateID.HasValue)
                {
                    WorkflowState State = WorkflowManager.GetStateByID(page.StateID.Value);
                    if (State != null)
                    {

                        bool IsFirstState = WorkflowManager.IsFirstState(State.WorkflowID, State.StateID);
                        bool IsLastState = WorkflowManager.IsLastState(State.WorkflowID, State.StateID);
                        IsPageDraft = WorkflowManager.IsFirstState(State.WorkflowID, State.StateID);
                        IsLocked = !IsFirstState && !IsLastState;

                        if (IsLastState && State.WorkflowID != CurruntWorkflowID)
                        {
                            State = WorkflowManager.GetFirstStateID(CurruntWorkflowID);
                        }

                        if (WorkflowManager.GetWorkflowType(State.WorkflowID) == Core.Components.Enum.WorkflowTypes.ContentApproval)
                        {
                            IsContentApproval = true;


                            if (WorkflowManager.HasReviewPermission(State.StateID, UserInfo) && TabPermissionController.CanManagePage(PortalSettings.ActiveTab))
                            {
                                WorkflowState NextState = Core.Managers.WorkflowManager.GetStateByID(Core.Managers.WorkflowManager.GetNextStateID(State.WorkflowID, State.StateID));
                                NextStateName = NextState != null ? NextState.Name : string.Empty;
                                IsModeratorEditPermission = true;
                            }
                            else
                            {
                                WorkflowState NextState = Core.Managers.WorkflowManager.GetStateByID(Core.Managers.WorkflowManager.GetNextStateID(State.WorkflowID, Core.Managers.WorkflowManager.GetFirstStateID(State.WorkflowID).StateID));
                                NextStateName = NextState != null ? NextState.Name : string.Empty;
                            }
                        }

                    }
                }
                else
                {
                    if (CurruntWorkflowID > 0)
                    {
                        IsContentApproval = Core.Managers.WorkflowManager.GetWorkflowType(CurruntWorkflowID) == Core.Components.Enum.WorkflowTypes.ContentApproval;
                        WorkflowState NextState = Core.Managers.WorkflowManager.GetStateByID(Core.Managers.WorkflowManager.GetNextStateID(CurruntWorkflowID, Core.Managers.WorkflowManager.GetFirstStateID(CurruntWorkflowID).StateID));
                        NextStateName = NextState != null ? NextState.Name : string.Empty;
                    }
                }

                ReviewSettings ReviewSetting = new ReviewSettings
                {
                    IsPageDraft = IsPageDraft,
                    IsContentApproval = IsContentApproval,
                    NextStateName = NextStateName,
                    IsLocked = IsLocked,
                    IsModeratorEditPermission = IsModeratorEditPermission
                };

                return ReviewSetting;
            }
            public static List<int> GetAllTabIdByPortalID(int Portalid, bool OnlyPublished = true)
            {
                return PageFactory.GetAllTabIdByPortalID(Portalid, OnlyPublished);
            }
            public static string TokenizeTemplateLinks(string content, bool IsJson, Dictionary<string, string> Assets)
            {
                if (IsJson)
                {
                    dynamic deserializeObject = JsonConvert.DeserializeObject(content);
                    if (deserializeObject != null)
                    {
                        foreach (dynamic arr in deserializeObject)
                        {
                            ProcessJsonObject(arr, Assets);
                        }
                        content = JsonConvert.SerializeObject(deserializeObject);
                    }
                }
                else
                {
                    HtmlDocument html = new HtmlDocument();
                    html.LoadHtml(content);
                    HtmlNodeCollection NodeCollectionSrc = html.DocumentNode.SelectNodes("//*[@src]");
                    if (NodeCollectionSrc != null)
                    {
                        foreach (HtmlNode node in NodeCollectionSrc)
                        {
                            node.Attributes["src"].Value = GetNewLink(node.Attributes["src"].Value, Assets);
                        }
                    }
                    HtmlNodeCollection NodeCollectionSrcSet = html.DocumentNode.SelectNodes("//*[@srcset]");
                    if (NodeCollectionSrcSet != null)
                    {
                        foreach (HtmlNode node in NodeCollectionSrcSet)
                        {
                            node.Attributes["srcset"].Value = GetNewLink(node.Attributes["srcset"].Value, Assets);
                        }
                    }
                    content = html.DocumentNode.OuterHtml;
                }
                return content;
            }

            private static string GetNewLink(string url, Dictionary<string, string> Assets)
            {
                if (url.Contains(','))
                {
                    return url;
                }
                else
                {
                    url = url.Split('?')[0];
                    string newurl = ExportTemplateRootToken + System.IO.Path.GetFileName(url);
                    if (!Assets.ContainsKey(newurl))
                    {
                        Assets.Add(newurl, url);
                    }
                    else if (Assets.ContainsKey(newurl) && Assets[newurl] != url)
                    {
                        string FileExtension = newurl.Substring(newurl.LastIndexOf('.'));
                        string tempNewUrl = newurl;
                        int count = 1;
                        Find:
                        if (Assets.ContainsKey(tempNewUrl) && Assets[tempNewUrl] != url)
                        {
                            tempNewUrl = newurl.Remove(newurl.Length - FileExtension.Length) + count + FileExtension;
                            count++;
                            goto Find;
                        }
                        else
                        {
                            newurl = tempNewUrl;
                            if (!Assets.ContainsKey(newurl))
                            {
                                Assets.Add(newurl, url);
                            }
                        }
                    }
                    return newurl;
                }
            }

            private static void ProcessJsonObject(dynamic arr, Dictionary<string, string> Assets)
            {
                foreach (JProperty prop in arr.Properties())
                {
                    if ((prop.Name == "src" || prop.Name == "srcset") && !string.IsNullOrEmpty(prop.Value.ToString()))
                    {
                        prop.Value = GetNewLink(prop.Value.ToString(), Assets);
                    }
                }
                if (arr.attributes != null)
                {
                    foreach (dynamic prop in arr.attributes)
                    {
                        if ((prop.Name == "src" || prop.Name == "srcset") && !string.IsNullOrEmpty(prop.Value.ToString()))
                        {
                            prop.Value = GetNewLink(prop.Value.ToString(), Assets);
                        }
                    }
                }
                if (arr.components != null)
                {
                    foreach (dynamic obj in arr.components)
                    {
                        ProcessJsonObject(obj, Assets);
                    }
                }
            }

            public static string TokenizeLinks(string content, int portalId)
            {
                string portalRoot = GetPortalRoot(portalId);
                Regex exp = new Regex(portalRoot, RegexOptions.IgnoreCase);
                content = exp.Replace(content, PortalRootToken);
                return content;
            }
            public static string DeTokenizeLinks(string content, int portalId)
            {
                string portalRoot = GetPortalRoot(portalId);
                content = content.Replace(PortalRootToken, portalRoot);
                return content;
            }

            public static string GetPortalRoot(int portalId)
            {

                PortalInfo portal = PortalController.Instance.GetPortal(portalId);

                string portalRoot = UrlUtils.Combine(DotNetNuke.Common.Globals.ApplicationPath, portal.HomeDirectory);

                if (!portalRoot.StartsWith("/"))
                {
                    portalRoot = "/" + portalRoot;
                }

                return portalRoot;
            }
        }
    }
}