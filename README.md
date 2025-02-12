# Blazor WASM + JWT Web API => Docker

A **step-by-step guide** on **containerizing** a **Blazor WebAssembly** and a **.NET Web API with JWT Authentication**.

At the time of this writing, I was working on a project in which a **.NET Web API** was consumed by a **Blazor WebAssembly** application.

The API is protected by **JSON Web Tokens authentication (JWT)** and both the **Blazor WASM app** and the **.NET Web API** had a reference on the same **Shared Class Library**.

In the accompanying **GitHub Repo**, in the Start folder, you find the same setup of a **.NET Web API protected by JWT authentication** where the API is consumed by a **Blazor WebAssembly application**. In the End Folder you find the final version, after all the steps are implemented.

To focus on the **Project's Containerization** and keep things simple, **only the Register and Login functionality** are present.

## Goal: Get the Latest Project Status with a Single Command

**Dockerize** the complete project, so that **project owners can always see the latest status** of the project,
by just entering a single ``docker compose up`` command via the Terminal.

This journey was more difficult than I expected and that's why I decided to write an article about it—**to organize my thoughts and provide a reference for others** who may encounter the same problems.

## Requirements

- Windows 11
- .NET 9.0 SDK
- Docker Desktop
- Git
- Azure subscription (or the possibility to create a Database on a remote SQL Server)
- Postman or Insomnia
- Notepad++

## What is Docker?

Docker is a platform for developing, shipping, and running applications inside lightweight, portable containers.
These containers can be started in seconds.

### Docker Image

To run a Docker Container you first need to create a **Docker Image**.

You create a **Docker Image** using a Dockerfile. A **Dockerfile** is a file with **step-by-step instructions** on how to create a **Docker Image** of your application.

You can store your Docker Images in an Image Repository, like **Docker Hub**, and share them with others.

### Docker Container

When you run a Docker Image a **Docker Container** is created.

A **Docker container** is a lightweight, standalone, and executable package,
that includes everything needed (application code, runtime, libraries, and dependencies) to run an application.

### Some useful Docker Commands

```bash
# create a Docker Image
docker build -t <image-name>:<tag> . 
# show list of Docker Images
docker images
# remove the Docker Image by its id
docker rmi <image-id>
# create and start a Docker container from a Docker Image
docker run -p [external port]:[internal port] --name <my-container> <image-name>:<tag>
# show list of running Docker containers
docker ps
# start a docker container by its id
docker start <docker-id>
# stop a docker container by its id
docker stop <docker-id>
```

## Containerize .NET Web API

### Step 1: Create a Docker Image for the .NET Web API

Open your code editor and add a Dockerfile to the Web API project.
I used **JetBrains Rider** to add the **Dockerfile**, and here is the resulting file.

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DotNet.JwtWebApi/DotNet.JwtWebApi.csproj", "DotNet.JwtWebApi/"]
COPY ["DotNet.Shared/DotNet.Shared.csproj", "DotNet.Shared/"]
RUN dotnet restore "DotNet.JwtWebApi/DotNet.JwtWebApi.csproj"
COPY . .
WORKDIR "/src/DotNet.JwtWebApi"
RUN dotnet build "DotNet.JwtWebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DotNet.JwtWebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotNet.JwtWebApi.dll"]

```

After your Dockerfile is created you want to build the Docker Image.
Open a Terminal in the root of the Web API project and run the command below.

```bash
docker build -t imagename-webapi:latest .
```

#### PROBLEM 1: COPY failed DotNet.JwtWebApi.csproj: not found

ERROR: failed to solve: failed to compute cache key: failed to calculate checksum of ref xzebf1n4v07hubsvxjhnei0p5::ueacb5w3fxctxf1m7dumbzbyf:
"/DotNet.JwtWebApi/DotNet.JwtWebApi.csproj": not found

As you can see, the DotNet.JwtWebApi.csproj cannot be found.
This is because of the **docker build command** where the **final dot** the context specifies.
In this case, the context is the root of the Web API project.

To solve the problem, you would typically remove 'DotNet.JwtWebApi/' from the COPY line.
Although this solves the problem, you will immediately encounter the next error.

Open a Terminal in the root of the Web API project and run the command below.

```bash
docker build -t imagename-webapi:latest .
```

ERROR: failed to solve: failed to compute cache key: failed to calculate checksum of ref xzebf1n4v07hubsvxjhnei0p5::4c6vv3fgpckdz0qvhotx557sf:
"/DotNet.Shared/DotNet.Shared.csproj": not found

The first problem has been solved, but now the Shared.csproj cannot be found.

This time, removing 'DotNet.JwtWebApi/' at the beginning of the COPY line does not help.

The **Docker context** (the final dot of the docker build command) is at the **Web API root level**.
Whereas the Shared.csproj file is in the root of the DotNet.Shared project and Docker is not able to find the Shared.csproj file.

I tried to navigate to the correct path location, by using relative paths, absolute paths, etc... but without success.
After searching for solutions, I decided to take a different approach.

#### SOLUTION 1: Create a DockerWebApi folder

The solution I came up with, was to create a DockerWebApi folder in the root of the solution of the WebApi project.
Once the DockerWebApi folder is created, copy/paste the Dockerfile into this folder.

Do not forget to re-add "DotNet.JwtWebApi/" again to the COPY line.

Open a Terminal in the parent folder of DockerWebApi and run the Docker build command to create the Image.

```bash
docker build -t imagename-webapi:latest -f DockerWebApi/Dockerfile .
```

This time, the Docker Image was created, and you can see the image in docker.desktop or by running the `docker images` command.

![Docker Images](Images/docker_images.png)

### Step 2: Create the Docker Container for the .NET Web API

We created the Docker Image in the previous step, now it is time to create and start the Docker Container by running the Image.
Open a Terminal and enter the command below:

```bash
docker run imagename-webapi
```

#### PROBLEM 2: appsettings.Production.json - FileNotFoundException

After running the command above, an Unhandled FileNotFoundException is thrown.
This is correct behavior, in the publishing step of the **DockerFile** **BUILD_CONFIGURATION=Release** is specified,
which **defaults to the Production environment**. In the project directory, there is no appsettings.Production.json file.

```bash
Unhandled exception. System.IO.FileNotFoundException: The configuration file 'appsettings.Production.json' was not found and is not optional. The expected physical path was '/app/appsettings.Production.json'.
   at Microsoft.Extensions.Configuration.FileConfigurationProvider.Load(Boolean reload)
   at Microsoft.Extensions.Configuration.ConfigurationManager.AddSource(IConfigurationSource source)
   at Microsoft.Extensions.Configuration.ConfigurationManager.Microsoft.Extensions.Configuration.IConfigurationBuilder.Add(IConfigurationSource source)
   at Program.<Main>$(String[] args) in /src/DotNet.JwtWebApi/Program.cs:line 8
```

#### SOLUTION 2

The solution would be to add an **appsettings.Production.json** file to the **Web API project**.
In this case, the Environment is not the Production environment, and I want to give the Environment another name.
See the next step!

### Step 3: Specify Environment at Container Start

In Step 1 we created the Docker Image first, ran the Image, and created/started the Docker Container after.
Let's run the Docker Image again and specify the Environment: DockerStatusEnv,

Open a Terminal in the root of the project and execute the command below:

```bash
docker run --env ASPNETCORE_ENVIRONMENT=DockerStatusEnv imagename-webapi
```

#### PROBLEM 3: appsettings.DockerStatusEnv.json - FileNotFoundException

```bash
Unhandled exception. System.IO.FileNotFoundException: The configuration file 'appsettings.DockerStatusEnv.json' was not found and is not optional.
```

#### SOLUTION 3: add appsettings.DockerStatusEnv.json file to the Web API project

This is the same FileNoFoundException as before, but this is the Exception we want.

We can now tell Docker to use the specific **appsettings.DockerStatusEnv.json** file.
The only thing to do is to copy/paste the **appsettings.Development.json** and name it **appsettings.DockerStatusEnv.json**

After you copy/paste the file, open a Terminal and run the `docker run` command below. FileNotFoundException again!  

```bash
docker run --env ASPNETCORE_ENVIRONMENT=DockerStatusEnv imagename-webapi
```

#### PROBLEM 4: appsettings.DockerStatusEnv.json - FileNotFoundException

As you can see, Docker can still not find the **appsettings.DockerStatusEnv.json**.
Although the **app settings file** is in the **Web API project**, it is **NOT in the Docker Image**.

```bash
Unhandled exception. System.IO.FileNotFoundException: The configuration file 'appsettings.
DockerStatusEnv.json' was not found and is not optional. The expected physical path was '/app/appsettings.DockerStatusEnv.json'.
```

The reason is that the configuration file we added to the project is not in the Docker Image yet.

#### SOLUTION 4: Regenerate the Docker Image

The solution to the above problem is rather simple.
Open a Terminal and run the `docker build` command again to generate a new Docker Image.

```bash
docker build -t imagename-webapi:latest -f DockerWebApi/Dockerfile .
```

### Step 4: Start the Docker Container from the newly created Docker Image

We just generated a new Docker Image. Now, it is time to start the Docker Container again.

```bash
docker run --env ASPNETCORE_ENVIRONMENT=DockerStatusEnv imagename-webapi
```

Finally, The **Web API Docker Container is up and running**, but another problem arises.  

![Docker Container](Images/docker_container_webapi_no_portforwarding.png)

#### PROBLEM 5: Container running, but unreachable from the outside world

Although the Docker Container is running, see image above, you can not reach it by navigating to <http://localhost:8080>

#### SOLUTION 5: Publishing ports

From the docker docs: Publishing a port provides the ability to break through a little bit of networking isolation by setting up a forwarding rule.
As an example, you can indicate that requests on your host’s port 5000 should be forwarded to the container’s port 8080.
Publishing ports happens during container creation using the -p (or --publish) flag with docker run.

The syntax is: `docker run -d -p HOST_PORT:CONTAINER_PORT nginx`

In this case, I would like to use the port **7177** specified in **Properties/launchSettings.json** file of the Web API project.

```bash
docker run -p 7177:8080 --env ASPNETCORE_ENVIRONMENT=DockerStatusEnv imagename-webapi
```

You can reach the **Web API Docker Container** on your local machine by navigating to <http://localhost:7177/weatherforecast>

![Weather forecast](Images/docker_container_webapi_weatherforecast.png)

### Step 5: Register a User in the running Web API Docker Container

In the step above, we **forwarded the Docker Container Port to a Port on our local machine**,
and we reached the **WeatherController** in the **Web API Docker Container** and **received Weather Data in JSON format**.

Next, we will try out the **Register a User** by sending a **Register Request** to the Web API Docker Container.
Open **Postman** or **Insomnia**, and make a **Post Request** to the <http://localhost:7177/api/account/register> URL. **Kaboom!**

![Register User](Images/internal_server_error_register_user.png)

#### PROBLEM 6: 500 Internal Server Error - LocalDB is not supported on this platform

```bash
 System.PlatformNotSupportedException: LocalDB is not supported on this platform.
         at Microsoft.Data.SqlClient.SNI.LocalDB.GetLocalDBConnectionString(String localDbInstance)
         at Microsoft.Data.SqlClient.SNI.SNIProxy.GetLocalDBDataSource(String fullServerName, Boolean& error)
         at Microsoft.Data.SqlClient.SNI.SNIProxy.CreateConnectionHandle(String fullServerName, ...
```

When you send the **Register Request** to the **Web API Docker Container** the **RegisterController** is reached.
The code in the controller gets executed but throws an exception when trying to insert the newly created user in the Local Database.

In **Step 3**, we copy/paste the **appsettings.Development.json** and name it **appsettings.DockerStatusEnv.json** but
the **Database Connection string** is still the same and Docker tries to make a connection to your **Local Machine's Database**.

As you can read in the exception message, **LocalDB is not supported on this platform**.
Actually, we don't want docker uses the LocalDB on the Local Machine. We want Docker to use a SQL Server Database on a Remote Machine.

#### SOLUTION 6: Create a remote SQL database in Azure (or somewhere else)

Open the **Azure Portal** and Log in. Click on the **Create Resource** button and Search for **SQL database**.
Click on the **Create SQL database** and follow the instructions to create an **SQL database server** and an **SQL database**.

**IMPORTANT**: Choose **Use SQL Authentication** when creating the **SQL database server**,
and write down the **Server admin login** and **Password** as you will need them later.

Give the **SQL database** the name: **DotNetDb**.

After the SQL database and SQL Server have been created, you need to *Add your client IPv4 address* as a **Firewall rule**
In Azure go to the **SQL database server you created**. In the **Security menu**, click on the **Networking sub menu**.
In the **Public Access** Tab, enable **Selected networks**. At the **Firewall rules** section, click on the **Add your client IPv4 address (your IP address)** button and click Save.  

Next, Go to the **SQL database**, and click on **Show database Connection strings**. Copy the **ADO SQL Authentication Connection string**  for later use.

### Step 6: Update Connection string in appsettings.Development.json file - Update Database

Temporarily change the Connection string in the appsettings.Development.json file to the copied Connection string.
IMPORTANT: **Do not forget to update the Password**.

```bash
Server=tcp:yourservername.database.windows.net,1433;Initial Catalog=DotNetDb;Persist Security Info=False;User ID=yourserveradminlogin;Password=yourpassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Open a **Terminal** at the root of the **DotNet.JwtWebApi project** and run the command below to **update the SQL Database in Azure**.
When the Database update is successful, change the Connection string back to its original value.

```bash
dotnet ef database update
```

#### PROBLEM 7: Microsoft.Data.SqlClient.SqlException (0x80131904): Reason: An instance-specific error occurred while establishing a connection to SQL Server

You would encounter this exception when you forgot to **Add your client IPv4 address (your IP address)** in your **SQL Database server**

Microsoft.Data.SqlClient.SqlException (0x80131904): Reason: An instance-specific error occurred while establishing a connection to SQL Server.
Connection was denied since Deny Public Network Access is set to Yes. ...

### Step 5: Update Connection string in appsettings.DockerStatusEnv.json file - Register User

Update the **Connection string** in the **appsettings.DockerStatusEnv.json** file.

```bash
Server=tcp:yourservername.database.windows.net,1433;Initial Catalog=DotNetDb;Persist Security Info=False;User ID=yourserveradminlogin;Password=yourpassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

The **Connection string** in the **appsettings.DockerStatusEnv.json** file has been updated.

Because we changed the appsettings.DockerStatusEnv.json file in the project, we need to apply this change to the Docker Image by creating a new Image.
Open a Terminal in the root of solution and run the command below:

```bash
docker build -t imagename-webapi:latest -f DockerWebApi/Dockerfile .
```

After the Docker Image has been created, Run the Docker Image as a Docker Container by executing this command:

```bash
docker run -p 7177:8080 --env ASPNETCORE_ENVIRONMENT=DockerStatusEnv imagename-webapi
```

#### PROBLEM 8: There is already a Docker Container running on Port 7177

docker: Error response from daemon:
driver failed programming external connectivity on endpoint gifted_babbage (a2e4ab415d8797edb42c54da23002c37e7224471c1b03add5ffe68fc8a20acfe):
Bind for 0.0.0.0:7177 failed: port is already allocated.

#### SOLUTION 8: Stop the running Container

Open a Terminal and run `docker ps` to see all the running containers.

![Running Containers](Images/overview_running_containers.png)

To stop a running container, use the **docker stop** command and provide the container ID, in this case.

```bash
docker stop cc38db1cccc9
```

The old running container is stopped, execute the command below to run the new docker container.

```bash
docker run -p 7177:8080 --env ASPNETCORE_ENVIRONMENT=DockerStatusEnv imagename-webapi
```

### Step 7: Register a User in the running Web API Docker Container

In the step above, we created and updated a remote SQL Database in Azure.
And, we generated a new Docker Image and the new Docker Container is running on <http://localhost:7177/weatherforecast>

We can reach the **WeatherController** in the **Web API Docker Container** because we **receive Weather Data in JSON format**.

Next, we will try out the **Register a User** by sending a **Register Request** to the Web API Docker Container.
Open **Postman** or **Insomnia**, and make a **Post Request** to the <http://localhost:7177/api/account/register> URL.

![Register User](Images/statuscode_200_register_user.png)

Finally, the hard work pays off! We successfully registered a user in our running Docker Container.  

### Step 8: Test User Login and obtain Access and Refresh token

Open **Postman** or **Insomnia**, and make a **Post Request** to the <http://localhost:7177/api/account/login> URL.

![Login User](Images/statuscode_200_login_user.png)

The login functionality works as expected, and we receive the Access- and Refresh tokens needed to access the JWT-protected Web API.

## Containerize a Blazor WebAssembly application

The API is up and running. Now it's time to Containerize the Blazor WebAssembly application.

### Step 1: Create a Docker Image for the Blazor WebAssembly application

Open your code editor and add a Dockerfile to the Blazor WebAssembly project.
I used JetBrains Rider to add the Dockerfile, and below is the file that was generated.

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DotNet.BlazorWasmApp/DotNet.BlazorWasmApp.csproj", "DotNet.BlazorWasmApp/"]
COPY ["DotNet.Shared/DotNet.Shared.csproj", "DotNet.Shared/"]
RUN dotnet restore "DotNet.BlazorWasmApp/DotNet.BlazorWasmApp.csproj"
COPY . .
WORKDIR "/src/DotNet.BlazorWasmApp"
RUN dotnet build "DotNet.BlazorWasmApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DotNet.BlazorWasmApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotNet.BlazorWasmApp.dll"]
```

After your Dockerfile is created you want to build the Docker Image.
Open a Terminal in the root of the Blazor Web Assembly project and run the command below.

```bash
docker build -t imagename-wasm:latest .
```

#### PROBLEM 9: COPY failed DotNet.BlazorWasmApp.csproj: not found

ERROR: failed to solve: failed to compute cache key: failed to calculate checksum of ref xzebf1n4v07hubsvxjhnei0p5::iw3202cg2dqjiyey8q031dz9t:
"/DotNet.BlazorWasmApp/DotNet.BlazorWasmApp.csproj": not found

As you can see, the DotNet.BlazorWasmApp.csproj cannot be found.
This is because of the **docker build command** where the **final dot** the context specifies.
In this case, the context is the root of the BlazorWasmApp project.

To solve the problem, you would typically remove 'DotNet.BlazorWasmApp/' from the COPY line.
Although this solves the problem, you will immediately encounter the next error.

Open a Terminal in the root of the BlazorWasmApp project and run the command below.

```bash
docker build -t imagename-wasm:latest .
```

ERROR: failed to solve: failed to compute cache key: failed to calculate checksum of ref xzebf1n4v07hubsvxjhnei0p5::4c6vv3fgpckdz0qvhotx557sf:
"/DotNet.Shared/DotNet.Shared.csproj": not found

The first problem has been solved, but now the Shared.csproj cannot be found.

This time, removing 'DotNet.BlazorWasmApp/' at the beginning of the COPY line does not help.

The **Docker context** (the final dot of the docker build command) is at the **Blazor WebAssembly root level**.
Whereas the Shared.csproj file is in the root of the DotNet.Shared project and Docker is not able to find the Shared.csproj file.

I tried to navigate to the correct path location, by using relative paths, absolute paths, etc... but without success.
After searching for solutions, I decided to take a different approach.

#### SOLUTION 9: Create a DockerWasm folder

The solution I came up with, was to create a DockerWasm folder in the root of the solution.
Once the DockerWasm folder is created, copy/paste the Dockerfile into this folder.

Do not forget to re-add "DotNet.BlazorWasmApp/" again to the COPY line.

Open a Terminal in the root of the solution and run the Docker build command to create the Image.

```bash
docker build -t imagename-wasm:latest -f DockerWasm/Dockerfile .
```

This time, the Docker Image was created, and you can see the image in docker.desktop or by running the `docker images` command.

![Docker Images](Images/docker_images_with_wasm.png)

### Step 2: Create the Docker Container for the Blazor WebAssembly application

We created the Docker Image in the previous step, now it is time to create and start the Docker Container by running the Image.
Open a Terminal and enter the command below:

```bash
docker run imagename-wasm
```

#### PROBLEM 10: The command could not be loaded, possibly because

After running the command above, this message with 2 possible reasons, is shown in the terminal.
We can exclude the last one because we would have had this problem before when working on the Web API container.

Why is Docker showing the message: "The application 'DotNet.BlazorWasmApp.dll' does not exist"?

```bash
C:\>docker run imagename-wasm
The command could not be loaded, possibly because:
  * You intended to execute a .NET application:
      The application 'DotNet.BlazorWasmApp.dll' does not exist.
  * You intended to execute a .NET SDK command:
      No .NET SDKs were found.

Download a .NET SDK:
https://aka.ms/dotnet/download

Learn about SDK resolution:
https://aka.ms/dotnet/sdk-not-found
```

#### SOLUTION 10: use the NGINX Docker Image as final image for the Dockerfile

Blazor WebAssembly produces static files when published and there is no need for the ASP.NET Core runtime to serve these files.
Instead of the ASP.NET Core runtime Docker Image as the base for the final image, do we need another final image to serve our files?  

NGINX to the rescue!

NGINX is a free and open-source web server that can be used to serve static content and, there is also a Docker Image available.

Update the Dockerfile in the DockerWasm folder, like the file below.

```dockerfile
FROM nginx:alpine AS nginxbase

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["DotNet.BlazorWasmApp/DotNet.BlazorWasmApp.csproj", "DotNet.BlazorWasmApp/"]
COPY ["DotNet.Shared/DotNet.Shared.csproj", "DotNet.Shared/"]

RUN dotnet restore "DotNet.Shared/DotNet.Shared.csproj"
RUN dotnet restore "DotNet.BlazorWasmApp/DotNet.BlazorWasmApp.csproj"
COPY . .

WORKDIR "/src/DotNet.BlazorWasmApp"
RUN dotnet build "DotNet.BlazorWasmApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DotNet.BlazorWasmApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

RUN ls --recursive /src/DotNet.BlazorWasmApp/

FROM  nginxbase AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY  DotNet.BlazorWasmApp/nginx.conf /etc/nginx/nginx.conf
```

Because we have changed the Dockerfile, we now have to rebuild the Docker Image.

Open a Terminal in the parent folder of the DockerWasm folder and run the Docker build command to create the Image.

```bash
docker build -t imagename-wasm:latest -f DockerWasm/Dockerfile .
```

#### PROBLEM 11: file "/DotNet.BlazorWasmApp/nginx.conf": not found

```bash
ERROR: failed to solve: failed to compute cache key: failed to calculate checksum of ref xzebf1n4v07hubsvxjhnei0p5::jzwwhgfipwbhnm3khwghbk1zh: 
"/DotNet.BlazorWasmApp/nginx.conf": not found
```

#### SOLUTION 11: Add a nginx.conf file to the DotNet.BlazorWasmApp folder

Add a nginx.conf file with the content below to the DotNet.BlazorWasmApp folder.

```nginx configuration
events{}
http{
    include /etc/nginx/mime.types;
    server {
        listen 80;
        server_name localhost;
        root /usr/share/nginx/html;
        index index.html;
        location / {
            try_files $uri $uri/ /index.html;
        }
    }
}
```

Open a Terminal in the root of the solution and run the Docker build command to create the Image.

```bash
docker build -t imagename-wasm:latest -f DockerWasm/Dockerfile .
```

After the Image has been created, start a Docker Container by executing the command below:

```bash
docker run -p 7248:80 imagename-wasm 
```

#### PROBLEM 12: nginx: unknown directive "events" in /etc/nginx/nginx.conf:1

This error message is a bit confusing because the file content is correct.
The problem has something to do with the encoding of the file. In my case, the encoding was UTF-8.

#### SOLUTION 12: Change the Encoding of the nginx.conf file

Open the nginx.conf file in Notepad++ and change its encoding to ANSI.
After you change the encoding run the Docker build command again to create the Image.

```bash
docker build -t imagename-wasm:latest -f DockerWasm/Dockerfile .
```

After the Image has been created, start a Docker Container by executing the command below:

```bash
docker run -p 7248:80 imagename-wasm 
```

Open a browser and navigate to the <http://localhost:7248/account/login> URL.
Congratulations! The Login page of the Blazor WebAssembly application is displayed in the browser.

![Login screen](Images/login_screen.png)

### Step 3: Try the Register and the Login Functionality

Click the [Sign up](http://localhost:7248/account/register) link and try to Register a new user.

#### PROBLEM 13: An unhandled error has occurred. Reload

Open the Developers Tools of the Browser (in Chrome click F12) and check the Console window.

The problem is that the register request is sent to <https://localhost:7177/api/account/register>,
but the Web API is only reachable on <http://localhost:7177>

![SSL Protocol Error](Images/err_ssl_protocol_error.png)

#### SOLUTION 13

The port <https://localhost:7177> is specified in the **appsettings.json** file from the **Blazor WebAssembly** project,
and a solution could be to change it there.

This is not what I want, because I don't want to change the values in the appsettings.json file. Let's try something else.

Here, I first tried to pass DockerStatusEnv as an Environment variable at the start of the container, as we did before
Then Docker could read the correct values from the appsettings.DockerStatusEnv.json file in the Blazor WebAssembly app.
For some obscure reason, this didn't work ... Let's try plan B.

In my search to find a solution for the problem, I came across the article [How to use Docker environment variables for Blazor WebAssembly](https://medium.com/@yoann.visentin/blazor-webassembly-docker-environment-variables-and-appsettings-json-3106dfedff90)
The author of the article used another approach and added a little script to the root of the Blazor WebAssembly project.

Docker **executes the script at every startup** of the container. The **script changes the values of the appsettings.json** file in the Docker container,
and the values in the appsettings.json file in the repo stay unchanged.

I will implement his solution in the next step.

### Step 4: Add script.sh and execute script at Container Start-up

First, create a **script.sh** file at the root of the **Blazor WebAssembly project** with the following contents.

```shell
cat /usr/share/nginx/html/appsettings.json | jq --arg aVar "$(printenv ApiUrl)" '.ApiUrl = "http://localhost:7177"' > /usr/share/nginx/html/appsettings.json
cat /usr/share/nginx/html/appsettings.json | jq --arg aVar "$(printenv Jwt:ValidAudience)" '.Jwt.ValidAudience = "http://localhost:7248"' > /usr/share/nginx/html/appsettings.json
cat /usr/share/nginx/html/appsettings.json | jq --arg aVar "$(printenv Jwt:ValidAudience)" '.Jwt.ValidIssuer = "http://localhost:7177"' > /usr/share/nginx/html/appsettings.json
```

We also need to update our Dockerfile in the DockerWasm directory, because the **jq library** needs to be installed.
(jq is a lightweight and flexible command-line JSON processor)

At the end of the Dockerfile, you can find 2 new lines added.

```bash
RUN apk add jq
COPY ["DotNet.BlazorWasmApp/script.sh", "/docker-entrypoint.d/40-script.sh"]
```

Below is the updated and final Dockerfile.

```dockerfile
FROM nginx:alpine AS nginxbase
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["DotNet.BlazorWasmApp/DotNet.BlazorWasmApp.csproj", "DotNet.BlazorWasmApp/"]
COPY ["DotNet.Shared/DotNet.Shared.csproj", "DotNet.Shared/"]

RUN dotnet restore "DotNet.Shared/DotNet.Shared.csproj"
RUN dotnet restore "DotNet.BlazorWasmApp/DotNet.BlazorWasmApp.csproj"
COPY . .

WORKDIR "/src/DotNet.BlazorWasmApp"
RUN dotnet build "DotNet.BlazorWasmApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DotNet.BlazorWasmApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM  nginxbase AS final
WORKDIR /usr/share/nginx/html

RUN apk add jq
COPY ["DotNet.BlazorWasmApp/script.sh", "/docker-entrypoint.d/40-script.sh"]
COPY --from=publish /app/publish/wwwroot .
COPY  DotNet.BlazorWasmApp/nginx.conf /etc/nginx/nginx.conf
```

Because we have added a script and updated the Dockerfile, we need to regenerate the Docker Image.
Open a Terminal at root of the solution and run the command below:

```bash
docker build -t imagename-wasm:latest -f DockerWasm/Dockerfile .
```

After the Image has been created, start a Docker Container by executing the next command.

```bash
docker run -p 7248:80 imagename-wasm 
```

Everything seems to work, and we can navigate to the <http://localhost:7248/account/login> URL.
The Login page of the Blazor WebAssembly application is displayed in the browser.

A user can log in, and we receive the **Access- and Refresh token** needed **to access the JWT protected Web API**.

### Step 5: User not Authenticated -IDX10206: Unable to validate audience

There is still one problem. After a successful login, the user is redirected to the home page of the application,
but it looks like he isn't authenticated because the Register- and Login links are still visible.

#### Problem 14: IDX10206: Unable to validate audience. The 'audiences' parameter is empty

Open the Developer tools (click F12) in the browser, and a little error message appears: IDX10206: Unable to validate audience. The 'audiences' parameter is empty.

The IDX10206 error occurs during JWT token validation when the audience claim cannot be properly validated.
This typically happens due to either incorrect token creation, or improper validation configuration.

In our case, it is an improper validation configuration. In the appsettings.DockerStatusEnv.json you find the JWT configuration settings.
When you have a close look, you can see we still use https instead of http in the URLs.

#### Solution 14: change https to http in the appsettings.DockerStatusEnv.json file

Below is the updated and final version of the appsettings.DockerStatusEnv.json file

```bash
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:dotnetdockerserver.database.windows.net,1433;Initial Catalog=DotNetDb;Persist Security Info=False;User ID=serveradmin;Password=Server2008!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "Jwt": {
    "ValidAudiences": [
      "http://localhost:7177",
      "http://localhost:7248"
    ],
    "ValidIssuer": "http://localhost:7177",
    "SecurityKey": "k2uEmrgwWSW34hofdX5jzJ0PNw6Qsnzm",
    "AccessTokenExpiryInSeconds": 3600,
    "RefreshTokenExpiryInHours" : 24
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ASPNETCORE_ENVIRONMENT": "Development"
}
```

First, Let's clean up our Docker environment by removing all the running and stopped containers from the previous steps.

```bash
# Display all containers with their status
docker ps -a
## Remove a stopped or running container by its ID. Do this for all the containers
docker remove -f <docker-id>
```
A
As the appsettings.DockerStatusEnv.json file has changed we need to regenerate the Docker Image.
Open a Terminal and run the `docker build` command again.

```bash
# if needed regenerate the Docker Image for the Blazor WebAssembly 
# docker build -t imagename-wasm:latest -f DockerWasm/Dockerfile .  
docker build -t imagename-webapi:latest -f DockerWebApi/Dockerfile .
```

After the cleanup and the Docker Image creation of the Web API, we start both the BlazorWasm and the Web API Docker Containers.
Open a Terminal and run the following commands:

```bash
docker run -p 7177:8080 --env ASPNETCORE_ENVIRONMENT=DockerStatusEnv imagename-webapi
docker run -p 7248:80 imagename-wasm
```

When we open a browser and navigate to the <http://localhost:7248/account/login> the Login page of the application is displayed and,
 a user can log in and the user is redirected to the home page of the application,

This time, the **authentication** works as expected, the **Login and Register links** are not there anymore,
instead, you can see the Logout button and the username.

![Containerize DotNET Final](Images/containerise_dotnet_final.png)

### Step 6:  Update and Run all the Containers at once - Docker Compose

The **.NET Web API** and the **Blazor WebAssembly** application are up and running. We can register as a user, log in as a user, log out as a user.

There is still something we can improve. When our code has changed, we wrote a new feature or fixed a bug, and we want the latest version,
we need to stop the running Docker containers. Then we need to rebuild the Docker Images.
Finally, we need to restart the Docker containers. This means a user needs to enter 7 commands in the Terminal.

```bash
# Stop Docker containers
docker ps
docker stop <docker-id-webapi> 
docker stop <docker-id-wasm> 

# Generate Docker Images
docker build -t imagename-wasm:latest -f DockerWasm/Dockerfile .
docker build -t imagename-webapi:latest -f DockerWebApi/Dockerfile .

# Start Docker containers
docker run -p 7177:8080 --env ASPNETCORE_ENVIRONMENT=DockerStatusEnv imagename-webapi
docker run -p 7248:80 imagename-wasm 
```

We can replace all these statements with one statement. `docker compose up`
To do this we first need to add a **compose.yaml** file in the root of the solution.

```bash
# content of the compose.yaml file
services:
  dotnet.blazor-wasm-app:
    build:
      context: .
      dockerfile: DockerWasm/Dockerfile
    ports:
      - "7248:80"
    container_name: ctr-blazor-wasm-app
  
  dotnet.jwt-web-api:
   build:
     context: .
     dockerfile: DockerWebApi/Dockerfile
   ports:
     - "7177:8080"
   environment:
     - ASPNETCORE_ENVIRONMENT=DockerStatusEnv
   container_name: ctr-jwt-web-api
```

In this file you find 2 services: The **Blazor WebAssembly service** and the **Web API service**.

When you run the `docker compose up` command, Docker searches per service for the correct Dockerfile.
It also forwards the **Host port** to the correct **Container port** and the correct **environment variables** are set.

The only thing we need to do is to open a Terminal in the root of the solution and run the command below:

```bash
# docker compose up --build --force-recreate --no-deps -d dotnet.blazor-wasm-app dotnet.jwt-web-api
docker compose up --build --force-recreate --no-deps -d
```




