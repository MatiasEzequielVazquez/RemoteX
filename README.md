# RemoteX - Web SSH Terminal

<div align="center">

![RemoteX Banner](https://img.shields.io/badge/RemoteX-SSH_Terminal-00d9ff?style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET_10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**A modern, browser-based SSH client with Tron Legacy aesthetic**

[Features](#-features) • [Demo](#-demo) • [Installation](#-installation) • [Usage](#-usage) • [Architecture](#-architecture)

</div>

---

## About

RemoteX is a web-based SSH terminal that allows you to connect to remote servers directly from your browser. Built with ASP.NET Core and SignalR for real-time communication, it features a sleek interface with glassmorphic design.

### Why RemoteX?

- **Browser-based**: No desktop SSH client needed
- **Real-time**: WebSocket communication via SignalR
- **Clean Architecture**: Separation of concerns with 3-layer design
- **Secure**: SSH.NET implementation with password/key authentication
- **Performance**: Async/await patterns throughout

---

## Tech Stack

### Backend
- **Framework**: ASP.NET Core 10.0
- **Real-time**: SignalR
- **SSH Library**: SSH.NET
- **Logging**: Serilog
- **Architecture**: Clean Architecture

### Frontend
- **UI**: HTML5 / CSS3 / JavaScript (Vanilla)
- **WebSocket**: SignalR Client

---

## Installation

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Visual Studio 2026 or VS Code
- Git

### Setup

```bash
# Clone repository
git clone https://github.com/MatiasEzequielVazquez/RemoteX.git
cd RemoteX

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run
cd src/RemoteX.API
dotnet run
```

Open browser at `http://localhost:5000`

---

## Usage

### Local Commands

```bash
remotex@local:~$ help      # Show available commands
remotex@local:~$ about     # About RemoteX
remotex@local:~$ clear     # Clear terminal
remotex@local:~$ ssh       # Open SSH connection dialog
```

### SSH Connection

1. Click **"SSH Connect"** button
2. Enter connection details (host, port, username, password)
3. Click **"Connect"**
4. Execute commands on remote server

---

## Architecture

```
RemoteX/
├── src/
│   ├── RemoteX.API/              # Presentation Layer
│   │   ├── Hubs/SshHub.cs        # SignalR Hub
│   │   ├── wwwroot/              # Frontend
│   │   └── Program.cs
│   │
│   ├── RemoteX.Core/             # Domain Layer
│   │   ├── Models/
│   │   ├── Interfaces/
│   │   └── Services/
│   │
│   └── RemoteX.Infrastructure/   # Infrastructure Layer
│       └── SSH/
│
└── tests/
    └── RemoteX.Tests/
```

### Communication Flow

```
Browser ←→ SignalR Hub ←→ Session Manager ←→ SSH Client ←→ Remote Server
```

---

## Author

**Matias Ezequiel Vazquez**

- GitHub: [@MatiasEzequielVazquez](https://github.com/MatiasEzequielVazquez)
- Email: vazquez.matias.e@gmail.com

---

## License

MIT License - see [LICENSE](LICENSE) file for details
