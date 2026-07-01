mergeInto(LibraryManager.library, {
    // Existing DownloadFile function
    DownloadFile: function (base64, fileName) {
        var binaryString = atob(Pointer_stringify(base64));
        var len = binaryString.length;
        var bytes = new Uint8Array(len);
        for (var i = 0; i < len; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        var blob = new Blob([bytes], { type: 'image/png' });
        var url = URL.createObjectURL(blob);

        var a = document.createElement('a');
        a.href = url;
        a.download = Pointer_stringify(fileName);
        a.click();
        URL.revokeObjectURL(url);
    },




LoadImageFromURL: function (urlPtr, onSuccessPtr, onErrorPtr) {
    var url = UTF8ToString(urlPtr); // Convert Unity string pointer to JavaScript string

    var img = new Image();
    img.crossOrigin = "anonymous"; // Handle cross-origin requests

    img.onload = function() {
        var canvas = document.createElement('canvas');
        canvas.width = img.width;
        canvas.height = img.height;
        var ctx = canvas.getContext('2d');
        ctx.drawImage(img, 0, 0);

        // Convert the image to a base64 string
        var imageData = canvas.toDataURL(); // This creates a base64 string

        // Allocate memory in Unity for the string
        var buffer = Module._malloc(imageData.length + 1); // +1 for null terminator

        // Write the base64 image data to the allocated memory
        writeAsciiToMemory(imageData, buffer);

        // Call the success callback in Unity with the pointer to the image data
        Runtime.dynCall('vi', onSuccessPtr, [buffer]);

        // Free the allocated memory after usage
        Module._free(buffer);
    };

    img.onerror = function() {
        // If there's an error loading the image, call the error callback in Unity
        Runtime.dynCall('v', onErrorPtr);
    };

    // Start loading the image
    img.src = url; // Use the JavaScript string directly
}



});
