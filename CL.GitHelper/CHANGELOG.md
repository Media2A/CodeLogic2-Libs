# Changelog

All notable changes to the CL.GitHelper library will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2024-10-26

### Added

#### Core Features
- Complete rewrite for CodeLogic Framework 2.0 with modern architecture
- Full integration with CodeLogic logging, configuration, and dependency injection systems
- LibGit2Sharp integration for robust Git operations
- Support for HTTPS (username/password/PAT) and SSH (key-based) authentication

#### Repository Operations
- **Clone**: Repository cloning with progress reporting and diagnostics
  - Support for shallow clones (depth parameter)
  - Single branch cloning
  - Submodule recursion
  - Bare repository cloning
- **Fetch**: Fetch updates from remote with pruning and tag support
- **Pull**: Pull changes with configurable merge strategies (merge, rebase, fast-forward-only)
- **Push**: Push changes with force, upstream tracking, and tag support
- **Commit**: Create commits with custom author, file staging, and amend support
- **Status**: Comprehensive repository status with file-level details
- **Branch**: List, checkout, and manage branches (local and remote)

#### Performance & Diagnostics
- **OperationDiagnostics**: Detailed tracking of all operations
  - Start/end timestamps and duration
  - Object counts and bytes transferred
  - Custom diagnostic messages
  - Performance metrics dictionary
- **Progress Reporting**: Real-time progress callbacks for long-running operations
  - Clone progress (objects, bytes, percentage)
  - Fetch progress with transfer statistics
  - Push progress with upload tracking
- **Thread Safety**: SemaphoreSlim-based operation locking for concurrent safety

#### Repository Management
- **GitManager Service**: Advanced repository lifecycle management
  - Repository instance caching with configurable timeout
  - Automatic cache cleanup with background timer
  - Cache statistics and monitoring
  - Health checks for all repositories
- **Multi-Repository Support**: Manage multiple Git repositories with different configurations
  - Independent authentication per repository
  - Per-repository timeout and diagnostic settings
  - Runtime repository registration/unregistration

#### Batch Operations
- **Concurrent Processing**: Execute operations across multiple repositories in parallel
  - Configurable maximum concurrency
  - `FetchAllAsync()`: Fetch all repositories concurrently
  - `GetAllStatusAsync()`: Get status for all repositories
  - `ExecuteOnAllAsync()`: Custom batch operations with callback
  - `HealthCheckAsync()`: Health check all repositories

#### Configuration System
- **JSON Configuration**: Full configuration support via `config/git.json`
  - Repository-specific settings (URL, path, credentials, branch)
  - Global settings (caching, timeouts, concurrency)
  - Auto-generation of default configuration template
- **Dynamic Configuration**: Runtime configuration management
  - Register new repositories dynamically
  - Unregister repositories with cache cleanup
  - Update configuration without restart

#### Models & Data Structures
- **GitOperationResult<T>**: Strongly-typed operation results with diagnostics
- **RepositoryInfo**: Comprehensive repository information
  - Current branch, HEAD commit, state
  - Branch/tag/stash counts
  - Dirty state and file change statistics
- **CommitInfo**: Detailed commit information
  - SHA, message, author/committer details
  - File changes and line statistics
- **BranchInfo**: Branch details with tracking information
  - Local/remote status
  - Ahead/behind tracking statistics
- **RepositoryStatus**: Working directory status
  - Modified, staged, untracked, ignored, conflicted files
  - File-level status with old paths (for renames)
- **Operation Options**: Comprehensive options for all operations
  - CloneOptions, FetchOptions, PullOptions, CommitOptions, PushOptions
  - Cancellation token support
  - Progress callback configuration

#### Library Integration
- **ILibrary Implementation**: Full CodeLogic library lifecycle
  - `OnLoadAsync()`: Configuration loading and manager initialization
  - `OnInitializeAsync()`: Repository connection testing
  - `OnUnloadAsync()`: Cleanup and disposal
  - `HealthCheckAsync()`: Framework health check integration
- **ILogger Integration**: Context-aware logging throughout
  - Success, error, warning, and info logging
  - Diagnostic message logging
  - Performance tracking logs

#### Error Handling
- Comprehensive error handling with detailed error messages
- Exception capture and propagation in operation results
- Validation of configurations before operations
- Graceful failure handling with diagnostic information

### Changed

- Migrated from old CodeLogic framework to CodeLogic 2.0 architecture
- Replaced custom Git implementation with LibGit2Sharp for better reliability
- Improved performance with repository caching and concurrent operations
- Enhanced error handling with detailed diagnostics

### Performance Improvements

- **Repository Caching**: Reuse repository instances to avoid repeated initialization
- **Concurrent Batch Operations**: Process multiple repositories in parallel
- **Optimized Locking**: Fine-grained SemaphoreSlim usage for thread safety
- **Background Cache Cleanup**: Automatic cleanup of expired cache entries
- **Progress Callbacks**: Optional progress reporting to avoid overhead when not needed

### Technical Details

#### Dependencies
- LibGit2Sharp v0.30.0+ for Git operations
- CL.Core v2.0.0+ for CodeLogic abstractions
- CodeLogic v2.0.0+ for framework integration
- .NET 10.0 target framework

#### Architecture
- **Services Layer**:
  - `GitRepository`: Core Git operations service
  - `GitManager`: Repository lifecycle and cache management
- **Models Layer**:
  - `Configuration`: Repository and global configuration models
  - `GitModels`: Operation results, diagnostics, and data models
- **Library Layer**:
  - `GitHelperLibrary`: Main library class with ILibrary implementation
  - `GitHelperManifest`: Library metadata and dependencies

### Documentation

- Comprehensive README.md with features, configuration, and usage examples
- EXAMPLES.md with 30+ detailed examples covering all scenarios
- CHANGELOG.md for version tracking
- Inline XML documentation for all public APIs
- HTML documentation generation support

### Migration Notes

For users upgrading from the old CL.GitHelper:

1. **Configuration Format Changed**: Update `config/git.json` to new format
2. **API Changes**: All operations now return `GitOperationResult<T>` instead of void
3. **Async Pattern**: All operations are now async with `Task<>` return types
4. **New Features**: Take advantage of caching, diagnostics, and batch operations
5. **Authentication**: Update credentials format for HTTPS/SSH support

### Known Limitations

- Requires LibGit2Sharp which has platform-specific native dependencies
- SSH authentication requires SSH keys to be in standard formats
- Background cache cleanup runs every minute (not configurable)
- Maximum operation timeout is configured per repository

### Future Enhancements

Potential features for future releases:
- Git hooks support
- Tag creation and management
- Stash operations
- Cherry-pick and rebase support
- Diff and patch operations
- Blame and history analysis
- Git LFS support
- Webhook integration
- Background auto-fetch scheduling

---

## Version History

### [2.0.0] - 2024-10-26
- Complete rewrite for CodeLogic Framework 2.0
- Modern architecture with performance improvements and diagnostics
- LibGit2Sharp integration
- First release under new framework

---

**Note**: This is the first release of CL.GitHelper 2.0 for the CodeLogic Framework 2.0. The previous version was part of the legacy CodeLogic framework and is not directly compatible with this version.
