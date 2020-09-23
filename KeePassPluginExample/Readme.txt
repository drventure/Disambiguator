WebAutoType
===========
http://sourceforge.net/projects/webautotype


This is a plugin to KeePass <http://www.KeePass.info> to allow the AutoType functionality to
work with browser URLs as well as window titles. It uses IAccessible screen assistive technology
to 'read' the browser window, and is therefore at this time only supported on Windows.

Features
 * Support for all major browsers: Firefox, Chrome (and chrome-based browsers like Opera),
    Internet Explorer, and Edge
 * Create custom AutoType target URLs, or optionally use the standard URL field to match against
 * Create custom AutoType sequences for different URLs in the same entry
 * Automatically skip User Name part of AutoType sequence when starting in a password box
 * Optionally define a shortcut key to create a new entry, pre-populated with information from
    the current browser page
 * Optionally show the search window on the second attempt to AutoType for a page with no
    entry found.


Installation
------------
Place WebAutoType.plgx in your KeePass Plugins folder. A "WebAutoType Options" menu item will
be added to the KeePass "Tools" menu.


Uninstallation
--------------
Delete WebAutoType.plgx from your KeePass Plugins folder.


Google Chrome
-------------
Chrome may not automatically enable accessibility, depending on the version. For several versions
the automatic detection of applications requesting accessibility was broken (issue #342319). This
has now been partially fixed, however if you are experiencing issues with Chrome then try forcing
accessiblity to be enabled by launching Chrome with the "--force-renderer-accessibility" flag on
the command line.


Pale Moon
---------
Pale Moon (and similar FireFox variants) have Accessibility deliberately disabled in an attempt
to improve performance. Therefore WebAutoType can not read the URL from them, and will not work
at all. It is not possible for WebAutoType to support browsers which do not expose accessibility
information.


Edge
----
Edge has limited support for IAccessible, therefore the URLs will be missing the http:// or
https:// scheme prefix.

Edgium (new Edge based on Chrome) does not suffer this limitation, and is treated as any other
Chrome variant by WebAutoType


Firefox
-------
The "Tile Tabs" addon modifies the layout and internal structure of Firefox pages in a way which is
incompatible with WebAutoType 5.X. Users of this addon should stick with WebAutoType version 4.2.

Very rarely, a site will include a custom "role" accessibility attribute on the body tag, which has
the effect of preventing Firefox from exposing the URL through accessibility. If you there is a
single site that doesn't work, where others do, then the solution may be to install the tiny addon:
<https://addons.mozilla.org/en-GB/firefox/addon/prevent-custom-document-role/> which will remove
the "role" attribute and make the site work normally.


Usage
-----
To enable AutoType matching against the URL field in your entries, click the "WebAutoType Options"
entry in your "Tools" menu, and check the "Use the URL field value for matching" checkbox. When
this option is selected, the value of the URL field will be checked against the start of the URL
in the browser window, so if you URL field states "https://www.example.com" then the browser URL
"https://www.example.com/login.php" would match against that.

To define alternative, or custom URLs to match against for an entry, use the AutoType tab on the
KeePass entry editing window. Click the "Add" button, then under in the Edit Auto-Type Item box
click the URL button, and enter the URL to match against. Here, you can also include wildcards
and regular expressions, just as you can for window titles, so if you want the same behaviour of
matching just the start of the URL, end it with a * character (to mean any further characters
are valid here).

For multi-page logins, you can use these additional auto-type entries with the URLs of each page,
and a custom keystroke sequence for each page.


Automatically skipping User Name
--------------------------------
To have the AutoType sequence automatically skip the User Name part when starting from a password
entry box, check the "Automatically skip user name for passwords" box in the "WebAutoType Options"
window. When this option is enabled, if the cursor is in a Password edit box when the AutoType
hot key is pressed, then if the entry's AutoType sequence starts with "{username}{tab}" then that
part is ignored. Note that this won't be done for explicitly definied custom sequences for
specific windows or URLs, just the sequence defined for the entry, or the one it inherits from
its group. This functionality is not available for Edge.


Creating new Entries
--------------------
WebAutoType also offers the ability to set a shortcut for creating a new entry. To do this, click
the "WebAutoType Options" entry in your "Tools" menu, and enter a keyboard shortcut in the Global
hot key box. You may also select the group into which the new entry should be added. When the
hot key is pressed, a new entry will be created pre-populated with the following information:

Url: The root part of the URL of the current web page
Title: The title of the current web page.
User name: The contents of the textbox with the focus, if any. (usefull if your username is already
           entered in the form)
           
Title and User name are not supported for Edge.

The URL box in the Entry window will also show a drop-down button which allows you to choose a more
specific URL to use, if using just the root part is not appropriate.

The Create Entry behavior may also be triggered from the command line, by passing the parameter:
/e:WebAutoType.CreateEntry to KeePass.exe.


Searching for Entries
---------------------
WebAutoType offers the ability to search for an entry. To enable this functionality, click the
"WebAutoType Options" entry in your "Tools" menu, and check the "Show search for repeated autotype"
box. Once enabled, if you trigger an AutoType for a web page, but no AutoType is performed (as
no matching entry for the URL was found), then simply trigger the AutoType for the same page a
second time (hit the same shortcut key again) and if it was still unable to find an AutoType match,
the Search window will be shown.

This is useful if you think that there should already be an entry for the page, but perhaps the URL
didn't match exactly or the entry might have AutoType disabled, or be in a group with AutoType
disabled.

The search text is pre-populated with the detected URL for the page.


Security
--------
WebAutoType does not expose access to your KeePass database in any way. It only extracts additional
data from the browser, using standard interfaces designed for screen readers and automation tools.

Password data is still transferred by KeePass, using Autotype, and the mechanism for this transfer
is not altered at all by WebAutoType.


Checking for updates
--------------------
If you want to use the KeePass Check for Updates function to check for updates to this plugin
then it requires the SourceForgeUpdateChecker plugin to be installed too:
http://sourceforge.net/projects/kpsfupdatechecker


A note on UIA and IAccessible
-----------------------------
WebAutoType was initially developed using the UIA interfaces for screen reading. This is a newer
technology than MSAA (IAccessible). However, Firefox 52 and above only enable IAccessible2 if
multiprocess is enabled, not UIA.

IAccessible2 is not suitable for this plugin due to it's requirement for an installed system-level
COM proxy dll, but it is based on the older IAccessible interfaces. This is sufficient for reading
the URL in most browsers. The only exception is Microsoft Edge, does not have full support for
IAccessible. With Edge, the URL can therefore only be obtained from the address bar, resulting in
more limited functionality.


Credits
-------
WebAutoType was initially developed by CEPOCTb. With his permission, version 3.0 has been released
as a derived project by Alex Vallat.


Bug Reporting, Questions, Comments, Feedback, Donations
-------------------------------------------------------
Please use the SourceForge project page: <http://sourceforge.net/projects/webautotype>
Bugs can be reported using the issue tracker, for anything else, a discussion forum is available.


Changelog
--------
v6.4
 Improved Edgium support, will retry up to a 3 second timeout due to delays in enabling accessibility

v6.3
 Improved Chrome and chromium-based browser support. In many cases the --force-renderer-accessibility
  is no longer required.

v6.2.1
 Fix for displaying multiple matching auto-type sequences in a single entry

v6.2
 Compatibility with Firefox 59+

v6.1
 Made URL field matching more intelligent. Rather than just starts-with, it will now try and match
  any URL that has the URL field value as a base, now including sub-domains.

v6.0.0
 Compatibility with KeePass 2.42. For versions of KeePass prior to 2.42, use an 5.X version.

v5.3.2
 Provide extra error checking to protect against errors that are returned for custom web 
  components Role property accessors.

v5.3.1
 Compatibility with PEDCalc plugin - PwEntry ParentGroup now available for newly created entry in
  PwEntryForm

v5.3
 Added support for a "/e:WebAutoType.CreateEntry" KeePass parameter.

v5.2
 Populate the entry title when creating a new entry without a focused edit box.

v5.1
 Fixed crash when showing the list of URLs drop-down with Firefox 57+
 Improvements to compatibility with Firefox 58

v5.0
 Switched to using IAccessible instead of UIA
 Added support for Yandex.Browser

v4.2
 Fixed bug where Escape key no longer dismissed Edit Auto-Type Item window
 Removed delay when auto-typing into non-browser windows

v4.1
 Added support for Microsoft Edge browser

v4.0
 Default compatibility with CLR 4 (.NET Framework 4, 4.5, 4.6) 
  instead of CLR 2 (.NET Framework 2, 3.5)
 Added drop-down selection of URL part when creating a new entry using the New Entry hotkey.

v3.8
 Improved performance under certain UIA circumstances (usually first time usage under Firefox)
 Fixed crash where focused window has no handle

v3.7
 Improved UI in the window for setting custom Auto-Type matches

v3.6
 Fixed support for Chrome v32 with accessibility turned off

v3.5
 Re-enabled support for Chrome accessibility (see above section on Chrome for further details)
 Added support for update checking, using SourceForgeUpdateChecker

v3.4
 Fixed crash if the options window was shown with no database loaded
 Added support for Chrome accessibility probing, so chrome should now automatically expose
  accessibility when KeePass with WebAutoType is running.

v3.3
 Added option for showing the Search window if AutoType is invoked twice for the same URL,
  unsuccessfully.
 Improved support for internationalised versions of Chrome

v3.2
 Fixed support for Chrome v29
 Improved reliability of UIA field detection after focus shift (for example, after using the unlock
  dialog in response to global autotype)
 When KeePass is minimized to the tray, the Add Entry dialog launched by the hotkey will now be
  given a taskbar button and brought to the front.

v3.1
 Added support for automatically skipping the UserName part AutoType sequences when starting from
  a password entry box.

v3.0
 Added support for URL field matching
 Added Create Entry hot key
 
v2.1.9
 Initial release by CEPOCTb
 <http://sourceforge.net/u/cepoctb/webautotype/>