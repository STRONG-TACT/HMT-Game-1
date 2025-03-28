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


# Scenes
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


# Launch Parameters
- `-hmtsocketport` <port number for the HMT API to open on>:
The port you should use for the HMTInterface connection. This needs to be unique for each instance of the game running on the server and will be managed by the flask environment.\
- `-photonroom` <the name of the photon room provided by the flask call>: 
The name of the photon room to join. This will be provided to the flask server by the call to launch_game.\
- `-trainingmode`: A flag that can be provided to launch the game directly into local multiplayer mode intended for batch training agents without human players. This disables logging and prevents the game from connecting to remote human clients and should only be used for an all agent game.\
- `-relaodonend`: A flag that sets the game up to loop back to the lobby once the game has finished and sent a GameOver response to an agent call. Useful for continuously training a batch of agents in trainingmode.
- `-tracelogs`: Activate logging to standard debug console.
- `-batchmode` and `-nographics` are built Unity in flags for [Desktop Headless Mode](https://docs.unity3d.com/Manual/desktop-headless-mode.html) that may be useful to agent training runs


# Levels
Levels in the game are based on schema descriptions and you could design your own levels too. Refer the [schema design document](https://docs.google.com/document/d/1OhPlfYfoKjUuYjsSkr330V0Sl4VWTWIvQXsPBW-5Gx4/edit?tab=t.0) to get started.


