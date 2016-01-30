define(['browser'], function (browser) {

    function getAnimationPerformance() {

        if (browser.mobile) {
            return 1;
        }

        return 5;
    }

    return {
        getAnimationPerformance: getAnimationPerformance
    };
});