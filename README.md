8 BRICKS DEFENSE
----------------
In this tutorial we will see how to convert a single-player game in a multiplayer game with YourNetworkTools.

PRE-REQUISITES
--------------
This is not a tutorial for people who don't know about game programming. You have to be familiar 
with game programming, OOP, Event Programming, Design Patterns and Git tools.

SOURCE CODE
-----------

The package contains the code of the game in multiplayer mode. In order to do the tutorial you 
will have to request access to the repository to clone it and start the tutorial from the beginning.

Email me to the address esteban@yourvrexperience.com with your purchase order and I will give you access
to the repository:

https://github.com/EstebanGameDevelopment/8BricksDefense

TUTORIAL
--------

 0. VIDEO TUTORIAL PLAYLIST:
 
    https://www.youtube.com/playlist?list=PLPtjK_bez3T5Bxb-TRUW5hYNjriPVhE_C

 1. INSTALLATION OF FREE SDKs:
 
	First, before to proceed to do the tutorial you have to download this free packages first:

		Google VR SDK:
		https://developers.google.com/vr/unity/download

		iTween (easy to use plugin for animations):
		https://assetstore.unity.com/packages/tools/animation/itween-84

  2. CLONE THE REPOSITORY:
  
	You have to clone the repository and get the code related to the first commit:
	
		Commit Number: c764d9c
		Commit Description: Complete single-player 8 Bricks Defense game

	If you are not interested to do the tutorial, just import the package 8BricksDefense.unitypackage and you are good to go.

  3. IMPORT YourNetworkTools TO YOUR PROJECT, PREPARE YOUR FRAMEWORK AND TEST CONNECTION:

		Import the free package that contains the basic functionallity of the network tools:
		https://assetstore.unity.com/packages/tools/animation/itween-84
		
		Follow the instruction of the video tutorial:
		https://www.youtube.com/watch?v=CxOC6bri6GU
		
  4. USE THE NETWORKS EVENTS TO CONTROL THE MAIN STATE OF THE GAME:
  
		Thanks to the network events you will be able to control the game state. 
		
		Follow the video tutorial:
		https://www.youtube.com/watch?v=y_nz60ttO2w

		If you are not into game programming but network programming this is 
		a good solution if you want an state-based machine that synchronizes
		multiple clients.
		
  5. MANAGE THE CONNECTION OF THE MULTIPLE CLIENTS TO THE GAME
	
		We will start first with the code related to manage the multiple players in the game.
		
		Follow the video tutorial:
		https://www.youtube.com/watch?v=NP25_knBsfw
		
  6. WE WILL REPLACE THE CALL TO THE SINGLETON TO CREATE A SHOOT BY NETWORK Event
  
		All the players have the hability to shoot so the shoots will be created by a network event.
		
		Follow the video tutorial:
		https://www.youtube.com/watch?v=aTyZX7uYvy8

  7. CREATION OF THE ENEMIES IN THE MASTER PLAYER
  
		Only the master player will be able to send a network event to create the enemies.
		
		Follow the video tutorial:
		https://www.youtube.com/watch?v=felPzceK1Qw
		
  8. MASTER CREATES THE POWER UPS AND ANY PLAYER CAN CONSUME THEM

		The master is reponsible for the creation of the powerups through network events 
		and the players will send network events to consume them.
	
		Follow the video tutorial:
		https://www.youtube.com/watch?v=D4TYjAoO-Hc
  
  9. EXPLOSIONS CAN MODIFY THE LAYOUT AND KILL INSTANTLY ENEMIES
  
		We have to manage the explosions in order to destroy the level layout 
		and all the enemies in its range.
		
		Follow the video tutorial:
		https://www.youtube.com/watch?v=i7Fhz_wEVaw

  10. EVENT TO CREATE DEFENSE TOWERS
  
		Each player can send a network event to create a tower.
		
		Follow the video tutorial:
		https://www.youtube.com/watch?v=H7aSliHznr4
