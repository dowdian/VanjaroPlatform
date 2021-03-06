app.service('LocalizationServices', function ($compile, $routeParams) {
    var LocalizationSvc = this;
    LocalizationSvc.BindLocalization = function ($scope, Object, Languages) {
        if ($scope != null && Object != null) {
            if (!("LocalizedObjects" in $scope))
                $scope.LocalizedObjects = new Array();

            LocalizationSvc.SetLocalizedElements($scope, Object, Languages);
            if (Object.LocaleProperties != null && Object.LocaleProperties.length) {
                $.each(Object.LocaleProperties, function (index, value) {
                    if ($scope[Object.Entity + '_' + value.Name] != null && $scope[Object.Entity + '_' + value.Name] != undefined) {
                        $scope[Object.Entity + '_' + value.Name][value.Language] = value; // updating locale propertise value into element model 
                    }
                });
            }

            if (Object.URLs != null && typeof Object.URLs != 'undefined') {
                $.each(Object.URLs, function (index, value) {
                    if (value.Language == null)
                        value.Language = $scope.ui.data.IsDefaultLocale.Value;
                    if ($scope[Object.Entity + "_" + "Slug"][value.Language] != null) {
                        $scope[Object.Entity + "_" + "Slug"][value.Language].LocalizationID = value.URLID; // updating locale propertise value into element model object
                        $scope[Object.Entity + "_" + "Slug"][value.Language].Language = value.Language;
                        $scope[Object.Entity + "_" + "Slug"][value.Language].Value = value.Slug;
                        $scope[Object.Entity + "_" + "Slug"][value.Language].HasChanged = true;
                    }
                });
            }
        }
    };
    LocalizationSvc.GetLocalizedValues = function ($scope, Object) {
        $scope.Localization = { Properties: [], Slug: [] };
        if ($scope.ui.data != null && $scope.LocalizedObjects != null) {
            $.each($scope.LocalizedObjects, function (index, lObject) { //Loop on localized  elements using Localized Objects By Name
                if (lObject.indexOf(Object.Entity + '_') != -1) {
                    $.each($scope[lObject], function (i, obj) { //Creating or updating EntityLocalization Object and push into Localized properties
                        if (obj.Name == "Slug") {
                            $scope.Localization.Slug.push({ EntityID: obj.LocalizationID, Entity: obj.EntityName, Language: obj.Language, Slug: obj.Value, HasChanged: true, EntityID: obj.EntityID });
                        }
                        else
                            $scope.Localization.Properties.push({ LocalizationID: obj.LocalizationID, Language: obj.Language, EntityName: obj.EntityName, Name: obj.Name, Value: obj.Value, HasChanged: true, EntityID: obj.EntityID });
                    });
                }
            });
        }
    };
    LocalizationSvc.SetLocalizedElements = function ($scope, Object, Languages) {
        $.each($("*[localized]"), function (index, value) { //Creating Scope of localization  
            arrName = $(value).attr('localized');
            $scope[Object.Entity + '_' + arrName] = [];
            if ($scope.LocalizedObjects.indexOf(Object.Entity + '_' + arrName) == -1)
                $scope.LocalizedObjects.push(Object.Entity + '_' + arrName);
        });

        $.each($scope.LocalizedObjects, function (index, value) {
            var data = {};
            if (value.indexOf(Object.Entity + '_') != -1) {
                $.each(Languages, function (i, locale) {
                    var entityvalue = "";
                    if (locale.Value == $scope.ui.data.IsDefaultLocale.Value)
                        entityvalue = (Object[value.substring(Object.Entity.length + 1)] == null) ? '' : Object[value.substring(Object.Entity.length + 1)];
                    var d = ({ LocalizationID: 0, Language: locale.Value, EntityName: Object.Entity, Name: value.substring(Object.Entity.length + 1), Value: entityvalue });
                    data[locale.Value] = d;//Ex: $scope["Name"] Array                                  
                });
                $scope[value] = data;
            }
        });
    };
});

app.controller('setting_detail', function ($scope, $routeParams, CommonSvc, SweetAlert, FileUploader, LocalizationServices, $http) {
    $scope.pid = $routeParams["pid"] ? $routeParams["pid"] : 0;
    if ($scope.pid == 0) {
        $('.CreateNewPage').css('display', 'block');
    }
    else {
        $('.CreateNewPage').css('display', 'none');
    }
    $scope.DashSign = "—";
    $scope.Layout = null;
    $scope.ShowPage_detailsTab = false;
    $scope.ShowPermissionsTab = false;
    $scope.EnableScheduling = false;
    $scope.ShowSEOTab = false;
    $scope.ShowUrlEdit = false;
    $scope.ShowDeletedModule = false;
    $scope.PageFile = new FileUploader();
    $scope.PageFileDetails = [];
    $scope.Show_Tab = false;
    var common = CommonSvc.getData($scope);
    //Init Scope
    $scope.onInit = function () {
        if ($scope.ui.data.HasTabPermission.Value == 'True') {
            if ($scope.pid > 0) {
                var startdate = null;
                var enddate = null;
                if ($scope.ui.data.PagesTemplate.Options.startDate != null) {
                    startdate = new Date($scope.ui.data.PagesTemplate.Options.startDate);
                    $scope.ui.data.PagesTemplate.Options.startDate = ((startdate.getMonth() + 1) < 10 ? '0' + (startdate.getMonth() + 1) : (startdate.getMonth() + 1)) + "/" + (startdate.getDate() < 10 ? '0' + startdate.getDate() : startdate.getDate()) + "/" + startdate.getFullYear();
                }
                if ($scope.ui.data.PagesTemplate.Options.endDate != null) {
                    enddate = new Date($scope.ui.data.PagesTemplate.Options.endDate);
                    $scope.ui.data.PagesTemplate.Options.endDate = ((enddate.getMonth() + 1) < 10 ? '0' + (enddate.getMonth() + 1) : (enddate.getMonth() + 1)) + "/" + (enddate.getDate() < 10 ? '0' + enddate.getDate() : enddate.getDate()) + "/" + enddate.getFullYear();
                }
                $scope.Click_ShowPageType($scope.PageTypes);
                $scope.EnableScheduling = ($scope.ui.data.PagesTemplate.Options.startDate != null || $scope.ui.data.PagesTemplate.Options.endDate != null);
            }

            if (!$scope.EnableScheduling) {
                $scope.ui.data.PagesTemplate.Options.startDate = null;
                $scope.ui.data.PagesTemplate.Options.endDate = null;
            }
            if ($scope.ui.data.ParentPage.Value != null)
                $scope.ui.data.ParentPage.Value = parseInt($scope.ui.data.ParentPage.Value);
            if ($scope.ui.data.URLType.Value != null)
                $scope.ui.data.URLType.Value = parseInt($scope.ui.data.URLType.Value);
            if ($scope.ui.data.SiteAlias != undefined && $scope.ui.data.SiteAlias.Value != null)
                $scope.ui.data.SiteAlias.Value = parseInt($scope.ui.data.SiteAlias.Value);
            LocalizationServices.BindLocalization($scope, $scope.ui.data.LocalizedPage.Options, $scope.ui.data.Languages.Options);
            BindSitemapPriorityList();
        }
        else {
            window.parent.swal('[LS:DonotHaveEditPermission_Message]');
        }
        $('.date_picker input').datepicker({
            autoclose: true,
            clearBtn: true,
            todayHighlight: true
        });
    };

    $scope.Click_Back = function () {
        if ($scope.Show_Tab) {
            $scope.Show_Tab = false;
        }
    };

    $scope.ExportLayout = function (option) {
        event.preventDefault();
        swal({
            title: "[LS:Confirm]",
            text: "[L:ExportMessage]" + option.Name,
            type: "warning",
            showCancelButton: true,
            confirmButtonColor: "#8CD4F5", confirmButtonText: "[LS:Yes]",
            cancelButtonText: "[LS:Cancel]",
            closeOnConfirm: true,
            closeOnCancel: true
        },
            function (IsConfirm) {
                if (IsConfirm) {
                    $http({
                        method: 'GET',
                        url: window.location.origin + jQuery.ServicesFramework(-1).getServiceRoot('Pages') + "Pages/ExportLayout?Name=" + option.Name + "&IsSystem=" + option.IsSystem,
                        responseType: 'arraybuffer',
                        headers: {
                            'ModuleId': -1,
                            'TabId': parseInt($.ServicesFramework(-1).getTabId()),
                            'RequestVerificationToken': $.ServicesFramework(-1).getAntiForgeryValue()
                        }
                    }).success(function (data, status, headers) {
                        headers = headers();
                        var filename = headers['x-filename'];
                        var contentType = headers['content-type'];
                        var linkElement = document.createElement('a');
                        try {
                            var blob = new Blob([data], { type: contentType });
                            var url = window.URL.createObjectURL(blob);
                            linkElement.setAttribute('href', url);
                            linkElement.setAttribute("download", filename);
                            var clickEvent = new MouseEvent("click", {
                                "view": window,
                                "bubbles": true,
                                "cancelable": false
                            });
                            linkElement.dispatchEvent(clickEvent);
                        } catch (ex) {
                            alert(ex);
                        }
                    }).error(function (data) {
                        alert(data);
                    });
                }
            });
        return false;
    };

    $scope.Click_ShowTab = function (type) {
        if (type == 'Page_details') {
            $('#Page_details a.nav-link').addClass("active");
            $('#Permissions a.nav-link').removeClass("active");
            $('#Scheduled a.nav-link').removeClass("active");
            $('#Advanced a.nav-link').removeClass("active");
            $('#DeletedModule a.nav-link').removeClass("active");
            $scope.ShowPage_detailsTab = true;
            $scope.ShowPermissionsTab = false;
            $scope.Scheduled = false;
            $scope.ShowSEOTab = false;
            $scope.ShowDeletedModule = false;
        }
        else if (type == 'Permissions') {
            $('#Page_details a.nav-link').removeClass("active");
            $('#Permissions a.nav-link').addClass("active");
            $('#Scheduled a.nav-link').removeClass("active");
            $('#Advanced a.nav-link').removeClass("active");
            $('#DeletedModule a.nav-link').removeClass("active");
            $scope.ShowPage_detailsTab = false;
            $scope.Scheduled = false;
            $scope.ShowPermissionsTab = true;
            $scope.ShowSEOTab = false;
            $scope.ShowDeletedModule = false;
        }
        else if (type == 'Advanced') {
            $('#Page_details a.nav-link').removeClass("active");
            $('#Permissions a.nav-link').removeClass("active");
            $('#Scheduled a.nav-link').removeClass("active");
            $('#Advanced a.nav-link').addClass("active");
            $('#DeletedModule a.nav-link').removeClass("active");
            $scope.ShowPage_detailsTab = false;
            $scope.ShowPermissionsTab = false;
            $scope.Scheduled = false;
            $scope.ShowSEOTab = true;
            $scope.ShowDeletedModule = false;
        }
        else if (type == 'Scheduled') {
            $('#Page_details a.nav-link').removeClass("active");
            $('#Permissions a.nav-link').removeClass("active");
            $('#Advanced a.nav-link').removeClass("active");
            $('#Scheduled a.nav-link').addClass("active");
            $('#DeletedModule a.nav-link').removeClass("active");
            $scope.ShowPage_detailsTab = false;
            $scope.ShowPermissionsTab = false;
            $scope.ShowSEOTab = false;
            $scope.Scheduled = true;
            $scope.ShowDeletedModule = false;
        }
        else if (type == 'DeletedModule') {
            $('#Page_details a.nav-link').removeClass("active");
            $('#Permissions a.nav-link').removeClass("active");
            $('#Advanced a.nav-link').removeClass("active");
            $('#Scheduled a.nav-link').removeClass("active");
            $('#DeletedModule a.nav-link').addClass("active");
            $scope.ShowPage_detailsTab = false;
            $scope.ShowPermissionsTab = false;
            $scope.ShowSEOTab = false;
            $scope.Scheduled = false;
            $scope.ShowDeletedModule = true;
        }
    };

    $scope.Click_ShowPageType = function (type, Layout) {
        if (Layout != undefined)
            $scope.Layout = Layout;
        $scope.Click_ShowTab('Page_details');
        $scope.Show_Tab = true;
        if (type == 'Standard') {
            $('#Standard').addClass("active");
            $('#URL').removeClass("active");
            $('#Folder').removeClass("active");
            $scope.ui.data.PagesTemplate.Options.pageType = type;
            if ($scope.pid > 0)
                $("#defaultModal .modal-title", parent.document).html('[L:EditPages]');
            else {
                $("#defaultModal .modal-title", parent.document).html('[L:AddPages]');
                $scope.ui.data.PagesTemplate.Options.includeInMenu = true;
            }
        }
        else if (type == 'URL') {
            $('#Standard').removeClass("active");
            $('#URL').addClass("active");
            $('#Folder').removeClass("active");
            $scope.ui.data.PagesTemplate.Options.pageType = type;
            if ($scope.pid > 0)
                $("#defaultModal .modal-title", parent.document).html('[L:EditRedirectLink]');
            else {
                $("#defaultModal .modal-title", parent.document).html('[L:AddRedirectLink]');
                $scope.ui.data.PagesTemplate.Options.includeInMenu = true;
            }
        }
        else if (type == 'Folder') {
            $('#Standard').removeClass("active");
            $('#URL').removeClass("active");
            $('#Folder').addClass("active");
            $scope.ui.data.PagesTemplate.Options.pageType = type;
            if ($scope.pid > 0)
                $("#defaultModal .modal-title", parent.document).html('[L:EditFolder]');
            else
                $("#defaultModal .modal-title", parent.document).html('[L:AddFolder]');
            $scope.ui.data.PagesTemplate.Options.includeInMenu = true;
        }
        setTimeout(function () {
            $('input.pagename ').focus();
        }, 200);
    };

    $scope.Click_Save = function (type) {
        if (mnValidationService.DoValidationAndSubmit('', 'setting_detail')) {
            if ($scope.PageFileDetails.length > 0) {
                $scope.ui.data.PagesTemplate.Options.fileFolderPathRedirection = $scope.PageFileDetails[0].ImageUrl;
                $scope.ui.data.PagesTemplate.Options.fileIdRedirection = $scope.PageFileDetails[0].FileId;
                $scope.ui.data.PagesTemplate.Options.fileNameRedirection = $scope.PageFileDetails[0].Name;
            }
            LocalizationServices.GetLocalizedValues($scope, $scope.ui.data.LocalizedPage.Options);
            $scope.ui.data.PagesTemplate.Options.parentId = $scope.ui.data.ParentPage.Value;
            $scope.ui.data.PagesTemplate.Options.SiteMapPriority = $scope.ui.data.SitemapPriority.Value;
            //Remove unselected permission
            var rolePermissions = [];
            var userPermissions = [];
            $($scope.PermissionsRoles).each(function (index, Role) {
                var allowedRoles = [];
                $(Role.Permissions).each(function (index, permission) {
                    if (permission.AllowAccess)
                        allowedRoles.push(permission);
                });
                if (allowedRoles.length > 0) {
                    Role.Permissions = allowedRoles;
                    rolePermissions.push(Role);
                }
            });

            $($scope.PermissionsUsers).each(function (index, User) {
                var allowedRoles = [];
                $(User.Permissions).each(function (index, permission) {
                    if (permission.AllowAccess)
                        allowedRoles.push(permission);
                });
                if (allowedRoles.length > 0) {
                    User.Permissions = allowedRoles;
                    userPermissions.push(User);
                }
            });

            $scope.ui.data.PagesTemplate.Options.permissions.rolePermissions = rolePermissions;
            $scope.ui.data.PagesTemplate.Options.permissions.userPermissions = userPermissions;
            if (!$scope.EnableScheduling) {
                $scope.ui.data.PagesTemplate.Options.startDate = null;
                $scope.ui.data.PagesTemplate.Options.endDate = null;
            }
            var valid = true;
            if ($scope.EnableScheduling && $scope.ui.data.PagesTemplate.Options.startDate != null && $scope.ui.data.PagesTemplate.Options.endDate != null) {
                valid = new Date($scope.ui.data.PagesTemplate.Options.startDate) < new Date($scope.ui.data.PagesTemplate.Options.endDate);
            }
            if (valid) {
                var IsCopy = false;
                if ($scope.ui.data.IsCopy != undefined && $scope.ui.data.IsCopy.Value != null)
                    IsCopy = Boolean($scope.ui.data.IsCopy.Value);
                var formdata = {
                    PageSettings: $scope.ui.data.PagesTemplate.Options,
                    PageLayout: $scope.Layout,
                    LocaleProperties: $scope.Localization.Properties,
                    ReplaceTokens: $scope.ui.data.ReplaceTokens.Value
                }
                var TabID = $scope.ui.data.PagesTemplate.Options.tabId;
                common.webApi.post('pages/savepagedetails', 'DefaultWorkflow=' + $scope.ui.data.ddlWorkFlows.Value + '&MaxRevisions=' + $scope.ui.data.MaxRevisions.Value + '&Copy=' + IsCopy, formdata).success(function (data) {
                    if (data.HasErrors) {
                        window.parent.ShowNotification('[LS:Pages]', data.Message, 'error');
                    }
                    if (data.IsSuccess) {
                        if ($scope.ui.data.PagesTemplate.Options.tabId == parseInt($.ServicesFramework(-1).getTabId())) {
                            window.parent.VJIsContentApproval = data.Data.IsContentApproval ? "True" : "False";
                            window.parent.VJNextStateName = data.Data.NextStateName;
                            window.parent.VJIsPageDraft = data.Data.IsPageDraft;
                            window.parent.VJIsLocked = data.Data.IsLocked ? "True" : "False";
                            window.parent.VJIsModeratorEditPermission = data.Data.IsModeratorEditPermission;

                            if (data.Data.IsPageDraft)
                                $(window.parent.document.body).find('#VJBtnPublish').removeClass('disabled');

                            else
                                $(window.parent.document.body).find('#VJBtnPublish').addClass('disabled');

                            if (!data.Data.IsPageDraft || data.Data.IsLocked)
                                $(window.parent.document.body).find('#VJBtnPublish').addClass('disabled');

                            if (data.Data.IsContentApproval && data.Data.IsLocked)
                                $(window.parent.document.body).find('.gjs-cv-canvas__frames').addClass('lockcanvas');
                            else
                                $(window.parent.document.body).find('.gjs-cv-canvas__frames').removeClass('lockcanvas');

                        }
                        var ParentScope = parent.document.getElementById("iframe").contentWindow.angular;
                        if (ParentScope != null && ParentScope != undefined) {
                            var Menuextension = ParentScope.element(".menuextension");
                            if (ParentScope != undefined && Menuextension != undefined && Menuextension.scope() != undefined && Menuextension.scope().FetchPages != undefined) {
                                Menuextension.scope().FetchPages();
                                Menuextension.scope().$apply();
                                Menuextension.scope().init();
                            }
                        }
                        $scope.RenderMarkup();
                        if (parent.GetParameterByName('m2vsetup', parent.window.location) != null && typeof parent.GetParameterByName('m2vsetup', parent.window.location) != undefined && data.Data.url != null) {
                            parent.window.location.href = data.Data.url + "?migrate=true";
                        }
                        else {
                            if (TabID == 0 || IsCopy) {
                                if (IsCopy) {
                                    window.parent.ShowNotification($scope.ui.data.PagesTemplate.Options.name, '[L:PageCreatedSuccess]', 'success', data.Data.url);
                                }
                                else {
                                    window.parent.ShowNotification($scope.ui.data.PagesTemplate.Options.name, '[L:PageCreatedSuccess]', 'success', data.Data.url);
                                }
                            }
                            else {
                                window.parent.ShowNotification($scope.ui.data.PagesTemplate.Options.name, '[L:PageUpdatedSuccess]', 'success');
                            }
                        }
                        $(window.parent.document.body).find('[data-dismiss="modal"]').click();

                    }
                });
            }
            else {
                window.parent.ShowNotification('[L:InvalidDateTitle]', '[L:InvalidDateText]', 'error');
            }
        }
    };

    $scope.DeleteLayout = function (option) {
        event.preventDefault();
        swal({
            title: "[L:DeleteLayoutTitle]",
            text: "[L:DeleteLayoutText]",
            type: "warning",
            showCancelButton: true,
            confirmButtonColor: "#DD6B55", confirmButtonText: "[L:DeleteButton]",
            cancelButtonText: "[L:CancelButton]",
            closeOnConfirm: true,
            closeOnCancel: true
        },
            function (isConfirm) {
                if (isConfirm) {
                    common.webApi.post('pages/deletelayout', 'name=' + option.Name, '').success(function (response) {
                        if (response.IsSuccess) {
                            $.each($scope.ui.data.PageLayouts.Options, function (k, v) {
                                if (!v.IsSystem && option.Name == v.Name) {
                                    $scope.ui.data.PageLayouts.Options.splice(k, 1);
                                }
                            });
                        }
                    });
                }
            });
        return false;
    };

    $scope.Click_EditUrl = function (row) {
        if ($scope.ui.data.PageUrls.Options.length > 0) {
            $.each($scope.ui.data.PageUrls.Options, function (key, value) {
                if (row.id == value.id) {
                    $scope.ui.data.WorkingPageUrls.Options.tabId = parseInt($scope.pid);
                    $scope.ui.data.WorkingPageUrls.Options.saveUrl =
                    {
                        Id: value.id,
                        IsSystem: value.autoGenerated,
                        LocaleKey: value.locale.Key,
                        Path: value.path,
                        QueryString: value.queryString,
                        SiteAliasKey: value.siteAlias.Key,
                        SiteAliasUsage: value.siteAliasUsage,
                        StatusCodeKey: value.statusCode.Key
                    };
                    return false;
                }
            });
        }
    };
    $scope.Click_ShowAddURL = function () {
        $scope.ui.data.WorkingPageUrls.Options.tabId = parseInt($scope.pid);
        $scope.ui.data.WorkingPageUrls.Options.saveUrl =
        {
            Id: -1,
            IsSystem: false,
            LocaleKey: 1,
            Path: "",
            QueryString: "",
            SiteAliasKey: $scope.ui.data.SiteAlias.Value,
            SiteAliasUsage: 0,
            StatusCodeKey: 200
        };
        return false;
    };

    $scope.Click_UrlAddUpdate = function (type) {
        common.webApi.post('pages/urladdupdate', '', $scope.ui.data.WorkingPageUrls.Options).success(function (response) {
            if (response.HasErrors) {
                window.parent.ShowNotification('[LS:Pages]', response.Message, 'error');
                $scope.ui.data.WorkingPageUrls.Options.saveUrl.Path = response.Data.SuggestedUrlPath;
            }
            if (response.IsSuccess) {
                $scope.ui.data.PageUrls.Options = [];
                $scope.ui.data.PageUrls.Options = response.Data;
                $('#PagesUrldefaultModal').find('[data-dismiss="modal"]').click();
            }
        });
    };

    $scope.Click_DeleteUrl = function (row) {
        event.preventDefault();
        window.parent.swal({
            title: "[L:DeletePageUrlTitle]",
            text: "[L:DeletePageUrlText]",
            type: "warning",
            showCancelButton: true,
            confirmButtonColor: "#DD6B55", confirmButtonText: "[L:DeleteButton]",
            cancelButtonText: "[L:CancelButton]",
            closeOnConfirm: true,
            closeOnCancel: true
        },
            function (isConfirm) {
                if (isConfirm) {
                    var UrlIdDto = {
                        Id: row.id,
                        TabId: $scope.pid
                    };
                    common.webApi.post('pages/deletecustomurl', '', UrlIdDto).success(function (response) {
                        if (response.HasErrors) {
                            window.parent.ShowNotification('[LS:Error]', response.Message, 'error');
                        }
                        if (response.IsSuccess) {
                            $scope.ui.data.PageUrls.Options = [];
                            $scope.ui.data.PageUrls.Options = response.Data;
                            $('#PagesUrldefaultModal').find('[data-dismiss="modal"]').click();
                        }
                    });
                }
            });
        return false;
    };

    $scope.PageFile.onCompleteAll = function () {
        if ($scope.PageFile.progress == 100) {
            var FileIds = [];
            $.each($scope.PageFile.queue, function (key, value) {
                if (value.file.name != "File/s not uploaded successfully!")
                    FileIds.push(parseInt(value.file.name.split('fileid')[1]));
            });
            if (FileIds.length > 0) {
                common.webApi.get('Upload/GetMultipleFileDetails', 'fileids=' + FileIds.join()).success(function (response) {
                    $.each(response, function (key, value) {
                        if (value.Name != null) {
                            var Title = (value.Name.split('/').pop()).split('.')[0];
                        }
                        var data = {
                            "Name": value.Name,
                            "ImageUrl": value.FileUrl,
                            "FileId": value.FileId,
                            "Title": Title,
                            "KBSize": value.Size,
                            "Size": (value.Size / 1024).toFixed(2) + " KB",
                            "SortOrder": key
                        };
                        $scope.PageFileDetails = [];
                        $scope.PageFileDetails.push(data);
                    });
                    $scope.PageFile.queue = [];
                });
            }
        }
    };

    $scope.$watch('PageFile .selectqueue', function (newValue, oldValue) {
        if (newValue != undefined && newValue.length > 0) {
            $.each(newValue, function (key, value) {
                var FileId = parseInt(value.fileid);
                if (FileId > 0) {
                    common.webApi.get('Upload/GetFile', 'fileid=' + FileId).success(function (response) {
                        if (response.Name != null) {
                            var Title = (response.Name.split('/').pop()).split('.')[0];
                        }
                        var data = {
                            "Name": response.Name,
                            "ImageUrl": response.FileUrl,
                            "FileId": FileId,
                            "Title": Title,
                            "KBSize": 0,
                            "Size": (0 / 1024).toFixed(2) + " KB",
                            "SortOrder": key
                        };
                        $scope.PageFileDetails = [];
                        $scope.PageFileDetails.push(data);
                    });
                    $scope.PageFile.selectqueue = [];
                }
            });
        }
    });

    var BindSitemapPriorityList = function () {
        var priorityOptions = [];
        priorityOptions.push({ "value": -1, "label": "[L:SitemapPrioritySelect]" });
        priorityOptions.push({ "value": 0, "label": "0" });
        priorityOptions.push({ "value": 0.1, "label": "0.1" });
        priorityOptions.push({ "value": 0.2, "label": "0.2" });
        priorityOptions.push({ "value": 0.3, "label": "0.3" });
        priorityOptions.push({ "value": 0.4, "label": "0.4" });
        priorityOptions.push({ "value": 0.5, "label": "0.5" });
        priorityOptions.push({ "value": 0.6, "label": "0.6" });
        priorityOptions.push({ "value": 0.7, "label": "0.7" });
        priorityOptions.push({ "value": 0.8, "label": "0.8" });
        priorityOptions.push({ "value": 0.9, "label": "0.9" });
        priorityOptions.push({ "value": 1, "label": "1" });

        $scope.ui.data.SitemapPriority.Options = priorityOptions;
        if ($scope.ui.data.SitemapPriority.Value != null)
            $scope.ui.data.SitemapPriority.Value = parseFloat($scope.ui.data.SitemapPriority.Value);
    };

    $scope.Restore_Module = function (row) {
        window.parent.swal({
            title: "[L:RestoreModuleTitle]",
            text: "[L:RestoreModuleText]",
            type: "warning",
            showCancelButton: true,
            confirmButtonColor: "#DD6B55", confirmButtonText: "[L:ConfirmButton]",
            cancelButtonText: "[L:CancelButton]",
            closeOnConfirm: true,
            closeOnCancel: true
        },
            function (isConfirm) {
                if (isConfirm) {
                    common.webApi.get('pages/restoremodule', 'PageId=' + $scope.pid + '&ModuleID=' + row.TabModuleId).success(function (Response) {
                        window.parent.location.reload();
                    });
                }
            });
    };


    $scope.Remove_All_Module = function () {
        window.parent.swal({
            title: "[L:RemoveModuleTitle]",
            text: "[L:DeleteAllModuleText]",
            type: "warning",
            showCancelButton: true,
            confirmButtonColor: "#DD6B55", confirmButtonText: "[L:DeleteButton]",
            cancelButtonText: "[L:CancelButton]",
            closeOnConfirm: true,
            closeOnCancel: true
        },
            function (isConfirm) {
                if (isConfirm) {
                    common.webApi.post('pages/removeallmodule', 'PageId=' + $scope.pid, '').success(function (Response) {
                        if (Response.IsSuccess) {
                            $scope.ui.data.DeletedModules.Options = [];
                            $scope.ui.data.DeletedModules.Options = Response.Data.DeletedModules;
                            if ($scope.ui.data.DeletedModules.Options.length <= 0)
                                $scope.Click_ShowTab('Page_details');
                        }
                        if (Response.HasErrors) {
                            window.parent.ShowNotification('[L:DeleteAllAppsError]', Response.Message, 'error');
                        }
                    });

                }
            });

        return false;
    };

    $scope.Change_WorkflowStateInfo = function () {

        if ($scope.ui.data.ddlWorkFlows.Value) {
            CommonSvc.SweetAlert.swal({
                title: "[L:WorkflowAreYouSure]",
                text: "[L:WorkflowMessageStateText]",
                type: "warning",
                showCancelButton: true,
                confirmButtonColor: "#DD6B55", confirmButtonText: "[LS:Yes]",
                cancelButtonText: "[LS:No]",
                closeOnConfirm: true,
                closeOnCancel: true
            },
                function (isConfirm) {
                    if (isConfirm) {
                        $.each($scope.ui.data.ddlWorkFlows.Options, function (key, w) {
                            if ($scope.ui.data.ddlWorkFlows.Value == w.Value) {
                                $scope.ui.data.WorkflowStateInfo.Value = w.Content;

                                common.webApi.post('pages/updateworkflow', 'WorkflowID=' + $scope.ui.data.ddlWorkFlows.Value + '&PageID=' + $scope.pid).success(function (Response) {
                                    if (Response.IsSuccess && Response.Data != undefined && Response.Data.Revisions != undefined) {
                                        $scope.ui.data.MaxRevisions.Value = Response.Data.Revisions;
                                        $scope.PermissionsRoles = Response.Data.Permissions.RolePermissions
                                        $scope.PermissionsUsers = Response.Data.Permissions.UserPermissions;

                                    }
                                });
                                return;
                            }
                        });
                    }
                });
        }
    };

    $scope.RenderMarkup = function () {
        if (window.parent.VjEditor != null) {
            $.each(window.parent.getAllComponents().filter(function (component) {
                return (component.attributes.name == "Menu" || component.attributes["custom-name"] != undefined && component.attributes["custom-name"] == "Menu");
            }), function (index, value) {
                window.parent.RenderBlock(value);
            });
        }
    };
});