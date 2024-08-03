# Dr. Robotnik's Ring Racers Mod Manager
This is a cross-platform mod manager with user interface

- Supports updating addons from the Message Board and Gamebanana
- Allows toggling addons at game startup using ringexec.cfg and batch loading addons while the game is running (Windows only)

## Usage
- Download the [latest version](https://github.com/troy236/DRRR-ModManager/releases/latest) that matches your system and CPU architecture. Mac M1/2/3 are arm64
- If you are unsure of your CPU architecture assume it is x64. If the mod manager does not open try the arm64 build
- Open DRRRModManager.exe
- On 1st launch it will try to automatically determine your Ring Racers install path. If this fails you will be prompted to locate it
- Some addons will be automatically detected. Any that can't be linked will need to be added manually
- When adding a addon manually if you specify the Message Board/Gamebanana URL it came from this will allow the mod manager to update it
- If you are on Linux it is recommended you run the mod manager through a terminal as it is not automatically opened unlike Windows/Mac. This allows you to see the logging

## Developers/Programmers
To Kart Krew/Message Board staff: Please give us a API to get addons! Preferably with filters including by addon id and addons last updated by epoch timestamp

1-click install is a placeholder right now. Not sure how I am meant to integrate this with Gamebanana and for the Message Board that'd require Kart Krew/Message Board staff cooperation

May expand to SRB2/Kart if a API/1-click install gets added as it'd be a lot better on their servers

If you are familar with the DWARF debug file format or know a library that I can use to read them and get source data/line numbers with a IP (instruction pointer) it would allow adding that info to the error log for Linux/Mac with the debug symbols present