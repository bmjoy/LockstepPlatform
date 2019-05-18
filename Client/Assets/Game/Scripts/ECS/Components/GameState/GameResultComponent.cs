﻿using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace Lockstep.ECS.GameState
{        
    public enum EGameResult {
        Falied,
        Win,
        NextLevel,
    }
    
    [GameState]
    [Unique]
    [Event(false, EventType.Added)]
    [Event(false, EventType.Removed)]
    public class GameResultComponent : IComponent {
        public EGameResult value;
    }
}