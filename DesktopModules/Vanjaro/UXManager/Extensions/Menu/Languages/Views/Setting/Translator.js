app.controller('setting_translator', function ($scope, $attrs, $routeParams, $http, CommonSvc, SweetAlert) {
    var common = CommonSvc.getData($scope);
    $scope.onInit = function () {
        $scope.RoleGroup = parseInt($scope.ui.data.RoleGroups.Value);
        $scope.Change_RoleGroup();
    };
    
    $scope.Click_Update = function () {
        common.webApi.post('Translator/UpdateRoles', 'lid=' + $scope.ui.data.LanguageID.Options, $scope.ui.data.SelectedRoles.Options).success(function (Response) {
            if (Response.IsSuccess) {
                $scope.Click_Cancel();
            }
            else if (Response.HasErrors)
                CommonSvc.SweetAlert.swal(Response.Message);
        });
    };
    $scope.Click_Selected = function (row) {
        if (row.Selected) {
            var role = $scope.ui.data.SelectedRoles.Options.filter(function (r) {
                return r === row.RoleName;
            });
            if (role.length === 0) {
                $scope.ui.data.SelectedRoles.Options.push(row.RoleName);
            }
        } else {
            $scope.ui.data.SelectedRoles.Options = $scope.ui.data.SelectedRoles.Options.filter(function (r) {
                return r !== row.RoleName;
            });
        }
    };

    $scope.Change_RoleGroup = function () {
        common.webApi.get('Translator/GetRoles', 'groupid=' + $scope.RoleGroup + '&lid=' + $scope.ui.data.LanguageID.Options).success(function (Response) {
            if (Response.IsSuccess) {
                $scope.ui.data.Roles.Options = Response.Data;
            }
            else if (Response.HasErrors)
                CommonSvc.SweetAlert.swal(Response.Message);
        });
    };
    $scope.Click_Cancel = function () {
        $(window.parent.document.body).find('[data-dismiss="modal"]').click();
    };
});