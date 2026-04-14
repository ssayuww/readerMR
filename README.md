link to showcase video: https://www.youtube.com/watch?v=Ne_UYBXUIcI

Overview

This project consists of two main components:

1. Reader Application

   A desktop application called reader runs in the system tray.

It allows you to:

   Activate monitoring
   Deactivate monitoring
   Exit the application

When active, it collects static and dynamic system information from the local machine and sends it to the server.

2. Unity Mixed Reality Application

   A Unity project that visualizes the data in mixed reality using Oculus Passthrough.

   For each connected device, a panel is created in the scene displaying its system information.

Data Collected

The reader gathers and sends:

   General system information
   CPU data
   Memory usage
   GPU data
   Storage information
   Network activity
   Running applications (processes)
   Features
   Reader
   Runs in system tray
   Toggle data collection on/off
   Sends real-time system data to server
   Unity MR App
   Displays devices in mixed reality
   One panel per device
   Section-based UI (General, CPU, Memory, GPU, Storage, Network, Apps)
   Scrollable content
   Movable and resizable panels
   VR interaction support
   Architecture
   Reader app collects system data
   Data is sent to the server
   Unity app retrieves device data
   A panel is created per device
   User views system information in mixed reality
   Purpose

To monitor and visualize multiple computers in real time inside a mixed reality environment.
