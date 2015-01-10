# Dota2TextToSpeech
Reads DOTA2 All and Teamchat aloud, so now I can hear the insults in the voice of a middle-aged British woman.

Idea came from: https://github.com/patriksletmo/Dota2Translator
Also took his Dota2ChatDLL and modified it a bit. Please give all credit to patriksletmo.

To build:

1. Make sure that Dota2TextToSpeech is the startup project.

    This can be changed by right-clicking the project and selecting "Set as Startup Project".

    If you downloaded the 2012 version of Visual Studio you have to change the platform toolset of the project Dota2ChatDLL.

2. Right-click on each project, navigate to properties and changw "Platform Toolset" to "Visual Studio 2012 (v110).

    While this enables you to build the project without installing Visual Studio 2010, it will result in end users having to        download the Visual C++ 2012 Redistributable instead of the 2010 version.

3. Make sure to build the project for Release and x86.

    The projects have not been configured for building in any other mode and will most likely not succeed.
