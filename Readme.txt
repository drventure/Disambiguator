The Disambiguator
===========
https://github.com/drventure/Disambiguator


This is a plugin to KeePass <http://www.KeePass.info> to allow the AutoType functionality to
work by ALSO matching the associated application file (the root EXE of the matched window)
as well as matching a child control name or value within the target window. 
This allows a much finer grained control of matching that just window title alone.

For example, it's a fairly common occurrance to have two different applications both prompt
the user for a password with a window title of "Enter Password".

In this case, KeePass can't distinguish one application's window from the other, and thus is
forced to display the "Credential Chooser" window and make the user decide.

With Disambiguator, you can specify the actual Application Name in the Window Title Matching 
template string, which can then be used to tell one application's "Enter Password" dialog
from another.

You can also specify a particular control name or property value that, if detected, can
be used to choose a particular credential entry of others.


Features
--------
 * Allows user to specify the Application Name to match in addition to Window Title
 * Allows user to specify a control name or property in addition to Window Title
 * Has a rudementary but functional "Report" that can be used to determine the Application Name
   or child control names and properties.
 * Is All-Natural and Preservative Free


Installation
------------
Place Disambiguator.plgx in your KeePass Plugins folder.


Uninstallation
--------------
Delete Disambiguator.plgx from your KeePass Plugins folder.


Scenario
--------
You have several KeePass entries that match a single target input field and you'd like to be able 
to apply additional search constraints to eliminate the ambiguity and automatically select the
correct entry to autotype.

For instance, Quicken has several popup windows that are titled "Enter Password".
However, other apps ALSO have several popup windows titled "Enter Password".
Since the Window Title is the only thing you can use for matching in this case, you'll always get 
the KeePass entry selection window when using AutoType.


Application Disambiguation Usage
--------------------------------
To remove the ambiguity, The Disambiguator allows you to enter additional "tags" in the Window Title
field in KeePass.

As an example, your current Window Title field contains
   
   Enter Password

But this is also the Window title of several other KeePass entries, which causes an ambiguity.

Change the title to

   Enter Password{exe:quicken}

And this wil cause The Disambiguator to check that the current window's application is
"quicken.exe". If not, this Entry won't be considered for AutoType.

For the exe parameter, you can specify:
   o   just the exe filename (the extension will be assumed to be "exe")
   o   The filename including extension; {exe:quicken.exe}
   o   a full path; {exe:c:\program files\MyApp\MyApp.exe}
   o   A wildcard path; {exe:*\MyApp.exe}
   o   A RegEx path; {exe://.*quicken/.exe//}

NOTE: All standard KeePass replaceable parameters are still honored in both the Window Title
and in the exe parameter value.
Also note that the Window Title (minus any specified tag entries) is also still compared to the
title of the autotype target, unless the resulting Window Title is empty, in which case it's
ignored.


Control Disambiguation Usage
--------------------------------
Similiarly to using the Application to disambiguate a credentials entry, specific control properties
of other controls on the target window can be used during the matching process. To support this
Disambiguator uses the {ctl:} tag.

As an example, as in the above example, your current Window Title field contains:
   
   Enter Password

But this is also the Window title of several other KeePass entries, which causes an ambiguity.

Using the {report} tag (described below), you can see that there is a child control named "lblPasswordValue"
that is unique to this window.

Change the title to:

   Enter Password{ctl:lblPasswordValue}

And this wil cause The Disambiguator to check that the current window contains a child control
with the name "lblPasswordValue".

For the ctl parameter, you can specify:
   o   The automation ID (or ID) of a control
   o   The Name of a control
   o   The Class Name of a control
   o   a simple wildcard to match any of the above properties of a control, {ctl:lblPassword*}
   o   A RegEx; {ctl://lblPassword(s)?Value//}

NOTE: All standard KeePass replaceable parameters are still honored in both the Window Title
and in the ctl parameter value.
Also note that the Window Title (minus any specified tag entries) is also still compared to the
title of the autotype target, unless the resulting Window Title is empty, in which case it's
ignored.


Report
------
Sometimes, determining what to use for the Application Name or disambiguating child control can 
be tricky, so Disambiguator includes a "Report" tag to assist.

To use it, in a Window Title field, where you would normally enter the title and/or the {exe:name} 
or {ctl:name} tags, add the tag {report}.

Then invoke KeePass using the AutoType hotkey in the target application as normal.

KeePass will not actually autotype any credentials.
Instead, a "Report" window will be displayed showing key information about the target window
that can be used with the {exe:} or {ctl:} tags.


Security
--------
The Disambiguator does not expose access to your KeePass database in any way. It only extracts additional
data from the target application, using standard Windows interfaces.

Password data is still transferred by KeePass, using Autotype, and the mechanism for this transfer
is not altered at all by The Disambiguator.


Checking for updates
--------------------
If you want to use the KeePass Check for Updates function to check for updates to this plugin
then it requires the SourceForgeUpdateChecker plugin to be installed too:
http://sourceforge.net/projects/kpsfupdatechecker


Credits
-------
The Disambiguator was developed by drventure, based loosely on the Open Source WebAutoType
project by CEPOCTb and a derived project by Alex Vallat.



Bug Reporting, Questions, Comments, Feedback, Donations
-------------------------------------------------------
Please use the GitHub project page: <https://github.com/drventure/Disambiguator>
Bugs can be reported using the issue tracker, for anything else, a wiki is available.


Changelog
--------
v1.0.3.0
Changed from depth first to breath first control search. Should make resolving windows and 
controls much faster, esp for very complex windows.

v1.0.2.0
Added menus for turning on Report mode and logging, plus some simple help.

v1.0.0.0
Official first release version.

v0.0.2
Added support for the {ctl:} and {report} tags.
Removed dead code.
Updated the readme

v0.0.1
 Initial release by drventure
 https://github.com/drventure/Disambiguator
