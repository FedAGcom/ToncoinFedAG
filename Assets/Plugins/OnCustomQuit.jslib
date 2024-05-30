mergeInto(LibraryManager.library, {
    // Define a function to handle the onbeforeunload event
    HandleBeforeUnload: function() {
        // Call a Unity function named "OnCustomQuit" on a GameObject named "GameManager"
        Module.ccall('SendMessage', // Function name
                      null, // Return type (void)
                      ['string', 'string', 'string'], // Argument types
                      ['GameManager', 'OnCustomQuit', '']); // Arguments
        // Return a message to prompt the user
        return 'Are you sure you want to leave?';
    }
});