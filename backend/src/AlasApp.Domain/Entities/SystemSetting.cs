using AlasApp.Domain.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class SystemSetting : AuditableEntity
{
    private SystemSetting()
    {
    }

    private SystemSetting(Guid id, string key, string jsonValue, DateTimeOffset timestamp)
    {
        Id = id;
        Key = key;
        JsonValue = jsonValue;
        SetCreated(timestamp);
    }

    public string Key { get; private set; } = string.Empty;

    public string JsonValue { get; private set; } = "{}";

    public static SystemSetting Create(string key, string jsonValue, DateTimeOffset timestamp)
    {
        Validate(key, jsonValue);
        return new SystemSetting(Guid.NewGuid(), key.Trim(), jsonValue, timestamp);
    }

    public void Update(string jsonValue, DateTimeOffset timestamp)
    {
        Validate(Key, jsonValue);
        JsonValue = jsonValue;
        SetUpdated(timestamp);
    }

    private static void Validate(string key, string jsonValue)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainRuleException("La clave de configuracion es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(jsonValue))
        {
            throw new DomainRuleException("El valor de configuracion es obligatorio.");
        }
    }
}
