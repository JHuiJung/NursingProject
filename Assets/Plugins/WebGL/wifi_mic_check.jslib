mergeInto(LibraryManager.library, {
    CheckMicPermission: function () {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            window.unityInstance.SendMessage('WifiMicChecker', 'OnMicResult', 'false');
            return;
        }

        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function(stream) {
                stream.getTracks().forEach(track => track.stop());
                window.unityInstance.SendMessage('WifiMicChecker', 'OnMicResult', 'true');
            })
            .catch(function() {
                window.unityInstance.SendMessage('WifiMicChecker', 'OnMicResult', 'false');
            });
    }
});
