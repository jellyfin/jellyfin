Emby Server
============

Emby Server is a personal media server with apps on just about every device.

It features a REST-based API with built-in documention to facilitate client development. We also have client libraries for our API to enable rapid development.

This repository is a hard fork of Emby 3.4.1.18 which unlocks Premium status by default and includes a buildable source repository for Debian and other package types.

## Rationale

I will start by saying I am only an Emby user and a professional sysadmin who lightly dabbles in Python. This is the story of why I'm forking Emby completely and forming a separate project.

In 2015, I and a number of other users requested LDAP support for Emby. I had been trying it out for about a year, as I was specifically looking for a Free Software alternative to Plex. This was the last missing feature for me, and one that I viewed as simple and critical to any piece of authenticated web-based software. I was also very willing to support the project with a premium membership when it fit my last need, so I waited patiently.

Emby continued to operate and update as it did until 2017. During this time however the Emby team began taking various extensions, as well as all mobile apps, out of their public repositories. I'm at this point still unsure of their status. This raised suspicion on my part but alone is legitimate. However in light of future events it certainly appears part of a pattern. The LDAP issue in particular also lingered, with numerous delays due to seemingly unrelated code changes and a number of noncommittal responses from the developers as to the status of the feature.

In the early part of 2017, the core developers added a nag screen. While the details are disputed, what I can say for certain is that for at least one minor version release, I was experencing a 10 second nagscreen prompting me to purchase a Premium licens, before *almost every* video I watched. The developers had indented this to be once every 24 hours, but in either case I was surprised. Further investigation into the Emby user forums as well as Github showed an extremely stark and, to me, chilling attitude from the core developers and the premium-paid community: an attitude that the developers can and should be making money off of Emby, a GPL project that, to my knowledge, was gifted to the current developers under this license in the past; and a hostile contempt towards any user who dared criticize this decision (specifically new users, non-premium, who were testing Emby or who had no desire for the Premium features).

This attitude was further demonstrated in the response to the issue on GitHub, where the devlopers appeared openly hostile to any criticism, though without the echo-chamber of the user forums to police the critics. The nagscreen was eventually removed, however the stain of this behaviour and attitude remained apparent.

In late 2017, it was then discovered that there were possible violations of the GPL license within the projects main repository. Specifically, a number of binary-only DLL files to which no source was provided and that the project would not build without; these files are at this time sill present in this repository as well. There was further hostility and outright evasiveness on the part of the lead developer to answering simple questions regarding the license status of these components, with delayed, terse, and noncommittal answers.

At long last earlier this year, the LDAP feature was released - as a Premium-only plugin. And issues of source control and user builds finally came to a head with the 3.5.0 version, which split several components of the source package into separate repositories. Public access to these repositories was not granted for some time, with further noncommittal and evasive answers from the Emby developers. While this situation was eventually rectified, for me this was the last straw.

In light of these actions, I beleve that the core Emby developers do not respect the Free Software community that their project implies it is a part of, nor do they respect their gratis users. The developers seek to monitize this project, as is their right, but their methods are overbearing, arbitrary, and demonstrate contempt for users who choose them because they are a Free Software project, including package maintainers and contributors. For these reasons, I do not believe they are good stewarts of the project, and this fork seeks to build an alternative to them in the Free Software space, while declaring to always remain both gratis and libre and respect its community.

I welcome any feedback, forks, or pull requests.

 * Joshua

## Building the Debian package

Enter the repository directory and run `dpkg-buildpackage -us -uc -jX`, where X is your core count. It will take some time to build and has many dependencies, especially `mono-devel` version 5.18 or newer. Obtain the latest version by using the [instructions here](https://www.mono-project.com/download/stable/#download-lin-debian).

---

The original README follows

---

## Emby Apps

- [Android Mobile (Play Store)](https://play.google.com/store/apps/details?id=com.mb.android "Android Mobile (Play Store)")
- [Android Mobile (Amazon)](http://www.amazon.com/Emby-for-Android/dp/B00GVH9O0I "Android Mobile (Amazon)")
- [Android TV](https://play.google.com/store/apps/details?id=tv.emby.embyatv "Android TV")
- [Amazon Fire TV](http://www.amazon.com/Emby-for-Fire-TV/dp/B00VVJKTW8 "Amazon Fire TV")
- [HTML5](http://app.emby.media "HTML5")
- [iPad](https://itunes.apple.com/us/app/emby/id992180193?ls=1&mt=8 "iPad")
- [iPhone](https://itunes.apple.com/us/app/emby/id992180193?ls=1&mt=8 "iPhone")
- [Kodi](http://emby.media/download/ "Kodi")
- [Media Portal](http://www.team-mediaportal.com/ "Media Portal")
- [Roku](https://www.roku.com/channels#!details/44191/emby "Roku")
- [Windows Desktop](http://emby.media/download/ "Windows Desktop")
- [Windows Media Center](http://emby.media/download/ "Windows Media Center")
- [Windows Phone](http://www.windowsphone.com/s?appid=f4971ed9-f651-4bf6-84bb-94fd98613b86 "Windows Phone")
- [Windows 8](http://apps.microsoft.com/windows/en-us/app/media-browser/ad55a2f0-9897-47bd-8944-bed3aefd5d06 "Windows 8.1")

## New Users ##

If you're a new user looking to install Emby Server, please head over to [emby.media](http://www.emby.media/ "emby.media")

## Developer Info ##

[Api Docs](https://github.com/MediaBrowser/MediaBrowser/wiki "Api Workflow")

[How to Build a Server Plugin](https://github.com/MediaBrowser/MediaBrowser/wiki/How-to-build-a-Server-Plugin "How to build a server plugin")


## Visit our community: ##

http://emby.media/community

## Images

![Android](https://dl.dropboxusercontent.com/u/4038856/android1.png)
![Android](https://dl.dropboxusercontent.com/u/4038856/android2.png)
![Html5](https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/apps/html5.png)
![iOS](https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/apps/ios_1.jpg)
![iOS](https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/apps/ios_2.jpg)
![Emby Theater](https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/apps/mbt.png)
![Emby Theater](https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/apps/mbt1.png)
![Windows Phone](https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/apps/winphone.png)
![Roku](https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/apps/roku2.jpg)
![iOS](https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/apps/ios_3.jpg)
![Dashboard](https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/apps/dashboard.png)
![iOS](http://i.imgur.com/prrzxMc.jpg)
![iOS](http://i.imgur.com/c9Vd1w5.jpg)

