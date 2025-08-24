mergeInto(LibraryManager.library, {
  OpenNewTab: function (url) {
    window.open(Pointer_stringify(url), '_blank');
  }
});
