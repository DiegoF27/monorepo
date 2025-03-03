﻿function getQueryParam(param) {
    return (new URLSearchParams(window.location.search)).get(param);
}

function loginCallback() {
    const callbackValue = $("#tipoTempoLivre").val();
    window.location.href = "/Identity/Account/Login?returnUrl=" +
        encodeURIComponent(window.location.pathname) +
        "?callbackValue=" + callbackValue;
}

function loginCallbackForLive() {
    $("#tipoTempoLivre").val("liveModal");
    loginCallback();
}
function validCalllbackValue(callbackValue) {
    const validCallbacks = ["1:1", "cursos", "requestHelp", "liveModal"];
    return validCallbacks.includes(callbackValue);
}

document.addEventListener('DOMContentLoaded', function () {
    const callbackValue = getQueryParam('callbackValue');
    const isValidCallback = validCalllbackValue(callbackValue);

    if (!isValidCallback
        && callbackValue === ""
        || callbackValue == null
    ) {
        return;
    }
    else if (callbackValue !== "liveModal") {
        $("#timePickerModal").modal("show");
        $("#tipoTempoLivre").val(callbackValue);
    }
    else if (callbackValue === "liveModal") {
        $("#liveModal").modal("show");
    }
});
