using Opc.Ua;
using Opc.Ua.Server;
using CCStudio.Tunneler.Core.Interfaces;
using CCStudio.Tunneler.Core.Models;
using Microsoft.Extensions.Logging;

namespace CCStudio.Tunneler.Service.OPC;

/// <summary>
/// OPC UA Server implementation using OPC Foundation UA .NET Standard (open-source, MIT license)
/// </summary>
public class OpcUaServer : IOpcUaServer
{
    private readonly ILogger _logger;
    private ApplicationInstance? _application;
    private TunnelerUaServer? _server;
    private readonly Dictionary<string, BaseDataVariableState> _nodes = new();
    private OpcUaConfiguration? _configuration;

    public event EventHandler<NodeWriteEventArgs>? NodeWritten;
    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;

    public bool IsRunning { get; private set; }

    public OpcUaServer(ILogger? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    public async Task<bool> StartAsync(OpcUaConfiguration configuration)
    {
        try
        {
            _configuration = configuration;
            _logger.LogInformation("Starting OPC UA Server on port {Port}", configuration.ServerPort);

            // Create application instance
            _application = new ApplicationInstance
            {
                ApplicationName = configuration.ApplicationName,
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "CCStudioTunneler"
            };

            // Load application configuration
            var config = await CreateApplicationConfiguration(configuration);
            await _application.LoadApplicationConfiguration(config, silent: true);

            // Check application instance certificate
            bool certOk = await _application.CheckApplicationInstanceCertificate(
                silent: true,
                minimumKeySize: 2048);

            if (!certOk)
            {
                _logger.LogWarning("Application certificate invalid or missing, created new certificate");
            }

            // Create and start the server
            _server = new TunnelerUaServer(_logger, this);
            await _application.Start(_server);

            IsRunning = true;
            _logger.LogInformation("OPC UA Server started successfully at {Endpoint}",
                configuration.EndpointUrl);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting OPC UA Server");
            IsRunning = false;
            return false;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }

            _nodes.Clear();
            IsRunning = false;

            _logger.LogInformation("OPC UA Server stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping OPC UA Server");
        }

        await Task.CompletedTask;
    }

    public async Task<bool> AddOrUpdateNodeAsync(TagMapping mapping, TagValue? initialValue = null)
    {
        if (_server == null)
        {
            _logger.LogWarning("Cannot add node - server not started");
            return false;
        }

        try
        {
            var nodeId = new NodeId(mapping.UaNodeName, mapping.NamespaceIndex);

            BaseDataVariableState node;

            if (_nodes.ContainsKey(mapping.UaNodeName))
            {
                // Update existing node
                node = _nodes[mapping.UaNodeName];
                node.Value = initialValue?.Value;
                node.Timestamp = initialValue?.Timestamp ?? DateTime.UtcNow;
                node.StatusCode = MapQuality(initialValue?.Quality ?? DataQuality.Good);
            }
            else
            {
                // Create new node
                node = new BaseDataVariableState(null)
                {
                    NodeId = nodeId,
                    BrowseName = new QualifiedName(mapping.UaNodeName, mapping.NamespaceIndex),
                    DisplayName = new LocalizedText(mapping.UaNodeName),
                    Description = new LocalizedText(mapping.Description ?? mapping.UaNodeName),
                    WriteMask = AttributeWriteMask.None,
                    UserWriteMask = AttributeWriteMask.None,
                    Value = initialValue?.Value,
                    DataType = DataTypeIds.BaseDataType,
                    ValueRank = ValueRanks.Scalar,
                    AccessLevel = MapAccessLevel(mapping.AccessLevel),
                    UserAccessLevel = MapAccessLevel(mapping.AccessLevel),
                    Historizing = false,
                    StatusCode = MapQuality(initialValue?.Quality ?? DataQuality.Good),
                    Timestamp = initialValue?.Timestamp ?? DateTime.UtcNow
                };

                // Handle write events if writable
                if (mapping.AccessLevel != AccessLevel.Read)
                {
                    node.OnWriteValue = OnNodeValueWrite;
                }

                _nodes[mapping.UaNodeName] = node;
                _server.AddNode(node);

                _logger.LogDebug("Added OPC UA node: {NodeName}", mapping.UaNodeName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding/updating node: {NodeName}", mapping.UaNodeName);
            return false;
        }
    }

    public async Task<bool> RemoveNodeAsync(string nodeName)
    {
        if (_nodes.Remove(nodeName, out var node))
        {
            // Node removal from server would go here
            _logger.LogDebug("Removed OPC UA node: {NodeName}", nodeName);
            return true;
        }

        return await Task.FromResult(false);
    }

    public async Task<bool> UpdateNodeValueAsync(string nodeName, TagValue value)
    {
        if (!_nodes.TryGetValue(nodeName, out var node))
        {
            _logger.LogWarning("Node not found: {NodeName}", nodeName);
            return false;
        }

        try
        {
            node.Value = value.Value;
            node.Timestamp = value.Timestamp;
            node.StatusCode = MapQuality(value.Quality);
            node.ClearChangeMasks(null, false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating node value: {NodeName}", nodeName);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetRegisteredNodesAsync()
    {
        return await Task.FromResult(_nodes.Keys);
    }

    public int GetConnectedClientCount()
    {
        return _server?.CurrentSessionCount ?? 0;
    }

    private ServiceResult OnNodeValueWrite(ISystemContext context, NodeState node, ref object value)
    {
        try
        {
            var nodeName = node.BrowseName.Name;
            _logger.LogDebug("Write request for node {NodeName}: {Value}", nodeName, value);

            // Raise event for bridge logic to handle
            NodeWritten?.Invoke(this, new NodeWriteEventArgs
            {
                NodeName = nodeName,
                Value = value,
                ClientId = context.SessionId?.ToString()
            });

            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in node write callback");
            return new ServiceResult(StatusCodes.Bad);
        }
    }

    internal void OnClientConnected(string sessionId, string? clientName)
    {
        _logger.LogInformation("OPC UA Client connected: {SessionId} ({ClientName})",
            sessionId, clientName ?? "Unknown");

        ClientConnected?.Invoke(this, new ClientConnectedEventArgs
        {
            ClientId = sessionId,
            ClientName = clientName,
            ConnectedAt = DateTime.UtcNow
        });
    }

    internal void OnClientDisconnected(string sessionId)
    {
        _logger.LogInformation("OPC UA Client disconnected: {SessionId}", sessionId);

        ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs
        {
            ClientId = sessionId,
            DisconnectedAt = DateTime.UtcNow
        });
    }

    private byte MapAccessLevel(AccessLevel accessLevel)
    {
        return accessLevel switch
        {
            AccessLevel.Read => AccessLevels.CurrentRead,
            AccessLevel.Write => AccessLevels.CurrentWrite,
            AccessLevel.ReadWrite => AccessLevels.CurrentReadOrWrite,
            _ => AccessLevels.CurrentRead
        };
    }

    private StatusCode MapQuality(DataQuality quality)
    {
        return quality switch
        {
            DataQuality.Good => StatusCodes.Good,
            DataQuality.Bad => StatusCodes.Bad,
            DataQuality.Uncertain => StatusCodes.Uncertain,
            _ => StatusCodes.Good
        };
    }

    private async Task<ApplicationConfiguration> CreateApplicationConfiguration(OpcUaConfiguration config)
    {
        var appConfig = new ApplicationConfiguration
        {
            ApplicationName = config.ApplicationName,
            ApplicationUri = config.ApplicationUri,
            ProductUri = config.ProductUri,
            ApplicationType = ApplicationType.Server,

            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = new StringCollection { config.EndpointUrl },
                MinRequestThreadCount = 5,
                MaxRequestThreadCount = 100,
                MaxQueuedRequestCount = 2000
            },

            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault",
                    SubjectName = $"CN={config.ApplicationName}, O=DHC Automation, DC={Environment.MachineName}"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications"
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities"
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates"
                },
                AutoAcceptUntrustedCertificates = config.AllowAnonymous,
                RejectSHA1SignedCertificates = false,
                MinimumCertificateKeySize = 2048
            },

            TransportQuotas = new TransportQuotas
            {
                OperationTimeout = 120000,
                MaxStringLength = 1048576,
                MaxByteStringLength = 1048576,
                MaxArrayLength = 65535,
                MaxMessageSize = 4194304,
                MaxBufferSize = 65535,
                ChannelLifetime = 300000,
                SecurityTokenLifetime = 3600000
            }
        };

        await appConfig.Validate(ApplicationType.Server);
        return appConfig;
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _application?.Dispose();
    }
}

/// <summary>
/// Custom OPC UA Server implementation
/// </summary>
internal class TunnelerUaServer : StandardServer
{
    private readonly ILogger _logger;
    private readonly OpcUaServer _parent;

    public TunnelerUaServer(ILogger logger, OpcUaServer parent)
    {
        _logger = logger;
        _parent = parent;
    }

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        _logger.LogDebug("Creating node manager");

        var nodeManagers = new List<INodeManager>
        {
            new TunnelerNodeManager(server, configuration, _logger, _parent)
        };

        return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
    }

    protected override ServerProperties LoadServerProperties()
    {
        var properties = new ServerProperties
        {
            ManufacturerName = "DHC Automation and Controls",
            ProductName = "CCStudio-Tunneler",
            ProductUri = "https://dhcautomation.com/ccstudio-tunneler",
            SoftwareVersion = "1.0.0",
            BuildNumber = "1.0.0.0",
            BuildDate = DateTime.UtcNow
        };

        return properties;
    }

    protected override void OnSessionCreated(Session session)
    {
        base.OnSessionCreated(session);
        _parent.OnClientConnected(session.Id.ToString(), session.SessionName);
    }

    protected override void OnSessionClosing(Session session, bool deleteSubscriptions)
    {
        _parent.OnClientDisconnected(session.Id.ToString());
        base.OnSessionClosing(session, deleteSubscriptions);
    }
}

/// <summary>
/// Custom Node Manager for CCStudio-Tunneler nodes
/// </summary>
internal class TunnelerNodeManager : CustomNodeManager2
{
    private readonly ILogger _logger;
    private readonly OpcUaServer _parent;

    public TunnelerNodeManager(IServerInternal server, ApplicationConfiguration configuration, ILogger logger, OpcUaServer parent)
        : base(server, configuration, "http://dhcautomation.com/ccstudio-tunneler")
    {
        _logger = logger;
        _parent = parent;

        SystemContext.NodeIdFactory = this;
    }

    public override NodeId New(ISystemContext context, NodeState node)
    {
        return new NodeId(node.BrowseName.Name, NamespaceIndex);
    }

    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();

        // Create root folder for tunneled nodes
        var root = CreateFolderState(null, "CCStudioTunneler", "CCStudio Tunneler", "Root folder for OPC DA tunneled tags");
        predefinedNodes.Add(root);

        return predefinedNodes;
    }

    public void AddNode(BaseDataVariableState node)
    {
        AddPredefinedNode(SystemContext, node);
    }

    private FolderState CreateFolderState(NodeState parent, string path, string name, string description)
    {
        var folder = new FolderState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypes.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId(path, NamespaceIndex),
            BrowseName = new QualifiedName(path, NamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            Description = new LocalizedText("en", description),
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None,
            EventNotifier = EventNotifiers.None
        };

        parent?.AddChild(folder);

        return folder;
    }
}
