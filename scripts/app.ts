/// <reference path="axios/axios.d.ts" />
/// <reference path="knockout/knockout.d.ts" />

import * as axios from 'axios';
import * as ko from "knockout";

class GlobalModel {
    outputGlobal: KnockoutObservable<string>

    constructor(input: KnockoutObservable<string>) {
        this.outputGlobal = ko.pureComputed(() => {
            return input();
        }, this);
    }
}

class SubstitutionModel {
    letterToNumberActive: KnockoutObservable<string>
    rotate13Active: KnockoutObservable<string>
    rotateValue: KnockoutObservable<string>
    aciiActive: KnockoutObservable<string>

    delimiter: KnockoutObservable<string>

    output: KnockoutObservable<string>

    constructor(input: KnockoutObservable<string>) {
        this.letterToNumberActive = ko.observable("active");
        this.rotate13Active = ko.observable("active");
        this.rotateValue = ko.observable("13");
        this.aciiActive = ko.observable("active");
        this.delimiter = ko.observable(" ");

        this.output = ko.pureComputed(() => {
            let outputString = "";
            let inputText = input();
            let parts = inputText.split(this.delimiter());
            if (this.letterToNumberActive()) {
                outputString += SubstitutionModel.letterToNumber(parts);
            }

            if (this.rotate13Active()) {
                outputString += SubstitutionModel.rot13(parts, this.rotateValue());
            }

            if (this.aciiActive()) {
                outputString += SubstitutionModel.numberToAscii(parts);
            }

            return outputString;
        }, this);
    }

    static letterToNumber(parts: string[]): string {
        let a1z26String = "";
        for (let i = 0; i < parts.length; i++) {
            let integer = (parseInt(parts[i]) - 1) % 26;
            if (isNaN(integer)) {
                a1z26String += parts[i];
            } else {
                a1z26String += String.fromCharCode(65 + integer);
            }

            a1z26String += " ";
        }

        return a1z26String;
    }

    static rot13(parts: string[], rotateAmount: string): string {
        let rot13String = "";
        for (let i = 0; i < parts.length; i++) {
            if (parts[i].length == 1) {
                let charValue = parts[i].toUpperCase().charCodeAt(0)
                charValue += parseInt(rotateAmount);
                if (charValue > "Z".charCodeAt(0)) {
                    charValue -= 26;
                }

                rot13String += String.fromCharCode(charValue);
            } else {
                rot13String += parts[i];
            }

            rot13String += " ";
        }

        return rot13String;
    }

    static numberToAscii(parts: string[]): string {
        let asciiString = "";
        for (var i = 0; i < parts.length; i++) {
            let integer = parseInt(parts[i]);
            if (isNaN(integer)) {
                asciiString += parts[i];
            } else {
                asciiString += String.fromCharCode(integer);
            }

            asciiString += " ";
        }

        return asciiString;
    }
}

interface WordSearchResult {
    count: number,
    results: string[],
    errorMessage: string
}

class WordSearchModel {
    query: KnockoutObservable<string>

    output: KnockoutObservable<string>
    resultCount: KnockoutObservable<string>
    dbStatus: KnockoutObservable<string>

    outputProcessor: KnockoutObservable<string>

    constructor() {
        this.query = ko.observable("");
        this.resultCount = ko.observable("0");
        this.dbStatus = ko.observable("Idler");
        this.output = ko.observable("");

        this.outputProcessor = ko.computed(() => {
            this.dbStatus("Querying...");

            let encodedQuery = encodeURIComponent(this.query());
            axios.get("/api/WordSearch/FindMatchingWords?search=" + encodedQuery)
                .then((response) => {
                    let data: WordSearchResult = response.data

                    if (data.count < 0) {
                        this.dbStatus("Query error: " + data.errorMessage);
                    } else {
                        this.dbStatus("Idle");
                        this.resultCount(data.count.toString());
                        this.output(data.results.join("\n"));
                    }
                })
                .catch((err) => {
                    this.dbStatus("Error: " + JSON.stringify(err));
                });

            return encodedQuery;
        })
    }
}

interface CrosswordSearchResult {
    count: number,
    clueResults: string[],
    answerResults: string[],
    errorMessage: string
}

class CrosswordSearchModel {
    query: KnockoutObservable<string>

    clueOutput: KnockoutObservable<string>
    answerOutput: KnockoutObservable<string>
    resultCount: KnockoutObservable<string>
    dbStatus: KnockoutObservable<string>

    outputProcessor: KnockoutObservable<string>

    constructor() {
        this.query = ko.observable("");
        this.resultCount = ko.observable("0");
        this.dbStatus = ko.observable("Idler");
        this.clueOutput = ko.observable("");
        this.answerOutput = ko.observable("");

        this.outputProcessor = ko.computed(() => {
            this.dbStatus("Querying...");

            let encodedQuery = encodeURIComponent(this.query());
            axios.get("/api/CrosswordSearch/FindMatchingWords?search=" + encodedQuery)
                .then((response) => {
                    let data: CrosswordSearchResult = response.data

                    if (data.count < 0) {
                        this.dbStatus("Query error: " + data.errorMessage);
                    } else {
                        this.dbStatus("Idle");
                        this.resultCount(data.count.toString());
                        this.clueOutput(data.clueResults.join("\n"));
                        this.answerOutput(data.answerResults.join("\n"));
                    }
                })
                .catch((err) => {
                    this.dbStatus("Error: " + JSON.stringify(err));
                });

            return encodedQuery;
        })
    }
}

class UtilityModel {
    toggler: (item: KnockoutObservable<string>) => void

    constructor() {
        this.toggler = (item: KnockoutObservable<string>) => {
            if (item() === "") {
                item("active");
            } else {
                item("");
            }
        }
    }
}

class MainModel {
    input: KnockoutObservable<string>

    global: GlobalModel
    utility: UtilityModel
    subst: SubstitutionModel
    wordSearch: WordSearchModel
    crosswordSearch: CrosswordSearchModel

    constructor() {
        this.input = ko.observable("");

        this.global = new GlobalModel(this.input);
        this.utility = new UtilityModel();
        this.subst = new SubstitutionModel(this.input);
        this.wordSearch = new WordSearchModel();
        this.crosswordSearch = new CrosswordSearchModel();
    }
}

ko.applyBindings(new MainModel());