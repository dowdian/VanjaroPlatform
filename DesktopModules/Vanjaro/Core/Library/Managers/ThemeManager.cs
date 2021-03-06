using DotNetNuke.Entities.Portals;
using LibSassHost;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using Vanjaro.Core.Components;
using Vanjaro.Core.Entities.Interface;
using Vanjaro.Core.Entities.Theme;
using static Vanjaro.Core.Factories;

namespace Vanjaro.Core
{
    public static partial class Managers
    {
        public class ThemeManager
        {
            public static string GetCurrentThemeName(int? PortalID = null)
            {
                int PortalId = -1;
                if (PortalID != null)
                    PortalId = PortalID.Value;
                else if (PortalSettings.Current != null)
                    PortalId = PortalSettings.Current.PortalId;
                string ThemeValue = "Basic";
                Data.Entities.Setting ThemeSetting = SettingManager.GetSettings(PortalId, -1, "setting_theme").Where(s => s.Name == "Theme").FirstOrDefault();
                if (ThemeSetting != null)
                {
                    ThemeValue = ThemeSetting.Value;
                }
                return ThemeValue;
            }
            public static List<string> GetControlTypes()
            {
                List<string> result = new List<string>
                {
                    "Slider",
                    "Dropdown",
                    "Color Picker",
                    "Fonts"
                };
                return result;
            }
            public static ThemeEditor GetThemeEditor(string categoryguid, string guid)
            {
                int index = 0;
                ThemeEditorWrapper ThemeEditorWrapper = GetThemeEditors(PortalSettings.Current.PortalId, categoryguid);
                if (ThemeEditorWrapper != null)
                {
                    return GetThemeEditor(ThemeEditorWrapper.ThemeEditors, guid, ref index);
                }
                else
                {
                    return null;
                }
            }
            public static List<IThemeEditor> GetCategories()
            {
                string CacheKey = CacheFactory.GetCacheKey(CacheFactory.Keys.ThemeCategory, "AllPortals");
                List<IThemeEditor> Items = CacheFactory.Get(CacheKey);
                if (Items == null || Items.Count == 0)
                {
                    Items = new List<IThemeEditor>();
                    string[] binAssemblies = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")).Where(c => c.EndsWith(".dll")).ToArray();
                    foreach (string Path in binAssemblies)
                    {
                        try
                        {
                            Items.AddRange((from t in System.Reflection.Assembly.LoadFrom(Path).GetTypes()
                                            where t != (typeof(IThemeEditor)) && (typeof(IThemeEditor).IsAssignableFrom(t))
                                            select Activator.CreateInstance(t) as IThemeEditor).ToList());
                        }
                        catch { continue; }
                    }
                    CacheFactory.Set(CacheKey, Items);
                }
                return Items;
            }
            internal static ThemeEditor GetThemeEditor(List<ThemeEditor> themeEditors, string guid, ref int index)
            {
                if (!string.IsNullOrEmpty(guid) && themeEditors != null)
                {
                    foreach (ThemeEditor item in themeEditors)
                    {
                        index++;
                        if (item.Guid.ToLower() == guid.ToLower())
                        {
                            return item;
                        }
                    }
                }
                index = -1;
                return null;
            }
            public static void ProcessScss(int PortalID)
            {
                StringBuilder sb = new StringBuilder();
                string ThemeName = GetCurrentThemeName(PortalID);
                string BootstrapPath = HttpContext.Current.Server.MapPath("~/Portals/_default/vThemes/" + ThemeName + "/scss/Bootstrap/bootstrap.scss");
                string BeforePath = HttpContext.Current.Server.MapPath("~/Portals/_default/vThemes/" + ThemeName + "/scss/Before.scss");
                string AfterPath = HttpContext.Current.Server.MapPath("~/Portals/_default/vThemes/" + ThemeName + "/scss/After.scss");

                foreach (ThemeFont font in GetFonts(PortalID, "all"))
                {
                    if (!string.IsNullOrEmpty(font.Css))
                    {
                        sb.Append(font.Css);
                    }
                }

                if (File.Exists(BeforePath))
                {
                    sb.Append(File.ReadAllText(BeforePath));
                }

                List<string> Css = new List<string>();
                foreach (IThemeEditor category in GetCategories())
                {
                    List<ThemeEditorValue> themeEditorValues = GetThemeEditorValues(PortalID, category.Guid);
                    ThemeEditorWrapper editors = GetThemeEditors(PortalID, category.Guid);
                    if (editors != null && editors.ThemeEditors != null)
                    {
                        foreach (IGrouping<string, ThemeEditor> themeEditorGroup in GetThemeEditors(PortalID, category.Guid).ThemeEditors.GroupBy(g => g.Category).OrderBy(a => a.Key).ToList())
                        {
                            foreach (ThemeEditor item in themeEditorGroup.OrderBy(a => a.Title).ToList())
                            {
                                string sass = item.Sass;
                                foreach (dynamic ctl in item.Controls)
                                {
                                    string id = ctl.Guid.ToString();
                                    string css = ctl.CustomCSS.ToString();
                                    string variable = ctl.LessVariable.ToString();
                                    string DefaultValue = ctl.DefaultValue.ToString();

                                    ThemeEditorValue editorValue = themeEditorValues?.Where(t => t.Guid.ToLower() == id.ToLower()).FirstOrDefault();
                                    if (editorValue != null)
                                    {
                                        DefaultValue = editorValue.Value;
                                    }

                                    if (!string.IsNullOrEmpty(DefaultValue) && !string.IsNullOrEmpty(css))
                                    {
                                        string[] strings = new string[] { variable };
                                        strings = css.Split(strings, StringSplitOptions.None);
                                        css = string.Join(DefaultValue, strings);
                                        Css.Add(css + ';');
                                    }
                                    else if (!string.IsNullOrEmpty(DefaultValue) && !string.IsNullOrEmpty(variable) && variable.StartsWith("$"))
                                    {
                                        Css.Add(variable + ":" + DefaultValue + " !default;");
                                    }

                                    if (!string.IsNullOrEmpty(sass))
                                    {
                                        Css.Add(sass + ';');
                                    }
                                }
                            }
                        }
                    }
                }

                if (Css != null && Css.Count > 0)
                {
                    Css = Css.Distinct().ToList();
                    foreach (string str in Css)
                    {
                        sb.Append(str);
                    }
                }

                if (File.Exists(BootstrapPath))
                {
                    sb.Append(File.ReadAllText(BootstrapPath));
                }

                if (File.Exists(AfterPath))
                {
                    sb.Append(File.ReadAllText(AfterPath));
                }

                if (sb.Length > 0)
                {
                    string ThemeCss = HttpContext.Current.Server.MapPath("~/Portals/" + PortalID + "/vThemes/" + ThemeName + "/Theme.css");
                    if (!File.Exists(ThemeCss))
                    {
                        File.Create(ThemeCss).Dispose();
                    }
                    else
                    {
                        File.Copy(ThemeCss, ThemeCss.Replace("Theme.css", "Theme.backup.css"), true);
                    }

                    CompilationResult result = SassCompiler.Compile(sb.ToString(), HttpContext.Current.Server.MapPath("~/Portals/_default/vThemes/" + ThemeName + "/scss/Bootstrap/"));
                    File.WriteAllText(ThemeCss, result.CompiledContent);
                    PortalController.IncrementCrmVersion(PortalID);

                    UnloadSassCompiler();
                }
            }

            [DllImport("kernel32")]
            private static extern bool FreeLibrary(IntPtr hModule);
            /// <summary>
            /// Frees libsass.dll so it can be replaced in Vanjaro Package Update. 
            /// </summary>
            public static void UnloadSassCompiler()
            {
                foreach (var p in Process.GetProcesses().Where(p => p.ProcessName.ToLower() == "w3wp"))
                {
                    foreach (ProcessModule mod in p.Modules)
                    {
                        if (mod.ModuleName.ToLower() == "libsass.dll")
                        {
                            FreeLibrary(mod.BaseAddress);
                            break;
                        }
                    }
                }
            }

            public static void Save(string CategoryGuid, List<ThemeEditorValue> ThemeEditorValues)
            {
                File.WriteAllText(GetThemeEditorValueJsonPath(PortalSettings.Current.PortalId, CategoryGuid), JsonConvert.SerializeObject(ThemeEditorValues));
                CacheFactory.Clear(CacheFactory.Keys.ThemeManager);
            }
            public static bool Delete(string CategoryGuid, string Category, string SubCategory)
            {
                try
                {
                    ThemeEditorWrapper ThemeEditorWrapper = GetThemeEditors(PortalSettings.Current.PortalId, CategoryGuid);
                    if (ThemeEditorWrapper != null && ThemeEditorWrapper.ThemeEditors != null)
                    {
                        if (!string.IsNullOrEmpty(Category) && !string.IsNullOrEmpty(SubCategory))
                        {
                            List<string> FilteredGuids = ThemeEditorWrapper.ThemeEditors.Where(t => t.Category.ToLower() == Category.ToLower() && !string.IsNullOrEmpty(t.Title) && t.Title.ToLower() == SubCategory.ToLower()).Select(s => s.Guid).ToList();
                            if (FilteredGuids != null && FilteredGuids.Count > 0)
                            {
                                ThemeEditorWrapper.ThemeEditors = ThemeEditorWrapper.ThemeEditors.Where(t => !FilteredGuids.Contains(t.Guid)).ToList();
                            }
                        }
                        else
                        {
                            ThemeEditorWrapper.ThemeEditors = ThemeEditorWrapper.ThemeEditors.Where(t => t.Category.ToLower() != Category.ToLower()).ToList();
                        }

                        UpdateThemeEditorJson(CategoryGuid, ThemeEditorWrapper);
                    }
                    return true;
                }
                catch (Exception ex) { DotNetNuke.Services.Exceptions.Exceptions.LogException(ex); return false; }
            }
            private static void UpdateThemeEditorJson(string CategoryGuid, ThemeEditorWrapper ThemeEditorWrapper)
            {
                string ThemeEditorJsonPath = GetThemeEditorJsonPath(PortalSettings.Current.PortalId, CategoryGuid);
                if (ThemeEditorJsonPath.EndsWith("theme.editor.custom.json"))
                {
                    ThemeEditorWrapper.DeveloperMode = true;
                }

                string Content = JsonConvert.SerializeObject(ThemeEditorWrapper);
                File.WriteAllText(ThemeEditorJsonPath, Content);
                CacheFactory.Clear(CacheFactory.Keys.ThemeManager);
            }
            public static bool Update(string categoryGuid, ThemeEditor themeEditor)
            {
                try
                {
                    if (string.IsNullOrEmpty(themeEditor.Guid))
                    {
                        themeEditor.Guid = Guid.NewGuid().ToString();
                    }
                    ThemeEditorWrapper ThemeEditorWrapper = GetThemeEditors(PortalSettings.Current.PortalId, categoryGuid);
                    if (ThemeEditorWrapper != null && ThemeEditorWrapper.ThemeEditors != null)
                    {
                        int index = -1;
                        ThemeEditor existingThemeEditor = GetThemeEditor(ThemeEditorWrapper.ThemeEditors, themeEditor.Guid, ref index);
                        if (existingThemeEditor != null && index >= 0)
                        {
                            ThemeEditorWrapper.ThemeEditors[index] = themeEditor;
                        }
                        else
                        {
                            ThemeEditorWrapper.ThemeEditors.Add(themeEditor);
                        }
                    }
                    else
                    {
                        if (ThemeEditorWrapper == null)
                        {
                            ThemeEditorWrapper = new ThemeEditorWrapper();
                        }

                        ThemeEditorWrapper.ThemeEditors = new List<ThemeEditor>
                        {
                            themeEditor
                        };
                    }
                    UpdateThemeEditorJson(categoryGuid, ThemeEditorWrapper);
                    return true;
                }
                catch (Exception ex) { DotNetNuke.Services.Exceptions.Exceptions.LogException(ex); return false; }
            }
            public static void BuildThemeEditor(ThemeEditor themeEditor)
            {
                List<ThemeEditorControl> NewControls = new List<ThemeEditorControl>();
                foreach (dynamic control in themeEditor.Controls)
                {
                    if (control.Type == "Slider")
                    {
                        Slider slider = JsonConvert.DeserializeObject<Slider>(control.ToString());
                        if (slider != null)
                        {
                            if (string.IsNullOrEmpty(slider.Guid))
                            {
                                slider.Guid = Guid.NewGuid().ToString();
                            }

                            NewControls.Add(slider);
                        }
                    }
                    else if (control.Type == "Dropdown")
                    {
                        Dropdown dropdown = JsonConvert.DeserializeObject<Dropdown>(control.ToString());
                        if (dropdown != null)
                        {
                            if (string.IsNullOrEmpty(dropdown.Guid))
                            {
                                dropdown.Guid = Guid.NewGuid().ToString();
                            }

                            NewControls.Add(dropdown);
                        }
                    }
                    else if (control.Type == "Color Picker")
                    {
                        ColorPicker colorPicker = JsonConvert.DeserializeObject<ColorPicker>(control.ToString());
                        if (colorPicker != null)
                        {
                            if (string.IsNullOrEmpty(colorPicker.Guid))
                            {
                                colorPicker.Guid = Guid.NewGuid().ToString();
                            }

                            NewControls.Add(colorPicker);
                        }
                    }
                    else
                    {
                        Fonts fonts = JsonConvert.DeserializeObject<Fonts>(control.ToString());
                        if (fonts != null)
                        {
                            if (string.IsNullOrEmpty(fonts.Guid))
                            {
                                fonts.Guid = Guid.NewGuid().ToString();
                            }

                            NewControls.Add(fonts);
                        }
                    }
                }
                themeEditor.Controls = new List<dynamic>();
                themeEditor.Controls.AddRange(NewControls);
            }
            public static ThemeEditorWrapper GetThemeEditors(int PortalID, string CategoryGuid)
            {
                string ThemeEditorJsonPath = GetThemeEditorJsonPath(PortalID, CategoryGuid);

                if (!File.Exists(ThemeEditorJsonPath))
                {
                    File.Create(ThemeEditorJsonPath).Dispose();
                }

                string CacheKey = CacheFactory.GetCacheKey(CacheFactory.Keys.ThemeManager, PortalID, CategoryGuid);
                ThemeEditorWrapper result = CacheFactory.Get(CacheKey);
                if (result == null)
                {
                    result = JsonConvert.DeserializeObject<ThemeEditorWrapper>(File.ReadAllText(ThemeEditorJsonPath));
                    CacheFactory.Set(CacheKey, result);
                }
                return result;
            }
            public static List<ThemeFont> GetFonts(int PortalID, string CategoryGuid)
            {
                List<ThemeFont> Fonts = new List<ThemeFont>();
                if (!string.IsNullOrEmpty(CategoryGuid))
                {
                    if (CategoryGuid.ToLower() == "all")
                    {
                        foreach (IThemeEditor te in GetCategories())
                        {
                            ThemeEditorWrapper ThemeEditorWrapper = GetThemeEditors(PortalID, te.Guid);
                            if (ThemeEditorWrapper != null && ThemeEditorWrapper.Fonts != null)
                            {
                                Fonts.AddRange(ThemeEditorWrapper.Fonts);
                            }
                        }
                    }
                    else
                    {
                        ThemeEditorWrapper ThemeEditorWrapper = GetThemeEditors(PortalID, CategoryGuid);
                        if (ThemeEditorWrapper != null && ThemeEditorWrapper.Fonts != null)
                        {
                            Fonts.AddRange(ThemeEditorWrapper.Fonts);
                        }
                    }
                }
                return Fonts;
            }
            public static List<StringTextNV> GetDDLFonts(string CategoryGuid)
            {
                List<StringTextNV> FontList = new List<StringTextNV>();
                FontList.AddRange(GetFonts(PortalSettings.Current.PortalId, CategoryGuid).Select(x => new StringTextNV { Name = x.Name, Value = x.Family }));
                return FontList;
            }
            public static void UpdateFonts(string CategoryGuid, dynamic data)
            {
                ThemeEditorWrapper ThemeEditorWrapper = GetThemeEditors(PortalSettings.Current.PortalId, CategoryGuid);

                if (ThemeEditorWrapper == null)
                {
                    ThemeEditorWrapper = new ThemeEditorWrapper();
                }

                if (ThemeEditorWrapper.Fonts == null)
                {
                    ThemeEditorWrapper.Fonts = new List<ThemeFont>();
                }

                string GUID = !string.IsNullOrEmpty(data.Guid.ToString()) ? data.Guid.ToString() : Guid.NewGuid().ToString();

                if (ThemeEditorWrapper.Fonts.Where(a => a.Guid.ToLower() == GUID.ToLower()).FirstOrDefault() != null)
                {
                    ThemeFont ThemeFont = ThemeEditorWrapper.Fonts.Where(a => a.Guid.ToLower() == GUID.ToLower()).FirstOrDefault();
                    if (ThemeFont != null)
                    {
                        ThemeFont.Name = data.Name;
                        ThemeFont.Family = data.Family;
                        ThemeFont.Css = data.Css;
                    }
                }
                else
                {
                    ThemeEditorWrapper.Fonts.Add(new ThemeFont { Guid = GUID, Name = data.Name.ToString(), Family = data.Family.ToString(), Css = data.Css.ToString() });
                }

                UpdateThemeEditorJson(CategoryGuid, ThemeEditorWrapper);

            }
            public static void DeleteFonts(string CategoryGuid, ThemeFont data)
            {
                ThemeEditorWrapper ThemeEditorWrapper = GetThemeEditors(PortalSettings.Current.PortalId, CategoryGuid);

                if (ThemeEditorWrapper == null)
                {
                    ThemeEditorWrapper = new ThemeEditorWrapper();
                }

                if (ThemeEditorWrapper.Fonts == null)
                {
                    ThemeEditorWrapper.Fonts = new List<ThemeFont>();
                }

                string GUID = !string.IsNullOrEmpty(data.Guid.ToString()) ? data.Guid.ToString() : Guid.NewGuid().ToString();

                if (ThemeEditorWrapper.Fonts.Where(a => a.Guid.ToLower() == GUID.ToLower()).FirstOrDefault() != null)
                {
                    ThemeFont ThemeFont = ThemeEditorWrapper.Fonts.Where(a => a.Guid.ToLower() == GUID.ToLower()).FirstOrDefault();
                    ThemeEditorWrapper.Fonts.Remove(ThemeFont);
                    UpdateThemeEditorJson(CategoryGuid, ThemeEditorWrapper);
                }
            }
            internal static List<ThemeEditorValue> GetThemeEditorValues(int PortalId, string CategoryGuid)
            {
                string CacheKey = CacheFactory.GetCacheKey(CacheFactory.Keys.ThemeManager, PortalId, "Values", CategoryGuid);
                List<ThemeEditorValue> result = CacheFactory.Get(CacheKey);
                if (result == null)
                {
                    result = JsonConvert.DeserializeObject<List<ThemeEditorValue>>(File.ReadAllText(GetThemeEditorValueJsonPath(PortalId, CategoryGuid)));
                    CacheFactory.Set(CacheKey, result);
                }
                return result;
            }
            public static string GetMarkUp(string identifier, string Guid)
            {
                StringBuilder sb = new StringBuilder();
                ThemeEditorWrapper editors = GetThemeEditors(PortalSettings.Current.PortalId, Guid);
                if (editors != null && editors.ThemeEditors != null)
                {
                    List<ThemeEditorValue> themeEditorValues = GetThemeEditorValues(PortalSettings.Current.PortalId, Guid);
                    foreach (IGrouping<string, ThemeEditor> item in GetThemeEditors(PortalSettings.Current.PortalId, Guid).ThemeEditors.GroupBy(g => g.Category).OrderBy(a => a.Key).ToList())
                    {
                        sb.Append(GetMarkUp(identifier, item, themeEditorValues, editors.DeveloperMode, Guid));
                    }
                }
                return sb.ToString();
            }
            private static string GetMarkUp(string identifier, IGrouping<string, ThemeEditor> themeEditorGroup, List<ThemeEditorValue> themeEditorValues, bool developerMode, string Guid)
            {
                StringBuilder sb = new StringBuilder();
                if (identifier == "setting_settings")
                {
                    sb.Append("<div class=\"firstblock\"><div class=\"optiontheme mainblocks\"><i id=\"gjs-sm-caret\" class=\"fa fa-caret-right\"></i><label>" + themeEditorGroup.Key + "</label></div><div class=\"child-wrapper\">");
                }
                else
                {
                    sb.Append("<div class=\"firstblock\"><div class=\"optiontheme mainblocks\" style=\"font-weight: bold;font-size: 16px;\"><i id=\"gjs-sm-caret\" class=\"fa fa-caret-right\"></i><label>" + themeEditorGroup.Key + "</label>" + GetCategoryEditClick(themeEditorGroup, developerMode, Guid) + "</div><div class=\"child-wrapper\">");
                }

                foreach (ThemeEditor item in themeEditorGroup.OrderBy(a => a.Title).ToList())
                {
                    if (!string.IsNullOrEmpty(item.Title) && identifier == "setting_settings")
                    {
                        sb.Append("<div class=\"dropdown dropbtn optioncontrol\"><div class=\"togglelabel\" data-toggle=\"dropdown\" data-placement=\"bottom-start\" aria-haspopup=\"true\" data-nodrag=\"\" aria-expanded=\"true\"><label>" + item.Title + "</label><a id=\"dropdownMenuLink\" class=\"dropdownmenu grptitle\" ><em class=\"fas fa-chevron-down\"></em></a></div><div class=\"dropdown-menu subtMenu\" aria-labelledby=\"dropdownMenuLink\">");
                    }

                    if (identifier == "setting_settings")
                    {
                        foreach (dynamic ctl in item.Controls)
                        {
                            if (ctl.Type == "Slider")
                            {
                                Slider slider = JsonConvert.DeserializeObject<Slider>(ctl.ToString());
                                if (slider != null)
                                {
                                    string value = GetGuidValue(themeEditorValues, slider);
                                    sb.Append("<div class=\"field csslider optioncontrol \" id=" + item.Guid + "><label>" + slider.Title + "</label>  <span class=\"input-wrapper\"><input type=\"range\" value=" + value + " guid=" + slider.Guid + " name=" + slider.Title + " value=" + value + " min=" + slider.RangeMin + " max=" + slider.RangeMax + " /><input type=\"number\" guid=" + slider.Guid + " name=" + slider.Title + " value=" + value + " min=" + slider.RangeMin + " max=" + slider.RangeMax + "><span class=\"units\">" + slider.Suffix + "</span></span> " + GetCssMarkup(slider.Guid, slider.CustomCSS, slider.PreviewCSS, slider.LessVariable, item.Sass) + GetPvNotAvailableMarkup(slider.PreviewCSS) + "</div>");
                                }
                            }
                            else if (ctl.Type == "Dropdown")
                            {
                                Dropdown dropdown = JsonConvert.DeserializeObject<Dropdown>(ctl.ToString());
                                if (dropdown != null)
                                {
                                    sb.Append("<div class=\"dropdownselect optioncontrol\" id=" + item.Guid + "><div class=\"dropdownlabel\" ><label >" + dropdown.Title + ":</label></div>");
                                    sb.Append("<div class=\"dropdownOption\"><select  guid=" + dropdown.Guid + ">");
                                    foreach (dynamic opt in dropdown.Options)
                                    {
                                        string Key = string.Empty;
                                        string Value = string.Empty;
                                        foreach (JToken attribute in JToken.Parse(opt.ToString()))
                                        {
                                            JProperty jProperty = attribute.ToObject<JProperty>();
                                            if (jProperty != null)
                                            {
                                                Key = jProperty.Name;
                                                Value = jProperty.Value.ToString();
                                            }
                                        }
                                        string value = GetGuidValue(themeEditorValues, dropdown);
                                        if (Key == value)
                                        {
                                            sb.Append("<option selected=\"selected\" value=" + Key + ">" + Value + "</option>");
                                        }
                                        else
                                        {
                                            sb.Append("<option value=" + Key + ">" + Value + "</option>");
                                        }
                                    }
                                    sb.Append("</select><span class=\"units\">" + dropdown.Suffix + "</span></div>");
                                    //sb.Append("<span class=\"units\">" + dropdown.Suffix + "</span>");
                                    sb.Append(GetCssMarkup(dropdown.Guid, dropdown.CustomCSS, dropdown.PreviewCSS, dropdown.LessVariable, item.Sass) + GetPvNotAvailableMarkup(dropdown.PreviewCSS) + "</div>");
                                }
                            }
                            else if (ctl.Type == "Color Picker")
                            {
                                ColorPicker colorPicker = JsonConvert.DeserializeObject<ColorPicker>(ctl.ToString());
                                if (colorPicker != null)
                                {
                                    sb.Append("<div class=\"field fieldcolor optioncontrol\" id=" + item.Guid + "><label>" + colorPicker.Title + " </label>");
                                    string value = GetGuidValue(themeEditorValues, colorPicker);
                                    sb.Append("<span class=\"input-wrapper\"><input class=\"color\" guid=" + colorPicker.Guid + " type=\"text\" value=" + value + ">");
                                    sb.Append("<span class=\"units\">" + colorPicker.Suffix + "</span>");
                                    sb.Append(GetCssMarkup(colorPicker.Guid, colorPicker.CustomCSS, colorPicker.PreviewCSS, colorPicker.LessVariable, item.Sass) + "</span>" + GetPvNotAvailableMarkup(colorPicker.PreviewCSS) + "</div>");
                                }
                            }
                            else
                            {
                                Fonts fonts = JsonConvert.DeserializeObject<Fonts>(ctl.ToString());
                                if (fonts != null)
                                {
                                    sb.Append("<div class=\"dropdownselect optioncontrol\" id=" + item.Guid + "><div class=\"dropdownlabel\" ><label>" + fonts.Title + ":</label></div>");
                                    sb.Append("<div class=\"dropdownOption\"><select  guid=" + fonts.Guid + ">");
                                    foreach (StringTextNV opt in GetDDLFonts(Guid))
                                    {
                                        string value = GetGuidValue(themeEditorValues, fonts);
                                        if (opt.Value == value)
                                        {
                                            sb.Append("<option selected=\"selected\" value=\"" + opt.Value + "\">" + opt.Name + "</option>");
                                        }
                                        else
                                        {
                                            sb.Append("<option value=\"" + opt.Value + "\">" + opt.Name + "</option>");
                                        }
                                    }
                                    sb.Append("</select><span class=\"units\">" + fonts.Suffix + "</span></div>");
                                    // sb.Append("<span class=\"units\">" + fonts.Suffix + "</span>");
                                    sb.Append(GetCssMarkup(fonts.Guid, fonts.CustomCSS, fonts.PreviewCSS, fonts.LessVariable, item.Sass) + GetPvNotAvailableMarkup(fonts.PreviewCSS) + "</div>");
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(item.Title))
                        {
                            sb.Append("</div></div>");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.Title))
                        {
                            sb.Append("<div class=\"dropdown dropbtn optioncontrol\"><label style=\"font-weight: 600;\">" + item.Title + "</label>" + GetSubCategoryEditClick(item, developerMode, Guid) + "</div>");
                        }
                    }
                }
                sb.Append("</div></div>");
                return sb.ToString();
            }
            private static string GetPvNotAvailableMarkup(string previewCSS)
            {
                if (string.IsNullOrEmpty(previewCSS))
                {
                    return "<span class=\"PvNotAvailable\" title=\"Preview not available. Changes will be reflected after theme is saved.\"><em class=\"far fa-eye-slash\" ></em></span>";
                }
                return string.Empty;
            }
            private static string GetSubCategoryEditClick(ThemeEditor item, bool developerMode, string Guid)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<div class=\"dropdown float-right dropbtn\">");
                sb.Append("<a id=\"dropdownMenuLink\" class=\"dropdownmenu\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"false\"><em class=\"fas fa-ellipsis-v\"></em></a>");
                sb.Append("<div class=\"dropdown-menu\" aria-labelledby=\"dropdownMenuLink\">");
                sb.Append("<a class=\"dropdown-item box-icon\" ng-click=\"OpenPopUp('edit/" + Guid + "/" + item.Guid + "')\"><em class=\"fas fa-cog mr-xs\"></em><span>Settings</span></a>");
                if (developerMode)
                {
                    sb.Append("<hr class=\"small\" />");
                    sb.Append("<a class=\"dropdown-item box-icon\" ng-click=\"DeleteSubCategory('" + item.Category + "','" + item.Title + "')\"><em class=\"fas fa-trash mr-xs\"></em><span>Delete</span></a>");
                }
                sb.Append("</div>");
                sb.Append("</div>");
                return sb.ToString();
            }
            private static string GetCategoryEditClick(IGrouping<string, ThemeEditor> themeEditorGroup, bool developerMode, string Guid)
            {
                StringBuilder sb = new StringBuilder();

                if (developerMode)
                {
                    sb.Append("<div class=\"dropdown float-right dropbtn\">");
                    sb.Append("<a id=\"dropdownMenuLink\" class=\"dropdownmenu\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"false\"><em class=\"fas fa-ellipsis-v\"></em></a>");
                    sb.Append("<div class=\"dropdown-menu\" aria-labelledby=\"dropdownMenuLink\">");
                    if (themeEditorGroup.Where(g => string.IsNullOrEmpty(g.Title) == true && g.Controls.Count == 0).FirstOrDefault() != null)
                    {
                        sb.Append("<a class=\"dropdown-item box-icon\" ng-click=\"OpenPopUp('edit/" + Guid + "/" + themeEditorGroup.Key + "/new')\"><em class=\"fas fa-cog mr-xs\"></em><span>Settings</span></a>");
                    }
                    else if (themeEditorGroup.Where(g => string.IsNullOrEmpty(g.Title) == true && g.Controls.Count > 0).FirstOrDefault() != null)
                    {
                        sb.Append("<a class=\"dropdown-item box-icon\" ng-click=\"OpenPopUp('edit/" + Guid + "/" + themeEditorGroup.Where(g => string.IsNullOrEmpty(g.Title) == true && g.Controls.Count > 0).FirstOrDefault().Guid + "')\"><em class=\"fas fa-cog mr-xs\"></em><span>Settings</span></a>");
                    }
                    else
                    {
                        sb.Append("<a class=\"dropdown-item box-icon\" ng-click=\"OpenPopUp('edit/" + Guid + "/" + themeEditorGroup.Key + "/new')\"><em class=\"fas fa-cog mr-xs\"></em><span>Settings</span></a>");
                    }

                    sb.Append("<a class=\"dropdown-item box-icon\" ng-click=\"OpenPopUp('edit/" + Guid + "/" + themeEditorGroup.Key + "/newsub')\"><em class=\"fas fa-plus mr-xs\"></em><span>Add Subcategory</span></a>");
                    sb.Append("<hr class=\"small\" />");
                    sb.Append("<a class=\"dropdown-item box-icon\" ng-click=\"Delete('" + themeEditorGroup.Key + "')\"><em class=\"fas fa-trash mr-xs\"></em><span>Delete</span></a>");
                    sb.Append("</div>");
                    sb.Append("</div>");
                }
                else if (!developerMode && themeEditorGroup.Where(g => string.IsNullOrEmpty(g.Title) == true && g.Controls.Count > 0).FirstOrDefault() != null)
                {
                    sb.Append("<div class=\"dropdown float-right dropbtn\">");
                    sb.Append("<a id=\"dropdownMenuLink\" class=\"dropdownmenu\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"false\"><em class=\"fas fa-ellipsis-v\"></em></a>");
                    sb.Append("<div class=\"dropdown-menu\" aria-labelledby=\"dropdownMenuLink\">");
                    sb.Append("<a class=\"dropdown-item box-icon\" ng-click=\"OpenPopUp('edit/" + Guid + "/" + themeEditorGroup.Where(g => string.IsNullOrEmpty(g.Title) == true && g.Controls.Count > 0).FirstOrDefault().Guid + "')\"><em class=\"fas fa-cog mr-xs\"></em><span>Settings</span></a>");
                    sb.Append("</div>");
                    sb.Append("</div>");
                }
                return sb.ToString();
            }
            private static string GetGuidValue(List<ThemeEditorValue> themeEditorValues, ThemeEditorControl control)
            {
                string result = control.DefaultValue;
                if (themeEditorValues != null && themeEditorValues.Where(t => t.Guid.ToLower() == control.Guid.ToLower()).FirstOrDefault() != null)
                {
                    result = themeEditorValues.Where(t => t.Guid.ToLower() == control.Guid.ToLower()).FirstOrDefault().Value;
                }
                return result;
            }
            private static string GetCssMarkup(string Guid, string CustomCSS, string PreviewCSS, string LessVariable, string Sass)
            {
                return "<input type=\"hidden\" id=" + Guid + " value=\"" + LessVariable + "\" css=\"" + CustomCSS + "\" prevcss=\"" + PreviewCSS + "\" sass=\"" + Sass + "\">";
            }
            private static string GetThemeEditorJsonPath(int PortalID, string CategoryGuid)
            {
                IThemeEditor themeEditor = GetCategories().Where(c => c.Guid.ToLower() == CategoryGuid.ToLower()).FirstOrDefault();
                if (themeEditor != null)
                {
                    string path = themeEditor.JsonPath.Replace("{{PortalID}}", PortalID.ToString()).Replace("{{ThemeName}}", Core.Managers.ThemeManager.GetCurrentThemeName(PortalID));
                    string folder = Path.GetDirectoryName(path);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    return path;
                }
                return string.Empty;
            }
            private static string GetThemeEditorValueJsonPath(int PortalId, string CategoryGuid)
            {
                string FolderPath = HttpContext.Current.Server.MapPath("~/Portals/" + PortalId + "/vThemes/" + Core.Managers.ThemeManager.GetCurrentThemeName(PortalId) + "/editor/" + CategoryGuid);

                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                if (!File.Exists(FolderPath + "\\theme.json"))
                {
                    File.Create(FolderPath + "\\theme.json").Dispose();
                }

                return FolderPath + "\\theme.json";
            }
        }
    }
}