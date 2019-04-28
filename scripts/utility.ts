/// <reference path="knockout/knockout.d.ts" />

class UtilityModel {
    toggler: (item: KnockoutObservable<string>) => void

    constructor() {
        this.toggler = (item: KnockoutObservable<string>) => {
            if (item() !== "active") {
                item("active");
            } else {
                item("");
            }
        }
    }
}