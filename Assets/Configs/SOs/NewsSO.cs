using System;
using Lean.Localization;

[Serializable]
public class NewsEntry
{
    [LeanTranslationName] public string messageTerm; // Ключ локализации
    public string unlockID;     // ID улучшения для открытия
    public string hideID;       // ID улучшения для скрытия
}