# Blazor WASM + JWT Web API => Docker

A **step-by-step guide** on how to **containerize** a **Blazor Webassembly** and a **.NET Web API with JWT Authentication**.

At the moment of this writing, I was working on a project where a **.NET Web API** was consumed by a **Blazor Webassembly** application.
The API was protected by **Json Web Tokens authentication (JWT)** and both the WASM and the API had a reference on the same **shared class library**.

In the accompanying **GitHub Repo** you find a **.NET Web API protected by JWT authentication**. The API is consumed by a Blazor Webassembly application.
To focus on the **Containerization of the project** and to keep things simple **only the Register and Login functionality** are implemented. 

## Goal

**Dockerize** the complete project, so that project owners could always see the latest status of their project, 
by just entering a few commands via the terminal. 

This journey was a bit more difficult than I expected. That's why I decided to write an article about it—to organize my thoughts and provide a reference for others who may encounter the same problems.

## Requirements

- Windows 11
- .NET 9.0 SDK
- Docker Desktop
- Git

## Docker

### What is Docker?

Docker is a platform for developing, shipping, and running applications inside lightweight, portable containers.
These containers can be started in seconds.

### Docker Image

To run a Docker Container you first need to create a **Docker Image**. 

You create a **Docker Image** using a Dockerfile. A **Dockerfile** is a file with the **step-by-step instructions** on how to create a **Docker Image** of your application.

You can store your Docker Images in a Image Repository, like **Docker Hub** and share them with other people.

### Docker Container

When you run a Docker Image a **Docker Container** is created. 

A **Docker container** is a lightweight, standalone, and executable package,
that includes everything needed (application code, runtime, libraries and dependencies) to run an application. 

## Step-by-step

### Step 1: Create a Docker Image for the .NET Web API


 









