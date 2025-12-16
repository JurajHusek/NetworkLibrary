# Network Library – Unity Networking Framework
Thir repository contains ource code for my Bachelor's project - Network solution for Unity. It includes source code of the solution Library, Unity package for importing the NetworkLibrary to any Unity projects (Unity 2019 and up), and source files for online documenation. 

## Documentation and Thesis

### Code Documentation
The complete source code documentation is available online and provides a detailed description of the library architecture, classes, and networking mechanisms.

**Documentation:**  
https://jurajhusek.github.io/NetworkLibrary/doc/annotated.html

---

### Bachelor’s Thesis
This networking library was developed as part of a bachelor’s thesis, which provides theoretical background, architectural design, implementation details, and evaluation of the solution.

**Bachelor’s Thesis:**  
https://opac.crzp.sk/?fn=detailBiblioForm&sid=BA75C2D62745AD5723EB949ED92F

**Title:** *Network Solution for Unity*  
**Author:** Bc. Juraj Hušek  
**Institution:** Faculty of Electrical Engineering and Informatics,  
Slovak University of Technology in Bratislava (FEI STU)


---

## Simple Overview (read documentation for more information)

This networking library provides a modular and extensible architecture for building multiplayer Unity applications.  
It is based on an **Event-Driven Architecture (EDA)** and separates responsibilities clearly between client-side and server-side logic.

Key goals of the library:
- Custom low-level networking
- Full control over packet structure and serialization
- Support for both TCP and UDP
- Network performance monitoring
- Easy integration into Unity scenes

---

## Key Features

- TCP and UDP communication
- Client–server architecture
- Custom packet serialization (`Packet` class)
- Networked transforms and animations
- Chat and online user list demos
- Network quality monitoring (RTT, jitter, packet loss, bandwidth, throughput)
- Basic security mechanisms (HMAC, authentication tokens)
- Multithreaded server handling

---

## Library Structure

The following overview describes the main classes and their responsibilities.

### Client-side Components

- **Client**  
  Main MonoBehaviour singleton handling the client-side networking logic (TCP, UDP).

- **ClientReceiveHandler**  
  Processes incoming packets from the server and executes client-side actions.

- **ClientSendHandler**  
  Handles sending network requests from the client to the server using TCP or UDP.

- **NetworkManager**  
  Manages all client-side networking operations, including:
  - Connection handling
  - User synchronization
  - Transform and animation updates
  - Messaging  
  This class is part of the **EDA architecture** of the library.

- **NetworkTransform**  
  Synchronizes position and rotation over the network.  
  - Local users: send updates at defined intervals  
  - Remote users: interpolate received data

- **NetworkAnimator**  
  Synchronizes animator parameters (bools, floats, triggers).  
  Sends local changes to the server and applies updates to remote clients.

- **NetworkUser**  
  Main network user component.  
  Must be attached to the user prefab.

- **NetworkMonitor**  
  Actively monitors network quality metrics:
  - Latency (RTT)
  - Jitter
  - Packet loss
  - Bandwidth
  - Throughput  
  Provides real-time updates for UI visualization.

---

### Server-side Components

- **NetworkServer**  
  Singleton MonoBehaviour that initializes and manages the server logic inside a Unity scene.

- **ServerLogic**  
  Main server logic class responsible for managing server state and operations.

- **ServerReceiveHandler**  
  Handles and processes packets received by the server.

- **ServerSendHandler**  
  Responsible for sending packets to clients based on packet type.

- **ServerSideClient**  
  Represents a connected client on the server side.  
  Contains:
  - TCP and UDP handlers
  - User state
  - Security data (HMAC, authentication tokens)

- **ServerSideClientInstance**  
  Component attached to a GameObject representing a client in the server scene.  
  Used mainly for **testing inside Unity**.

---

### Shared & Utility Components

- **Packet**  
  Core data representation class used for:
  - Reading and writing network data
  - Serialization and deserialization of primitive and complex data types

- **ThreadsController**  
  Manages multithreaded execution, mainly on the server side.

- **ClientSettings / DefaultNetworkSettings**  
  Configuration classes for client-side networking.

- **ServerSettings**  
  Configuration class for server-side networking.

---

## Technologies

- **Unity (C#)**
- **.NET Framework**
- Multithreading
- Event-Driven Architecture (EDA)

---

## Use Cases

- Multiplayer games / apps
- Real-time simulations
- Networking experiments and teaching
- Custom networking research projects

---

## Author

**Bc. Juraj Hušek**  
Faculty of Electrical Engineering and Informatics  
Slovak University of Technology in Bratislava (FEI STU)

