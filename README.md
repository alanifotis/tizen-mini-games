# Kids Mini-Games Tizen App

A collection of classic arcade and puzzle mini-games designed for kids, built using **Tizen .NET (NUI)** and successfully tested on Samsung Tizen OS 9 hardware (Model: `GQ55Q60DAUXZG`).

## Description

This project bundles multiple interactive games into a single Tizen application tailored for Samsung smart displays and TVs. It features a card reveal memory game, Snake, Tetris, and a Galaxy Shooter, complete with custom remote-control support, secret cheat-code navigation, and multi-screen routing.

## Included Mini-Games

* **Card Reveal Memory Game**


* **Tetris**


* **Snake**


* **Galaxy Shooter (Invaders)**



## Prerequisites

* Visual Studio Code installed.
* Tizen .NET / Visual Studio Code Tizen Extension configured.
* Samsung Tizen TV / Device (tested on Tizen OS 9, Model: `GQ55Q60DAUXZG`) with Developer Mode enabled.

## Build & Installation Guide

1. **Clone or Download the Project:**
Open your terminal and clone the repository.
2. **Open in VS Code:**
Open the project folder inside Visual Studio Code where the Tizen extension is installed.
3. **Build & Install via Tizen Extension:**
* Use the Tizen VS Code extension tools to build the package (`.tpk`).
* Connect to your target Samsung device (`GQ55Q60DAUXZG`) using its IP address via the Tizen Device Manager.
* Run / Install the package directly to your TV.



## Secret Cheat Codes

During execution, you can use the TV remote numbers to instantly switch between specific screens:

* Press `1`, `2`, `3`, `4` sequentially -> Jump to **Tetris**


* Press `9`, `8`, `7`, `6` sequentially -> Jump to **Galaxy Shooter (Invaders)**


* Press `5`, `6`, `7`, `8` sequentially -> Jump to **Snake**