using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Unity.Entities;

namespace XPShared.Services;

public class SystemService(World world)
{
    private readonly World _world = world ?? throw new ArgumentNullException(nameof(world));

    private ClientScriptMapper _clientScriptMapper;
    public ClientScriptMapper ClientScriptMapper => _clientScriptMapper ??= GetSystem<ClientScriptMapper>();

    private PrefabCollectionSystem _prefabCollectionSystem;
    public PrefabCollectionSystem PrefabCollectionSystem => _prefabCollectionSystem ??= GetSystem<PrefabCollectionSystem>();

    private GameDataSystem _gameDataSystem;
    public GameDataSystem GameDataSystem => _gameDataSystem ??= GetSystem<GameDataSystem>();

    private ManagedDataSystem _managedDataSystem;
    public ManagedDataSystem ManagedDataSystem => _managedDataSystem ??= GetSystem<ManagedDataSystem>();

    private TutorialSystem _tutorialSystem;
    public TutorialSystem TutorialSystem => _tutorialSystem ??= GetSystem<TutorialSystem>();

    private ServerScriptMapper _serverScriptMapper;
    public ServerScriptMapper ServerScriptMapper => _serverScriptMapper ??= GetSystem<ServerScriptMapper>();
    
    NetworkIdSystem.Singleton _networkIdSystem_Singleton;
    public NetworkIdSystem.Singleton NetworkIdSystem
    {
        get
        {
            if (_networkIdSystem_Singleton.Equals(default(NetworkIdSystem.Singleton)))
            {
                _networkIdSystem_Singleton = GetSingleton<NetworkIdSystem.Singleton>();
            }

            return _networkIdSystem_Singleton;
        }
    }
    
    private T GetSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetExistingSystemManaged<T>() ?? throw new InvalidOperationException($"Failed to get {Il2CppType.Of<T>().FullName} from the Server...");
    }
    T GetSingleton<T>() // where T : ComponentSystemBase
    {
        return ServerScriptMapper.GetSingleton<T>() ?? throw new InvalidOperationException($"[{_world.Name}] - failed to get singleton ({Il2CppType.Of<T>().FullName})");
    }
}