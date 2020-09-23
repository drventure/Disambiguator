The Disambiguator
===========
http://sourceforge.net/projects/disambiguator


This is a plugin to KeePass <http://www.KeePass.info> to allow the AutoType functionality to
work with by ALSO matching the associated application file (the root EXE
of the matched window). This allows a much finer grained control of matching that just window
title alone

Features
 * 
 * 
 * 
 * 


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


Usage
-----
To remove the ambiguity, The Disambiguator allows you to enter additional "tags" in the Window Title
field in KeePass.

Currently, the only tag supported is "exe".

As an example, your current Window Title field contains
   
   Enter Password

But this is also the Window title of several other KeePass entries, which causes an ambiguity.

Change the title to

   Enter Password{exe:quicken}

And this was cause The Disambiguator to check that the current window's application is
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
Please use the SourceForge project page: <http://sourceforge.net/projects/disambiguator>
Bugs can be reported using the issue tracker, for anything else, a discussion forum is available.


Changelog
--------
v0.1
 Initial release by drventure
 <http://sourceforge.net/u/drventure/disambiguator/>