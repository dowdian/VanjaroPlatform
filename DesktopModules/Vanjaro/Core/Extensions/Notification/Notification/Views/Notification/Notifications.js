app.controller('notification_notifications', function ($scope, $attrs, $http, CommonSvc, SweetAlert) {
    var common = CommonSvc.getData($scope);

    $scope.onInit = function () {
        $('.Texttab').removeClass('active');
        $('.Messagetab').addClass('active');
    };

    $scope.Pipe_NotificationsPages = function (tableState) {
        $scope.NotificationsPagestableState = tableState;

        common.webApi.get('Notification/GetNotificationList', 'Page=' + tableState.pagination.start + '&PageSize=' + tableState.pagination.number).success(function (data) {
            if (data) {
                tableState.pagination.numberOfPages = data.TotalNotifications;
                $scope.Notifications = [];
                $scope.Notifications = data.Notifications;
            }
        });
    };

});