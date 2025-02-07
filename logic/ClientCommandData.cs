// namespace logic;

// public enum ClientCommandType{ Set, Get}

// public record ClientCommandData
// {
//     public ClientCommandType Type { get; }
//     public string Key { get; }
//     public string Value { get; }
//     public Action<bool, Guid?> RespondToClient { get; }

//     public ClientCommandData(ClientCommandType type, string key, string value, Action<bool, Guid?> respondToClient)
//     {
//         Type = type;
//         Key = key;
//         Value = value;
//         RespondToClient = respondToClient ?? throw new ArgumentNullException(nameof(respondToClient));
//     }
// }

namespace logic;

public enum ClientCommandType { Set, Get }

public record ClientCommandData
{
    public ClientCommandType Type { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public Action<bool, Guid?> RespondToClient { get; init; } = (success, id) => { };

    public ClientCommandData(ClientCommandType type, string key, string value, Action<bool, Guid?> respondToClient)
    {
        Type = type;
        Key = key;
        Value = value;
        RespondToClient = respondToClient ?? ((success, id) => { });
    }

    public ClientCommandData() { }
}
