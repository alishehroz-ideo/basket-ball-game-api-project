mergeInto(LibraryManager.library, {
  GameBullCopyToClipboard: function(strPtr) {
    var str = UTF8ToString(strPtr);
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(str);
      } else {
        // fallback: hidden textarea + execCommand
        var ta = document.createElement('textarea');
        ta.value = str;
        ta.style.position = 'fixed';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.focus();
        ta.select();
        document.execCommand('copy');
        document.body.removeChild(ta);
      }
    } catch (e) {
      console.error('GameBull clipboard copy failed:', e);
    }
  }
});
