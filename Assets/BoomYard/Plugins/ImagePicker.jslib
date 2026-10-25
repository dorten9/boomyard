// Picks a picture for a stencil in the browser: a hidden file input, the image scaled down on a canvas, and its
// pixels (RGBA, top row first) sent back to the game as "width,height,base64". Browsers only open a file dialog
// from a real tap, so the game arms this on the button's pointer down and the dialog opens on the pointer up.
mergeInto(LibraryManager.library, {
  BoomPickImage: function (objPtr, maxSide) {
    var obj = UTF8ToString(objPtr);
    var open = function () {
      var input = document.createElement('input');
      input.type = 'file';
      input.accept = 'image/*';
      input.style.display = 'none';
      document.body.appendChild(input);
      input.onchange = function () {
        var file = input.files && input.files[0];
        document.body.removeChild(input);
        if (!file) { SendMessage(obj, 'OnPickCancelled', ''); return; }
        var url = URL.createObjectURL(file);
        var img = new Image();
        img.onload = function () {
          var k = Math.min(1, maxSide / Math.max(img.width, img.height));
          var w = Math.max(1, Math.round(img.width * k)), h = Math.max(1, Math.round(img.height * k));
          var c = document.createElement('canvas');
          c.width = w; c.height = h;
          var ctx = c.getContext('2d');
          ctx.imageSmoothingQuality = 'high';
          ctx.drawImage(img, 0, 0, w, h);
          var d = ctx.getImageData(0, 0, w, h).data;
          var bin = '';
          for (var i = 0; i < d.length; i++) bin += String.fromCharCode(d[i]);
          URL.revokeObjectURL(url);
          SendMessage(obj, 'OnPickedPixels', w + ',' + h + ',' + btoa(bin));
        };
        img.onerror = function () { URL.revokeObjectURL(url); SendMessage(obj, 'OnPickFailed', ''); };
        img.src = url;
      };
      input.click();
    };
    var once = function () {
      document.removeEventListener('pointerup', once, true);
      document.removeEventListener('touchend', once, true);
      open();
    };
    document.addEventListener('pointerup', once, true);
    document.addEventListener('touchend', once, true);
  }
});
