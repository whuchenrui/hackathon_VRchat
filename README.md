# hackathon_VRchat
Kinect + Google Cardboard to build a new fancy way to chat with each other. Not only voice, picture, but also real movement in virtual world.

## Feature
1: Player enter the virtual world by take on Google Cardboard, with mobile phone inside.  
2: Player can see himself and the another player.  
3: Player can move around, but still in the available area supported by Kinect.  
4: Real-time communication with voice and guesture.  

## Equipment
1: Windows operation system to run the server  
2: Kinect 2  
3: Google Cardboard * 2  
4: Mobile phone * 2  

## Kinect
Using Kinect SDK to capture player movement, sending body joints data to application, supporting up to two people.

## Unity
Using Unity to build 3D model and deploy to different platform, including Android and iOS.

## Communication
1: Player login from mobile app, using TCP.  
2: Server sending Kinect data to player, using UDP.  
3: Players communicate with each other, using TCP.  
