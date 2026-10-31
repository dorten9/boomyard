// Saves a photo from the game's gallery to the player's downloads (a PNG, sent as base64).
mergeInto(LibraryManager.library, {
  BoomDownload: function (namePtr, dataPtr) {
    var name = UTF8ToString(namePtr), data = UTF8ToString(dataPtr);
    var bin = atob(data), bytes = new Uint8Array(bin.length);
    for (var i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    var url = URL.createObjectURL(new Blob([bytes], { type: "image/png" }));
    var a = document.createElement("a");
    a.href = url;
    a.download = name;
    document.body.appendChild(a);
    a.click();
    setTimeout(function () { document.body.removeChild(a); URL.revokeObjectURL(url); }, 1000);
  }
});
