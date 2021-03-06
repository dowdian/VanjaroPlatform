'use strict';

(function (angular, factory) {
    if (typeof define === 'function' && define.amd) {
        define(['angular', 'ckeditor'], function (angular) {
            return factory(angular);
        });
    } else {
        return factory(angular);
    }
}(angular || null, function (angular) {
    var app = angular.module('ngCkeditor', []);
    var $defer, loaded = false;

    app.run(['$q', '$timeout', function ($q, $timeout) {
        $defer = $q.defer();

        if (angular.isUndefined(CKEDITOR)) {
            throw new Error('CKEDITOR not found');
        }
        CKEDITOR.disableAutoInline = true;
        function checkLoaded() {
            if (CKEDITOR.status === 'loaded') {
                loaded = true;
                $defer.resolve();
            } else {
                checkLoaded();
            }
        }

        CKEDITOR.on('loaded', checkLoaded);
        $timeout(checkLoaded, 100);
    }]);

    app.directive('ckeditor', ['$timeout', '$q', function ($timeout, $q) {

        return {
            restrict: 'AC',
            require: ['ngModel', '^?form'],
            scope: false,
            link: function (scope, element, attrs, ctrls) {
                var ngModel = ctrls[0];
                var form = ctrls[1] || null;
                var EMPTY_HTML = '<p></p>',
                    isTextarea = element[0].tagName.toLowerCase() === 'textarea',
                    data = [],
                    isReady = false;

                if (!isTextarea) {
                    element.attr('contenteditable', true);
                }

                var onLoad = function () {

                    var options = scope[attrs.ckeditor];

                    var instance = (isTextarea) ? CKEDITOR.replace(element[0], options) : CKEDITOR.inline(element[0], options),
                        configLoaderDef = $q.defer();

                    element.bind('$destroy', function () {
                        if (instance && CKEDITOR.instances[instance.name]) {
                            CKEDITOR.instances[instance.name].destroy();
                        }
                    });
                    var setModelData = function (setPristine) {
                        var data = instance.getData();
                        if (data === '') {
                            data = null;
                        }
                        $timeout(function () { // for key up event
                            if (setPristine.name == "key")
                                data = setPristine.editor.getData();
                            if (setPristine !== true || data !== ngModel.$viewValue) {
                                ngModel.$setViewValue(data);
                            }

                            if (setPristine === true && form) {
                                form.$setPristine();
                            }
                        }, 0);
                    }, onUpdateModelData = function (setPristine) {
                        if (!data.length) {
                            return;
                        }

                        var item = data.pop() || EMPTY_HTML;
                        isReady = false;
                        instance.setData(item, function () {
                            setModelData(setPristine);
                            isReady = true;
                        });
                    };

                    //instance.on('pasteState',   setModelData);
                    instance.on('change', setModelData);
                    instance.on('blur', setModelData);
                    instance.on('key', setModelData); // for source view
                    for (var ckinstances in CKEDITOR.instances) {
                        //var ckeditorinstance = CKEDITOR.instances[i];
                        CKEDITOR.instances[ckinstances].on('instanceReady', function () {
                            scope.$broadcast('ckeditor.ready');
                            scope.$apply(function () {
                                onUpdateModelData(true);
                            });

                            instance.document.on('keyup', setModelData);
                            $.each($('.cke_button__maximize'), function (key, value) { $('#' + $($('.cke_button__maximize')[key]).attr('id')).click(function (e) { $($($('.cke_button__maximize')[key]).closest('uiengine')).attr('style', 'display: block !important; opacity: 1;'); }) });
                        });
                    }
                    //instance.on('instanceReady', function () {
                    //    alert('instanceReady');
                    //    scope.$broadcast('ckeditor.ready');
                    //    scope.$apply(function () {
                    //        onUpdateModelData(true);
                    //    });

                    //    instance.document.on('keyup', setModelData);
                    //});
                    instance.on('customConfigLoaded', function () {
                        configLoaderDef.resolve();
                    });

                    ngModel.$render = function () {
                        data.push(ngModel.$viewValue);
                        onUpdateModelData();
                    };
                };

                if (CKEDITOR.status === 'loaded') {
                    loaded = true;
                }
                if (loaded) {
                    $timeout(function () { onLoad() }, 100);
                } else {
                    $defer.promise.then(onLoad);
                }
            }
        };
    }]);

    return app;
}));