using DotNetNuke.Abstractions;
using DotNetNuke.Common;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.Tokens;
using DotNetNuke.UI.Skins;
using DotNetNuke.Web.Client.ClientResourceManagement;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Vanjaro.Common;
using Vanjaro.Common.ASPNET;
using Vanjaro.Common.Engines.TokenEngine;
using Vanjaro.Common.Manager;
using Vanjaro.Core.Components;
using Vanjaro.Core.Data.Entities;
using Vanjaro.Core.Entities.Menu;
using static Vanjaro.Core.Managers;
using static Vanjaro.Skin.Managers;

namespace Vanjaro.Skin
{
    public partial class Base : DotNetNuke.UI.Skins.Skin
    {
        private readonly string[] JSExcludes = new string[] { "dnn.modalpopup.js", "dnncore.js", "dnn.dragdrop.js", "jquery.js", "jquery.min.js", "jquery-migrate.min.js", "jquery-ui.min.js", "jquery-ui.js", "jquery.hoverIntent.min.js", "dnn.jquery.js", "jquery-migrate.js", "knockout.mapping.js", "knockout.js", "query.tokeninput.js", "dnn.DropDownList.js", "dnn.DynamicTreeView.js", "dnn.TreeView.js", "dnn.jScrollBar.js", "jquery.mousewheel.js", "dnn.DataStructures.js", "dnn.jquery.extensions.js", "dnn.extensions.js" };
        private readonly string[] CSSExcludes = new string[] { "dnndefault/7.0.0/default.css", "dnndefault/8.0.0/default.css", "portals/" + PortalSettings.Current.PortalId + "/home.css", "portals/_default/admin.css", "dnn.dragdrop.css", "dnn.DropDownList.css", "dnn.jScrollBar.css", "token-input-facebook.css" };
        private readonly string[] JSExcludesContains = new string[] { "dnn.editbar" };
        private readonly string[] CSSExcludesContains = new string[] { "dnn.editbar" };

        private readonly string[] JSLExcludes = new string[] { CommonJs.KnockoutMapping };
        private Pages page = null;
        private bool? m2v = null;
        private dynamic ModulesDictObj;
        private readonly Dictionary<int, dynamic> ModulesDict = new Dictionary<int, dynamic>();
        bool HasReviewPermission = false;
        protected override void OnPreRender(EventArgs e)
        {
            HandleAppSettings();
            
            RemoveDNNDependencies();

        }

        private void RemoveDNNDependencies()
        {
            if (string.IsNullOrEmpty(Request.QueryString["ctl"]))
            {
                RemoveDependencies("ClientDependencyHeadJs");
                RemoveDependencies("ClientDependencyHeadCss");
                RemoveDependencies("BodySCRIPTS");
                RemoveDependencies("ClientResourcesFormBottom");
                RemoveDependencies("ClientResourceIncludes");
            }
        }


        protected override void OnInit(EventArgs e)
        {
            if (Request.QueryString["m2v"] != null)
                m2v = Convert.ToBoolean(Request.QueryString["m2v"]);

            HandleEditMode();

            if (m2v.HasValue || !string.IsNullOrEmpty(Request.QueryString["SkinSrc"]))
            {
                ToggleUserMode();
            }

            MigratePage();

            InjectViewport();

            GetCookieConsentMarkup();

            AddModulePanes();

            if (!m2v.HasValue || (m2v.HasValue && m2v.Value && !string.IsNullOrEmpty(Request.QueryString["SkinSrc"])))
            {
                LoadModuleInFrame();
            }

            base.OnInit(e);

            InjectExtensionControl();

            RenderLocalizedMetaData();

        }

        private void HandleEditMode()
        {
            ///Forcing View Mode Until we can find a solution to prevent Edit Bar from appearing. 
            ToggleUserMode("VIEW");
            return;

            ///Code to support IsEditable pending a solution to prevent Edit Bar from appearing. 
#pragma warning disable CS0162 // Unreachable code detected
            if (HttpContext.Current != null && HttpContext.Current.Request.Cookies["PageIsEdit"] != null && HttpContext.Current.Request.Cookies["PageIsEdit"].Value == "true")
#pragma warning restore CS0162 // Unreachable code detected
            {
                ToggleUserMode("EDIT");
            }
            else
            {
                ToggleUserMode("VIEW");
            }

            if (m2v.HasValue || !string.IsNullOrEmpty(Request.QueryString["SkinSrc"]))
            {
                ToggleUserMode();
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            JavaScript.RequestRegistration(CommonJs.jQuery, null, SpecificVersion.Latest);

            PageManager.Init(Page, PortalSettings);
            if (string.IsNullOrEmpty(Request.QueryString["mid"]) || (!string.IsNullOrEmpty(Request.QueryString["icp"]) && bool.Parse(Request.QueryString["icp"]) && (!string.IsNullOrEmpty(Request.QueryString["mid"]) && Request.QueryString["mid"] != "0")))
            {
                if (page != null && page.StateID.HasValue)
                    HasReviewPermission = WorkflowManager.HasReviewPermission(page.StateID.Value, PortalSettings.UserInfo);

                WebForms.LinkCSS(Page, "ThemeCSS", Page.ResolveUrl("~/Portals/" + PortalSettings.PortalId + "/vThemes/" + Core.Managers.ThemeManager.GetCurrentThemeName() + "/Theme.css"));
                WebForms.LinkCSS(Page, "SkinCSS", Page.ResolveUrl("~/Portals/_default/Skins/Vanjaro/Resources/css/skin.css"));

                //Skin js requried because using for openpopup update memeber Profile when user is registered user 
                //VjDefaultPath used in Skin.js for loading icon.
                WebForms.RegisterClientScriptBlock(Page, "DefaultPath", "var VjThemePath='" + Page.ResolveUrl("~/Portals/_default/vThemes/" + Core.Managers.ThemeManager.GetCurrentThemeName()) + "'; var VjDefaultPath='" + Page.ResolveUrl("~/DesktopModules/Vanjaro/UXManager/Library/Resources/Images/") + "'; var VjSitePath='" + Page.ResolveUrl("~/DesktopModules/Vanjaro/") + "';", true);
                ClientResourceManager.RegisterScript(Page, Page.ResolveUrl("~/Portals/_default/Skins/Vanjaro/Resources/js/skin.js"), 2, "DnnFormBottomProvider");
                ClientResourceManager.RegisterScript(Page, Page.ResolveUrl("~/DesktopModules/Vanjaro/Common/Frameworks/Bootstrap/4.5.0/js/bootstrap.min.js"), 1, "DnnFormBottomProvider");
                
                string DirectoryPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Portals/_default/vThemes/" + Core.Managers.ThemeManager.GetCurrentThemeName() + "/js/");
                if ((TabPermissionController.HasTabPermission("EDIT") || (page != null && page.StateID.HasValue && HasReviewPermission)) && Directory.Exists(DirectoryPath))
                {
                    string script = "";

                    foreach (string file in Directory.GetFiles(DirectoryPath))
                    {
                        string FileName = Path.GetFileName(file);
                        if (!string.IsNullOrEmpty(FileName))
                        {
                            if (FileName.EndsWith(".js"))
                                script += File.ReadAllText(DirectoryPath + "/" + FileName);
                        }
                    }

                    if (!string.IsNullOrEmpty(script.Trim()))
                        WebForms.RegisterStartupScript(Page, "ThemeBlocks", "LoadThemeBlocks = function (grapesjs) { grapesjs.plugins.add('ThemeBlocks', (editor, opts = {}) => { " + script + "}); };", true);
                }
                else
                {
                    string ThemeJS = "~/Portals/_default/vThemes/" + Core.Managers.ThemeManager.GetCurrentThemeName() + "/theme.js";
                    if (File.Exists(System.Web.Hosting.HostingEnvironment.MapPath(ThemeJS)))
                        ClientResourceManager.RegisterScript(Page, Page.ResolveUrl(ThemeJS));
                }
            }
            else
            {
                WebForms.LinkCSS(Page, "EditCSS", Page.ResolveUrl("~/Portals/_default/Skins/Vanjaro/Resources/css/edit.css"));
            }

            ResetModulePanes();
        }

        private void InjectViewport()
        {
            HtmlMeta tagKeyword = new HtmlMeta
            {
                ID = "Vanjaro_Viewport",
                Name = "viewport",
                Content = "width=device-width, initial-scale=1, minimum-scale=1"
            };
            //if (Viewport)
            //tagKeyword.Content += " user-scalable=1";
            //else
            tagKeyword.Content += " user-scalable=0";

            if (Page.Header.FindControl("Vanjaro_Viewport") == null)
            {
                Page.Header.Controls.Add(tagKeyword);
            }
        }

        private static Control FindControlRecursive(Control rootControl, string controlId)
        {
            if (rootControl.ID == controlId)
            {
                return rootControl;
            }

            foreach (Control controlToSearch in rootControl.Controls)
            {
                Control controlToReturn = FindControlRecursive(controlToSearch, controlId);
                if (controlToReturn != null)
                {
                    return controlToReturn;
                }
            }
            return null;
        }

        private void AddModulePanes()
        {
            //Get the Page Markup
            if (!IsAllowed())
            {
                if (FindControlRecursive(this, "ContentPane") == null)
                {
                    Controls.Add(ParseControl("<div id=\"ContentPane\" runat=\"server\" />"));
                }

                BuildPanes();
                return;
            }
            if (string.IsNullOrEmpty(Request.QueryString["ctl"]))
            {
                if (FindControlRecursive(this, "ContentPane") == null)
                {
                    Controls.Add(ParseControl("<div id=\"ContentPane\" runat=\"server\" />"));
                }

                Control ContentPane = FindControlRecursive(this, "ContentPane");

                StringBuilder sb = new StringBuilder();
                page = GetPage();
                if (page != null)
                {
                    InjectStyle(page);
                    if (page.ReplaceTokens)
                    {
                        sb.Append(new TokenReplace().ReplaceEnvironmentTokens(page.Content));
                    }
                    else
                    {
                        sb.Append(page.Content.ToString());
                    }
                }

                if (page != null && page.StateID.HasValue)
                    HasReviewPermission = WorkflowManager.HasReviewPermission(page.StateID.Value, PortalSettings.UserInfo);


                if (TabPermissionController.HasTabPermission("EDIT") || (page != null && page.StateID.HasValue && HasReviewPermission))
                {
                    FrameworkManager.Load(this, "jQuerySweetAlert");
                    FrameworkManager.Load(this, "Toastr");
                }

                if ((TabPermissionController.HasTabPermission("EDIT") || HasReviewPermission))
                    WebForms.RegisterClientScriptBlock(Page, "ReviewGlobalVariable", GetReviewGlobalVariable(PortalSettings, HasReviewPermission), true);

                if (!TabPermissionController.HasTabPermission("EDIT") && HasReviewPermission)
                {
                    FrameworkManager.Load(this, "FontAwesome");
                }

                if (TabPermissionController.HasTabPermission("EDIT"))
                {
                    try
                    {
                        string Fonts = JsonConvert.SerializeObject(Core.Managers.ThemeManager.GetDDLFonts("all"));
                        WebForms.RegisterClientScriptBlock(Page, "VJThemeFonts", "var VJFonts=" + Fonts + ";", true);
                    }
                    catch (Exception) { }
                }


                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(sb.ToString());
                InjectBlocks(page, html);

                string ClassName = "vj-wrapper";
                if (!string.IsNullOrEmpty(Request.QueryString["m2v"]) && string.IsNullOrEmpty(Request.QueryString["pv"]))
                {
                    ClassName += " m2vDisplayNone";
                }
                ContentPane.Controls.Add(ParseControl("<div class=\"" + ClassName + "\"><div id=\"VjContentPane\">" + InjectModules(html.DocumentNode.OuterHtml) + "</div></div>"));
                InjectLoginAuthentication();
            }
            else
            {
                string FolderPath = HttpContext.Current.Server.MapPath("~/Portals/_default/vThemes/" + Core.Managers.ThemeManager.GetCurrentThemeName() + "/Layout.Edit.html");
                string Content = System.IO.File.ReadAllText(FolderPath);
                Controls.Add(ParseControl(Content));
            }
            BuildPanes();
        }

        private void InjectLoginAuthentication()
        {
            if (FindControlRecursive(this, "vLoginControls") is HtmlContainerControl VLoginControls)
            {

                bool IsAuth = false;
                HtmlGenericControl createDiv = new HtmlGenericControl("div")
                {
                    ID = "TabDiv"
                };

                HtmlGenericControl TabContent = new HtmlGenericControl("div")
                {
                    ID = "TabContent"
                };
                TabContent.Attributes["class"] = "tab-content";

                HtmlGenericControl SocialRegistration = new HtmlGenericControl("div");
                SocialRegistration.Attributes["class"] = "dnnSocialRegistration";

                HtmlGenericControl socialControls = new HtmlGenericControl("div");
                socialControls.Attributes["id"] = "socialControls";

                HtmlGenericControl Authdiv = new HtmlGenericControl("ul");
                Authdiv.Attributes["class"] = "buttonList";

                int tab = 1;
                foreach (DotNetNuke.Services.Authentication.AuthenticationInfo authSystem in DotNetNuke.Services.Authentication.AuthenticationController.GetEnabledAuthenticationServices())
                {
                    if (authSystem.AuthenticationType != "Vanjaro")
                    {
                        var authLoginControl = (DotNetNuke.Services.Authentication.AuthenticationLoginBase)LoadControl("~/" + authSystem.LoginControlSrc);
                        if (authLoginControl.Enabled)
                        {
                            if (authLoginControl is DotNetNuke.Services.Authentication.OAuth.OAuthLoginBase oAuthLoginControl)
                            {
                                Authdiv.Controls.Add(oAuthLoginControl);
                            }
                            else
                            {
                                if (!IsAuth)
                                {
                                    string ResourceFile = Page.ResolveUrl("~/DesktopModules/AuthenticationServices") + "/Vanjaro/" + DotNetNuke.Services.Localization.Localization.LocalResourceDirectory + "/" + Path.GetFileNameWithoutExtension(authSystem.LoginControlSrc);
                                    createDiv.InnerHtml += "<ul class=\"nav nav-tabs\" id=\"nav-tab\" role=\"tablist\">";
                                    createDiv.InnerHtml += "<li class=\"nav-item\"><a class=\"nav-link active\" id=\"nav-tab-" + tab + "\" data-toggle=\"tab\" href=\"#dnn_navtab_" + tab + "\" role=\"tab\" aria-controls=\"dnn_navtab_" + tab + "\" aria-selected=\"true\">" + DotNetNuke.Services.Localization.Localization.GetString("Title", ResourceFile) + "</a></li>";

                                    HtmlGenericControl homediv = new HtmlGenericControl("div");
                                    homediv.Attributes["class"] = "tab-pane fade show active";
                                    homediv.Attributes["role"] = "tabpanel";
                                    homediv.Attributes["aria-labelledby"] = "nav-tab-" + tab;
                                    homediv.Attributes["data-toggle"] = "tab";
                                    homediv.ID = "navtab_" + tab;

                                    Control Login = FindControl("LoginControl");
                                    if (Login != null)
                                    {
                                        homediv.Controls.Add(Login);
                                    }

                                    TabContent.Controls.Add(homediv);
                                    tab++;
                                    IsAuth = true;
                                }

                                string LocalResourceFile = authLoginControl.LocalResourceFile + Path.GetFileNameWithoutExtension(authSystem.LoginControlSrc);
                                createDiv.InnerHtml += "<li class=\"nav-item\"><a class=\"nav-link\" id=\"nav-tab-" + tab + "\" data-toggle=\"tab\" href=\"#dnn_navtab_" + tab + "\" role=\"tab\" aria-controls=\"dnn_navtab_" + tab + "\" aria-selected=\"false\">" + DotNetNuke.Services.Localization.Localization.GetString("Title", LocalResourceFile) + "</a></li>";
                                HtmlGenericControl tabdiv = new HtmlGenericControl("div");
                                tabdiv.Attributes["class"] = "tab-pane fade";
                                tabdiv.Attributes["role"] = "tabpanel";
                                tabdiv.Attributes["aria-labelledby"] = "nav-tab-" + tab;
                                tabdiv.Attributes["data-toggle"] = "tab";
                                tabdiv.ID = "navtab_" + tab;
                                tabdiv.Controls.Add(authLoginControl);
                                TabContent.Controls.Add(tabdiv);
                                tab++;
                            }
                        }
                    }
                }

                if (IsAuth)
                {
                    createDiv.InnerHtml += "</ul>";
                    createDiv.Controls.Add(TabContent);

                    VLoginControls.Controls.Add(createDiv);
                }

                socialControls.Controls.Add(Authdiv);
                SocialRegistration.Controls.Add(socialControls);
                VLoginControls.Controls.Add(SocialRegistration);
            }
        }

        private void ResetModulePanes()
        {
            foreach (ModuleInfo m in PortalSettings.ActiveTab.Modules)
            {
                if (ModulesDict.ContainsKey(m.ModuleID))
                {
                    dynamic item = ModulesDict[m.ModuleID];
                    try
                    {
                        if (item != null && item.PaneName != null)
                        {
                            m.PaneName = item.PaneName;
                        }
                    }
                    catch { }
                    try
                    {
                        if (item != null && item.IsDeleted != null)
                        {
                            m.IsDeleted = item.IsDeleted;
                        }
                    }
                    catch { }
                }
            }
        }

        private static string GetReviewGlobalVariable(PortalSettings PortalSettings, bool HasReviewPermission)
        {
            string Script = string.Empty;
            if (TabPermissionController.HasTabPermission("EDIT") || HasReviewPermission)
            {
                ReviewSettings ReviewSetting = Core.Managers.PageManager.GetPageReviewSettings(PortalSettings);
                Script = "var VJIsPageDraft='" + ReviewSetting.IsPageDraft + "';  var VJIsContentApproval='" + ReviewSetting.IsContentApproval + "'; var VJNextStateName='" + ReviewSetting.NextStateName + "'; var VJIsLocked='" + ReviewSetting.IsLocked + "'; var VJIsModeratorEditPermission='" + ReviewSetting.IsModeratorEditPermission + "'; var VJLocal='" + Core.Managers.PageManager.GetCultureCode(PortalSettings) + "';";
            }
            return Script;
        }

        private void BuildPanes()
        {
            if (FindControlRecursive(this, "ContentPane") is HtmlContainerControl contentPane)
            {
                base.PortalSettings.ActiveTab.Panes.Add(contentPane.ID);
                Panes.Add(contentPane.ID.ToLowerInvariant(), new Pane(contentPane));
            }

            if (!m2v.HasValue || m2v.HasValue && m2v.Value && !string.IsNullOrEmpty(Request.QueryString["SkinSrc"]))
            {
                foreach (ModuleInfo m in PortalSettings.ActiveTab.Modules)
                {
                    if (FindControl("vj_" + m.ModuleID.ToString()) is HtmlContainerControl paneCtrl)
                    {
                        base.PortalSettings.ActiveTab.Panes.Add(paneCtrl.ID);
                        Panes.Add(paneCtrl.ID.ToLowerInvariant(), new Pane(paneCtrl));
                    }
                }
            }

        }

        private void InjectBlocks(Pages page, HtmlDocument html, bool ignoreFirstDiv = false, bool isGlobalBlockCall = false)
        {
            IEnumerable<HtmlNode> query = html.DocumentNode.Descendants("div");
            int ctr = 1;
            foreach (HtmlNode item in query.ToList())
            {
                if (ignoreFirstDiv && ctr == 1)
                {
                    ctr++;
                    continue;
                }
                if (item.Attributes.Where(a => a.Name == "data-block-guid").FirstOrDefault() != null && !string.IsNullOrEmpty(item.Attributes.Where(a => a.Name == "data-block-guid").FirstOrDefault().Value))
                {
                    Dictionary<string, string> Attributes = new Dictionary<string, string>();
                    foreach (HtmlAttribute attr in item.Attributes)
                    {
                        Attributes.Add(attr.Name, attr.Value);
                    }

                    ThemeTemplateResponse response = Core.Managers.BlockManager.Render(Attributes);
                    if (response != null)
                    {
                        if (item.Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault().Value == "Logo" && page != null)
                        {
                            dynamic contentJSON = JsonConvert.DeserializeObject<dynamic>(page.ContentJSON.ToString());
                            BuildLogoBlock(contentJSON, response, item, isGlobalBlockCall);
                        }
                        else
                        {
                            item.InnerHtml = response.Markup;
                        }

                        if (item.Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault().Value.ToLower() == "global")
                        {
                            HtmlDocument globalhtml = new HtmlDocument();
                            if (item.ChildNodes != null && item.ChildNodes.Count > 0 && item.ChildNodes[0].Attributes != null && item.ChildNodes[0].Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault() != null && item.ChildNodes[0].Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault().Value.ToLower() == "global")
                            {
                                globalhtml.LoadHtml(item.ChildNodes[0].InnerHtml);
                            }
                            else
                            {
                                globalhtml.LoadHtml(item.InnerHtml);
                            }

                            InjectBlocks(page, globalhtml, true, true);
                            item.InnerHtml = globalhtml.DocumentNode.OuterHtml;
                        }
                        else
                        {
                            HtmlDocument localhtml = new HtmlDocument();
                            localhtml.LoadHtml(item.InnerHtml);
                            if (localhtml.DocumentNode.ChildNodes.Count > 0)
                            {
                                item.InnerHtml = localhtml.DocumentNode.ChildNodes[0].InnerHtml;
                            }
                            else
                            {
                                item.InnerHtml = localhtml.DocumentNode.InnerHtml;
                            }
                        }

                        if (response.Scripts != null)
                        {
                            foreach (string script in response.Scripts)
                            {
                                if (!string.IsNullOrEmpty(script))
                                {
                                    WebForms.RegisterClientScriptInclude(Page, script, Page.ResolveUrl(script));
                                }
                            }
                        }

                        if (response.Styles != null)
                        {
                            foreach (string style in response.Styles)
                            {
                                if (!string.IsNullOrEmpty(style))
                                {
                                    WebForms.LinkCSS(Page, style, Page.ResolveUrl(style));
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(response.Script))
                        {
                            WebForms.RegisterClientScriptBlock(Page, "BlocksScript" + item.Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault().Value, response.Script, true);
                        }

                        if (!string.IsNullOrEmpty(response.Style))
                        {
                            WebForms.RegisterClientScriptBlock(Page, "BlocksStyle" + item.Attributes.Where(a => a.Name == "data-block-type").FirstOrDefault().Value, "<style type=\"text/css\">" + response.Style + "</style>", false);
                        }
                    }

                }
            }
        }

        private static void BuildLogoBlock(dynamic contentJSON, ThemeTemplateResponse Response, HtmlNode Node, bool isGlobalBlockCall)
        {
            if (Node.Attributes["id"] != null)
            {
                string id = Node.Attributes["id"].Value;
                if (contentJSON != null)
                {
                    foreach (dynamic con in contentJSON)
                    {
                        if (con.attributes != null && con.attributes["data-block-guid"] != null && con.attributes["data-block-guid"] != "" && con.attributes["data-block-type"] == "Logo" && con.attributes["id"] != null && con.attributes["id"] == id)
                        {
                            HtmlDocument contentJSONHtml = new HtmlDocument();
                            contentJSONHtml.LoadHtml(con["content"].Value.ToString());
                            IEnumerable<HtmlNode> contentJSONQuery = contentJSONHtml.DocumentNode.Descendants("img");
                            if (contentJSONQuery != null && contentJSONQuery.FirstOrDefault() != null && contentJSONQuery.FirstOrDefault().Attributes != null && contentJSONQuery.FirstOrDefault().Attributes.Where(a => a.Name == "style").FirstOrDefault() != null)
                            {
                                HtmlDocument LogoHtml = new HtmlDocument();
                                LogoHtml.LoadHtml(Response.Markup);
                                IEnumerable<HtmlNode> LogoImg = LogoHtml.DocumentNode.Descendants("img");
                                if (LogoImg != null && LogoImg.FirstOrDefault() != null && LogoImg.FirstOrDefault().Attributes != null)
                                {
                                    if (LogoImg.FirstOrDefault().Attributes.Where(a => a.Name == "style").FirstOrDefault() != null)
                                    {
                                        LogoImg.FirstOrDefault().Attributes["style"].Value = contentJSONQuery.FirstOrDefault().Attributes.Where(a => a.Name == "style").FirstOrDefault().Value;
                                    }
                                    else
                                    {
                                        LogoImg.FirstOrDefault().Attributes.Add("style", contentJSONQuery.FirstOrDefault().Attributes.Where(a => a.Name == "style").FirstOrDefault().Value);
                                    }
                                }
                                Node.InnerHtml = LogoHtml.DocumentNode.OuterHtml;
                            }
                            else
                            {
                                Node.InnerHtml = Response.Markup;
                            }
                        }
                        else if (con.components != null)
                        {
                            BuildLogoBlock(con.components, Response, Node, isGlobalBlockCall);
                        }
                    }
                }
            }
            else if (isGlobalBlockCall)
            {
                HtmlDocument LogoHtml = new HtmlDocument();
                LogoHtml.LoadHtml(Response.Markup);
                IEnumerable<HtmlNode> LogoImg = LogoHtml.DocumentNode.Descendants("img");
                if (LogoImg != null && LogoImg.FirstOrDefault() != null && LogoImg.FirstOrDefault().Attributes != null && Node.Attributes["data-style"] != null)
                {
                    if (LogoImg.FirstOrDefault().Attributes.Where(a => a.Name == "style").FirstOrDefault() != null)
                    {
                        LogoImg.FirstOrDefault().Attributes["style"].Value = Node.Attributes["data-style"].Value;
                    }
                    else
                    {
                        LogoImg.FirstOrDefault().Attributes.Add("style", Node.Attributes["data-style"].Value);
                    }
                }
                else if (LogoImg != null && LogoImg.FirstOrDefault() != null && LogoImg.FirstOrDefault().Attributes != null && LogoHtml.DocumentNode.ChildNodes[0].Attributes["data-style"] != null)
                {
                    if (LogoImg.FirstOrDefault().Attributes.Where(a => a.Name == "style").FirstOrDefault() != null)
                    {
                        LogoImg.FirstOrDefault().Attributes["style"].Value = LogoHtml.DocumentNode.ChildNodes[0].Attributes["data-style"].Value;
                    }
                    else
                    {
                        LogoImg.FirstOrDefault().Attributes.Add("style", LogoHtml.DocumentNode.ChildNodes[0].Attributes["data-style"].Value);
                    }
                }
                Node.InnerHtml = LogoHtml.DocumentNode.OuterHtml;
            }
        }

        private string InjectModules(string sb)
        {
            if (!string.IsNullOrEmpty(sb))
            {
                foreach (ModuleInfo m in PortalSettings.ActiveTab.Modules)
                {
                    if (sb.Contains("<app id=\"" + m.ModuleID + "\"></app>"))
                    {
                        ModulesDictObj = new ExpandoObject();
                        ModulesDictObj.PaneName = m.PaneName;
                        ModulesDictObj.IsDeleted = m.IsDeleted;
                        ModulesDict.Add(m.ModuleID, ModulesDictObj);
                        //Replace Tokens w/ID's
                        sb = sb.Replace("<app id=\"" + m.ModuleID + "\"></app>", "<div id=\"vj_" + m.ModuleID.ToString() + "\" runat=\"server\" />");
                        m.PaneName = "vj_" + m.ModuleID.ToString();
                        m.IsDeleted = false;
                        InjectModuleActions(m);
                    }
                    else
                    {
                        ModulesDictObj = new ExpandoObject();
                        ModulesDictObj.IsDeleted = m.IsDeleted;
                        ModulesDict.Add(m.ModuleID, ModulesDictObj);
                        m.IsDeleted = true;
                    }
                }
            }
            else
            {
                foreach (ModuleInfo m in PortalSettings.ActiveTab.Modules)
                {
                    ModulesDictObj = new ExpandoObject();
                    ModulesDictObj.IsDeleted = m.IsDeleted;
                    ModulesDict.Add(m.ModuleID, ModulesDictObj);
                    m.IsDeleted = true;
                }
            }
            return sb;
        }
        private string InjectModuleActions(ModuleInfo ModuleInfo)
        {
            try
            {
                if (LoadControl(Page.ResolveUrl("~/" + ModuleInfo.ModuleControl.ControlSrc)) is IActionable)
                {
                    IActionable ctl = LoadControl(Page.ResolveUrl("~/" + ModuleInfo.ModuleControl.ControlSrc)) as IActionable;
                    if (ctl != null && ctl.ModuleActions != null)
                    {
                        foreach (ModuleAction rootAction in ctl.ModuleActions)
                        {
                            //Process Children
                            List<ModuleAction> actions = new List<ModuleAction>();
                            foreach (ModuleAction action in rootAction.Actions)
                            {
                                if (action.Visible)
                                {
                                    if ((Globals.IsAdminControl() == false) ||
                                        (action.Secure != SecurityAccessLevel.Anonymous && action.Secure != SecurityAccessLevel.View))
                                    {
                                        if (!action.Icon.Contains("://")
                                                && !action.Icon.StartsWith("/")
                                                && !action.Icon.StartsWith("~/"))
                                        {
                                            action.Icon = "~/images/" + action.Icon;
                                        }
                                        if (action.Icon.StartsWith("~/"))
                                        {
                                            action.Icon = Globals.ResolveUrl(action.Icon);
                                        }

                                        actions.Add(action);
                                    }
                                }

                            }

                            //var oSerializer = new JavaScriptSerializer();
                            //if (rootAction.Title == Localization.GetString("ModuleGenericActions.Action", Localization.GlobalResourceFile))
                            //{
                            //    AdminActionsJSON = oSerializer.Serialize(actions);
                            //}
                            //else
                            //{
                            //    if (rootAction.Title == Localization.GetString("ModuleSpecificActions.Action", Localization.GlobalResourceFile))
                            //    {
                            //        CustomActionsJSON = oSerializer.Serialize(actions);
                            //    }
                            //}
                        }
                    }
                }
            }
            catch { }

            return string.Empty;

        }

        private void InjectStyle(Pages page)
        {
            HtmlGenericControl style = new HtmlGenericControl
            {
                TagName = "style"
            };
            style.Attributes.Add("type", "text/css");
            style.InnerHtml = page.Style.Trim('"');
            Page.Header.Controls.Add(style);
        }

        private Pages GetPage()
        {
            Pages page;
            if (!string.IsNullOrEmpty(Request.QueryString["revisionversion"]) && TabPermissionController.HasTabPermission("EDIT"))
            {
                page = PageManager.GetByVersion(PortalSettings.ActiveTab.TabID, Convert.ToInt32(Request.QueryString["revisionversion"]), PageManager.GetCultureCode(PortalSettings));
            }
            else if (!string.IsNullOrEmpty(Request.QueryString["uxmode"]) && Convert.ToBoolean(Request.QueryString["uxmode"]))
            {
                page = PageManager.GetLatestVersion(PortalSettings.ActiveTab.TabID, true, PageManager.GetCultureCode(PortalSettings), true);
            }
            else
            {
                page = PageManager.GetLatestVersion(PortalSettings.ActiveTab.TabID, PageManager.GetCultureCode(PortalSettings));
            }

            return page;
        }

        private void InjectExtensionControl()
        {
            //For menu
            if (!string.IsNullOrEmpty(Request.QueryString["mid"]) && int.Parse(Request.QueryString["mid"]) == 0)
            {
                UserControl ctl = (UserControl)Page.LoadControl(Page.ResolveUrl("~/DesktopModules/Vanjaro/UXManager/Library/Extension.ascx"));
                Page.Form.Controls.Add(ctl);
            }
        }

        private bool IsAllowed()
        {
            if (string.IsNullOrEmpty(Request.QueryString["icp"]) || Convert.ToBoolean(Request.QueryString["icp"]) == false)
            {
                return true;
            }
            else if (!string.IsNullOrEmpty(Request.QueryString["icp"]) && Convert.ToBoolean(Request.QueryString["icp"]) == true && !string.IsNullOrEmpty(Request.QueryString["pv"]) && Convert.ToBoolean(Request.QueryString["pv"]) == true)
            {
                return true;
            }

            return false;
        }

        private void RemoveDependencies(string ResourceControl)
        {
            Control loader = Page.FindControl(ResourceControl);

            if (loader != null)
            {
                for (int i = loader.Controls.Count - 1; i >= 0; i--)
                {
                    DnnCssInclude cssInclude = loader.Controls[i] as DnnCssInclude;
                    DnnJsInclude jsInclude = loader.Controls[i] as DnnJsInclude;

                    if (cssInclude != null && (CSSExcludes.Any(c => cssInclude.FilePath.ToLower().EndsWith(c.ToLower())) || CSSExcludesContains.Any(c => cssInclude.FilePath.ToLower().Contains(c.ToLower()))))
                    {
                        loader.Controls.Remove(cssInclude);
                        continue;
                    }
                    else if (jsInclude != null && (JSExcludes.Any(j => jsInclude.FilePath.ToLower().EndsWith(j.ToLower())) || JSExcludesContains.Any(j => jsInclude.FilePath.ToLower().Contains(j.ToLower()))))
                    {
                        loader.Controls.Remove(jsInclude);
                        continue;
                    }
                }
            }
        }

        private void LoadModuleInFrame()
        {
            if (!string.IsNullOrEmpty(Request.QueryString["mid"]) && TabPermissionController.HasTabPermission("EDIT"))
            {
                int mid = int.Parse(Request.QueryString["mid"]);
                List<ModuleInfo> ModulesToRemove = new List<ModuleInfo>();
                foreach (ModuleInfo m in PortalSettings.ActiveTab.Modules)
                {
                    if (m.ModuleID != mid)
                    {
                        ModulesToRemove.Add(m);
                    }
                }
                foreach (ModuleInfo m in ModulesToRemove)
                {
                    PortalSettings.ActiveTab.Modules.Remove(m);
                }
            }
        }

        private void HandleAppSettings()
        {
            if (!string.IsNullOrEmpty(Request.QueryString["ctl"]))
            {
                string script = @"$(document).ready(function () {                               
                               $('.dnnActions').click(function () {
                                   $(window.parent.document.body).find('#defaultModalnew [data-dismiss=" + @"modal" + @"]').click();
                               });
                               setTimeout(function () {$('[href=""#msSpecificSettings""]').click();},100);
                          });";
                WebForms.RegisterStartupScript(Page, "ModuleSettingScript", script, true);
            }
        }
        private void RenderLocalizedMetaData()
        {
            DotNetNuke.Framework.CDefault basePage = (DotNetNuke.Framework.CDefault)Page;
            if (page != null)
            {
                basePage.Title = !string.IsNullOrEmpty(page.Title) ? page.Title : basePage.Title;
                basePage.Description = !string.IsNullOrEmpty(page.Description) ? page.Description : basePage.Description;
            }

            // Set Default Description if page description empty or not given
            if (string.IsNullOrEmpty(basePage.Description))
            {
                string Vanjaro_Description = DotNetNuke.Services.Localization.Localization.GetString("Vanjaro_Description", DotNetNuke.Services.Localization.Localization.GlobalResourceFile);
                basePage.Description = !string.IsNullOrEmpty(Vanjaro_Description) ? Vanjaro_Description : "Vanjaro is built on DNN Platform";
            }

        }


        private void GetCookieConsentMarkup()
        {
            try
            {
                if (string.IsNullOrEmpty(Request.QueryString["mid"]) && string.IsNullOrEmpty(Request.QueryString["icp"]))
                {
                    if (Request.Cookies["cookieconsent_status"] == null)
                    {
                        string d = PortalController.GetPortalSetting("Vanjaro_CookieConsent", PortalSettings.PortalId, "False");
                        if (d == bool.TrueString)
                        {
                            if (FindControlRecursive(this, "CookieConsentPane") == null)
                            {
                                Controls.Add(ParseControl("<div id=\"CookieConsentPane\" runat=\"server\" />"));
                            }

                            if (FindControlRecursive(this, "CookieConsentPane") != null)
                            {

                                IDictionary<string, object> Objects = new ExpandoObject() as IDictionary<string, object>;
                                Objects.Add("CookieConsentUrl", PortalSettings.CookieMoreLink);
                                string Template = RazorEngineManager.RenderTemplate("", BlockPath + "/Cookie Consent/", "Default", Objects);
                                Template = new DNNLocalizationEngine(null, ResouceFilePath, false).Parse(Template);
                                FindControlRecursive(this, "CookieConsentPane").Controls.Add(ParseControl(Template));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DotNetNuke.Services.Exceptions.Exceptions.LogException(ex);
            }
        }



        public string ResouceFilePath
        {
            get
            {
                return Globals.ApplicationMapPath + @"\Portals\_default\" + Core.Managers.BlockManager.GetTheme() + "App_LocalResources\\Shared.resx";
            }
        }

        public string BlockPath
        {
            get
            {
                return Core.Managers.BlockManager.GetVirtualPath() + Core.Managers.BlockManager.GetTheme() + "templates/design";
            }
        }


        #region Private Method
        // Edit Page in DNN Persona Bar. Lock Edit Bar. Open a Vanjaro page is new window.
        public static void ToggleUserMode(string mode = "VIEW")
        {
            DotNetNuke.Services.Personalization.PersonalizationController personalizationController = new DotNetNuke.Services.Personalization.PersonalizationController();
            DotNetNuke.Services.Personalization.PersonalizationInfo personalization = personalizationController.LoadProfile(PortalSettings.Current.UserInfo.UserID, PortalSettings.Current.PortalId);
            if (personalization.Profile["Usability:UserMode" + PortalSettings.Current.PortalId] != null)
            {
                personalization.Profile["Usability:UserMode" + PortalSettings.Current.PortalId] = mode.ToUpper();
                personalization.IsModified = true;
                personalizationController.SaveProfile(personalization);
            }
        }

        #region Migrate Page
        private void MigratePage()
        {
            if (m2v.HasValue || !string.IsNullOrEmpty(Request.QueryString["m2vsetup"]))
            {
                WebForms.RegisterClientScriptBlock(Page, "m2vStyle", "<div class='style-wrapper'><style type=\"text/css\">#dnn_ContentPane >.DnnModule{display:none !important;}</style></div>", false);
            }

            //Only work when call is not from Iframe like page,role etc extension,
            if (string.IsNullOrEmpty(Request.QueryString["mid"]) && string.IsNullOrEmpty(Request.QueryString["icp"]))
            {
                if (m2v.HasValue && !string.IsNullOrEmpty(Request.QueryString["SkinSrc"]) && Request.QueryString["SkinSrc"].ToLower() == "[G]Skins/Vanjaro/Base".ToLower() && PortalSettings.UserInfo.IsInRole("Administrators"))
                {
                    Pages page = GetPage();
                    if (page != null && !page.IsPublished)
                    {
                        if (!m2v.Value && !page.Content.Contains("data-m2v"))
                        {
                            HtmlDocument html = new HtmlDocument();
                            html.LoadHtml(page.Content.ToString());
                            page.Content = MigrateManager.MigrateInjectBlocks(PortalSettings, html);
                            PageManager.UpdatePage(page, PortalSettings.UserInfo.UserID);
                            Response.Redirect(NavigationManager.NavigateURL(PortalSettings.ActiveTab.TabID, "", "m2v=true", "skinsrc=[g]skins/vanjaro/base", "containersrc=[g]containers/vanjaro/base"));
                        }

                        // Redirect if Page already migrated 
                        if (page.Content.ToString().Contains("data-m2v") && !m2v.Value)
                        {
                            Response.Redirect(NavigationManager.NavigateURL(PortalSettings.ActiveTab.TabID, "", "m2v=true", "skinsrc=[g]skins/vanjaro/base", "containersrc=[g]containers/vanjaro/base"));
                        }
                        else
                        {
                            if (!(!string.IsNullOrEmpty(Request.QueryString["pv"]) && Request.QueryString["pv"] == "true"))
                            {
                                WebForms.RegisterStartupScript(Page, "MigratePage", "<script type=\"text/javascript\" vanjarocore=\"true\">" + MigrateManager.GetMigratePageToastMarkup(Page) + "</script> <style type=\"text/css\">" + MigrateManager.GetMigratePageCSS(Page) + "</style>", false);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}