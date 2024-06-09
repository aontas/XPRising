using Unity.Collections;
using Unity.Entities;

namespace XPRising.Models;

public struct PlayerData(
    FixedString64Bytes characterName,
    ulong steamID,
    bool isOnline,
    bool isAdmin,
    Entity userEntity,
    Entity charEntity)
{
    public FixedString64Bytes CharacterName { get; set; } = characterName;
    public ulong SteamID { get; set; } = steamID;
    public bool IsOnline { get; set; } = isOnline;
    public bool IsAdmin { get; set; } = isAdmin;
    public Entity UserEntity { get; set; } = userEntity;
    public Entity CharEntity { get; set; } = charEntity;
}