# Changelog

All notable changes to CCStudio-Tunneler will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned Features
- OPC DA client implementation with full DCOM support
- OPC UA server implementation with security profiles
- Tag browser for OPC DA server discovery
- Web-based remote configuration interface
- Multi-server support (connect to multiple OPC DA servers)
- Historical data buffering during network outages
- REST API for programmatic access
- Docker containerization support
- Performance optimization for high-throughput scenarios

## [1.0.0] - 2025-01-XX

### Added
- Initial release of CCStudio-Tunneler
- OPC DA to OPC UA protocol bridge architecture
- Windows Service for reliable background operation
- WPF System Tray application for configuration and monitoring
- JSON-based configuration management
- Real-time status monitoring
- Comprehensive logging with Serilog
- Tag mapping with aliasing and scaling
- Bidirectional data flow (read/write)
- Automatic reconnection with exponential backoff
- Support for anonymous and authenticated OPC UA access
- Material Design UI theme matching brand identity
- User documentation and developer guide

### Features

#### Core Library
- Configuration service with validation
- Extensible interface design for OPC components
- Tag value models with quality and timestamp
- Performance metrics tracking
- Utility classes for logging and extensions

#### Windows Service
- Hosted service architecture using .NET Generic Host
- Graceful startup and shutdown
- Configuration hot-reload capability
- Service lifecycle management
- Comprehensive error handling and recovery

#### Tray Application
- System tray integration with status indicators
- Configuration window with tabbed interface
- OPC DA server settings
- OPC UA server settings
- Tag mapping grid with import/export
- Logging configuration
- Status monitoring window
- About window with product information
- Service control (start/stop/restart)
- Log file access
- Configuration folder access

### Technical Details
- Built on .NET 8.0 LTS (Long Term Support until November 2026)
- Uses OPC Foundation UA .NET Standard (open-source)
- Serilog for structured logging
- Material Design themes for modern UI
- Newtonsoft.Json for configuration serialization

### Documentation
- Comprehensive user guide with troubleshooting
- Developer guide with architecture documentation
- README with quick start instructions
- Inline code documentation

### Known Limitations
- OPC DA client implementation is placeholder (requires commercial/custom library)
- OPC UA server implementation is placeholder (requires OPC Foundation integration)
- No web-based configuration interface yet
- Single OPC DA server support only
- Windows-only (OPC DA limitation)

### Requirements
- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime
- OPC Core Components 3.0
- 512 MB RAM minimum
- TCP port 4840 (configurable)

---

## Version History Template

## [X.Y.Z] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes in existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Security improvements
