mergeInto(LibraryManager.library, {
  SkyMarblesIsMobileBrowser: function () {
    var ua = navigator.userAgent || '';
    var ipad = navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1;
    return /Android|iPhone|iPad|iPod|Mobile/i.test(ua) || ipad ? 1 : 0;
  }
});
