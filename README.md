# Ulster Travel Kiosk Application

## Overview

The Ulster Travel Kiosk Application is a desktop application built using C#, WPF, and .NET 8. The project was developed as part of a Computing course project and simulates a self-service travel kiosk system where users can browse destinations, airports, airlines, routes, and delay prediction information through an interactive graphical interface.

The application follows a layered architecture with separate business logic and UI projects to improve maintainability, scalability, and code organisation.

---

## Features

* Browse travel destinations
* View airport and airline information
* Explore airline routes and destinations
* Delay prediction functionality
* Admin login and management screens
* Logging system for application activity tracking
* CSV-based data storage
* Modern WPF interface

---

## Screenshots

### Home Screen
![Home Screen](Screenshots/home-screen.png)

### Home Help Screen
![Home Screen](Screenshots/home-help-screen.png)

### Admin Screen
![Admin Panel](Screenshots/admin-screen.png)

## Admin Screen (Credentials)
![Admin Panel](Screenshots/admin-screen-credentials.png)

---

## Technologies Used

* C#
* .NET 8
* WPF
* XAML
* Material Design in XAML Toolkit
* FontAwesome.WPF
* CSV Data Storage
* Git & GitHub

---

## Project Structure

```bash
UlsterTravelKioskApplication/
│
├── UlsterTravelKioskApplication/        # Core business logic and services
│   ├── Models/
│   ├── Services/
│   └── Data/
│
├── UlsterTravelKioskApplication.UI/     # WPF user interface
│   ├── Screens/
│   ├── Admin/
│   └── Assets/
│
└── UlsterTravelKioskApplication.sln
```

---

## Key Components

### Models

Contains the application's data models including:

* Airlines
* Airports
* Routes
* Destinations
* Delay Predictions
* Logging
* Settings

### Services

Handles the application's business logic and data processing:

* Data management
* API processing
* Delay prediction services
* Logging services
* Password hashing
* Administrative functionality

### UI Layer

* Interactive travel kiosk screens
* Administrative management panels
* Login functionality
* Material Design styling

---

## Installation & Setup

### Requirements

* Visual Studio 2022
* .NET 8 SDK
* Windows OS

### Running the Application

#### 1. Clone the repository

```bash
git clone https://github.com/kaitlyndeschner/UlsterTravelKioskApplication.git
```

#### 2. Open the project folder

```bash
cd UlsterTravelKioskApplication
```

#### 3. Open the solution file

```bash
UlsterTravelKioskApplicationFULLPROJECT.sln
```

#### 4. Restore NuGet packages

Restore packages automatically through Visual Studio or run:

```bash
dotnet restore
```

#### 5. Build and run the application

Run the application through Visual Studio or use:

```bash
dotnet run --project .\UlsterTravelKioskApplication.UI\
```

---

## Learning Outcomes

This project helped develop skills in:

* Event-driven programming
* Desktop application development
* WPF and XAML UI design
* Application architecture
* Data management
* Git version control
* Software debugging and testing
* Working with external packages and libraries

---

## Future Improvements

Potential future enhancements include:

* Database integration using SQL Server
* API integration for live travel data
* Improved authentication and security
* Enhanced UI/UX animations and responsiveness
* Advanced analytics and reporting
* Cloud-based data storage

---

## Author

Kaitlyn Deschner

Computing Student & Graduate Software Developer

---

## License

This project was created for educational purposes.
