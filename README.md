# Dice Adventure (HMT Game 1)
This repository contains the code for the first human-machine teaming game - **[Dice Adventure](https://strong-tact.github.io/game)** developed as part of the STRONG TACT project.

# Game Modes
The game has 2 modes:
1. Local Mode (Offline mode) - Single Player experience - (Released)
2. Competition Mode (Online mode) - Multi Player experience - (Soon to be Released)


# Getting Started
1. Download the [Unity Editor](https://unity.com/download) (2020.3.32f1 or above)
2. Download this repo (*.zip or *.git). **NOTE**: Always pull from the [main branch](https://github.com/STRONG-TACT/HMT-Game-1/tree/main) of this repo if using version control.
3. [Open this repo](https://docs.unity3d.com/2019.1/Documentation/Manual/GettingStartedOpeningProjects.html) in the Unity Editor.


# Project Details
## Scenes
The valid scene order is:
1. /Assets/Scenes/Networked/Lobby_n
2. /Assets/Scenes/Networked/Room_n
3. /Assets/Scenes/Networked/LocalAnimated
4. /Assets/Scenes/Networked/NetworkGamePlay
5. /Assets/Scenes/Networked/SurveyScene


## Scene Description
### /Assets/Scenes/Networked/Lobby_n
1. Main Menu Options (Choose between Local and Competition Mode)
2. HMT Interface Initialization based on assigned launch parameters.
3. Connect to Photon Servers.



### /Assets/Scenes/Networked/Room_n
1. Waiting area for all players to join the same room
2. Game initialization after all click on "I'm Ready"

### /Assets/Scenes/Networked/LocalAnimated
1. Local mode of the game. Single player experience.

### /Assets/Scenes/Networked/NetworkGamePlay
1. Online mode of the game. Multiplayer experience

### /Assets/Scenes/Networked/SurveyScene
1. Player experience survey



# -------------------------------------------------------
The game currently requires that three separate client instances connect to each other in order to play the game.

The easiest way to run the game locally is to:
1. Open the project in the Unity Editor
2. Compile the game to your local platform (or use the WebGL build target) See [Unity's Documentation](https://docs.unity3d.com/Manual/PublishingBuilds.html) for how to build a project.
3. Run the compiled build twice, and run a third instance from within the Unity Editor.
4. Use one instance to create a room and the other two to connect.
