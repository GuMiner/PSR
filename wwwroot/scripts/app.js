/// <reference path="axios/axios.d.ts" />
/// <reference path="knockout/knockout.d.ts" />
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (Object.hasOwnProperty.call(mod, k)) result[k] = mod[k];
    result["default"] = mod;
    return result;
};
define(["require", "exports", "axios", "knockout"], function (require, exports, axios, ko) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    axios = __importStar(axios);
    ko = __importStar(ko);
    var GlobalModel = /** @class */ (function () {
        function GlobalModel(input) {
            this.outputGlobal = ko.pureComputed(function () {
                return input();
            }, this);
        }
        return GlobalModel;
    }());
    var SubstitutionModel = /** @class */ (function () {
        function SubstitutionModel(input) {
            var _this = this;
            this.letterToNumberActive = ko.observable("active");
            this.rotate13Active = ko.observable("active");
            this.rotateValue = ko.observable("13");
            this.aciiActive = ko.observable("active");
            this.delimiter = ko.observable(" ");
            this.output = ko.pureComputed(function () {
                var outputString = "";
                var inputText = input();
                var parts = inputText.split(_this.delimiter());
                if (_this.letterToNumberActive()) {
                    outputString += SubstitutionModel.letterToNumber(parts);
                }
                if (_this.rotate13Active()) {
                    outputString += SubstitutionModel.rot13(parts, _this.rotateValue());
                }
                if (_this.aciiActive()) {
                    outputString += SubstitutionModel.numberToAscii(parts);
                }
                return outputString;
            }, this);
        }
        SubstitutionModel.letterToNumber = function (parts) {
            var a1z26String = "";
            for (var i = 0; i < parts.length; i++) {
                var integer = (parseInt(parts[i]) - 1) % 26;
                if (isNaN(integer)) {
                    a1z26String += parts[i];
                }
                else {
                    a1z26String += String.fromCharCode(65 + integer);
                }
                a1z26String += " ";
            }
            return a1z26String;
        };
        SubstitutionModel.rot13 = function (parts, rotateAmount) {
            var rot13String = "";
            for (var i = 0; i < parts.length; i++) {
                if (parts[i].length == 1) {
                    var charValue = parts[i].toUpperCase().charCodeAt(0);
                    charValue += parseInt(rotateAmount);
                    if (charValue > "Z".charCodeAt(0)) {
                        charValue -= 26;
                    }
                    rot13String += String.fromCharCode(charValue);
                }
                else {
                    rot13String += parts[i];
                }
                rot13String += " ";
            }
            return rot13String;
        };
        SubstitutionModel.numberToAscii = function (parts) {
            var asciiString = "";
            for (var i = 0; i < parts.length; i++) {
                var integer = parseInt(parts[i]);
                if (isNaN(integer)) {
                    asciiString += parts[i];
                }
                else {
                    asciiString += String.fromCharCode(integer);
                }
                asciiString += " ";
            }
            return asciiString;
        };
        return SubstitutionModel;
    }());
    var WordSearchModel = /** @class */ (function () {
        function WordSearchModel() {
            var _this = this;
            this.query = ko.observable("");
            this.resultCount = ko.observable("0");
            this.dbStatus = ko.observable("Idler");
            this.output = ko.observable("");
            this.outputProcessor = ko.computed(function () {
                _this.dbStatus("Querying...");
                var encodedQuery = encodeURIComponent(_this.query());
                axios.get("/api/WordSearch/FindMatchingWords?search=" + encodedQuery)
                    .then(function (response) {
                    var data = response.data;
                    if (data.count < 0) {
                        _this.dbStatus("Query error: " + data.errorMessage);
                    }
                    else {
                        _this.dbStatus("Idle");
                        _this.resultCount(data.count.toString());
                        _this.output(data.results.join("\n"));
                    }
                })
                    .catch(function (err) {
                    _this.dbStatus("Error: " + JSON.stringify(err));
                });
                return encodedQuery;
            });
        }
        return WordSearchModel;
    }());
    var UtilityModel = /** @class */ (function () {
        function UtilityModel() {
            this.toggler = function (item) {
                if (item() === "") {
                    item("active");
                }
                else {
                    item("");
                }
            };
        }
        return UtilityModel;
    }());
    var MainModel = /** @class */ (function () {
        function MainModel() {
            this.input = ko.observable("");
            this.global = new GlobalModel(this.input);
            this.utility = new UtilityModel();
            this.subst = new SubstitutionModel(this.input);
            this.wordSearch = new WordSearchModel();
        }
        return MainModel;
    }());
    ko.applyBindings(new MainModel());
});
//# sourceMappingURL=app.js.map