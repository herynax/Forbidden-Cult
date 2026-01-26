using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class NewsEntry
{
    [TextArea(2, 5)]
    public string message;      // Текст новости
    public string unlockID;     // ID улучшения, которое ОТКРЫВАЕТ новость (пусто — доступно всегда)
    public string hideID;       // ID улучшения, которое СКРЫВАЕТ новость (чтобы старые шутки пропадали)
}
