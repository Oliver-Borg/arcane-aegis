# Arcane Aegis

## Installation
Download a build from [builds](https://uctcloud-my.sharepoint.com/%253Af%253A/g/personal/wrtcam003_myuct_ac_za/EmVK3yRCfYNJnNWZ1AFtf8UBHIO28m_FMOpw0gGkEZddWA?e%253DqWalhQ) and run ArcaneAegis.exe.

## Build from source
Clone this [repo](https://github.com/Oliver-Borg/arcane-aegis)

Download the packages from [packages](https://uctcloud-my.sharepoint.com/%253Af%253A/g/personal/wrtcam003_myuct_ac_za/EhAgjH8VAuVBmAoSeMEX0r0BB7MeVwTA04UY4fDVeTtaFw?e%253DtaSOXb) and place them in ArcaneAegis/Assets/Packages.

Open the Unity Project at ArcaneAegis



## Changes since DEMO:

We have implemented most feedback items, namely:

> Player feedback when hitting monsters with spells
	~> Animated hitmarkers whose size is proporational to damage delt, and turn red when the monster is killed
	~> Hit sounds
	~> Numbers representing damaged done to monsters (red if the monster is )
	~> Blue tint covers monster when a slow effect is applied from the ice spell

> Sounds
	~> 4 sound effects per monster: Attack, hit sound, random growl/breath, death sound (Credit to Liam Dormehl and Sam Kurgan for custom goblin sound effects)
	~> round start sound
	~> time machine sound
	~> door opening sound

> End game functionality
	~> added the Time Bomb which can only be activated by collecting the tech runes (killing the warden in the future map) and the 4 catalysts (each correspond to a spell and give a huge boost to that spell)
	
> Play testing tweaks
	~> We adjusted the monster spawn rate and delays. Now monsters spawn in constantly throughout the round instead of all at once
	
> GUI
	~> home page GUI
	~> Mouse sensitivity slider
	~> GUI for assigning runes (to upgrade either damage, cooldown, or firerate) to your character through the alchemist
    ~> Visual cues for all actions
    ~> Text cues where necessary

> Map Lighting
	~> Added more lights in dark corridors
