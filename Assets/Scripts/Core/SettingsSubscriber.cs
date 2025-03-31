using UnityEngine;


public abstract class SettingsSubscriber : MonoBehaviour
{
    public abstract GameSettings Settings { get; set; }


#if UNITY_EDITOR
    protected virtual void OnDisable()
    {
        if (Settings is null)
        {
            Debug.LogError($"settings is null: OnSettingsChanged won't be unsibscribed!");
            return;
        }

        Settings.OnSettingsChanged -= ApplyGameSettings;
    }
#endif

    public virtual void UseSettings(SettingsSubscriber subscriber)
    {
        if (Settings is null)
        {
            Debug.LogError($"{subscriber.name}'s settings is null: won't be initialized!");
            return;
        }

#if UNITY_EDITOR
        Settings.OnSettingsChanged += ApplyGameSettings;
#endif

        ApplyGameSettings();
    }

    public abstract void ApplyGameSettings();
}