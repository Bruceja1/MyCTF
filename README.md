# Bruceja's Capture The Flag

## What is it?
This is a custom Capture The Flag plugin for MCGalaxy. It builds on top of the built-in CTF code by adding numerous features.
This is also my first plugin and first 'real' programming project. I really miss Caznowl CTF so this is my attempt at bringing it back (sort of). It is mainly the lasergun that was inspired by Caznowl. All the other features are more custom.

## Features
### Lasergun
The main inspiration behind the whole project; the infamous Caznowl CTF lasergun. This is where the whole project began. The lasergun is fired by placing a gravel block on the ground and will fire a lava laser in the direction you are facing. This is your only way of killing other players. It only supports firing in four directions.
The gravel block does not fall and the laser does not phase through blocks. The laser only works when physics are enabled on the map. After playtesting it turns out that, while the laser does work, the hit registration is not the best.
I don't know why it's not working properly but perhaps this is an opportunity for another developer to take it and improve upon it. I'd personally love to see the lasergun working properly on a server since this is the main reason I started
this whole project in the first place.\
<img width="720" height="480" alt="Screenshot 2025-12-28 200921" src="https://github.com/user-attachments/assets/add0ad64-d407-4cdc-a24b-1ec51a8b38b3" />

### Stats
This plugin also includes custom stats. It tracks your kills, killstreaks, wins, winstreaks, flag captures, XP, etc. XP is earned by getting kills, wins and capturing the flag. You also gain XP when a teammate captures the opponent's flag.
These stats are also available in /top.\
<img width="720" height="480" alt="stats" src="https://github.com/user-attachments/assets/0da3a472-c9ff-4533-9400-bd8974815476" />


### Announcements
Every time you get a kill, capture or lose a flag, get a killstreak, get an achievement, etc, it tells you in chat. For some events, like when a flag is captured or lost, an announcement appears in the middle of the screen.
<img width="720" height="480" alt="Screenshot 2025-12-28 195622" src="https://github.com/user-attachments/assets/78a94c1a-aa70-4bc3-9b30-eb824db39162" />

### Joining teams
Before a round begins, players can join a team using /mc join red or /mc join blue and leave their team using /mc leave. If a player joins in the middle of the round, they will automatically be assigned to the team with the least amount of players. If both teams are of equal size, the player will be 
assigned a random team.
<img width="899" height="198" alt="Screenshot 2025-12-28 200716" src="https://github.com/user-attachments/assets/4653555c-67c4-41cf-b19d-37c56855a1ed" />

### Grabbing flags
While MCGalaxy's built-in CTF already has a feature where the flag hovers above a flag carrier's head, it doesn't look very smooth. This is because the code checks the player's position every tick and then spawns and displays the flag block above the carrier's head.
This plugin uses a bot instead. This makes the flag displayed above the carrier's head move smoothly.
<img width="720" height="480" alt="Screenshot 2025-12-28 200515" src="https://github.com/user-attachments/assets/e3e98696-e9bd-4a21-bec5-f8751caa3782" />

### Ranking system
Every time you get a kill, win a round or capture a flag, you get XP. When you earn enough XP, you will rank up. You can check to see what your next rank is and how much XP you need by using /xp.
<img width="769" height="60" alt="Screenshot 2025-12-28 201242" src="https://github.com/user-attachments/assets/d4fdc020-c64c-4907-a493-96a2c07a9c17" />
<img width="855" height="587" alt="Screenshot 2025-12-28 201328" src="https://github.com/user-attachments/assets/a4087f09-f23e-412c-a5eb-923cc757fa9b" />\
XP requirements are calculated using a simple recursive formula that can be adjusted inside MyCTFGame.cs:
<img width="870" height="228" alt="image" src="https://github.com/user-attachments/assets/b12fd4c0-86ca-4477-aae6-6e189d24d729" />

### Achievements
For replayability, custom awards (achievements) were added as well. These are made functional by using custom events. For example, when a player gets a win, a custom OnWin event is triggered that checks whether or not the player meets the win threshold to receive
the 'Superstar' achievement.
<img width="919" height="469" alt="Screenshot 2025-12-28 231124" src="https://github.com/user-attachments/assets/9c8e0447-3c01-4aaa-b7b2-749bcf285ccd" />


## Setup
Before the game can run, a couple things need to be set up. Firstly, you need to add one or more maps to the map pool. Go to the map you want to add and use /mc add. On each map, you need to specify the flag and spawn location for each team.
Optionally, you can specify the round time using /mc set time. If you don't, a default value will be used. Once you have setup everything, use /mc start <map>. The round countdown should begin. See the image below for all the setup commands:

<img width="950" height="296" alt="Screenshot 2025-12-28 224349" src="https://github.com/user-attachments/assets/6bd4d36e-2c06-42d1-8833-af2879c1f9fe" />

## How to get this working on your server
If you want to use this plugin on your own server, it will be very tricky to get it working properly. The plugin in its current state only works with the custom ranks, colors and other settings I used on my main server. 
You will most likely get many errors trying to get this running on your own server.
I tried to set this plugin up on a fresh MCGalaxy server but it is way too tedious to do it that way since all the settings need to be exactly the same as they are on my main server. 
I think a better approach is to browse my code, see how I did things, copy bits and pieces of it, improve it, experiment and make your own version of this plugin.

## Closing thoughts
I am pretty satisfied with the end result. While the server doesn't have active players, the few active sessions I did manage to witness, players gave mostly positive feedback. For now I am not planning on actively maintaining the server.
Maybe some time in the future I might return and give the server another chance by investing more time into improving the plugin; mainly the lasergun's hit registration. I think that is the main thing holding this back from potentially being an active server.

Perhaps I might integrate Goodly's effect plugin in the future so that particles appear when you get a kill.
