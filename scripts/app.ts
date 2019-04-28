/// <reference path="axios/axios.d.ts" />
/// <reference path="knockout/knockout.d.ts" />
/// <reference path="utility.ts" />

import * as axios from 'axios';
import * as ko from "knockout";

class SubstitutionModel {
    input: KnockoutObservable<string>

    letterToNumberActive: KnockoutObservable<string>
    rotate13Active: KnockoutObservable<string>
    rotateValue: KnockoutObservable<string>
    aciiActive: KnockoutObservable<string>
    morseActive: KnockoutObservable<string>

    delimiter: KnockoutObservable<string>

    morseDot: KnockoutObservable<string>
    morseDash: KnockoutObservable<string>

    output: KnockoutObservable<string>

    constructor() {
        this.input = ko.observable("");

        this.letterToNumberActive = ko.observable("active");
        this.rotate13Active = ko.observable("active");
        this.rotateValue = ko.observable("13");
        this.aciiActive = ko.observable("active");
        this.morseActive = ko.observable("");

        this.delimiter = ko.observable(" ");
        this.morseDot = ko.observable(".");
        this.morseDash = ko.observable("-");

        this.output = ko.pureComputed(() => {
            let outputString = "";
            let inputText = this.input();
            let parts = inputText.split(this.delimiter());
            if (this.letterToNumberActive()) {
                outputString += SubstitutionModel.letterToNumber(parts) + "\r\n";
            }

            if (this.rotate13Active()) {
                outputString += SubstitutionModel.rot13(parts, this.rotateValue()) + "\r\n";
            }

            if (this.aciiActive()) {
                outputString += SubstitutionModel.numberToAscii(parts) + "\r\n";
            }

            if (this.morseActive()) {
                outputString += SubstitutionModel.convertMorse(parts, this.morseDot(), this.morseDash()) + "\r\n";
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

    static convertMorse(parts: string[], dotChar: string, dashChar: string): string {
        let morseString = "";
        for (var i = 0; i < parts.length; i++) {
            let letter = SubstitutionModel.parseMorseCharacter(parts[i], dotChar, dashChar);
            morseString += letter;
        }

        return morseString;
    }

    // https://stackoverflow.com/questions/3446170/escape-string-for-use-in-javascript-regex
    static escapeRegExp(input: string) {
        return input.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); // $& means the whole matched string
    }

    static parseMorseCharacter(character: string, dotChar: string, dashChar: string): string {
        // From https://en.wikipedia.org/wiki/Morse_code#/media/File:International_Morse_Code.svg
        
        let dotCharRegex = new RegExp(SubstitutionModel.escapeRegExp(dotChar), "g");
        let dashCharRegex = new RegExp(SubstitutionModel.escapeRegExp(dashChar), "g");
        let normalizedCharacter = character.replace(dotCharRegex, ".").replace(dashCharRegex, "-");
        switch (normalizedCharacter) {
            case ".-": return "A";
            case "-...": return "B";
            case "-.-.": return "C";
            case "-..": return "D";
            case ".": return "E";
            case "..-.": return "F";
            case "--.": return "G";
            case "....": return "H";
            case "..": return "I";
            case ".---": return "J";
            case "-.-": return "K";
            case ".-..": return "L";
            case "--": return "M";
            case "-.": return "N";
            case "---": return "O";
            case ".--.": return "P";
            case "--.-": return "Q";
            case ".-.": return "R";
            case "...": return "S";
            case "-": return "T";
            case "..-": return "U";
            case "...-": return "V";
            case ".--": return "W";
            case "-..-": return "X";
            case "-.--": return "Y";
            case "--..": return "Z";
            case ".----": return "1";
            case "..---": return "2";
            case "...--": return "3";
            case "....-": return "4";
            case ".....": return "5";
            case "-....": return "6";
            case "--...": return "7";
            case "---..": return "8";
            case "----.": return "9";
            case "-----": return "0";
            default: return "?";
        }
    }
}

interface WordSearchResult {
    count: number,
    results: string[],
    errorMessage: string
}

class WordSearchModel {
    query: KnockoutObservable<string>
    searchType: KnockoutObservable<boolean>

    output: KnockoutObservable<string>
    resultCount: KnockoutObservable<string>
    dbStatus: KnockoutObservable<string>

    outputProcessor: KnockoutObservable<string>

    constructor() {
        this.query = ko.observable("");
        this.searchType = ko.observable(true);
        this.resultCount = ko.observable("0");
        this.dbStatus = ko.observable("Idler");
        this.output = ko.observable("");

        this.outputProcessor = ko.computed(() => {
            this.dbStatus("Querying...");
            let encodedQuery = encodeURIComponent(this.query());
            if (this.searchType()) { // == search
                axios.get("/api/WordSearch/FindMatchingWords?search=" + encodedQuery)
                    .then((response) => {
                        let data: WordSearchResult = response.data;
                        this.ApplySearchResults(data);
                    })
                    .catch((err) => {
                        this.dbStatus("Error: " + JSON.stringify(err));
                    });
            } else {
                axios.get("/api/WordSearch/FindAnagrams?search=" + encodedQuery)
                    .then((response) => {
                        let data: WordSearchResult = response.data;
                        this.ApplySearchResults(data);
                    })
                    .catch((err) => {
                        this.dbStatus("Error: " + JSON.stringify(err));
                    });
            }

            return encodedQuery;
        })
    }

    ApplySearchResults(data: WordSearchResult) {
        if (data.count < 0) {
            this.dbStatus("Query error: " + data.errorMessage);
        } else {
            this.dbStatus("Idle");

            this.resultCount(this.GetResultCountText(data.count));
            this.output(data.results.join("\n"));
        }
    }

    GetResultCountText(resultCount: number): string {
        if (resultCount >= 200) {
            return resultCount.toString() + " (limited!)";
        }

        return resultCount.toString();
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
                        this.resultCount(this.GetResultCountText(data.count));
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

    GetResultCountText(resultCount: number): string {
        if (resultCount >= 400) {
            return resultCount.toString() + " (limited!)";
        }

        return resultCount.toString();
    }
}

class WordExtraModel {
    query: KnockoutObservable<string>
    searchType: KnockoutObservable<string>

    output: KnockoutObservable<string>
    resultCount: KnockoutObservable<string>
    dbStatus: KnockoutObservable<string>

    outputProcessor: KnockoutObservable<string>

    constructor() {
        this.query = ko.observable("");
        this.searchType = ko.observable("thesaurus");
        this.resultCount = ko.observable("0");
        this.dbStatus = ko.observable("Idler");
        this.output = ko.observable("");

        this.outputProcessor = ko.computed(() => {
            this.dbStatus("Querying...");
            let encodedQuery = encodeURIComponent(this.query());
            if (this.searchType() === "thesaurus") {
                axios.get("/api/WordExtra/FindSynonyms?search=" + encodedQuery)
                    .then((response) => {
                        let data: WordSearchResult = response.data;
                        this.ApplySearchResults(data, 10);
                    })
                    .catch((err) => {
                        this.dbStatus("Error: " + JSON.stringify(err));
                    });
            } else { // Homophones
                axios.get("/api/WordExtra/FindHomophones?search=" + encodedQuery)
                    .then((response) => {
                        let data: WordSearchResult = response.data;
                        this.ApplySearchResults(data, 50);
                    })
                    .catch((err) => {
                        this.dbStatus("Error: " + JSON.stringify(err));
                    });
            }

            return encodedQuery;
        })
    }

    ApplySearchResults(data: WordSearchResult, throttleLimit: number) {
        if (data.count < 0) {
            this.dbStatus("Query error: " + data.errorMessage);
        } else {
            this.dbStatus("Idle");

            this.resultCount(this.GetResultCountText(data.count, throttleLimit));
            this.output(data.results.join("\n"));
        }
    }

    GetResultCountText(resultCount: number, throttleLimit: number): string {
        if (resultCount >= throttleLimit) {
            return resultCount.toString() + " (limited!)";
        }
    
        return resultCount.toString();
    }
}

class EquationSolverModel {
    input: KnockoutObservable<string>

    delimiter: KnockoutObservable<string>
    inputBase: KnockoutObservable<string>

    output: KnockoutObservable<string>

    constructor() {
        this.input = ko.observable("");
        this.delimiter = ko.observable(" ");
        this.inputBase = ko.observable("10");

        this.output = ko.pureComputed(() => {
            let outputString = "";

            let base = parseInt(this.inputBase());
            let delimiter = this.delimiter();
            let parts = this.input().split(delimiter);
            for (var j = 2; j <= 36; j++) {
                // Header
                let convertedNumbers = "(" + j + ") ";
                if (j < 10) {
                    convertedNumbers += " ";
                }

                // Convert values (read as base, output to base 'j')
                for (var i = 0; i < parts.length; i++) {
                    let number = parseInt(parts[i], base);
                    convertedNumbers += (number.toString(j) + delimiter);
                }

                outputString += convertedNumbers + "\r\n";
            }

            return outputString;
        });
    }
}

class MainModel {
    utility: UtilityModel
    subst: SubstitutionModel
    wordSearch: WordSearchModel
    crosswordSearch: CrosswordSearchModel
    wordExtra: WordExtraModel
    equationSolver: EquationSolverModel

    constructor() {
        this.utility = new UtilityModel();
        this.subst = new SubstitutionModel();
        this.wordSearch = new WordSearchModel();
        this.crosswordSearch = new CrosswordSearchModel();
        this.wordExtra = new WordExtraModel();
        this.equationSolver = new EquationSolverModel();
    }
}

ko.applyBindings(new MainModel());