function showServerPopup() {
    var serverPopup = document.getElementById("serverPopup");
    serverPopup.style.display = "block";
}

function hideServerPopup() {
    var serverPopup = document.getElementById("serverPopup");
    serverPopup.style.display = "none";
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function() {
        console.log('Async: Copying to clipboard was successful!');
    }, function(err) {
        console.error('Async: Could not copy text: ', err);
    });
}
