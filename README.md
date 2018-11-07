# Maplestory 2 bot

How it works:
This bot takes small screenshots of select portions of your maplestory 2 game, and uses interception to send keys. This bot aims to be completely external from the game in order to remain under the radar.    

Because the bot takes screenshots of the UI, you need to follow these rules when fishing:
1. Game Resolution at 1280x960
2. Interface size is 50
3. Display scaling in windows is set to 100%

This project uses the c# wrapper for interception made by jasonpang https://github.com/jasonpang/Interceptor

Features:
Fishing
Custom Scripts
Fire Dragon (In progress, not working)
Other raids (Planned)

To get started developing:
1. install interception http://www.oblita.com/interception.html
2. Make sure it installed correctly, then reboot.
3. Clone this repository
4. Restore nuget packages using your IDE's nuget package manager
5. Add a reference to the interceptor.dll located in the root directory of this project
6. Build for x64, the others weren't tested


If you want to create custom scripts:
1. Go into Program.cs and comment out the fishing code lines on 16 and 17.
2. Uncomment lines 12 and 13.
3. Edit Blank.cs to whatever you want. An example is in Blank.cs
